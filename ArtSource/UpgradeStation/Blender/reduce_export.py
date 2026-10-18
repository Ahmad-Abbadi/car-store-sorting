import bpy
from pathlib import Path
p=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(p/'ToyRacingFactory_Export.blend'))
for o in bpy.context.scene.objects:
    if o.type=='MESH':
        bpy.context.view_layer.objects.active=o
        mod=o.modifiers.new('Mobile export reduction','DECIMATE');mod.ratio=.65;mod.use_collapse_triangulate=True
        bpy.ops.object.modifier_apply(modifier=mod.name)
bpy.ops.wm.save_as_mainfile(filepath=str(p/'ToyRacingFactory_Export.blend'))
bpy.ops.export_scene.fbx(filepath=str(p/'Export/ToyRacingFactory.fbx'),use_selection=True,object_types={'MESH'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',bake_space_transform=True,add_leaf_bones=False,path_mode='STRIP')
print('REDUCED_TRIANGLES',sum(len(o.data.polygons) for o in bpy.context.scene.objects if o.type=='MESH'))
