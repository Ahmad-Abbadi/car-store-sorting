"""Run with installed Blender 5.1 --background --factory-startup --python this.py -- THEME_IDS.
Builds from the preserved themed-cabinet sources; never operates on a user's live Blender scene.
"""
import bpy,json,math,sys,subprocess
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(HERE))
import signature_models as models
ROOT=HERE.parents[3]
SRC=ROOT/'ArtSource/Shelves/SignatureShelves'
BASE=ROOT/'ArtSource/Shelves/ThemedShelves/Blends'
ASSET=ROOT/'Assets/TinyRiders/Art/Shelves/Themed'
RECIPES=json.loads((ROOT/'Assets/TinyRiders/ThemedCollection/Data/Themes/theme_recipes.json').read_text(encoding='utf-8-sig'))['themes']
CHECKPOINT={'TH_WILD_WEST','TH_PIRATE','TH_CANDY','TH_SCIENCE_LAB'}
GPU=False
try:
    prefs=bpy.context.preferences.addons['cycles'].preferences
    prefs.compute_device_type='OPTIX';prefs.get_devices()
    for device in prefs.devices:device.use=device.type=='OPTIX'
    GPU=any(d.use for d in prefs.devices)
except (TypeError,RuntimeError):pass
for folder in ('Blends','Renders','Reports','Textures'):(SRC/folder).mkdir(parents=True,exist_ok=True)

def triangles(objects):
    n=0
    deps=bpy.context.evaluated_depsgraph_get()
    for o in objects:
        if o.type!='MESH':continue
        ev=o.evaluated_get(deps);mesh=ev.to_mesh();mesh.calc_loop_triangles();n+=len(mesh.loop_triangles);ev.to_mesh_clear()
    return n
def bounds(objects):
    points=[o.matrix_world@Vector(v) for o in objects if o.type=='MESH' for v in o.bound_box]
    return Vector([min(v[i] for v in points) for i in range(3)]),Vector([max(v[i] for v in points) for i in range(3)])
def material(theme):
    m=bpy.data.materials.new('Signature_'+theme['id']);m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Roughness'].default_value=.4
    for kind in ('BaseColor','Normal','Surface'):
        image=bpy.data.images.load(str(ASSET/'Textures'/f"{theme['id']}_Signature_{kind}.png"),check_existing=False)
        if kind!='BaseColor':image.colorspace_settings.name='Non-Color'
        node=m.node_tree.nodes.new('ShaderNodeTexImage');node.image=image;node.label=kind
        if kind=='BaseColor':m.node_tree.links.new(node.outputs['Color'],p.inputs['Base Color'])
        elif kind=='Normal':
            normal=m.node_tree.nodes.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=.35
            m.node_tree.links.new(node.outputs['Color'],normal.inputs['Color']);m.node_tree.links.new(normal.outputs['Normal'],p.inputs['Normal'])
        else:
            split=m.node_tree.nodes.new('ShaderNodeSeparateColor');m.node_tree.links.new(node.outputs['Color'],split.inputs['Color']);m.node_tree.links.new(split.outputs['Red'],p.inputs['Metallic'])
            inv=m.node_tree.nodes.new('ShaderNodeMath');inv.operation='SUBTRACT';inv.inputs[0].default_value=1
            m.node_tree.links.new(node.outputs['Alpha'],inv.inputs[1]);m.node_tree.links.new(inv.outputs[0],p.inputs['Roughness'])
    return m
def render(scene,path,size,target,position,ortho):
    scene.camera.location=position;scene.camera.rotation_euler=(Vector(target)-scene.camera.location).to_track_quat('-Z','Y').to_euler()
    scene.camera.data.type='ORTHO';scene.camera.data.ortho_scale=ortho
    scene.render.resolution_x=size[0];scene.render.resolution_y=size[1];scene.render.resolution_percentage=100
    scene.render.filepath=str(path);bpy.ops.render.render(write_still=True)

