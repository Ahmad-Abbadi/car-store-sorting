"""Render neutral material overrides to expose accidental modeled print."""
from pathlib import Path
import bpy

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/"previews"/"clay"; OUT.mkdir(parents=True,exist_ok=True)
for code in ("CC","TC","RR","DP","BW","RC","WY","SS","FF","CR"):
    key=f"TR-{code}-01-S-01"
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/"blender"/"products"/(key+".blend")),load_ui=False)
    mat=bpy.data.materials.new("QA_NEUTRAL_CLAY"); mat.diffuse_color=(.59,.62,.67,1)
    mat.use_nodes=True
    p=next(n for n in mat.node_tree.nodes if n.type=="BSDF_PRINCIPLED")
    p.inputs["Base Color"].default_value=(.59,.62,.67,1)
    bpy.context.scene.view_layers[0].material_override=mat
    bpy.context.scene.render.filepath=str(OUT/(key+"_clay.png"))
    bpy.ops.render.render(write_still=True)
    print("CLAY",key,flush=True)
key="TR-TC-01-S-01"
bpy.ops.wm.open_mainfile(filepath=str(ROOT/"blender"/"packages"/(key+".blend")),load_ui=False)
mat=bpy.data.materials.new("QA_NEUTRAL_CLAY"); mat.diffuse_color=(.59,.62,.67,1); mat.use_nodes=True
p=next(n for n in mat.node_tree.nodes if n.type=="BSDF_PRINCIPLED")
p.inputs["Base Color"].default_value=(.59,.62,.67,1)
bpy.context.scene.view_layers[0].material_override=mat
bpy.context.scene.render.filepath=str(OUT/(key+"_package_clay.png"))
bpy.ops.render.render(write_still=True)
print("CLAY PACKAGE",key,flush=True)
