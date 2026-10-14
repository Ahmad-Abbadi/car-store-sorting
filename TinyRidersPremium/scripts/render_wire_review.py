"""Render evaluated mesh edges as temporary QA geometry; never export or save it."""
from pathlib import Path
import sys
import bpy

root=Path(__file__).resolve().parents[1]
mid=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else 'M01'
source=next((root/'Blender/Candidates').glob(mid+'*.blend'))
bpy.ops.wm.open_mainfile(filepath=str(source))
scene=bpy.context.scene;scene.camera=bpy.data.objects['CAM_three_quarter']
scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO'
scene.display.shading.color_type='OBJECT';scene.display.shading.show_shadows=False
scene.display.shading.show_cavity=False
objects=[o for o in scene.objects if o.type=='MESH' and not o.name.startswith('STUDIO')]
deps=bpy.context.evaluated_depsgraph_get()
for ob in objects:
    ob.color=(.67,.71,.77,1)
    data=bpy.data.meshes.new_from_object(ob.evaluated_get(deps))
    wire=bpy.data.objects.new('QA_ONLY_WIRE_'+ob.name,data);scene.collection.objects.link(wire)
    wire.matrix_world=ob.matrix_world.copy();wire.color=(.018,.032,.055,1)
    modifier=wire.modifiers.new('QA evaluated edge display','WIREFRAME')
    modifier.thickness=.000045;modifier.use_replace=True;modifier.use_even_offset=False
scene.render.filepath=str(root/'Previews'/mid/'wireframe.png')
bpy.ops.render.render(write_still=True)
print('QA wireframe saved; source blend and exports unchanged')
