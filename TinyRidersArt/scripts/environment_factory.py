"""Build modular toy-shop room art and a Blender inspection scene."""
import math
from pathlib import Path
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
GROUPS={}
def group(name):
    if name not in GROUPS:
        c=bpy.data.collections.new(name); bpy.context.scene.collection.children.link(c); GROUPS[name]=c
    return GROUPS[name]
def move(ob,name):
    for old in list(ob.users_collection): old.objects.unlink(ob)
    group(name).objects.link(ob)
def mat(name,color,rough=.65,alpha=1):
    m=bpy.data.materials.new(name); m.use_nodes=True
    p=next(n for n in m.node_tree.nodes if n.type=="BSDF_PRINCIPLED")
    p.inputs["Base Color"].default_value=color; p.inputs["Roughness"].default_value=rough
    p.inputs["Alpha"].default_value=alpha
    if alpha<1: m.surface_render_method="DITHERED"
    m.diffuse_color=color; return m
def box(name,pos,size,m,where,bevel=.035):
    bpy.ops.mesh.primitive_cube_add(size=1,location=pos); o=bpy.context.object; o.name=name
    o.dimensions=size; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(m); move(o,where)
    if bevel:
        b=o.modifiers.new("toy edge","BEVEL"); b.width=min(bevel,min(size)*.30); b.segments=1
        o.modifiers.new("weighted normals","WEIGHTED_NORMAL")
    return o
def sign(code,pos):
    path=ROOT/"textures"/"environment"/"signs"/(code+".png")
    m=bpy.data.materials.new(code+"_PRINTED_SIGN"); m.use_nodes=True
    n=m.node_tree.nodes.new("ShaderNodeTexImage"); n.image=bpy.data.images.load(str(path))
    p=next(x for x in m.node_tree.nodes if x.type=="BSDF_PRINCIPLED")
    m.node_tree.links.new(n.outputs["Color"],p.inputs["Base Color"])
    o=box(code+"_sign",pos,(1.75,.06,.44),navy,"Signs",.025)
    o.data.materials.append(m)
    while o.data.uv_layers:
        o.data.uv_layers.remove(o.data.uv_layers[0])
    uv=o.data.uv_layers.new(name="SignPrintUV")
    for poly in o.data.polygons:
        if poly.normal.y<-.5: poly.material_index=1
        for li in poly.loop_indices:
            v=o.data.vertices[o.data.loops[li].vertex_index].co
            uv.data[li].uv=(v.x/1.75+.5,v.z/.44+.5)
    return o
def shelf(name,x,y,width,levels,industrial=False):
    key=name
    dark=steel if industrial else navy
    for sx in (-1,1):
        box(name+" upright",(x+sx*(width/2-.07),y,1.38),(.13,.68,2.76),dark,key,.025)
    for j in range(levels):
        z=.34+j*(2.33/max(1,levels-1))
        box(name+" plank",(x,y,z),(width,.72,.12),sand if not industrial else steel,key,.025)
        box(name+" shelf edge",(x,y-.37,z),(width,.05,.16),orange if j==0 else dark,key,.018)
    box(name+" top",(x,y,2.83),(width+.16,.79,.15),dark,key,.025)
    box(name+" back",(x,y+.39,1.42),(width,.045,2.57),wall,key,.0)
def carton(name,pos,size,open_top=False):
    x,y,z=pos; w,d,h=size
    box(name+" base",(x,y,z+h*.20),(w,d,h*.40),card,"Storage",.025)
    if open_top:
        for s in (-1,1): box(name+" side",(x+s*(w/2-.05),y,z+h*.68),(.10,d,h*.65),card,"Storage",.01)
        for s in (-1,1): box(name+" end",(x,y+s*(d/2-.05),z+h*.68),(w,.10,h*.65),card,"Storage",.01)
    else: box(name+" lid",(x,y,z+h*.68),(w,d,h*.46),card,"Storage",.02)
def bin(name,pos,color):
    x,y,z=pos
    box(name+" floor",(x,y,z+.06),(.77,.56,.12),color,"Storage",.03)
    for s in (-1,1):
        box(name+" long side",(x+s*.355,y,z+.23),(.07,.56,.35),color,"Storage",.02)
        box(name+" short side",(x,y+s*.25,z+.23),(.76,.06,.35),color,"Storage",.02)

bpy.ops.wm.read_factory_settings(use_empty=True)
navy=mat("toy shop navy",(.045,.12,.28,1)); blue=mat("toy shop cobalt",(.08,.36,.81,1))
orange=mat("toy shop orange",(1,.48,.09,1)); sand=mat("warm shelf wood",(.76,.64,.45,1))
wall=mat("light blue wall",(.72,.83,.91,1)); floor=mat("floor tile",(.65,.72,.77,1),.80)
steel=mat("industrial blue steel",(.29,.38,.48,1),.48); card=mat("carton tan",(.63,.49,.31,1))
clear=mat("display acrylic",(.9,.97,1,1),.08,.18)

box("room floor",(0,0,-.12),(12,9,.24),floor,"RoomShell",.0)
box("back wall",(0,4.53,1.65),(12,.22,3.30),wall,"RoomShell",.0)
for s in (-1,1): box("side wall",(s*6.1,0,1.65),(.20,9.3,3.3),wall,"RoomShell",.0)
box("back skirting",(0,4.39,.14),(12,.13,.28),navy,"RoomShell",.0)
for s in (-1,1): box("wall stripe",(s*6.0,0,2.77),(.04,9.0,.22),blue,"RoomShell",.0)

shelf("TallWallShelf",-3.45,3.70,3.28,4)
shelf("DisplayShelf",.05,3.70,3.05,3)
shelf("IndustrialRack",3.35,3.70,3.10,4,True)

