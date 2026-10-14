"""Validate the A1 Blender master and round-trip its FBX in Blender."""

import json
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[1]
BLEND = ROOT / "blender" / "TC-01_BlueRacer_A1.blend"
FBX = ROOT / "exports" / "cars" / "TC-01_BlueRacer_A1.fbx"
ATLAS = ROOT / "textures" / "cars" / "TC-01" / "TC-01_BlueRacer_23_Albedo.png"
OUTPUT = ROOT / "qa" / "phase_A1" / "TC-01_asset_validation.json"


def bounds(objects):
    points = [obj.matrix_world @ Vector(corner) for obj in objects for corner in obj.bound_box]
    return {
        "min": [round(min(point[i] for point in points), 4) for i in range(3)],
        "max": [round(max(point[i] for point in points), 4) for i in range(3)],
    }


def mesh_metrics(objects):
    return {
        "mesh_objects": len(objects),
        "polygons": sum(len(obj.data.polygons) for obj in objects),
        "triangles_before_modifiers": sum(
            sum(len(poly.vertices) - 2 for poly in obj.data.polygons)
            for obj in objects
        ),
        "bounds": bounds(objects),
    }


bpy.ops.wm.open_mainfile(filepath=str(BLEND))
car = bpy.data.collections.get("TC-01_BlueRacer_CAR")
assert car is not None, "Car collection missing"
car_meshes = [obj for obj in car.objects if obj.type == "MESH"]
all_car_objects = list(car.objects)
printed = []
uv_issues = []
uv_values = []
for obj in car_meshes:
    uses_print = any(
        mat and mat.name == "TR_TC01_UV_PrintedPaint"
        for mat in obj.data.materials
    )
    if not uses_print:
        continue
    printed.append(obj.name)
    uv_layer = obj.data.uv_layers.active
    if uv_layer is None:
        uv_issues.append(f"{obj.name}: no active UV layer")
        continue
    for loop in uv_layer.data:
        uv_values.extend((loop.uv.x, loop.uv.y))
    if any(value < -0.0001 or value > 1.0001 for loop in uv_layer.data for value in loop.uv):
        uv_issues.append(f"{obj.name}: UV outside atlas")

print_mat = bpy.data.materials.get("TR_TC01_UV_PrintedPaint")
images = [node.image for node in print_mat.node_tree.nodes if node.type == "TEX_IMAGE"] if print_mat else []
source = {
    **mesh_metrics(car_meshes),
    "non_mesh_car_objects": [obj.name for obj in all_car_objects if obj.type != "MESH"],
    "font_objects_in_file": [obj.name for obj in bpy.data.objects if obj.type == "FONT"],
    "printed_meshes": printed,
    "uv_issues": uv_issues,
    "uv_coordinate_range": [round(min(uv_values), 6), round(max(uv_values), 6)] if uv_values else None,
    "print_material_images": [image.name for image in images if image],
    "atlas_exists": ATLAS.is_file(),
    "blend_bytes": BLEND.stat().st_size,
    "fbx_bytes": FBX.stat().st_size,
}

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(FBX))
fbx_meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
fbx_materials = [mat for mat in bpy.data.materials if mat and mat.use_nodes]
fbx_images = [
    {"name": image.name, "size": list(image.size), "packed": bool(image.packed_file)}
    for image in bpy.data.images if image.source == "FILE"
]
roundtrip = {
    **mesh_metrics(fbx_meshes),
    "materials": [mat.name for mat in fbx_materials],
    "images": fbx_images,
}

checks = {
    "source_mesh_present": source["mesh_objects"] > 0,
    "source_geometry_only": not source["non_mesh_car_objects"] and not source["font_objects_in_file"],
    "printed_parts_have_uvs": bool(printed) and not uv_issues,
    "source_uvs_in_atlas": bool(uv_values) and min(uv_values) >= -0.0001 and max(uv_values) <= 1.0001,
    "source_texture_linked": bool(images) and all(image for image in images) and source["atlas_exists"],
    "fbx_imports": roundtrip["mesh_objects"] > 0,
    "fbx_mesh_count_matches": roundtrip["mesh_objects"] == source["mesh_objects"],
    "fbx_embedded_texture_present": any(image["packed"] and all(image["size"]) for image in fbx_images),
}
result = {"source": source, "fbx_roundtrip": roundtrip, "checks": checks, "passed": all(checks.values())}
OUTPUT.parent.mkdir(parents=True, exist_ok=True)
OUTPUT.write_text(json.dumps(result, indent=2), encoding="utf-8")
print("A1_VALIDATION", json.dumps(checks, sort_keys=True), "PASSED", result["passed"])
if not result["passed"]:
    raise SystemExit(1)
