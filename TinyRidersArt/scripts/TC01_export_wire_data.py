"""Export projected edges of the evaluated Blender meshes for honest wire QA."""
from pathlib import Path
import json
import bpy
from bpy_extras.object_utils import world_to_camera_view
from mathutils import Vector

ROOT = Path(r"C:\Users\User\Documents\Games_Projects\My_Games_Projects\Car Store Sorting\TinyRidersArt")
OUT = ROOT / "qa" / "phase_A1" / "TC-01_projected_wire_edges.json"
scene = bpy.context.scene
camera = bpy.data.objects["A1_Orthographic_Camera"]
camera.location = (-4.4, -5.5, 3.3)
camera.rotation_euler = (Vector((0, 0, 0.81)) - camera.location).to_track_quat("-Z", "Y").to_euler()
camera.data.ortho_scale = 4.7
scene.camera = camera
scene.render.resolution_x = 1200
scene.render.resolution_y = 900
bpy.context.view_layer.update()
depsgraph = bpy.context.evaluated_depsgraph_get()

edges = []
objects = 0
for obj in bpy.data.collections["TC-01_BlueRacer_CAR"].objects:
    if obj.type != "MESH":
        continue
    objects += 1
    evaluated = obj.evaluated_get(depsgraph)
    mesh = evaluated.to_mesh()
    try:
        for edge in mesh.edges:
            points = []
            for index in edge.vertices:
                point = world_to_camera_view(scene, camera, evaluated.matrix_world @ mesh.vertices[index].co)
                points.append([round(point.x * 1200, 2), round((1-point.y) * 900, 2), round(point.z, 4)])
            edges.append([points[0], points[1]])
    finally:
        evaluated.to_mesh_clear()

OUT.parent.mkdir(parents=True, exist_ok=True)
OUT.write_text(json.dumps({"width": 1200, "height": 900,
                           "evaluated_mesh_objects": objects,
                           "projected_edges": edges}, separators=(",", ":")), encoding="utf-8")
print("WIRE_DATA_OK", objects, len(edges), OUT)
