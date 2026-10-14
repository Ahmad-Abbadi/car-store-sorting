"""Blender-side mesh/UV/print separation audit of all generated builds."""
import json
from pathlib import Path
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
KIT=ROOT.parent/"Tiny_Riders_Autonomous_Art_Kit"/"Tiny_Riders_500_Car_Production_Kit"
BODIES=json.loads((KIT/"catalog"/"bodies_50.json").read_text(encoding="utf-8"))
results=[]; failed=[]
for body in BODIES:
    for version in ("S","C"):
        key=body["bodyId"]+"-"+version
        path=ROOT/"blender"/"builds"/(key+".blend")
        if not path.exists(): failed.append(key+": missing blend"); continue
        bpy.ops.wm.open_mainfile(filepath=str(path),load_ui=False)
        meshes=[o for o in bpy.context.scene.objects if o.type=="MESH"]
        fonts=[o.name for o in bpy.context.scene.objects if o.type in ("FONT","CURVE")]
        printed=[o for o in meshes if o.name.endswith("_connected_shell") or o.name.endswith("_cabin")]
        uv_issues=[]
        for o in printed:
            uv=o.data.uv_layers.active
            if uv is None: uv_issues.append(o.name+": no UV")
            elif any(not (0<=v.uv.x<=1 and 0<=v.uv.y<=1) for v in uv.data): uv_issues.append(o.name+": out-of-range UV")
            if not any(n.type=="TEX_IMAGE" and n.image for m in o.data.materials if m and m.use_nodes for n in m.node_tree.nodes):
                uv_issues.append(o.name+": no print image")
        wheels=[o for o in meshes if o.name.startswith("Tire")]
        wheel_mins=[]
        for o in wheels:
            coords=[o.matrix_world@Vector(c) for c in o.bound_box]
            wheel_mins.append(min(v.z for v in coords))
        tri=sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons)
        valid=len(meshes)>=6 and len(printed)==2 and not fonts and not uv_issues and len(wheels)>=4 and all(abs(z)<.055 for z in wheel_mins)
        row={"build":key,"meshes":len(meshes),"trianglesBeforeModifiers":tri,"printedMeshes":len(printed),
             "wheelCount":len(wheels),"wheelGroundMin":round(min(wheel_mins),4) if wheel_mins else None,
             "fontObjects":fonts,"uvIssues":uv_issues,"pass":valid}
        results.append(row)
        if not valid: failed.append(key)
package=ROOT/"blender"/"PackagingMaster.blend"
if package.exists():
    bpy.ops.wm.open_mainfile(filepath=str(package),load_ui=False)
    card=bpy.data.objects.get("PackagingCard_120x180mm")
    blister=bpy.data.objects.get("ClearBlister_StandardFootprint")
    package_check={"cardExists":card is not None,"blisterExists":blister is not None,
                   "cardBounds":list(card.dimensions) if card else None,
                   "cardMaterialNames":[m.name if m else None for m in card.data.materials] if card else [],
                   "cardHasUV":bool(card and card.data.uv_layers.active),
                   "fontObjects":[o.name for o in bpy.data.objects if o.type=="FONT"]}
else: package_check={"missing":True}
output={"builds":results,"failedBuilds":failed,"packageMaster":package_check,"pass":not failed and package_check.get("cardExists") and package_check.get("blisterExists")}
target=ROOT/"reports"/"GEOMETRY_AUDIT.json"; target.parent.mkdir(parents=True,exist_ok=True)
target.write_text(json.dumps(output,indent=2),encoding="utf-8")
print("GEOMETRY AUDIT",len(results),"builds, failures",len(failed),"package",package_check,flush=True)
