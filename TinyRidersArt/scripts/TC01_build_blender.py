"""Create the editable Tiny Riders TC-01 Blue Racer A1 pilot in live Blender."""
from pathlib import Path
from math import cos, sin, pi
import bpy
from mathutils import Vector

ROOT = Path(r"C:\Users\User\Documents\Games_Projects\My_Games_Projects\Car Store Sorting\TinyRidersArt")
BLEND = ROOT / "blender" / "TC-01_BlueRacer_A1.blend"
FBX = ROOT / "exports" / "cars" / "TC-01_BlueRacer_A1.fbx"
TEX = ROOT / "textures" / "cars" / "TC-01" / "TC-01_BlueRacer_23_Albedo.png"
SIZE = 2048
REGIONS = {
    "BODY_TOP": (32, 32, 1000, 448),
    "BODY_LEFT": (32, 512, 1000, 840),
    "BODY_RIGHT": (32, 900, 1000, 1228),
    "BODY_FRONT": (1050, 32, 1520, 448),
    "BODY_REAR": (1560, 32, 2030, 448),
    "CABIN_FRONT": (1050, 512, 1520, 840),
    "CABIN_REAR": (1560, 512, 2030, 840),
    "CABIN_LEFT": (1050, 900, 1520, 1228),
    "CABIN_RIGHT": (1560, 900, 2030, 1228),
    "CABIN_ROOF": (32, 1300, 1000, 1720),
}

for item in list(bpy.data.objects):
    bpy.data.objects.remove(item, do_unlink=True)
for collection in list(bpy.data.collections):
    bpy.data.collections.remove(collection)
bpy.data.orphans_purge(do_recursive=True)

scene = bpy.context.scene
car = bpy.data.collections.new("TC-01_BlueRacer_CAR")
scene.collection.children.link(car)
studio = bpy.data.collections.new("A1_Review_Studio")
scene.collection.children.link(studio)


def rgba(rgb):
    return tuple(v / 255.0 for v in rgb) + (1.0,)


def linear_rgba(rgb):
    srgb = [v / 255.0 for v in rgb]
    return tuple(c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4 for c in srgb) + (1.0,)


