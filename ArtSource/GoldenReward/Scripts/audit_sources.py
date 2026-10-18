import bpy, os, json, math
from mathutils import Vector
ROOT=os.getcwd()
OUT=os.path.join(ROOT,'ArtSource/GoldenReward')
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene
scene.render.engine='CYCLES'; scene.cycles.samples=16
scene.render.resolution_x=1600; scene.render.resolution_y=1000; scene.render.resolution_percentage=100
mat=bpy.data.materials.new('AuditClay'); mat.diffuse_color=(.3,.42,.52,1); mat.use_nodes=True
next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED').inputs['Base Color'].default_value=(.3,.42,.52,1)
report=[]
for i in range(1,16):
 path=os.path.join(ROOT,f'Assets/ithappy/Modular_Racing_Cars/Meshes/Car_{i:02}/Car_{i:02}.fbx')
 if not os.path.exists(path):continue
 before=set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=path); obs=list(set(bpy.data.objects)-before)
 meshes=[o for o in obs if o.type=='MESH']
 pts=[o.matrix_world@Vector(v) for o in meshes for v in o.bound_box]
 lo=Vector(tuple(min(p[j] for p in pts) for j in range(3))); hi=Vector(tuple(max(p[j] for p in pts) for j in range(3)))
 report.append({'source':path,'size':list(hi-lo),'objects':[{'name':o.name,'vertices':len(o.data.vertices),'polygons':len(o.data.polygons),'materials':[m.name if m else None for m in o.data.materials]} for o in meshes]})
 scale=3.5/max(hi-lo); offset=Vector(((i-1)%5*4.6, (i-1)//5*5.1,0))
 for o in meshes:
  world=o.matrix_world.copy(); o.parent=None; o.matrix_world=world
  for v in o.data.vertices:v.co=scale*(world@v.co-Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z)))+offset
  o.matrix_world.identity(); o.data.materials.clear();o.data.materials.append(mat)
 bpy.ops.object.text_add(location=offset+Vector((-1.5,-2.1,.01)))
 t=bpy.context.object;t.data.body=f'Car {i:02}';t.data.size=.45
json.dump(report,open(OUT+'/source_audit.json','w'),indent=2)
bpy.ops.object.camera_add(location=(18,-22,29)); cam=bpy.context.object;cam.rotation_euler=(Vector((9,5,0))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=25;scene.camera=cam
scene.world.color=(.35,.35,.35)
for loc,power,size in [((5,0,18),3000,12),((14,12,12),2500,10)]:
 bpy.ops.object.light_add(type='AREA',location=loc);l=bpy.context.object;l.data.energy=power;l.data.shape='DISK';l.data.size=size
scene.render.filepath=OUT+'/Renders/supplier_audit.png';bpy.ops.render.render(write_still=True)
