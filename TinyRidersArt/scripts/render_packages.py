"""Render assembled 3D blister products using one physical packaging master."""
import argparse
import json
import sys
from pathlib import Path
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
KIT=ROOT.parent/"Tiny_Riders_Autonomous_Art_Kit"/"Tiny_Riders_500_Car_Production_Kit"
PRODUCTS=json.loads((KIT/"catalog"/"collectibles_500.json").read_text(encoding="utf-8"))

def setup(build):
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/"blender"/"PackagingMaster.blend"),load_ui=False)
    before=set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(ROOT/"exports"/"builds"/(build+".fbx")))
    imported=[o for o in bpy.data.objects if o not in before]
    root=bpy.data.objects.new("PRODUCT_CAR_ROOT",None); bpy.context.scene.collection.objects.link(root)
    for ob in imported:
        if ob.parent not in imported:
            matrix=ob.matrix_world.copy(); ob.parent=root; ob.matrix_world=matrix
    root.scale=(.020,.020,.020); root.location=(0,-.022,.030)
    world=bpy.data.worlds.new("package studio"); bpy.context.scene.world=world; world.use_nodes=True
    world.node_tree.nodes["Background"].inputs["Color"].default_value=(.80,.85,.93,1)
    for pos,power,size in [((-.10,-.15,.25),.65,.16),((.10,-.05,.20),.45,.14)]:
        ld=bpy.data.lights.new("package softbox","AREA"); ld.energy=power; ld.shape="DISK"; ld.size=size
        ob=bpy.data.objects.new("package softbox",ld); bpy.context.scene.collection.objects.link(ob); ob.location=pos
        ob.rotation_euler=(Vector((0,0,.09))-ob.location).to_track_quat("-Z","Y").to_euler()
    camd=bpy.data.cameras.new("package camera"); cam=bpy.data.objects.new("package camera",camd)
    bpy.context.scene.collection.objects.link(cam); cam.location=(.005,-.35,.093)
    cam.rotation_euler=(Vector((0,0,.09))-cam.location).to_track_quat("-Z","Y").to_euler()
    camd.type="ORTHO"; camd.ortho_scale=.21
    scene=bpy.context.scene; scene.camera=cam; scene.render.engine="BLENDER_EEVEE"
    scene.view_settings.view_transform="Standard"
    scene.render.resolution_x=480; scene.render.resolution_y=720; scene.render.resolution_percentage=100
    scene.render.image_settings.file_format="PNG"
    return next(n for n in bpy.data.materials["TR_PRINT_MATERIAL"].node_tree.nodes if n.type=="TEX_IMAGE"), \
           next(n for n in bpy.data.materials["TR_CARD_FRONT"].node_tree.nodes if n.type=="TEX_IMAGE"), \
           next(n for n in bpy.data.materials["TR_CARD_BACK"].node_tree.nodes if n.type=="TEX_IMAGE")

def render_group(ps,force=False,export=False):
    p=ps[0]; build=p["bodyId"]+("-C" if p["buildId"]=="custom" else "-S")
    if not force and all((ROOT/"previews"/"packages"/(v["sku"]+".png")).exists() and
                         (not export or ((ROOT/"blender"/"packages"/(v["sku"]+".blend")).exists() and
                                         (ROOT/"exports"/"packages"/(v["sku"]+".fbx")).exists())) for v in ps): return 0
    car,front,back=setup(build)
    count=0
    for v in ps:
        out=ROOT/"previews"/"packages"/(v["sku"]+".png")
        blend=ROOT/"blender"/"packages"/(v["sku"]+".blend")
        fbx=ROOT/"exports"/"packages"/(v["sku"]+".fbx")
        if out.exists() and not force and (not export or (blend.exists() and fbx.exists())): continue
        car.image=bpy.data.images.load(str(ROOT/"textures"/"cars"/(v["sku"]+".png")),check_existing=True)
        front.image=bpy.data.images.load(str(ROOT/"textures"/"packages"/"front"/(v["sku"]+".png")),check_existing=True)
        back.image=bpy.data.images.load(str(ROOT/"textures"/"packages"/"back"/(v["sku"]+".png")),check_existing=True)
        if export and (force or not blend.exists() or not fbx.exists()):
            blend.parent.mkdir(parents=True,exist_ok=True); fbx.parent.mkdir(parents=True,exist_ok=True)
            bpy.ops.wm.save_as_mainfile(filepath=str(blend))
            bpy.ops.object.select_all(action="DESELECT")
            for ob in bpy.context.scene.objects:
                if ob.type=="MESH" or ob.name=="PRODUCT_CAR_ROOT": ob.select_set(True)
            bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,
                                     object_types={"MESH","EMPTY"},path_mode="AUTO")
        if force or not out.exists():
            out.parent.mkdir(parents=True,exist_ok=True)
            bpy.context.scene.render.filepath=str(out); bpy.ops.render.render(write_still=True)
        count+=1
    return count

if __name__=="__main__":
    args=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    ap=argparse.ArgumentParser(); ap.add_argument("--body-id"); ap.add_argument("--body-ids"); ap.add_argument("--family"); ap.add_argument("--limit-builds",type=int); ap.add_argument("--force",action="store_true"); ap.add_argument("--export",action="store_true")
    a=ap.parse_args(args)
    selected_ids=set(a.body_ids.split(",")) if a.body_ids else None
    selected=[p for p in PRODUCTS if (not a.body_id or p["bodyId"]==a.body_id) and (selected_ids is None or p["bodyId"] in selected_ids) and (not a.family or p["familyCode"]==a.family)]
    groups={}
    for p in selected: groups.setdefault((p["bodyId"],p["buildId"]),[]).append(p)
    if a.limit_builds: groups=dict(list(groups.items())[:a.limit_builds])
    for key,ps in groups.items():
        try: print("PACKAGE RENDERS",key,render_group(ps,a.force,a.export),flush=True)
        except Exception as e: print("PACKAGE FAIL",key,repr(e),flush=True)
