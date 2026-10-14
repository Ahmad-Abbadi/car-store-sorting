import bpy
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
SRC=ROOT/'ArtSource/Shelves/BaseShelf_01'
bpy.ops.wm.open_mainfile(filepath=str(SRC/'TR_Shelf_Base_01.blend'))
root=bpy.data.objects['TR_Shelf_Base_01']
bpy.ops.object.select_all(action='DESELECT')
for o in [root]+list(root.children_recursive):o.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.fbx(filepath=str(ROOT/'Assets/TinyRiders/Art/Shelves/BaseShelf_01/Models/TR_Shelf_Base_01.fbx'),use_selection=True,object_types={'EMPTY','MESH','OTHER'},use_mesh_modifiers=True,add_leaf_bones=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',bake_space_transform=False)
