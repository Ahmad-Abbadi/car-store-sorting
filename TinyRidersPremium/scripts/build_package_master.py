"""Original physical package, locked to the supplied 120 x 180 mm layout.

Coordinates are authored flat in XY, front +Z, then the package root rotates
upright with +Z up. The actual approved M03 is assembled inside the clear shell.
"""
from pathlib import Path
import sys,math,json
import bpy,bmesh
from mathutils import Vector
from mathutils.bvhtree import BVHTree
sys.path.insert(0,str(Path(__file__).resolve().parent))
import build_m03 as geo
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Blender/Packaging';OUT.mkdir(parents=True,exist_ok=True)
EXP=ROOT/'Exports/Packaging';EXP.mkdir(parents=True,exist_ok=True)
PRE=ROOT/'Previews/Packaging';PRE.mkdir(parents=True,exist_ok=True)
TEX=ROOT/'Textures/Packaging/Candidates'

def outline(w,h,r,cy=0,segments=8):
    points=[]
    for cx,yy,start in [(w/2-r,h/2-r,0),(-w/2+r,h/2-r,90),(-w/2+r,-h/2+r,180),(w/2-r,-h/2+r,270)]:
        for i in range(segments+1):
            angle=math.radians(start+90*i/segments)
            points.append((cx+r*math.cos(angle),cy+yy+r*math.sin(angle)))
    return points

def plate(name,w,h,r,z0,z1,mat,cy=0):
    p=outline(w,h,r,cy);n=len(p)
    v=[(x,y,z) for z in [z0,z1] for x,y in p]
    f=[tuple(reversed(range(n))),tuple(range(n,2*n))]
    f.extend((i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n))
    return geo.mesh(name,v,f,mat)

def uv_card(ob,front,back,edge):
    ob.data.materials.clear()
    for m in [front,back,edge]:ob.data.materials.append(m)
    uv=ob.data.uv_layers.new(name='PackagePrintUV0')
    for poly in ob.data.polygons:
        zone=0 if poly.normal.z>.7 else 1 if poly.normal.z<-.7 else 2
        poly.material_index=zone
        for li in poly.loop_indices:
            x,y,z=ob.data.vertices[ob.data.loops[li].vertex_index].co
            u=x/.12+.5 if zone!=1 else .5-x/.12
            uv.data[li].uv=(u,y/.18+.5)

