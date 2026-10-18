import bpy
from mathutils import Matrix
from pathlib import Path
p=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(p/'ToyRacingFactory_Export.blend'))
for o in bpy.context.scene.objects:
    if o.type=='MESH':
        o.data.transform(Matrix.Diagonal((1,-1,1,1)));o.data.flip_normals();o.location.y=-o.location.y
bpy.ops.wm.save_as_mainfile(filepath=str(p/'ToyRacingFactory_Export.blend'))
bpy.ops.export_scene.fbx(filepath=str(p/'Export/ToyRacingFactory.fbx'),use_selection=True,object_types={'MESH'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',bake_space_transform=True,add_leaf_bones=False,path_mode='STRIP')
