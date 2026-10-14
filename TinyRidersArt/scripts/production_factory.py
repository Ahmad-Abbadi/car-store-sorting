"""Deterministic Tiny Riders mesh factory. Run with Blender --background --python.

This produces editable build sources, FBX geometry, and small studio previews. Printed
graphics come from production_art.py and are UV mapped onto the shell, never meshes.
"""
import argparse
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
from palette import ACCESSORY_COLORS, CORE_COLORS

ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT.parent
KIT = PROJECT / "Tiny_Riders_Autonomous_Art_Kit" / "Tiny_Riders_500_Car_Production_Kit"
BODIES = json.loads((KIT / "catalog" / "bodies_50.json").read_text(encoding="utf-8"))
FAMILIES = {v["code"]: v for v in json.loads((KIT / "catalog" / "family_specs.json").read_text(encoding="utf-8"))}

# length, width, wheel radius, shell height, cab height, cab start, cab end, shape
# Front of every car is -X. These named recipes preserve each catalog silhouette.
RECIPES = {
    "CC": [(2.45,1.42,.37,1.02,.53,-.34,.67,"square"),(2.15,1.34,.35,1.00,.78,-.64,.70,"round"),(2.95,1.48,.38,1.05,.85,-.78,1.25,"van"),(2.78,1.48,.36,1.00,.76,-.85,1.15,"van"),(3.35,1.55,.39,1.04,.88,-1.22,1.38,"bus")],
    "TC": [(3.02,1.70,.47,1.08,.55,-.48,.73,"race"),(3.32,1.68,.43,.86,.39,-.17,.90,"wedge"),(2.95,1.69,.44,1.05,.58,-.55,.40,"pickup"),(4.04,1.42,.48,.76,.39,.60,1.34,"drag"),(3.22,1.82,.45,.93,.40,-.30,.77,"race")],
    "RR": [(2.94,1.58,.41,1.10,.59,-.48,.62,"round"),(3.58,1.58,.40,1.00,.66,-.62,1.41,"wagon"),(2.75,1.58,.43,1.00,.65,-.50,.55,"round"),(2.80,1.34,.45,.97,.40,-.27,.36,"hotrod"),(3.16,1.62,.41,1.02,.49,-.32,.98,"fastback")],
    "DP": [(2.83,1.60,.51,1.14,.57,-.34,.54,"buggy"),(3.48,1.63,.52,1.20,.67,-.73,.24,"pickup"),(2.92,1.64,.55,1.04,.30,-.12,.46,"open"),(3.28,1.66,.52,1.18,.69,-.68,1.02,"wagon"),(3.47,1.66,.51,1.20,.93,-.94,1.25,"van")],
    "BW": [(3.12,1.56,.72,1.55,.55,-.63,.41,"pickup"),(3.40,1.64,.72,1.57,.76,-.93,1.23,"van"),(2.78,1.49,.74,1.60,.56,-.50,.58,"square"),(3.31,1.66,.76,1.66,.55,-.90,-.23,"dump"),(3.20,1.68,.73,1.56,.46,-.40,.48,"buggy")],
    "RC": [(3.08,1.65,.43,1.05,.48,-.38,.71,"race"),(2.85,1.58,.46,1.10,.64,-.39,.79,"race"),(3.62,1.65,.44,1.14,.57,-1.08,-.20,"truck"),(3.33,1.64,.44,1.10,.60,-.85,-.01,"pickup"),(3.30,1.62,.45,1.11,.57,-.67,.08,"pickup")],
    "WY": [(3.64,1.66,.49,1.19,.64,-1.08,-.20,"dump"),(3.51,1.67,.48,1.15,.62,-1.06,-.22,"mixer"),(2.94,1.48,.46,1.11,.73,-.33,.45,"fork"),(3.67,1.65,.49,1.18,.63,-1.03,-.23,"crane"),(3.25,1.67,.53,1.14,.69,-.48,.43,"loader")],
    "SS": [(2.70,1.56,.42,1.03,.58,-.46,.64,"round"),(2.93,1.56,.42,.98,.47,-.26,.70,"wedge"),(2.91,1.60,.43,1.06,.67,-.67,.82,"van"),(3.18,1.61,.43,1.07,.78,-.92,1.12,"van"),(2.72,1.53,.43,1.01,.50,-.36,.69,"round")],
    "FF": [(3.19,1.70,.42,.89,.46,-.32,.85,"wedge"),(2.65,1.51,.39,.88,.66,-.53,.65,"capsule"),(3.36,1.68,.44,.93,.47,-.55,.30,"pickup"),(3.55,1.72,.41,1.12,.66,-.80,.56,"rover"),(3.54,1.81,.42,.77,.32,-.04,.86,"wedge")],
    "CR": [(2.88,1.56,.43,1.01,.46,-.27,.62,"shark"),(2.72,1.57,.42,.99,.65,-.55,.69,"shell"),(3.10,1.59,.44,1.07,.57,-.66,.56,"truck"),(2.80,1.70,.44,1.01,.55,-.38,.56,"frog"),(3.07,1.62,.44,.99,.44,-.29,.66,"dragon")],
}