def cam(name,pos,target,ortho):
    data=bpy.data.cameras.new(name);ob=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(ob)
    ob.location=pos;ob.rotation_euler=(Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()
    data.type='ORTHO';data.ortho_scale=ortho;data.clip_start=.001;data.clip_end=10
    return ob

def light(name,pos,power,size,target):
    data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size
    ob=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(ob);ob.location=pos
    ob.rotation_euler=(Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()

def evaluated_tree(ob):
    evaluated=ob.evaluated_get(bpy.context.evaluated_depsgraph_get());data=evaluated.to_mesh()
    tree=BVHTree.FromPolygons([ob.matrix_world@v.co for v in data.vertices],[list(p.vertices) for p in data.polygons])
    evaluated.to_mesh_clear();return tree

def build():
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Blender/Masters/M03_approved.blend'))
    car=bpy.data.objects['M03_CAR_ROOT'];keep={car,*car.children_recursive}
    for ob in list(bpy.data.objects):
        if ob not in keep:bpy.data.objects.remove(ob,do_unlink=True)
    front=geo.material('PKG_Front_Print',(1,1,1),.67,texture=TEX/'TC_master_front.png')
    back=geo.material('PKG_Back_Print',(1,1,1),.72,texture=TEX/'TC_master_back.png')
    edge=geo.material('PKG_Cardboard_Edge',(.65,.55,.40),.85)
    glass=geo.material('PKG_Clear_PET',(1,1,1),.055)
    bs=next(n for n in glass.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    bs.inputs['Transmission Weight'].default_value=1;bs.inputs['IOR'].default_value=1.46
    card=plate('PACK_Card_120x180mm',.120,.180,.003,-.00035,.00035,edge)
    cutter=plate('TOOL_Hanger_DieCut',.024,.006,.003,-.003,.003,edge,cy=.0825)
    bpy.context.view_layer.objects.active=card
    mod=card.modifiers.new('True centered hanging cutout','BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter
    bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cutter,do_unlink=True)
    bevel=card.modifiers.new('Card edge radius','BEVEL');bevel.width=.00010;bevel.segments=2
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    triangulate=card.modifiers.new('Deterministic hanger triangulation','TRIANGULATE')
    bpy.ops.object.modifier_apply(modifier=triangulate.name);uv_card(card,front,back,edge)
    flange=plate('PACK_Blister_SealingFlange',.1028,.054,.0062,.00045,.0008,glass,cy=-.0345)
    # Molded clear shell, open against card; Solidify supplies the real thin wall.
    profiles=[(.0988,.050,.006,.0008),(.0988,.050,.006,.004),
        (.0988,.050,.007,.061),(.097,.048,.0075,.067),(.091,.042,.008,.071)]
    verts=[];n=36
    for w,h,r,z in profiles:verts.extend((x,y,z) for x,y in outline(w,h,r,-.0345))
    faces=[]
    for j in range(len(profiles)-1):
        faces.extend((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for i in range(n))
    faces.append(tuple(range((len(profiles)-1)*n,len(profiles)*n)))
    blister=geo.mesh('PACK_ClearBlister_71mm',verts,faces,glass)
    for poly in blister.data.polygons:poly.use_smooth=True
    blister.data.set_sharp_from_angle(angle=.6)
    solid=blister.modifiers.new('PET wall 0.35mm','SOLIDIFY');solid.thickness=.00035;solid.offset=-1
    parts=[card,flange,blister]
    for x,label in [(-.02825,'Front'),(.02775,'Rear')]:
        support=geo.solid('PACK_InternalCradle_'+label,(x,-.0595,.035),(.012,.004,.018),glass,.0006)
        parts.append(support)
    car.rotation_euler=(-math.pi/2,0,0);car.location=(-.00069,-.0575,.035)
    root=bpy.data.objects.new('PACKAGE_ROOT',None);bpy.context.collection.objects.link(root)
    for ob in [*parts,car]:ob.parent=root
    root.rotation_euler=(math.pi/2,0,0);root.location=(0,0,.09)
    for ob in parts:
        if not ob.data.uv_layers:geo.generic_uv(ob)
    bpy.context.view_layer.update()
    failures=[];rows=[]
    deps=bpy.context.evaluated_depsgraph_get()
    for ob in parts:
        ev=ob.evaluated_get(deps);data=ev.to_mesh();data.calc_loop_triangles();bm=bmesh.new();bm.from_mesh(data)
        nonmanifold=sum(not e.is_manifold for e in bm.edges);volume=bm.calc_volume(signed=True)
        if nonmanifold:failures.append(ob.name+': nonmanifold '+str(nonmanifold))
        if volume<=0:failures.append(ob.name+': nonpositive volume')
        rows.append({'name':ob.name,'triangles':len(data.loop_triangles),'nonManifoldEdges':nonmanifold,'volumeM3':volume,'uvLayers':len(data.uv_layers)})
        bm.free();ev.to_mesh_clear()
    shell=evaluated_tree(blister)
    for ob in car.children_recursive:
        if ob.type=='MESH' and shell.overlap(evaluated_tree(ob)):failures.append(ob.name+': intersects blister shell')
    report={'status':'PACKAGING_GEOMETRY_CANDIDATE','cardDimensionsM':[.12,.18,.0007],
        'blisterFootprintPixels':[86,975,1114,1515],'hangerWidthM':.024,'hangerHeightM':.006,
        'blisterDepthM':.071,'sourceCar':'Blender/Masters/M03_approved.blend',
        'parts':rows,'technicalFailures':failures,'finishedPackages':0,
        'pending':['Visual review of front/back, clear shell and fit','Unity preview and transparency review','Family lineup and final SKU build assembly']}
    (PRE/'package_geometry_audit.json').write_text(json.dumps(report,indent=2),encoding='utf8')
    bpy.ops.object.select_all(action='DESELECT')
    for ob in [root,*parts]:ob.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(EXP/'PKG_Master.fbx'),use_selection=True,object_types={'MESH','EMPTY'},
        axis_forward='-Z',axis_up='Y',apply_unit_scale=True,use_mesh_modifiers=True,bake_anim=False,path_mode='COPY')
    bpy.ops.object.select_all(action='DESELECT')
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=64;scene.cycles.max_bounces=12
    scene.cycles.transmission_bounces=12;scene.render.film_transparent=False
    scene.render.resolution_x=900;scene.render.resolution_y=1200
    scene.render.image_settings.color_mode='RGBA';scene.view_layers[0].material_override=None
    world=next(n for n in scene.world.node_tree.nodes if n.type=='BACKGROUND')
    world.inputs[0].default_value=(.45,.5,.6,1);world.inputs[1].default_value=.35
    light('PACKAGE_Key',(-.2,-.3,.4),.8,.25,(0,0,.09));light('PACKAGE_Fill',(.25,-.1,.2),.4,.3,(0,0,.09))
    light('PACKAGE_Rim',(.1,.2,.35),.6,.2,(0,0,.09))
    ground=geo.material('PackageStudio',(.6,.64,.7),.8)
    bpy.ops.mesh.primitive_plane_add(size=2,location=(0,0,-.0005));bpy.context.object.name='STUDIO_Ground';bpy.context.object.data.materials.append(ground)
    cameras={'front':cam('CAM_PackageFront',(0,-.5,.09),(0,0,.09),.225),
        'three_quarter':cam('CAM_PackageThreeQuarter',(-.26,-.48,.21),(0,-.025,.09),.245),
        'back':cam('CAM_PackageBack',(0,.5,.09),(0,0,.09),.225)}
    scene.camera=cameras['three_quarter'];bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'PKG_Master_candidate.blend'))
    for name,camera in cameras.items():
        scene.camera=camera;scene.render.filepath=str(PRE/('package_'+name+'.png'));bpy.ops.render.render(write_still=True)
        print('PACKAGE_RENDER_DONE',name,flush=True)
    # Clear shell hidden only for the diagnostic clay image so the real car remains visible.
    clay=geo.material('Package_QA_Clay',(.48,.52,.58),.65)
    scene.view_layers[0].material_override=clay;blister.hide_render=True;flange.hide_render=True
    scene.camera=cameras['three_quarter'];scene.render.filepath=str(PRE/'package_clay.png');bpy.ops.render.render(write_still=True)
    scene.view_layers[0].material_override=None;blister.hide_render=False;flange.hide_render=False
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'PKG_Master_candidate.blend'))

if __name__=='__main__':build()
