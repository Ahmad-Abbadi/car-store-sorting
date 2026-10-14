"""Render A1 orthographic beauty, clay, wireframe and silhouette evidence."""
from pathlib import Path
import bpy
from mathutils import Vector

ROOT = Path(r"C:\Users\User\Documents\Games_Projects\My_Games_Projects\Car Store Sorting\TinyRidersArt")
OUT = ROOT / "renders" / "phase_A1"
OUT.mkdir(parents=True, exist_ok=True)
scene = bpy.context.scene
camera = bpy.data.objects["A1_Orthographic_Camera"]
floor = bpy.data.objects["A1_Ground"]
car = bpy.data.collections["TC-01_BlueRacer_CAR"]
layer = scene.view_layers[0]
scene.camera = camera
scene.render.resolution_x = 1200
scene.render.resolution_y = 900
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.image_settings.color_mode = "RGB"
scene.view_settings.view_transform = "Standard"
scene.view_settings.exposure = -0.20


def pose(location, target=(0, 0, 0.81), scale=4.7):
    camera.location = location
    camera.rotation_euler = (Vector(target) - camera.location).to_track_quat("-Z", "Y").to_euler()
    camera.data.ortho_scale = scale


def render(name):
    scene.render.filepath = str(OUT / name)
    bpy.ops.render.render(write_still=True)
    print("RENDER_OK", scene.render.filepath)


views = [
    ("TC-01_front_three_quarter.png", (-4.4, -5.5, 3.3), 4.7),
    ("TC-01_side.png", (0.0, -7.0, 1.6), 4.0),
    ("TC-01_right_side.png", (0.0, 7.0, 1.6), 4.0),
    ("TC-01_rear_three_quarter.png", (4.4, -5.5, 3.3), 4.7),
    ("TC-01_top.png", (-0.001, -0.001, 7.0), 4.25),
    ("TC-01_front.png", (-7.0, 0.0, 1.35), 3.35),
]

scene.render.engine = "BLENDER_EEVEE"
layer.material_override = None
floor.hide_render = False
for name, location, scale in views:
    pose(location, scale=scale)
    render(name)

clay = bpy.data.materials.get("A1_Clay_Override") or bpy.data.materials.new("A1_Clay_Override")
clay.use_nodes = True
bsdf = next(n for n in clay.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
bsdf.inputs["Base Color"].default_value = (0.39, 0.43, 0.49, 1)
bsdf.inputs["Roughness"].default_value = 0.65
layer.material_override = clay
floor.hide_render = True
pose((-4.4, -5.5, 3.3), scale=4.7)
render("TC-01_clay.png")
layer.material_override = None

def emission(name, color):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    emit = nodes.new("ShaderNodeEmission")
    emit.inputs["Color"].default_value = color
    emit.inputs["Strength"].default_value = 1.0
    mat.node_tree.links.new(emit.outputs["Emission"], output.inputs["Surface"])
    return mat


black = emission("A1_Black_Validation", (0.012, 0.016, 0.026, 1))
world_bs = next(n for n in scene.world.node_tree.nodes if n.type == "BACKGROUND")
world_bs.inputs["Color"].default_value = (1, 1, 1, 1)
world_bs.inputs["Strength"].default_value = 1.0
layer.material_override = black
render("TC-01_silhouette.png")
layer.material_override = None

print("A1_RENDERS_COMPLETE", len(list(OUT.glob("TC-01_*.png"))))