def hexrgb(value):
    def linear(n):
        c=int(value[n:n+2],16)/255
        return c/12.92 if c<=.04045 else ((c+.055)/1.055)**2.4
    return (linear(1),linear(3),linear(5),1)

def mat(name, color, rough=.46, metal=0):
    m=bpy.data.materials.new(name); m.use_nodes=True
    p=next(n for n in m.node_tree.nodes if n.type=="BSDF_PRINCIPLED")
    p.inputs["Base Color"].default_value=color; p.inputs["Roughness"].default_value=rough
    p.inputs["Metallic"].default_value=metal; m.diffuse_color=color
    return m

def box(name, pos, size, material, bevel=.025):
    bpy.ops.mesh.primitive_cube_add(size=1, location=pos); o=bpy.context.object; o.name=name
    o.dimensions=size; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(material)
    if bevel:
        b=o.modifiers.new("toy edge","BEVEL"); b.width=bevel; b.segments=1
        o.modifiers.new("normals","WEIGHTED_NORMAL")
    return o

def cylinder(name, pos, radius, depth, material, axis="Y", sides=12):
    bpy.ops.mesh.primitive_cylinder_add(vertices=sides, radius=radius, depth=depth, location=pos)
    o=bpy.context.object; o.name=name
    if axis=="Y": o.rotation_euler[0]=math.pi/2
    elif axis=="X": o.rotation_euler[1]=math.pi/2
    o.data.materials.append(material)
    b=o.modifiers.new("toy edge","BEVEL"); b.width=min(.035,radius*.1); b.segments=1
    o.modifiers.new("normals","WEIGHTED_NORMAL")
    return o

def cone(name, pos, radius, height, material, sides=4):
    bpy.ops.mesh.primitive_cone_add(vertices=sides, radius1=radius, radius2=0, depth=height, location=pos)
    o=bpy.context.object; o.name=name; o.data.materials.append(material); return o

def print_mesh(name, verts, face_defs, image_mat, tint_mat):
    mesh=bpy.data.meshes.new(name); mesh.from_pydata(verts, [], [a for a,u,mi in face_defs]); mesh.update()
    ob=bpy.data.objects.new(name,mesh); bpy.context.collection.objects.link(ob)
    mesh.materials.append(image_mat); mesh.materials.append(tint_mat)
    uv=mesh.uv_layers.new(name="TinyRiders_PrintUV")
    for poly,(_,coords,mi) in zip(mesh.polygons,face_defs):
        poly.material_index=mi
        for li,co in zip(poly.loop_indices,coords): uv.data[li].uv=co
    b=ob.modifiers.new("toy edge","BEVEL"); b.width=.025; b.segments=1
    ob.modifiers.new("normals","WEIGHTED_NORMAL")
    return ob

def uv(region, a, b):
    # image atlas: left side, right side, hood, front, rear, cabin faces
    x0,y0,x1,y1=region
    return ((x0+(x1-x0)*a)/1024,1-(y0+(y1-y0)*b)/1024)

REG={"left":(12,10,500,245),"right":(524,10,1012,245),
     "top":(12,264,500,490),"front":(524,264,760,490),"rear":(776,264,1012,490),
     "cab_left":(12,510,500,740),"cab_right":(524,510,1012,740),
     "cab_front":(12,760,240,1008),"cab_rear":(255,760,483,1008),
     "cab_roof":(500,760,1012,1008)}

