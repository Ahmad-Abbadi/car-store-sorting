import bpy,json,math
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[3];O=R/'ArtSource/GoldenReward';S=.35/4.4
for kind in ['Car','Display']:
 bpy.ops.wm.open_mainfile(filepath=str(O/'Blender/GoldenReward_Master.blend'))
 for name in ['TR_GoldenCar_Reward','TR_GoldenReward_Display','TR_GoldenReward_Accessories']:
  keep=(kind=='Car' and name=='TR_GoldenCar_Reward') or (kind=='Display' and name=='TR_GoldenReward_Display')
  if not keep:
   root=bpy.data.objects.get(name)
   for ob in list(root.children_recursive)+[root]:bpy.data.objects.remove(ob,do_unlink=True)
 cam=bpy.context.scene.camera
 if kind=='Car':
  cam.data.type='ORTHO'
  bpy.data.objects['TR_GoldenCar_Reward'].location=(0,0,0);cam.location=Vector((6,-8,5))*S;cam.rotation_euler=(Vector((0,0,.85))*S-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=6.2*S
 bpy.ops.wm.save_as_mainfile(filepath=str(O/'Blender'/('GoldenCar_Reward.blend' if kind=='Car' else 'GoldenDisplay_Empty.blend')))
print('SEPARATE_SOURCES_SAVED')
