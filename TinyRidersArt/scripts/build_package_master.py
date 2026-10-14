"""Make the 120 x 180 mm die-cut card and reusable clear blister in Blender."""
import math
from pathlib import Path
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
SAMPLE="TR-TC-01-S-01"

def rounded(w,z0,z1,r):
    result=[]
    for cx,cz,a0 in [(w/2-r,z0+r,-90),(w/2-r,z1-r,0),(-w/2+r,z1-r,90),(-w/2+r,z0+r,180)]:
        for j in range(5):
            a=math.radians(a0+j*90/4)
            result.append((cx+r*math.cos(a),cz+r*math.sin(a)))
    return result

def image_mat(name,path):
    m=bpy.data.materials.new(name); m.use_nodes=True
    p=next(n for n in m.node_tree.nodes if n.type=="BSDF_PRINCIPLED")
    p.inputs["Roughness"].default_value=.76
    t=m.node_tree.nodes.new("ShaderNodeTexImage"); t.image=bpy.data.images.load(str(path),check_existing=True)
    m.node_tree.links.new(t.outputs["Color"],p.inputs["Base Color"])
    return m

def solid(name,color,rough=.8,transmission=0):
    m=bpy.data.materials.new(name); m.use_nodes=True
    p=next(n for n in m.node_tree.nodes if n.type=="BSDF_PRINCIPLED")
    p.inputs["Base Color"].default_value=color; p.inputs["Roughness"].default_value=rough
    p.inputs["Transmission Weight"].default_value=transmission
    m.diffuse_color=color
    return m

def make_card():
    contour=rounded(.120,0,.180,.003)
    n=len(contour); verts=[(x,-.00035,z) for x,z in contour]+[(x,.00035,z) for x,z in contour]
    faces=[tuple(range(n)),tuple(reversed(range(n,2*n)))]
    for i in range(n): faces.append((i,(i+1)%n,(i+1)%n+n,i+n))
    mesh=bpy.data.meshes.new("card 120x180mm diecut mesh"); mesh.from_pydata(verts,[],faces); mesh.update()
    ob=bpy.data.objects.new("PackagingCard_120x180mm",mesh); bpy.context.collection.objects.link(ob)
    # The real through-cut is centered at x=600 px and y=45..105 px.
    bpy.ops.mesh.primitive_cube_add(size=1,location=(0,0,.1725)); cutter=bpy.context.object
    cutter.name="Hanger cutter"; cutter.dimensions=(.0135,.012,.006)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    b=cutter.modifiers.new("rounded slot","BEVEL"); b.width=.0028; b.segments=4
    bpy.context.view_layer.objects.active=cutter; bpy.ops.object.modifier_apply(modifier=b.name)
    mod=ob.modifiers.new("hanging cutout","BOOLEAN"); mod.operation="DIFFERENCE"; mod.object=cutter
    bpy.context.view_layer.objects.active=ob; bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.data.objects.remove(cutter,do_unlink=True)
    # Boolean inherits an empty cutter material slot at index zero in Blender 5.1.
    ob.data.materials.clear()
    ob.data.materials.append(image_mat("TR_CARD_FRONT",ROOT/"textures"/"packages"/"front"/(SAMPLE+".png")))
    ob.data.materials.append(image_mat("TR_CARD_BACK",ROOT/"textures"/"packages"/"back"/(SAMPLE+".png")))
    ob.data.materials.append(solid("TR_CARDBOARD_EDGE",(.72,.64,.51,1)))
    # The Boolean creates an empty default UVMap. FBX exports that first layer
    # as Unity's UV0 unless it is removed before authoring the print UVs.
    while ob.data.uv_layers:
        ob.data.uv_layers.remove(ob.data.uv_layers[0])
    uv=ob.data.uv_layers.new(name="CardFrontBack_PrintUV")
    for poly in ob.data.polygons:
        if poly.normal.y < -.5: poly.material_index=0
        elif poly.normal.y > .5: poly.material_index=1
        else: poly.material_index=2
        for li in poly.loop_indices:
            v=ob.data.vertices[ob.data.loops[li].vertex_index].co
            x=(v.x+.06)/.12 if poly.material_index==0 else (.06-v.x)/.12
            uv.data[li].uv=(x,v.z/.18)
    # Unity's FBX importer loses the print UVs on Boolean-generated n-gons.
    # Apply triangulation after UV assignment so every printed triangle has
    # explicit coordinates in the exported master mesh.
    tri=ob.modifiers.new("FBX print triangles","TRIANGULATE")
    tri.quad_method="BEAUTY"; tri.ngon_method="BEAUTY"
    bpy.context.view_layer.objects.active=ob
    bpy.ops.object.modifier_apply(modifier=tri.name)
    return ob