def shell(body, recipe, mats):
    length,width,r,h,cz,c0,c1,kind=recipe
    x0=-length/2; x1=length/2; wb=width/2
    bottom=max(r*.86,.38) if body["familyCode"]!="BW" else r*1.31
    nose_ratio={"wedge":.52,"drag":.58,"shark":.61,"capsule":.72,"round":.74,"hotrod":.80,"buggy":.84,"race":.86}.get(kind,.92)
    tail_ratio={"fastback":.75,"capsule":.76,"round":.82,"wedge":.78,"drag":.84}.get(kind,.94)
    xs=[x0,x0+.24,max(x0+.45,c0-.12),min(x1-.42,c1+.10),x1-.22,x1]
    # A long sloping hood and shaped rear profile give different families real
    # silhouette differences, rather than just changing cabin height.
    tops=[h*nose_ratio,h*(nose_ratio+.13*(1-nose_ratio)),h,h,h*tail_ratio,h*tail_ratio*.94]
    widths=[wb*.78,wb*.94,wb,wb,wb*.93,wb*.84]
    verts=[]
    for x,w,z in zip(xs,widths,tops): verts += [(x,-w,bottom),(x,w,bottom),(x,w,z),(x,-w,z)]
    faces=[]
    for i in range(len(xs)-1):
        a=4*i; b=4*(i+1)
        for inds,reg in [([a+3,b+3,b+2,a+2],"top"),([a,b,b+3,a+3],"left"),([a+1,a+2,b+2,b+1],"right")]:
            coords=[]
            for vi in inds:
                x,y,z=verts[vi]
                if reg=="top": q=((x-x0)/length,(y+wb)/(2*wb))
                else: q=((x-x0)/length if reg=="left" else (x1-x)/length,(h-z)/max(.1,h-bottom))
                coords.append(uv(REG[reg],*q))
            faces.append((inds,coords,0))
        faces.append(([a,a+1,b+1,b],[uv(REG["top"],.5,.5)]*4,1))
    last=4*(len(xs)-1)
    for inds,reg in [([0,3,2,1],"front"),([last,last+1,last+2,last+3],"rear")]:
        coords=[uv(REG[reg],(verts[i][1]+wb)/(2*wb),(h-verts[i][2])/max(.1,h-bottom)) for i in inds]
        faces.append((inds,coords,0))
    print_mesh(body["bodyId"]+"_connected_shell",verts,faces,mats["print"],mats["paint"])
    # Cab sides are trapezoids; the dark windows are pixels on their actual facets.
    cabw=wb*(.78 if kind in ("race","drag","wedge","hotrod") else .88)
    if kind in ("van","bus","truck","dump","mixer"): c0=min(c0,x0+.14)
    roof0=c0+(.11 if kind in ("van","bus","truck","dump") else .28)
    roof1=c1-(.07 if kind in ("van","bus","truck","dump") else .25)
    top=h+cz
    v=[(c0,-cabw,h),(c0,cabw,h),(c1,cabw,h),(c1,-cabw,h),
       (roof0,-cabw*.91,top),(roof0,cabw*.91,top),(roof1,cabw*.91,top),(roof1,-cabw*.91,top)]
    ff=[]
    for inds,reg in [([0,1,5,4],"cab_front"),([3,7,6,2],"cab_rear"),([0,4,7,3],"cab_left"),([1,2,6,5],"cab_right"),([4,5,6,7],"cab_roof")]:
        coords=[]
        for vi in inds:
            x,y,z=v[vi]
            if reg=="cab_roof": q=((x-c0)/(c1-c0),(y+cabw)/(2*cabw))
            elif reg in ("cab_left","cab_right"): q=((x-c0)/(c1-c0) if reg=="cab_left" else (c1-x)/(c1-c0),(top-z)/cz)
            else: q=((y+cabw)/(2*cabw),(top-z)/cz)
            coords.append(uv(REG[reg],*q))
        ff.append((inds,coords,0))
    ff.append(([0,3,2,1],[uv(REG["top"],.5,.5)]*4,1))
    print_mesh(body["bodyId"]+"_cabin",v,ff,mats["print"],mats["paint"])
    return top,bottom