def build(t):
    bpy.ops.wm.open_mainfile(filepath=str(BASE/(t['id']+'.blend')))
    bpy.context.preferences.filepaths.save_version=0
    scene=bpy.context.scene;geo=bpy.data.objects['Geometry'];root=bpy.data.objects['TR_Shelf_Base_01']
    # Replace the two coarse header symbols while retaining the patterned cabinet and side details.
    for name in ('ThemeBadge_0','ThemeBadge_1'):
        holder=bpy.data.objects.get(name)
        if holder:
            for o in list(holder.children_recursive):bpy.data.objects.remove(o,do_unlink=True)
            bpy.data.objects.remove(holder,do_unlink=True)
    subprocess.run(['C:/Python314/python.exe',str(HERE/'build_atlases.py'),t['id']],check=True)
    mat=material(t)
    parts=models.build(t['id'][3:],mat)
    bpy.context.view_layer.update();low,high=bounds(parts);extent=high-low
    factor=min(.85/extent.z,1.25/extent.x,.729/extent.y)
    center=(low+high)*.5
    # Canonical object geometry uses real depth, and rests exactly on its post.
    for o in parts:
        for v in o.data.vertices:v.co*=factor
        o.location=(o.location-center)*factor
    bpy.context.view_layer.update();low,high=bounds(parts)
    # Find a real central attachment surface. A skull's jaw is above the lowest crossed-bone tip.
    contacts=[]
    for o in parts:
        tree=BVHTree.FromPolygons([o.matrix_world@v.co for v in o.data.vertices],[list(p.vertices) for p in o.data.polygons])
        hit=tree.ray_cast(Vector((0,0,low.z-1)),Vector((0,0,1)))
        if hit[0] is not None:contacts.append(hit[0].z)
    attachment=min(contacts) if contacts else low.z
    # Isolated icon photograph from the same geometry; keep studio lights, hide cabinet/stage.
    hidden=[]
    for o in scene.objects:
        if o.type in {'MESH','FONT','CURVE'} and o not in parts: hidden.append((o,o.hide_render));o.hide_render=True
    scene.render.engine='CYCLES';scene.cycles.device='GPU' if GPU else 'CPU';scene.cycles.samples=32;scene.cycles.use_denoising=True
    scene.render.film_transparent=True
    icon=SRC/'Renders'/(t['id']+'_icon.png')
    render(scene,icon,(2048,2048),(0,0,0),(-1.4,-4,1.2),max(high.x-low.x,high.z-low.z)*1.35)
    for o,hide in hidden:o.hide_render=hide
    scene.render.film_transparent=False
    subprocess.run(['C:/Python314/python.exe',str(HERE/'build_atlases.py'),t['id'],str(icon)],check=True)
    for node in mat.node_tree.nodes:
        if node.type=='TEX_IMAGE':node.image.reload();node.image.pack()
    # Fit in a 60 x 35 cm footprint. Cabinet is imported at 0.48 scale.
    bpy.context.view_layer.update();cabinetTop=bounds(geo.children_recursive)[1].z
    postHeight=.23
    topper=bpy.data.objects.new('ThemeTopper',None);scene.collection.objects.link(topper);topper.parent=geo
    topper['themeId']=t['id'];topper['signatureRevision']=1
    for o in parts:o.parent=topper;o.location.z+=cabinetTop+postHeight-low.z
    models.PARTS=[]
    mount=models.cylinder('TopperMount',(0,0,cabinetTop+.018),.16,.036,2)
    post=models.cylinder('TopperPost',(0,0,cabinetTop+postHeight*.5),.035,postHeight,2,vertices=20)
    mount.parent=topper;post.parent=topper
    if attachment>low.z+.005:
        bracket=models.rod('TopperSaddle',(0,0,cabinetTop+postHeight-.01),(0,0,cabinetTop+postHeight+attachment-low.z+.02),.027,2)
        bracket.parent=topper
    # A shallow rounded plaque replaces each header relief; its front samples the rendered icon tile.
    for x in (-1.18,1.18):
        plaque=models.box('SignaturePlaque',(x,-.305,3.32),(.47,.045,.47),4,.04);plaque.parent=geo
        bpy.ops.mesh.primitive_plane_add(size=1,location=(x,-.33,3.32),rotation=(math.pi/2,0,0))
        face=bpy.context.object;face.name='SignatureIcon';face.scale=(.435,.435,1);face.data.materials.append(mat);face.parent=geo
        for uv in face.data.uv_layers.active.data:uv.uv=(.005+uv.uv.x*.49,.505+uv.uv.y*.49)
    bpy.context.view_layer.update()
    deco=bpy.data.objects['ThemeDecorations']
    decorations=list(deco.children_recursive)+list(topper.children_recursive)+[o for o in geo.children if o.name.startswith('Signature')]
    budget=triangles(decorations)
    if budget>8000:raise RuntimeError(f'{t["id"]}: {budget} decoration triangles exceeds 8000')
    lowAll,highAll=bounds(parts)
    # Export scene hierarchy and geometry only; markers remain supplied by the stable Unity base prefab.
    bpy.ops.object.select_all(action='DESELECT')
    for o in [root,geo]+list(geo.children_recursive):o.select_set(True)
    bpy.context.view_layer.objects.active=root
    for o in geo.children_recursive:
        if o.type=='MESH':o.data=o.data.copy()
    bpy.context.view_layer.update()
    bpy.ops.export_scene.fbx(filepath=str(ASSET/'Models'/(t['id']+'.fbx')),use_selection=True,object_types={'EMPTY','MESH'},use_mesh_modifiers=True,add_leaf_bones=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',bake_space_transform=False)
    for image in bpy.data.images:
        if image.source=='FILE' and image.packed_file is None:
            try:image.pack()
            except RuntimeError:pass
    scene.cycles.samples=32
    render(scene,SRC/'Renders'/(t['id']+'.png'),(800,1000),(0,0,2.35),(-6,-10,5.6),5.6)
    if t['id'] in CHECKPOINT:
        render(scene,SRC/'Renders'/(t['id']+'_detail.png'),(1400,1000),(0,0,3.75),(-2,-9,4.8),3.6)
    renderCamera=scene.camera
    renderCamera.location=(-6,-10,5.6);renderCamera.rotation_euler=(Vector((0,0,2.35))-renderCamera.location).to_track_quat('-Z','Y').to_euler();renderCamera.data.ortho_scale=5.6
    bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'Blends'/(t['id']+'.blend')))
    report={'theme':t['id'],'signatureRevision':1,'slots':15,'decorationTriangles':budget,'topperTriangles':triangles(list(topper.children_recursive)), 'totalTriangles':triangles(geo.children_recursive),'materials':4,'atlasSize':1024,'signatureAtlasSize':2048,'masterAtlasSize':4096,'postHeightMeters':postHeight*.48,'topperSizeMeters':list((highAll-lowAll)*.48),'topperTopMeters':highAll.z*.48,'source':str(SRC/'Blends'/(t['id']+'.blend'))}
    (SRC/'Reports'/(t['id']+'.json')).write_text(json.dumps(report,indent=2))
    print('SIGNATURE_COMPLETE '+json.dumps(report),flush=True)

if __name__=='__main__':
    chosen=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
    for theme in RECIPES:
        if not chosen or theme['id'] in chosen:build(theme)
