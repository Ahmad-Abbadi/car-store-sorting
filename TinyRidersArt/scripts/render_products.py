"""Render exact assembled car geometry/livery for each catalog product."""
import argparse
import json
import sys
from pathlib import Path
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
KIT=ROOT.parent/"Tiny_Riders_Autonomous_Art_Kit"/"Tiny_Riders_500_Car_Production_Kit"
PRODUCTS=json.loads((KIT/"catalog"/"collectibles_500.json").read_text(encoding="utf-8"))

def setup_studio():
    scene=bpy.context.scene
    world=bpy.data.worlds.new("Product studio"); scene.world=world
    world.use_nodes=True; world.node_tree.nodes["Background"].inputs["Color"].default_value=(.75,.82,.93,1)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value=.9
    for pos,power,size in [((-3,-4,6),850,4),((2,3,5),700,3),((0,-1,6),400,4)]:
        ld=bpy.data.lights.new("softbox","AREA"); ld.energy=power; ld.shape="DISK"; ld.size=size
        ob=bpy.data.objects.new("softbox",ld); scene.collection.objects.link(ob); ob.location=pos
        ob.rotation_euler=(Vector((0,0,.7))-ob.location).to_track_quat("-Z","Y").to_euler()
    camd=bpy.data.cameras.new("Product camera"); cam=bpy.data.objects.new("Product camera",camd)
    scene.collection.objects.link(cam); cam.location=(-5.6,-6.9,4.4)
    cam.rotation_euler=(Vector((0,0,.82))-cam.location).to_track_quat("-Z","Y").to_euler()
    camd.type="ORTHO"; camd.ortho_scale=5.2; scene.camera=cam
    scene.render.engine="BLENDER_EEVEE"; scene.render.resolution_x=640; scene.render.resolution_y=400
    scene.view_settings.view_transform="Standard"
    scene.view_settings.exposure=-1.2
    scene.render.resolution_percentage=100; scene.render.film_transparent=True
    scene.render.image_settings.file_format="PNG"; scene.render.image_settings.color_mode="RGBA"

def render_build(products,force=False,export=False):
    first=products[0]; bid=first["bodyId"]; b="C" if first["buildId"]=="custom" else "S"
    blend=ROOT/"blender"/"builds"/(bid+"-"+b+".blend")
    if not blend.exists(): raise FileNotFoundError(blend)
    bpy.ops.wm.open_mainfile(filepath=str(blend),load_ui=False)
    setup_studio()
    material=bpy.data.materials.get("TR_PRINT_MATERIAL")
    if material is None: raise ValueError(f"Missing UV print material in {blend}")
    node=next(n for n in material.node_tree.nodes if n.type=="TEX_IMAGE")
    count=0
    for p in products:
        target=ROOT/"previews"/"products"/(p["sku"]+".png")
        blend_out=ROOT/"blender"/"products"/(p["sku"]+".blend")
        fbx_out=ROOT/"exports"/"products"/(p["sku"]+".fbx")
        if target.exists() and not force and (not export or (blend_out.exists() and fbx_out.exists())): continue
        tex=ROOT/"textures"/"cars"/(p["sku"]+".png")
        if not tex.exists(): raise FileNotFoundError(tex)
        node.image=bpy.data.images.load(str(tex),check_existing=True)
        if export and (force or not blend_out.exists() or not fbx_out.exists()):
            blend_out.parent.mkdir(parents=True,exist_ok=True); fbx_out.parent.mkdir(parents=True,exist_ok=True)
            bpy.ops.wm.save_as_mainfile(filepath=str(blend_out))
            bpy.ops.object.select_all(action="DESELECT")
            for ob in bpy.context.scene.objects:
                if ob.type=="MESH" and ob.name!="STUDIO floor": ob.select_set(True)
            bpy.ops.export_scene.fbx(filepath=str(fbx_out),use_selection=True,object_types={"MESH"},path_mode="AUTO")
        if force or not target.exists():
            target.parent.mkdir(parents=True,exist_ok=True)
            bpy.context.scene.render.filepath=str(target)
            bpy.ops.render.render(write_still=True)
        count+=1
    return count

if __name__=="__main__":
    a=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    ap=argparse.ArgumentParser(); ap.add_argument("--body-id"); ap.add_argument("--body-ids"); ap.add_argument("--family"); ap.add_argument("--limit-builds",type=int); ap.add_argument("--force",action="store_true"); ap.add_argument("--export",action="store_true")
    args=ap.parse_args(a)
    selected_ids=set(args.body_ids.split(",")) if args.body_ids else None
    selected=[p for p in PRODUCTS if (not args.body_id or p["bodyId"]==args.body_id) and (selected_ids is None or p["bodyId"] in selected_ids) and (not args.family or p["familyCode"]==args.family)]
    groups={}
    for p in selected: groups.setdefault((p["bodyId"],p["buildId"]),[]).append(p)
    if args.limit_builds: groups=dict(list(groups.items())[:args.limit_builds])
    for key,ps in groups.items():
        try: print("PRODUCT RENDERS",key,render_build(ps,args.force,args.export),flush=True)
        except Exception as e: print("FAIL",key,repr(e),flush=True)