def wheels(recipe,mats,body):
    length,width,r,h,cz,c0,c1,kind=recipe
    xfront=-length*.33; xrear=length*.33
    if kind=="drag": xfront=-length*.39; xrear=length*.40
    xs=[xfront,xrear]
    if kind=="rover": xs=[-length*.34,0,length*.34]
    for x in xs:
        rr=r*(1.12 if kind=="drag" and x>0 else 1)
        for side in (-1,1):
            y=side*(width/2+.105)
            cylinder("Tire",(x,y,rr),rr,.26,mats["rubber"])
            cylinder("Rim",(x,y+side*.15,rr),rr*.59,.035,mats["rim"])
            cylinder("Hub",(x,y+side*.18,rr),rr*.28,.04,mats["hub"])
    return xs

def details(body,recipe,mats,custom):
    name=body["name"]; fam=body["familyCode"]
    L,W,r,h,ch,c0,c1,kind=recipe; hi=h+ch; accent=mats["accent"]; dark=mats["dark"]
    # Real structural parts shared by every toy, sized to the recipe.
    box("underbody",(0,0,max(.29,r*.7)),(L*.91,W*.79,.15),dark,.04)
    box("front bumper",(-L/2-.035,0,max(.42,r*.95)),(.18,W*.84,.18),dark,.04)
    box("rear bumper",(L/2+.025,0,max(.42,r*.95)),(.16,W*.82,.17),dark,.04)
    def roofbox(label,x=0,wide=.65,tall=.18): return box(label,(x,0,hi+tall/2),(max(.22,(c1-c0)*.55),wide,tall),accent,.035)
    def rack(label,height=.12):
        for s in (-1,1): box(label+" rail",(.1,s*W*.29,hi+height),(max(.55,c1-c0),.09,.13),dark,.02)
        box(label+" load",(.13,0,hi+height+.13),(max(.6,c1-c0)*.75,W*.50,.17),accent,.03)
    def wing(big=False):
        z=hi+(.58 if big else .19)
        for s in (-1,1): box("wing support",(L*.37,s*W*.28,(h+z)/2),(.17,.14,z-h),dark,.02)
        box("rear airfoil",(L*.37,0,z),(.46 if big else .37,W*(1.22 if big else .94),.16),accent,.03)
        if big:
            for s in (-1,1): box("wing endplate",(L*.37,s*W*.60,z),(.48,.10,.32),dark,.02)
    def bed(label,raised=False):
        z=h+(.22 if raised else .04)
        box(label+" floor",(L*.27,0,z),(L*.38,W*.82,.16),dark,.03)
        for s in (-1,1): box(label+" side",(L*.27,s*W*.42,z+.18),(L*.38,.10,.39),accent,.02)
        box(label+" tail",(L*.47,0,z+.17),(.10,W*.83,.38),accent,.02)
    def beacon():
        box("beacon mount",(0,0,hi+.07),(.65,.58,.13),dark,.02)
        for s in (-1,1): box("beacon lens",(0,s*.19,hi+.20),(.26,.24,.16),accent,.03)
    if kind in ("pickup","dump","truck","mixer","crane","loader") and name not in ("Volt",): bed("cargo",custom and name in ("Tipper","Mammoth"))
    if name in ("Blue Racer","Wedge","Apex","Pixel","Vector","Flash") or (custom and fam=="TC"): wing(custom)
    if name in ("Parcel","Metro","Diner","Bento"):
        box("roof sign",((c0+c1)/2,0,hi+.16),(.57,.43,.31),accent,.04)
    if name in ("Loop","Longroof","Scout","Trail","Pebble","Ridge","Rover") or (custom and name in ("Pocket","Bean")): rack("utility")
    if name=="Beach" and not custom:
        box("beach surfboard",(.10,0,hi+.18),(1.28,.35,.16),accent,.075)
    if name in ("Flash","Medic","Ladder","Harbor") or (custom and fam=="RC"): beacon()
    if name in ("Rocket","Chop"):
        box("engine scoop",(-L*.18,0,h+.21),(.51,.43,.40),dark,.04)
        if custom:
            for s in (-1,1): box("exhaust",(-L*.07,s*W*.47,h*.70),(.52,.09,.13),accent,.02)
    if name in ("Dune","Lift"):
        for s in (-1,1): box("cage upright",(0,s*W*.35,hi*.76),(.12,.11,hi*.49),dark,.02)
        box("cage top",(0,0,hi+.08),(.95,W*.77,.13),dark,.02)
    if name=="Lift":
        for s in (-1,1):
            box("fork mast",(-L*.44,s*.34,hi*.68),(.13,.15,hi*.95),dark,.02)
            box("fork tine",(-L*.62,s*.34,.20),(.80,.13,.12),dark,.02)
    if name=="Mixer":
        cylinder("large mixing drum",(L*.20,0,h+.67),.62,1.38,accent,"X",12)
        cylinder("rear drum collar",(L*.39,0,h+.67),.67,.13,dark,"X",12)
    if name in ("Tipper","Mammoth"):
        for s in (-1,1): box("raised tipper wall",(L*.29,s*W*.40,h+.55),(L*.45,.14,.86),accent,.035)
        box("raised tail gate",(L*.49,0,h+.52),(.14,W*.82,.78),accent,.035)
    if name=="Blockade":
        box("armored roof canopy",(.10,0,hi+.24),(L*.50,W*.83,.43),accent,.055)
        box("armored face guard",(-L*.48,0,h*.89),(.23,W*.95,.51),dark,.035)
    if name in ("Crane","Hook"):
        box("boom pillar",(L*.29,0,hi*.87),(.25,.29,hi*.76),dark,.02)
        boom=box("boom",(L*.07,0,hi*1.35),(L*.70,.31,.30),accent,.04)
        boom.rotation_euler[1]=-.22 if custom else -.10
        box("hook",(-L*.25,0,hi*1.18),(.17,.19,.31),dark,.02)
    if name=="Loader":
        for s in (-1,1): box("loader arm",(-L*.33,s*W*.33,h*.85),(.95,.16,.18),accent,.02)
        box("loader bucket",(-L*.61,0,h*.61),(.36,W*.95,.40),dark,.03)
    if name=="Ladder":
        for s in (-1,1): box("folded ladder rail",(L*.23,s*.29,h+.39),(L*.49,.11,.12),accent,.02)
        for j in range(5): box("ladder rung",(L*.04+j*L*.095,0,h+.41),(.10,.69,.10),dark,.01)
        for x in (-L*.18,L*.33):
            for s in (-1,1): box("elevated ladder support",(x,s*.29,hi+.26),(.15,.14,.61),dark,.02)
        for s in (-1,1):
            rail=box("long elevated ladder rail",(L*.13,s*.34,hi+.61),(L*.91,.11,.13),accent,.025)
            rail.rotation_euler[1]=-.18
        for j in range(7): box("elevated ladder rung",(-L*.23+j*L*.105,0,hi+.61),(.11,.72,.10),dark,.012)
    if name in ("Scoop","Slice","Burger","Bento","Donut"):
        if name=="Scoop":
            cone("ice cream base",(0,0,hi+.16),.46,.38,accent,10)
            for j,(x,z,rad) in enumerate(((0,.43,.43),(.10,.78,.32),(-.02,1.05,.21))):
                bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=rad,location=(x,0,hi+z))
                bpy.context.object.name=f"ice cream swirl {j+1}"; bpy.context.object.data.materials.append(accent)
        elif name=="Slice":
            bpy.ops.mesh.primitive_cone_add(vertices=3,radius1=.74,radius2=.74,depth=.20,location=(0,0,hi+.08))
            bpy.context.object.name="flat pizza wedge"; bpy.context.object.data.materials.append(accent)
        elif name=="Burger":
            for j in range(3): cylinder("burger layer",(0,0,hi+.06+j*.13),.51,.15,accent if j!=1 else dark,"Z",12)
        elif name=="Bento": roofbox("lunch box",.15,.90,.42)
        else:
            bpy.ops.mesh.primitive_torus_add(major_segments=16,minor_segments=6,
                major_radius=.55 if not custom else .62,minor_radius=.19,location=(0,0,hi+.16))
            bpy.context.object.name="real donut ring"; bpy.context.object.data.materials.append(accent)
    if fam=="CR":
        if name=="Shark":
            cone("dorsal fin",(.18,0,hi+.37),.40,.79,accent,3)
            cone("shark tail upper fin",(L*.44,0,h+.38),.36,.82,accent,3)
            cone("shark tail lower fin",(L*.44,0,h+.18),.27,.43,accent,3)
        elif name=="Turtle":
            for x in (-.32,.18,.55): box("shell plate",(x,0,hi+.08),(.46,W*.78,.20),accent,.08)
        elif name=="Dino":
            for x in (.05,.48,.89): cone("back plate",(x,0,hi+.20),.27,.45,accent,3)
        elif name=="Frog":
            for s in (-1,1): cylinder("eye housing",(-.24,s*.49,hi+.10),.25,.27,accent,"Z",10)
        elif name=="Dragon":
            for s in (-1,1): cone("horn",(-.25,s*.43,hi+.21),.20,.46,accent,4)
        if custom:
            for s in (-1,1): box("custom side fin",(.54,s*W*.51,h+.12),(.49,.20,.30),accent,.03)
    if custom:
        if name=="Bean": beacon()
        if name in ("Longroof","Beach"):
            box("surfboard",(.05,0,hi+.25),(1.25,.29,.12),accent,.055)
            box("surfboard stripe support",(.05,0,hi+.13),(.60,.34,.10),dark,.02)
        if name=="Ridge":
            for j in range(2): box("bed camping load",(L*.30,j*.37-.19,h+.38),(.54,.32,.42),accent,.03)
        if name=="Trail": cylinder("rear spare tire",(L/2+.13,0,h+.16),r*.78,.22,dark,"X",12)
        if name=="Harbor":
            bpy.ops.mesh.primitive_torus_add(major_segments=12,minor_segments=4,
                major_radius=.28,minor_radius=.08,location=(L*.28,0,h+.28))
            bpy.context.object.name="rescue float"; bpy.context.object.data.materials.append(accent)
        if name=="Orbit":
            box("sensor mast",(.25,0,hi+.23),(.12,.12,.42),dark,.02)
            cylinder("sensor dish",(.25,0,hi+.47),.32,.09,accent,"Z",12)
        if name=="Rover":
            for s in (-1,1): box("science sensor",(.12,s*.35,hi+.30),(.28,.25,.30),accent,.03)
        if name=="Ladder":
            box("raised ladder extension",(L*.16,0,h+.58),(L*.38,.57,.09),accent,.02)
        if name not in ("Blue Racer","Wedge","Apex","Pixel","Vector","Flash") and fam not in ("CR",):
            roofbox("custom equipment",.2,W*.42,.20)
        for s in (-1,1): box("custom side guard",(0,s*(W/2+.035),max(.48,r)),(L*.48,.12,.16),accent,.02)
        box("custom chin",(-L/2-.13,0,max(.34,r*.75)),(.28,W*.98,.10),accent,.02)