def make_blister():
    # Front shell, tapered walls and flat sealing flange; no printed car image here.
    front=rounded(.098,.031,.080,.004)
    rear=rounded(.1028,.0285,.0825,.004)
    flange=rounded(.109,.0255,.0855,.004)
    n=len(front)
    verts=[(x,-.043,z) for x,z in front]+[(x,-.001,z) for x,z in rear]+[(x,-.001,z) for x,z in flange]
    faces=[tuple(range(n))]
    for i in range(n):
        j=(i+1)%n
        faces.append((i,j,n+j,n+i))
        faces.append((n+i,n+j,2*n+j,2*n+i))
    mesh=bpy.data.meshes.new("clear blister shell mesh"); mesh.from_pydata(verts,[],faces); mesh.update()
    ob=bpy.data.objects.new("ClearBlister_StandardFootprint",mesh); bpy.context.collection.objects.link(ob)
    clear=solid("TR_CLEAR_BLISTER",(.95,.98,1,1),.12,.93)
    p=next(n for n in clear.node_tree.nodes if n.type=="BSDF_PRINCIPLED")
    p.inputs["Alpha"].default_value=.13
    clear.surface_render_method="DITHERED"
    ob.data.materials.append(clear)
    sol=ob.modifiers.new("plastic wall thickness","SOLIDIFY"); sol.thickness=.00055
    return ob

def render_sample():
    path=ROOT/"exports"/"builds"/"TC-01-S.fbx"
    before=set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(path))
    imported=[o for o in bpy.data.objects if o not in before]
    root=bpy.data.objects.new("SAMPLE_CAR_ROOT",None); bpy.context.collection.objects.link(root)
    for ob in imported:
        ob.name="SAMPLE_CAR_"+ob.name
        if ob.parent not in imported:
            world_matrix=ob.matrix_world.copy(); ob.parent=root; ob.matrix_world=world_matrix
    root.scale=(.020,.020,.020); root.location=(0,-.022,.030)
    world=bpy.data.worlds.new("preview world"); bpy.context.scene.world=world; world.use_nodes=True
    world.node_tree.nodes["Background"].inputs["Color"].default_value=(.80,.85,.93,1)
    for pos,power,size in [((-.10,-.15,.25),.65,.16),((.10,-.05,.20),.45,.14)]:
        ld=bpy.data.lights.new("package softbox","AREA"); ld.energy=power; ld.shape="DISK"; ld.size=size
        ob=bpy.data.objects.new("package softbox",ld); bpy.context.collection.objects.link(ob); ob.location=pos
        ob.rotation_euler=(Vector((0,0,.09))-ob.location).to_track_quat("-Z","Y").to_euler()
    camd=bpy.data.cameras.new("package preview camera"); cam=bpy.data.objects.new("package preview camera",camd)
    bpy.context.collection.objects.link(cam); cam.location=(.005,-.35,.093)
    cam.rotation_euler=(Vector((0,0,.09))-cam.location).to_track_quat("-Z","Y").to_euler()
    camd.type="ORTHO"; camd.ortho_scale=.21
    scene=bpy.context.scene; scene.camera=cam; scene.render.engine="BLENDER_EEVEE"
    scene.view_settings.view_transform="Standard"
    scene.render.resolution_x=700; scene.render.resolution_y=1050; scene.render.resolution_percentage=100
    scene.render.image_settings.file_format="PNG"; scene.render.filepath=str(ROOT/"previews"/"packages"/(SAMPLE+".png"))
    Path(scene.render.filepath).parent.mkdir(parents=True,exist_ok=True)
    bpy.ops.render.render(write_still=True)

if __name__=="__main__":
    bpy.ops.wm.read_factory_settings(use_empty=True)
    card=make_card(); blister=make_blister()
    out=ROOT/"blender"/"PackagingMaster.blend"; out.parent.mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(out))
    for o in bpy.data.objects: o.select_set(o in (card,blister))
    bpy.context.view_layer.objects.active=card
    fbx=ROOT/"exports"/"packages"/"PackagingMaster.fbx"; fbx.parent.mkdir(parents=True,exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,object_types={"MESH"},path_mode="AUTO")
    render_sample()
    print("PACKAGE MASTER",out,fbx,flush=True)
