import bpy,sys,json,math
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[3];O=R/'ArtSource/GoldenReward';S=.35/4.4
if (O/'RENDER_HOLD').exists():
 print('RENDER_DEFERRED: audio Editor verification window',flush=True);sys.exit(0)
bpy.ops.wm.open_mainfile(filepath=str(O/'Blender/GoldenReward_Master.blend'))
sc=bpy.context.scene;cam=sc.camera
sc.render.threads_mode='FIXED';sc.render.threads=8
if 'TR_Clay' not in bpy.data.materials:
 m=bpy.data.materials.new('TR_Clay');m.use_nodes=True;p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Base Color'].default_value=(.34,.38,.43,1);p.inputs['Roughness'].default_value=.55
roots={k:bpy.data.objects[n] for k,n in [('Car','TR_GoldenCar_Reward'),('Display','TR_GoldenReward_Display'),('Accessories','TR_GoldenReward_Accessories')]}
def visible(key,yes):
 for o in [roots[key]]+list(roots[key].children_recursive):o.hide_render=not yes
def shot(name,eye,target,span,car=True,display=False,access=False,clay=False):
 visible('Car',car);visible('Display',display);visible('Accessories',access)
 roots['Car'].location.z=(.68*S if display else 0)
 cam.location=Vector(eye)*S;cam.rotation_euler=(Vector(target)*S-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=span*S
 cam.data.type='PERSP' if name in ['display_empty','display_filled','low_effects'] else 'ORTHO';cam.data.lens=55
 sc.view_layers[0].material_override=bpy.data.materials['TR_Clay'] if clay else None
 bpy.data.objects['Studio floor'].hide_render=name=='detail_light_ring'
 sc.render.filepath=str(O/'Renders'/f'{name}.png');bpy.ops.render.render(write_still=True)
 print('RENDERED',name,flush=True)
views=[('car_hero',(6,-8,5),(0,0,.85),6.2),('car_side',(8,0,2.8),(0,0,.9),5.7),('car_front',(0,-9,2.6),(0,0,.9),3.6),('car_rear',(0,9,2.8),(0,0,1),3.6),('car_top',(0,0,10),(0,0,0),5.6)]
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
mode=args[0] if args else 'initial'
if mode=='initial':
 shot('car_clay',(6,-8,5),(0,0,.85),6.2,clay=True)
 shot(*views[0]);shot('display_empty',(8,-14,3.3),(0,0,2.45),8.5,False,True)
 shot('display_filled',(8,-14,3.3),(0,-.5,2.45),9.8,True,True,True)
else:
 if mode!='final_display':
  for v in views[1:]:shot(*v)
 for name,eye in [('front',(0,-12,4.5)),('side',(12,0,4.5)),('rear',(0,12,4.5))]:shot('display_'+name,eye,(0,0,2.45),7.8,False,True)
 # Remove only canopy/roof in this explicitly labelled inspection image to expose the sweep.
 for o in bpy.data.objects['Canopy'].children_recursive:o.hide_render=True
 # shot() restores visibility; hide canopy via a temporary collection of object names after setup below.
 visible('Car',True);visible('Display',True);visible('Accessories',False);roots['Car'].location.z=.68*S
 hidden=[]
 for group in ['Canopy','CrownPlaques']:
  for o in bpy.data.objects[group].children_recursive:o.hide_render=True;hidden.append(o)
 for o in bpy.data.objects['AccentEmission'].children_recursive:
  if o.matrix_world.translation.z>3*S:o.hide_render=True;hidden.append(o)
 cam.location=Vector((0,-.01,13))*S;cam.rotation_euler=(Vector((0,0,0))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=7.8*S;sc.render.filepath=str(O/'Renders/fit_check_canopy_hidden.png');bpy.ops.render.render(write_still=True)
 for o in hidden:o.hide_render=False
 shot('low_effects',(8,-14,3.3),(0,0,2.45),8.5,True,True)
 for name,eye,target,span,display in [
 ('detail_crown',(0,-5,1.4),(0,-3.2,.52),1.6,True),
 ('detail_checker',(3,-5,1.5),(1.7,-2.6,.42),1.9,True),
 ('detail_wheel',(4,-2,1.2),(1,-1.28,.54),1.5,False),
 ('detail_wing',(4,4,3),(0,1.7,1.7),3.1,False),
 ('detail_cover',(5,-6,4),(1.8,-1,2.1),4.9,True),
 ('detail_light_ring',(2,-4,2.8),(0,0,4.3),5.8,True)]:shot(name,eye,target,span,not display,display)
 # Wire overlay on real geometry, gray override removes printed patterns.
 for o in roots['Car'].children_recursive:
  if o.type=='MESH':
   for i in range(len(o.data.materials)):o.data.materials[i]=bpy.data.materials['TR_Clay']
   for p in o.data.polygons:p.material_index=0
   o.data.materials.append(bpy.data.materials['TR_Charcoal']);w=o.modifiers.new('QA wire overlay','WIREFRAME');w.thickness=.006*S;w.use_replace=False;w.material_offset=len(o.data.materials)-1
 shot('car_wireframe',(6,-8,5),(0,0,.85),6.2)
print('RENDER_BATCH_COMPLETE',flush=True)