def studio_and_render(path,body,recipe):
    floor=mat("studio floor",(.87,.90,.95,1),.86)
    box("STUDIO floor",(0,0,-.13),(8,7,.20),floor,.0)
    world=bpy.context.scene.world or bpy.data.worlds.new("studio world"); bpy.context.scene.world=world
    world.use_nodes=True; world.node_tree.nodes["Background"].inputs["Color"].default_value=(.75,.82,.93,1)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value=.8
    for pos,power,size in [((-3,-4,6),850,4),((2,3,5),700,3),((0,-1,6),400,4)]:
        data=bpy.data.lights.new("studio softbox","AREA"); data.energy=power; data.shape="DISK"; data.size=size
        ob=bpy.data.objects.new("studio softbox",data); bpy.context.collection.objects.link(ob); ob.location=pos
        ob.rotation_euler=(Vector((0,0,.75))-ob.location).to_track_quat("-Z","Y").to_euler()
    camd=bpy.data.cameras.new("studio camera"); cam=bpy.data.objects.new("studio camera",camd)
    bpy.context.collection.objects.link(cam); cam.location=(-5.6,-6.9,4.4)
    cam.rotation_euler=(Vector((0,0,.82))-cam.location).to_track_quat("-Z","Y").to_euler()
    camd.type="ORTHO"; camd.ortho_scale=max(4.9,recipe[0]*1.53)
    scene=bpy.context.scene; scene.camera=cam
    scene.view_settings.view_transform="Standard"
    scene.view_settings.exposure=-1.2
    scene.render.engine="BLENDER_EEVEE"; scene.render.resolution_x=512; scene.render.resolution_y=384
    scene.render.resolution_percentage=100; scene.render.image_settings.file_format="PNG"
    scene.render.film_transparent=False; scene.render.filepath=str(path)
    path.parent.mkdir(parents=True,exist_ok=True); bpy.ops.render.render(write_still=True)

