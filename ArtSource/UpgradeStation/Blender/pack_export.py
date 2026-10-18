import bpy,json
from pathlib import Path
p=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(p/'ToyRacingFactory_Export.blend'))
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(p/'ToyRacingFactory_Export.blend'))
obs=[o for o in bpy.context.scene.objects if o.type=='MESH']
stats={o.name:{'triangles':len(o.data.polygons),'pivot':list(o.location)} for o in obs}
stats['total_triangles']=sum(len(o.data.polygons) for o in obs)
(p/'Export/model-stats.json').write_text(json.dumps(stats,indent=2))