# The sorting workbench and tactile collection tray are modeled as separate modules.
box("sorting tabletop",(-2.15,-1.55,.97),(3.20,1.65,.18),sand,"SortingTable",.06)
for sx in (-1,1):
    for sy in (-1,1): box("table leg",(-2.15+sx*1.40,-1.55+sy*.68,.45),(.19,.19,.90),navy,"SortingTable",.03)
box("inspection podium",(-2.15,-1.55,1.10),(.75,.75,.13),blue,"SortingTable",.05)
box("inspection turntable",(-2.15,-1.55,1.20),(.52,.52,.07),orange,"SortingTable",.03)

box("pile tray base",(2.45,-1.32,.16),(3.35,2.25,.22),navy,"CollectionPit",.04)
for sx in (-1,1): box("pile tray long wall",(2.45+sx*1.61,-1.32,.44),(.14,2.25,.48),blue,"CollectionPit",.03)
for sy in (-1,1): box("pile tray short wall",(2.45,-1.32+sy*1.06,.44),(3.35,.14,.48),blue,"CollectionPit",.03)

for i in range(3): carton("shipping carton",(-4.8+i*.76,1.15,.02),(.64,.55,.65-.11*i),i==2)
for i in range(4): bin("sorting bin",(-.40+i*.80,-.11,.02),orange if i%2 else blue)
for i in range(3):
    box("drawer unit",(4.7,-3.6,.42+i*.42),(.95,.75,.38),steel,"Storage",.025)
    box("drawer face",(4.7,-4.0,.42+i*.42),(.82,.08,.30),navy,"Storage",.02)

box("premium plinth",(4.9,1.1,.45),(1.35,.95,.90),navy,"DisplayCase",.04)
box("premium case glass",(4.9,1.1,1.39),(1.23,.86,.96),clear,"DisplayCase",.025)
box("premium case cap",(4.9,1.1,1.91),(1.39,1.02,.12),orange,"DisplayCase",.03)

for i,code in enumerate(("CC","TC","RR","DP","BW","RC","WY","SS","FF","CR")):
    x=-4.3+(i%5)*2.15; z=3.02 if i<5 else 2.48
    sign(code,(x,4.36,z))

# Static art instances show actual finished car geometry on shelves and in the pile.
for i,code in enumerate(("CC","TC","RR","DP","BW","RC","WY","SS","FF","CR")):
    path=ROOT/"exports"/"builds"/(code+"-01-S.fbx")
    before=set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(path))
    imported=[o for o in bpy.data.objects if o not in before]
    root=bpy.data.objects.new(code+"_hero_static",None); group("SampleCars").objects.link(root)
    for ob in imported:
        if ob.parent not in imported:
            matrix=ob.matrix_world.copy(); ob.parent=root; ob.matrix_world=matrix
        move(ob,"SampleCars")
    root.scale=(.25,.25,.25)
    if i<3: root.location=(-4.40+i*.95,3.65,1.18)
    elif i<6: root.location=(-.90+(i-3)*.95,3.65,1.57)
    else:
        root.location=(1.40+(i-6)*.65,-1.38,.30)
        root.rotation_euler[2]=(.15,-.20,.28,-.33)[i-6]

# Warm studio rig for checking room composition in Blender.
for pos,power,size in [((-4,-5,7),1150,6),((5,2,7),850,5),((0,-1,7),700,6)]:
    ld=bpy.data.lights.new("shop softbox","AREA"); ld.energy=power; ld.shape="DISK"; ld.size=size
    ob=bpy.data.objects.new("shop softbox",ld); group("Lighting").objects.link(ob); ob.location=pos
    ob.rotation_euler=(Vector((0,0,1))-ob.location).to_track_quat("-Z","Y").to_euler()
camera_data=bpy.data.cameras.new("room camera"); camera=bpy.data.objects.new("room camera",camera_data)
group("Lighting").objects.link(camera); camera.location=(8,-11,7)
camera.rotation_euler=(Vector((0,.5,1.25))-camera.location).to_track_quat("-Z","Y").to_euler()
camera_data.type="ORTHO"; camera_data.ortho_scale=15
scene=bpy.context.scene; scene.camera=camera; scene.render.engine="BLENDER_EEVEE"
scene.render.resolution_x=1200; scene.render.resolution_y=800; scene.render.resolution_percentage=100
scene.render.image_settings.file_format="PNG"
scene.world=bpy.data.worlds.new("sorting room ambience")
scene.world.color=(.74,.82,.93)

blend=ROOT/"blender"/"SortingRoomArt.blend"; bpy.ops.wm.save_as_mainfile(filepath=str(blend))
fbx_dir=ROOT/"exports"/"environment"; fbx_dir.mkdir(parents=True,exist_ok=True)
for name,collection in GROUPS.items():
    if name in ("Lighting","SampleCars"): continue
    bpy.ops.object.select_all(action="DESELECT")
    for ob in collection.objects: ob.select_set(True)
    active=next(iter(collection.objects),None)
    if active: bpy.context.view_layer.objects.active=active
    bpy.ops.export_scene.fbx(filepath=str(fbx_dir/(name+".fbx")),use_selection=True,object_types={"MESH"},path_mode="AUTO")
bpy.ops.object.select_all(action="DESELECT")
for ob in bpy.data.objects:
    if ob.type=="MESH" and "SampleCars" not in [c.name for c in ob.users_collection]: ob.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(fbx_dir/"SortingRoom_All.fbx"),use_selection=True,object_types={"MESH"},path_mode="AUTO")
preview=ROOT/"previews"/"environment"/"SortingRoom.png"; preview.parent.mkdir(parents=True,exist_ok=True)
scene.render.filepath=str(preview); bpy.ops.render.render(write_still=True)
print("ENVIRONMENT ART",blend,preview,flush=True)