def material(name, rgb, roughness=0.42, metallic=0.0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = next(n for n in mat.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    bsdf.inputs["Base Color"].default_value = linear_rgba(rgb)
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Metallic"].default_value = metallic
    mat.diffuse_color = rgba(rgb)
    return mat


blue = material("TR_Plastic_Cobalt", (26, 95, 216), 0.34)
blue_dark = material("TR_Plastic_DeepBlue", (11, 61, 159), 0.43)
tire_mat = material("TR_Rubber_Charcoal", (30, 31, 38), 0.85)
rim_mat = material("TR_Rim_Ivory", (236, 238, 228), 0.35)
hub_mat = material("TR_Hub_Dark", (49, 52, 64), 0.55, 0.08)
bumper_mat = material("TR_Bumper_Ink", (27, 35, 49), 0.52)
floor_mat = material("A1_Studio_Floor", (228, 235, 244), 0.78)

print_mat = bpy.data.materials.new("TR_TC01_UV_PrintedPaint")
print_mat.use_nodes = True
bsdf = next(n for n in print_mat.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
image_node = print_mat.node_tree.nodes.new("ShaderNodeTexImage")
image_node.name = "TC01_Flat_Albedo_Paint"
image_node.image = bpy.data.images.load(str(TEX), check_existing=True)
image_node.image.filepath = str(TEX)
image_node.interpolation = "Linear"
print_mat.node_tree.links.new(image_node.outputs["Color"], bsdf.inputs["Base Color"])
bsdf.inputs["Roughness"].default_value = 0.36
print_mat.diffuse_color = rgba((26, 95, 216))


def to_uv(px, py):
    return px / SIZE, 1.0 - py / SIZE


def face_uv(region, a, b):
    x0, y0, x1, y1 = REGIONS[region]
    return to_uv(x0 + (x1 - x0) * a, y0 + (y1 - y0) * b)


def make_uv_mesh(name, vertices, face_data, default_material=print_mat):
    """face_data: (vertex indexes, UVs, material index)."""
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(vertices, [], [f[0] for f in face_data])
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    car.objects.link(obj)
    mesh.materials.append(default_material)
    mesh.materials.append(blue)
    uv = mesh.uv_layers.new(name="TC01_Paint_UV")
    for poly, (_, coords, index) in zip(mesh.polygons, face_data):
        poly.material_index = index
        for loop_index, coord in zip(poly.loop_indices, coords):
            uv.data[loop_index].uv = coord
    return obj


def add_bevel(obj, width=0.035):
    bevel = obj.modifiers.new("One_Plane_Toy_Bevel", "BEVEL")
    bevel.width = width
    bevel.segments = 1
    bevel.affect = "EDGES"
    normal = obj.modifiers.new("Weighted_Plane_Normals", "WEIGHTED_NORMAL")
    normal.keep_sharp = True


# Five connected sections give the nose and rear a subtle, deliberate taper.
sections = [
    (-1.51, 0.72, 1.00),
    (-1.34, 0.82, 1.12),
    (-0.39, 0.84, 1.14),
    (0.88, 0.84, 1.12),
    (1.42, 0.76, 1.01),
]
bottom = 0.54
verts = []
for x, halfwidth, top in sections:
    verts.extend([(x, -halfwidth, bottom), (x, halfwidth, bottom),
                  (x, halfwidth, top), (x, -halfwidth, top)])


def body_top_uv(index):
    x, y, _ = verts[index]
    return face_uv("BODY_TOP", (x + 1.51) / 2.93, (y + 0.84) / 1.68)


def body_side_uv(index, side):
    x, _, z = verts[index]
    across = (x + 1.51) / 2.93 if side == "BODY_LEFT" else (1.42 - x) / 2.93
    return face_uv(side, across, (1.14 - z) / 0.60)


def body_end_uv(index, end):
    _, y, z = verts[index]
    across = (0.84 - y) / 1.68 if end == "BODY_FRONT" else (y + 0.84) / 1.68
    return face_uv(end, across, (1.14 - z) / 0.60)


faces = []
for i in range(len(sections) - 1):
    a, b = 4 * i, 4 * (i + 1)
    for ids, region, func in [
        ([a+3, b+3, b+2, a+2], "BODY_TOP", body_top_uv),
        ([a+0, b+0, b+3, a+3], "BODY_LEFT", lambda k: body_side_uv(k, "BODY_LEFT")),
        ([a+1, a+2, b+2, b+1], "BODY_RIGHT", lambda k: body_side_uv(k, "BODY_RIGHT")),
    ]:
        faces.append((ids, [func(k) for k in ids], 0))
    ids = [a+0, a+1, b+1, b+0]
    faces.append((ids, [to_uv(0, 0)] * 4, 1))
for ids, region in [([0, 3, 2, 1], "BODY_FRONT"),
                    ([16, 17, 18, 19], "BODY_REAR")]:
    faces.append((ids, [body_end_uv(k, region) for k in ids], 0))
body = make_uv_mesh("TC01_Body_ConnectedShell", verts, faces)
add_bevel(body, 0.045)

# A separate connected wedge is real cabin geometry; dark windows are its UV print.
cabin_verts = [
    (-0.50, -0.67, 1.13), (-0.50, 0.67, 1.13),
    (0.87, 0.67, 1.13), (0.87, -0.67, 1.13),
    (-0.20, -0.57, 1.63), (-0.20, 0.57, 1.63),
    (0.59, 0.57, 1.63), (0.59, -0.57, 1.63),
]


def cabin_uv(index, region):
    x, y, z = cabin_verts[index]
    if region == "CABIN_ROOF":
        return face_uv(region, (x + 0.20) / 0.79, (y + 0.57) / 1.14)
    if region in ("CABIN_LEFT", "CABIN_RIGHT"):
        a = (x + 0.50) / 1.37 if region == "CABIN_LEFT" else (0.87 - x) / 1.37
        return face_uv(region, a, (1.63 - z) / 0.50)
    a = (0.67 - y) / 1.34 if region == "CABIN_FRONT" else (y + 0.67) / 1.34
    return face_uv(region, a, (1.63 - z) / 0.50)


cabin_faces = []
for ids, region in [
    ([0, 1, 5, 4], "CABIN_FRONT"),
    ([3, 7, 6, 2], "CABIN_REAR"),
    ([0, 4, 7, 3], "CABIN_LEFT"),
    ([1, 2, 6, 5], "CABIN_RIGHT"),
    ([4, 5, 6, 7], "CABIN_ROOF"),
]:
    cabin_faces.append((ids, [cabin_uv(k, region) for k in ids], 0))
cabin_faces.append(([0, 3, 2, 1], [to_uv(0, 0)] * 4, 1))
cabin = make_uv_mesh("TC01_Cabin_PrintOnFacets", cabin_verts, cabin_faces)
add_bevel(cabin, 0.024)


def move_to(obj, collection):
    for old in list(obj.users_collection):
        old.objects.unlink(obj)
    collection.objects.link(obj)


def box(name, location, dimensions, mat, bevel=0.0, group=car):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    move_to(obj, group)
    obj.data.materials.append(mat)
    if bevel:
        add_bevel(obj, bevel)
    return obj


# Shallow physical parts; their visual paint stays in the atlas.
box("TC01_Underbody", (0.0, 0.0, 0.53), (2.79, 1.39, 0.18), bumper_mat, 0.05)
box("TC01_Front_Bumper", (-1.55, 0.0, 0.54), (0.16, 1.37, 0.20), bumper_mat, 0.045)
box("TC01_Rear_Bumper", (1.47, 0.0, 0.56), (0.16, 1.36, 0.18), bumper_mat, 0.035)
box("TC01_Front_Splitter", (-1.50, 0.0, 0.42), (0.30, 1.58, 0.07), blue_dark, 0.025)
for side in (-1, 1):
    box("TC01_Physical_Sideskirt_" + str(side),
        (0.0, side * 0.81, 0.58), (1.48, 0.10, 0.10), blue_dark, 0.02)

# Chunky wing with two supports: no lettering or printed stripe geometry.
for side in (-1, 1):
    box("TC01_Wing_Support_" + str(side),
        (1.10, side * 0.55, 1.28), (0.24, 0.16, 0.40), blue_dark, 0.025)
box("TC01_Rear_Wing", (1.10, 0.0, 1.54), (0.39, 1.95, 0.17), blue, 0.045)
for side in (-1, 1):
    box("TC01_Wing_Endplate_" + str(side),
        (1.10, side * 0.92, 1.52), (0.44, 0.095, 0.24), blue_dark, 0.025)


def wheel_cylinder(name, cx, cy, cz, radius, depth, mat):
    vertices = []
    for yy in (cy - depth / 2, cy + depth / 2):
        for i in range(12):
            a = 2 * pi * i / 12
            vertices.append((cx + radius * cos(a), yy, cz + radius * sin(a)))
    side_faces = []
    for i in range(12):
        j = (i + 1) % 12
        side_faces.append((i, j, 12 + j, 12 + i))
    side_faces.extend([tuple(reversed(range(12))), tuple(range(12, 24))])
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(vertices, [], side_faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    car.objects.link(obj)
    mesh.materials.append(mat)
    add_bevel(obj, 0.035)
    return obj


def annulus(name, cx, cy, cz, inner, outer, mat):
    vertices = []
    for radius in (inner, outer):
        for i in range(12):
            a = 2 * pi * i / 12
            vertices.append((cx + radius * cos(a), cy, cz + radius * sin(a)))
    faces = [(i, (i+1)%12, 12+(i+1)%12, 12+i) for i in range(12)]
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    car.objects.link(obj)
    mesh.materials.append(mat)
    return obj


def fender_brow(name, cx, side):
    """Faceted, physically thick wheel arch attached to the side body."""
    vertices = []
    for yy in (side * 0.80, side * 0.91):
        for radius in (0.46, 0.55):
            for i in range(9):
                angle = pi * i / 8
                vertices.append((cx + radius * cos(angle), yy,
                                 0.45 + radius * sin(angle)))
    faces = []
    for i in range(8):
        faces.append((i, i+1, 9+i+1, 9+i))
        faces.append((18+i, 27+i, 27+i+1, 18+i+1))
        faces.append((9+i, 9+i+1, 27+i+1, 27+i))
        faces.append((i, 18+i, 18+i+1, i+1))
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    car.objects.link(obj)
    mesh.materials.append(blue_dark)
    return obj


for x_name, x in (("Front", -1.01), ("Rear", 0.98)):
    for side in (-1, 1):
        y = side * 0.88
        wheel_cylinder(f"TC01_{x_name}_Tire_{side}", x, y, 0.45, 0.45, 0.31, tire_mat)
        outward = side * 1.051
        annulus(f"TC01_{x_name}_WhiteRim_{side}", x, outward, 0.45, 0.255, 0.33, rim_mat)
        wheel_cylinder(f"TC01_{x_name}_Hub_{side}", x, side * 1.054, 0.45, 0.23, 0.025, hub_mat)
        fender_brow(f"TC01_{x_name}_Arch_{side}", x, side)

# Studio collection is never selected for export.
box("A1_Ground", (0, 0, -0.055), (200, 200, 0.10), floor_mat, 0.0, studio)

world = bpy.data.worlds.new("A1_SoftSky") if not bpy.data.worlds else bpy.data.worlds[0]
scene.world = world
world.use_nodes = True
world_bs = next(n for n in world.node_tree.nodes if n.type == "BACKGROUND")
world_bs.inputs["Color"].default_value = (0.78, 0.86, 1.0, 1.0)
world_bs.inputs["Strength"].default_value = 0.45


def light(name, location, power, size):
    data = bpy.data.lights.new(name, "AREA")
    obj = bpy.data.objects.new(name, data)
    studio.objects.link(obj)
    obj.location = location
    obj.rotation_euler = (Vector((0, 0, 0.7)) - obj.location).to_track_quat("-Z", "Y").to_euler()
    data.energy = power
    data.shape = "DISK"
    data.size = size


light("A1_Key", (-3.5, -4.0, 6.0), 650, 5.0)
light("A1_Fill", (2.6, 3.6, 4.3), 390, 4.5)
light("A1_Rim", (3.5, -2.1, 5.0), 280, 3.0)

camera_data = bpy.data.cameras.new("A1_Orthographic_Camera")
camera = bpy.data.objects.new("A1_Orthographic_Camera", camera_data)
studio.objects.link(camera)
scene.camera = camera
camera_data.type = "ORTHO"
camera_data.ortho_scale = 4.7
camera.location = (-4.4, -5.5, 3.3)
camera.rotation_euler = (Vector((0, 0, 0.78)) - camera.location).to_track_quat("-Z", "Y").to_euler()

scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 1200
scene.render.resolution_y = 900
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False
scene.view_settings.view_transform = "Standard"
scene.view_settings.exposure = -0.20
scene.render.filepath = str(ROOT / "renders" / "phase_A1" / "TC-01_front_three_quarter.png")
scene.unit_settings.system = "METRIC"
scene.unit_settings.scale_length = 1.0

for obj in bpy.context.selected_objects:
    obj.select_set(False)
for obj in car.objects:
    obj.select_set(True)
bpy.context.view_layer.objects.active = body

scene["tiny_riders_body_id"] = "TC-01"
scene["reference"] = "02_track_club.png Blue Racer #23"
scene["stage"] = "A1 pilot; awaiting visual approval"

bpy.ops.wm.save_as_mainfile(filepath=str(BLEND))
bpy.ops.export_scene.fbx(
    filepath=str(FBX), use_selection=True, apply_unit_scale=True,
    apply_scale_options="FBX_SCALE_ALL", path_mode="COPY", embed_textures=True,
    add_leaf_bones=False,
)

mesh_objects = [o for o in car.objects if o.type == "MESH"]
faces = sum(len(o.data.polygons) for o in mesh_objects)
triangles = sum(sum(max(1, len(p.vertices) - 2) for p in o.data.polygons) for o in mesh_objects)
print("A1_BUILD_OK", "objects", len(mesh_objects), "faces", faces,
      "triangles_before_modifiers", triangles, "blend", BLEND, "fbx", FBX)