def build(body, custom, preview=False, force=False):
    bid=body["bodyId"]; code=body["familyCode"]; idx=int(bid[-2:])-1
    buildkey="C" if custom else "S"; key=f"{bid}-{buildkey}"
    blend=ROOT/"blender"/"builds"/(key+".blend")
    fbx=ROOT/"exports"/"builds"/(key+".fbx")
    png=ROOT/"previews"/"builds"/(key+".png")
    if not force and blend.exists() and fbx.exists() and (not preview or png.exists()): return "skipped"
    tex=ROOT/"textures"/"cars"/(f"TR-{bid}-{buildkey}-01.png")
    if not tex.exists(): raise FileNotFoundError(tex)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    fam=FAMILIES[code]; recipe=RECIPES[code][idx]
    image=bpy.data.images.load(str(tex),check_existing=True)
    printed=bpy.data.materials.new("TR_PRINT_MATERIAL"); printed.use_nodes=True
    p=next(n for n in printed.node_tree.nodes if n.type=="BSDF_PRINCIPLED")
    p.inputs["Roughness"].default_value=.38
    n=printed.node_tree.nodes.new("ShaderNodeTexImage"); n.image=image
    printed.node_tree.links.new(n.outputs["Color"],p.inputs["Base Color"])
    paint=mat("TR_PAINT_BASE",hexrgb(CORE_COLORS[bid]))
    accent=mat("TR_ACCENT_PART",hexrgb(ACCESSORY_COLORS[bid]))
    dark=mat("TR_DARK_PLASTIC",(.065,.08,.11,1),.58)
    mats={"print":printed,"paint":paint,"accent":accent,"dark":dark,
          "rubber":mat("TR_RUBBER",(.045,.047,.056,1),.88),
          "rim":mat("TR_RIM",(.77,.80,.84,1),.32,.15),
          "hub":mat("TR_HUB",(.15,.18,.23,1),.5,.1)}
    shell(body,recipe,mats); wheels(recipe,mats,body); details(body,recipe,mats,custom)
    car_objects=list(bpy.context.scene.objects)
    for ob in car_objects: ob["TinyRidersBodyId"]=bid; ob["PhysicalBuild"]="custom" if custom else "standard"
    blend.parent.mkdir(parents=True,exist_ok=True); bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    bpy.ops.object.select_all(action="DESELECT")
    for ob in car_objects: ob.select_set(True)
    bpy.context.view_layer.objects.active=car_objects[0]
    fbx.parent.mkdir(parents=True,exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,object_types={"MESH"},
                             apply_unit_scale=True,add_leaf_bones=False,path_mode="AUTO")
    if preview: studio_and_render(png,body,recipe)
    return "created"

if __name__=="__main__":
    args=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    p=argparse.ArgumentParser(); p.add_argument("--body-id"); p.add_argument("--body-ids"); p.add_argument("--family"); p.add_argument("--limit",type=int)
    p.add_argument("--build",choices=["standard","custom","both"],default="both")
    p.add_argument("--preview",action="store_true"); p.add_argument("--force",action="store_true")
    p.add_argument("--heroes",action="store_true",help="first catalog body in each family")
    a=p.parse_args(args)
    selected_ids=set(a.body_ids.split(",")) if a.body_ids else None
    selected=[b for b in BODIES if (not a.body_id or b["bodyId"]==a.body_id) and (selected_ids is None or b["bodyId"] in selected_ids) and (not a.family or b["familyCode"]==a.family)]
    if a.heroes: selected=[b for b in selected if b["bodyId"].endswith("-01")]
    if a.limit: selected=selected[:a.limit]
    for b in selected:
        for custom in ([False,True] if a.build=="both" else [a.build=="custom"]):
            try: print("BUILD",b["bodyId"],"custom" if custom else "standard",build(b,custom,a.preview,a.force),flush=True)
            except Exception as exc: print("FAIL",b["bodyId"],str(exc),flush=True)
