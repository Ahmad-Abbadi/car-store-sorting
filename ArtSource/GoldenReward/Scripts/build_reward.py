import bpy, bmesh, math, os, json, sys
from mathutils import Vector
from pathlib import Path
R=Path(__file__).resolve().parents[3]; O=R/'ArtSource/GoldenReward'; A=R/'Assets/TinyRiders/Art/GoldenReward'
for d in ['Models','Textures','Materials','Prefabs','PreviewScenes']:(A/d).mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
S=.35/4.4
scene=bpy.context.scene
scene.unit_settings.system='METRIC'
M={}
def material(n,col,metal=0,rough=.35,emission=0):
 m=bpy.data.materials.new('TR_'+n);m.use_nodes=True;m.diffuse_color=(*col,1)
 p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Base Color'].default_value=(*col,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
 if emission:p.inputs['Emission Color'].default_value=(*col,1);p.inputs['Emission Strength'].default_value=emission
 M[n]=m;return m
material('Gold',(.83,.44,.065),.88,.23);material('GoldEdge',(1,.66,.20),.83,.19)
material('Charcoal',(.012,.017,.023),.25,.29);material('Rubber',(.017,.019,.023),0,.6)
material('Window',(.009,.015,.025),.12,.24);material('WarmLight',(1,.67,.23),.1,.24,2)
material('RedLight',(.75,.008,.004),.1,.24,1.7);material('Fabric',(.32,.009,.022),0,.88)
material('Checker',(.85,.85,.85),.12,.34);material('Grille',(.04,.04,.04),.15,.4)
for key,file in [('Checker','TR_Checker_BaseColor.png'),('Grille','TR_Grille_BaseColor.png')]:
 m=M[key];p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');t=m.node_tree.nodes.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(A/'Textures'/file));m.node_tree.links.new(t.outputs['Color'],p.inputs['Base Color'])
glass=material('Cover',(.88,.95,1),0,.12);p=next(n for n in glass.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Transmission Weight'].default_value=1;p.inputs['IOR'].default_value=1.08;p.inputs['Alpha'].default_value=.12
nt=glass.node_tree;transparent=nt.nodes.new('ShaderNodeBsdfTransparent');mix=nt.nodes.new('ShaderNodeMixShader');mix.inputs[0].default_value=.025
nt.links.new(transparent.outputs[0],mix.inputs[1]);nt.links.new(p.outputs[0],mix.inputs[2]);nt.links.new(mix.outputs[0],next(n for n in nt.nodes if n.type=='OUTPUT_MATERIAL').inputs['Surface'])
material('Clay',(.34,.38,.43),0,.55)
roots={}; groups={}
def empty(n,parent=None):
 o=bpy.data.objects.new(n,None);scene.collection.objects.link(o);o.parent=parent;return o
for name in ['Car','Display','Accessories']:roots[name]=empty('TR_Golden'+('Car_Reward' if name=='Car' else 'Reward_'+name))
for n in ['Body','Cabin','Trim','Wing']:groups[n]=empty(n,roots['Car'])
geo=empty('Geometry',roots['Display'])
for n in ['Base','Columns','Canopy','CrownPlaques','SpotlightHousings']:groups[n]=empty(n,geo)
groups['TurntablePivot']=empty('TurntablePivot',roots['Display']);groups['GlassCover']=empty('GlassCover',roots['Display']);groups['AccentEmission']=empty('AccentEmission',roots['Display'])
anchor=empty('Anchor_Car',groups['TurntablePivot']);anchor.location=(0,0,.68)
def mesh(n,verts,faces,mat,parent,bevel=0,uv=None):
 if mat=='Grille' and uv is None:uv=[(x/.8,z/.6) for x,y,z in verts]
 me=bpy.data.meshes.new(n);me.from_pydata(verts,[],faces);me.update();o=bpy.data.objects.new(n,me);scene.collection.objects.link(o);o.parent=parent;me.materials.append(M[mat])
 if uv:
  layer=me.uv_layers.new(name='UVMap')
  for poly in me.polygons:
   for idx in poly.loop_indices:layer.data[idx].uv=uv[me.loops[idx].vertex_index]
 if bevel:
  mod=o.modifiers.new('Soft manufactured edges','BEVEL');mod.width=bevel;mod.segments=3
  mod=o.modifiers.new('Weighted surface normals','WEIGHTED_NORMAL');mod.keep_sharp=True
 return o
def cube(n,loc,dim,mat,parent,bev=.03):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=n;o.dimensions=dim;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.parent=parent;o.data.materials.append(M[mat])
 if mat=='Grille':
  uv=o.data.uv_layers.active
  for poly in o.data.polygons:
   for li in poly.loop_indices:
    co=o.data.vertices[o.data.loops[li].vertex_index].co;uv.data[li].uv=(co.x/.8,co.z/.6)
 if bev:
  b=o.modifiers.new('Rounded edges','BEVEL');b.width=bev;b.segments=3;o.modifiers.new('Weighted normals','WEIGHTED_NORMAL')
 return o
def cylinder(n,loc,r,depth,mat,parent,axis='Z',verts=48,bev=.015):
 bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=r,depth=depth,location=loc);o=bpy.context.object;o.name=n;o.parent=parent;o.data.materials.append(M[mat])
 if axis=='X':o.rotation_euler.y=math.pi/2
 if axis=='Y':o.rotation_euler.x=math.pi/2
 if bev:b=o.modifiers.new('Edge roll','BEVEL');b.width=bev;b.segments=2;o.modifiers.new('Weighted normals','WEIGHTED_NORMAL')
 for p in o.data.polygons:p.use_smooth=len(p.vertices)==4
 return o
def torus(n,loc,r,t,mat,parent,axis='Z',seg=64):
 bpy.ops.mesh.primitive_torus_add(major_segments=seg,minor_segments=8,location=loc,major_radius=r,minor_radius=t);o=bpy.context.object;o.name=n;o.parent=parent;o.data.materials.append(M[mat])
 if axis=='X':o.rotation_euler.y=math.pi/2
 if axis=='Y':o.rotation_euler.x=math.pi/2
 for p in o.data.polygons:p.use_smooth=True
 return o
def prism(n,profile,depth,loc,mat,parent,axis='Y',bev=.015):
 # Profile coordinates are horizontal and vertical; thickness axis is Y or X.
 vs=[]
 for d in [-depth/2,depth/2]:
  for x,z in profile:vs.append((x,d,z) if axis=='Y' else (d,x,z))
 N=len(profile);fs=[tuple(reversed(range(N))),tuple(range(N,2*N))]+[(i,(i+1)%N,(i+1)%N+N,i+N) for i in range(N)]
 o=mesh(n,vs,fs,mat,parent,bev);o.location=loc;return o
def band(n,r,z,h,mat,parent,repeats=24,thick=.05):
 vs=[];uv=[];fs=[];N=96
 for rad,zz in [(r,z),(r,z+h),(r-thick,z),(r-thick,z+h)]:
  for i in range(N+1):
   a=2*math.pi*i/N;vs.append((rad*math.sin(a),-rad*math.cos(a),zz));uv.append((i/N*repeats,.0 if zz==z else 1))
 for row1,row2 in [(0,1),(1,3),(3,2),(2,0)]:
  for i in range(N):a=row1*(N+1)+i;b=row2*(N+1)+i;fs.append((a,a+1,b+1,b))
 o=mesh(n,vs,fs,mat,parent,0,uv)
 for p in o.data.polygons:p.use_smooth=True
 return o
profile=json.loads((O/'Artwork/Crown_Profile.json').read_text())
crown_mesh=None
def crown(n,loc,size,parent,axis='Y',sign=-1):
 global crown_mesh
 o=prism(n,profile,.075,(0,0,0),'GoldEdge',parent,bev=.015) if crown_mesh is None else bpy.data.objects.new(n,crown_mesh)
 if crown_mesh is None:crown_mesh=o.data
 else:scene.collection.objects.link(o);o.parent=parent;b=o.modifiers.new('Relief bevel','BEVEL');b.width=.015;b.segments=2;o.modifiers.new('Relief normals','WEIGHTED_NORMAL')
 o.location=loc;o.scale=(size,size,size)
 if axis=='X':o.rotation_euler.z=math.pi/2
 for x,z in [(-.6,.64),(0,1),(.6,.64)]:
  pos=Vector((x,0,z))*size
  if axis=='X':pos=Vector((0,x,z))*size
  pos+=Vector(loc)
  bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=6,radius=.055*size,location=pos);s=bpy.context.object;s.name=n+'_Tip';s.parent=parent;s.data.materials.append(M['GoldEdge'])
 return o
# CAR: a solid narrow central shell with real clearance outboard for all four tires.
stations=[(-2.12,.78,.38,.88),(-1.82,.82,.34,1.03),(-.62,.82,.32,1.23),(.55,.82,.32,1.16),(1.55,.83,.34,1.06),(2.05,.78,.40,.98)]
vs=[]
for y,w,b,t in stations:vs.extend([(-w,y,b),(w,y,b),(w,y,t-.1),(.8*w,y,t),(.43,y,t),(-.43,y,t),(-.8*w,y,t),(-w,y,t-.1)])
fs=[tuple(reversed(range(8))),tuple(range((len(stations)-1)*8,len(stations)*8))]
for i in range(len(stations)-1):
 for j in range(8):fs.append((i*8+j,i*8+(j+1)%8,(i+1)*8+(j+1)%8,(i+1)*8+j))
body=mesh('Sculpted central monocoque',vs,fs,'Gold',groups['Body'],.035,[(x/.18,y/.18) for x,y,z in vs]);body.data.materials.append(M['Checker'])
for i in [0,1]:
 for j in [3,5]:body.data.polygons[2+i*8+j].material_index=1
cube('Undertray',(0,0,.29),(1.55,3.88,.14),'Charcoal',groups['Trim'],.05)
# Crowned arch cross-sections: the opening is empty, not a black disc.
for side in [-1,1]:
 for yy,label in [(-1.28,'Front'),(1.30,'Rear')]:
  vs=[];uv=[];N=32
  for x,rad in [(side*.77,.605),(side*.90,.695),(side*1.12,.675),(side*1.18,.59),(side*1.10,.575)]:
   for i in range(N+1):a=-.17+(math.pi+.34)*i/N;vs.append((x,yy+rad*math.cos(a),.54+rad*math.sin(a)));uv.append((i/N,x))
  fs=[]
  for j in range(4):
   for i in range(N):a=j*(N+1)+i;fs.append((a,a+1,a+N+2,a+N+1))
  fs.extend([tuple(j*(N+1) for j in reversed(range(5))),tuple(j*(N+1)+N for j in range(5))])
  f=mesh(f'{label} flared arch {side}',vs,fs,'Gold',groups['Body'],.012,uv)
  for p in f.data.polygons:p.use_smooth=True
 # Door slab with sloping upper edge and structural lower rocker, integrated printed surface.
 door=prism('Sculpted door '+str(side),[(-.64,.51),(.63,.51),(.66,1.10),(-.57,1.22)],.15,(side*.86,0,0),'Gold',groups['Body'],axis='X',bev=.045)
 skirt=cube('Checker rocker '+str(side),(side*.91,.0,.40),(.16,1.27,.23),'Checker',groups['Trim'],.018)
 # Assign physical rocker faces planar coordinates, square checks on the outside.
 uv=skirt.data.uv_layers.active
 for poly in skirt.data.polygons:
  for li in poly.loop_indices:
   v=skirt.data.vertices[skirt.data.loops[li].vertex_index].co;uv.data[li].uv=(v.y/.23,v.z/.23)
 cube('Gold sill '+str(side),(side*.98,0,.27),(.21,1.48,.10),'GoldEdge',groups['Trim'],.025)
 intake=prism('Side intake '+str(side),[(.34,.60),(.58,.64),(.77,1.04),(.51,1.04)],.035,(side*.968,0,0),'Charcoal',groups['Trim'],axis='X',bev=.025)
 cube('Door pull '+str(side),(side*.96,.34,1.08),(.035,.22,.045),'GoldEdge',groups['Trim'],.015)
# Cabin solid gold enclosure; windows are inset structural face sections.
cab_vs=[(-.82,-.73,1.13),(.82,-.73,1.13),(.78,1.10,1.08),(-.78,1.10,1.08),(-.65,-.22,1.76),(.65,-.22,1.76),(.64,.69,1.73),(-.64,.69,1.73)]
mesh('Cabin frame',cab_vs,[(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7),(3,2,1,0)],'Gold',groups['Cabin'],.055)
mesh('Windshield',[(-.705,-.714,1.22),(.705,-.714,1.22),(.59,-.257,1.705),(-.59,-.257,1.705)],[(0,1,2,3)],'Window',groups['Cabin'],0)
mesh('Rear glass',[(-.59,.744,1.67),(.59,.744,1.67),(.69,1.10,1.18),(-.69,1.10,1.18)],[(0,1,2,3)],'Window',groups['Cabin'],0)
for s in [-1,1]:
 mesh('Side glazing '+str(s),[(s*.803,-.61,1.22),(s*.67,-.18,1.68),(s*.665,.57,1.66),(s*.77,.90,1.20)],[(0,1,2,3)],'Window',groups['Cabin'])
 pillar=cube('Rear quarter pillar '+str(s),(s*.735,.59,1.43),(.065,.065,.51),'Gold',groups['Cabin'],.012);pillar.rotation_euler.x=.29
 cube('Mirror stem '+str(s),(s*.94,-.51,1.22),(.26,.095,.085),'Charcoal',groups['Trim'],.02)
 cube('Mirror '+str(s),(s*1.055,-.55,1.29),(.28,.27,.16),'Gold',groups['Trim'],.035)
 cube('Mirror glass '+str(s),(s*1.06,-.402,1.29),(.22,.018,.11),'Window',groups['Trim'],.018)
# Hood scoop follows the hood slope and joins the body; the dark mouth is a mounted insert.
prism('Hood scoop housing',[(-1.57,1.065),(-.89,1.18),(-.89,1.38),(-1.57,1.38)],.72,(0,0,0),'Gold',groups['Body'],axis='X',bev=.035)
cube('Hood scoop mouth',(0,-1.592,1.28),(.58,.025,.145),'Grille',groups['Trim'],.022)
cube('Roof intake housing',(0,.11,1.80),(.55,.54,.18),'Gold',groups['Body'],.045)
cube('Roof intake opening',(0,-.175,1.82),(.43,.025,.10),'Charcoal',groups['Trim'],.02)
# Front bumper, upper intake and lights.
prism('Front valance',[(-1.08,.27),(1.08,.27),(1.04,.70),(.89,.82),(-.89,.82),(-1.04,.70)],.26,(0,-2.02,0),'Gold',groups['Body'],bev=.055)
cube('Front grille',(0,-2.163,.78),(.94,.035,.32),'Grille',groups['Trim'],.025)
prism('Lower front intake',[(-.62,.31),(.62,.31),(.46,.58),(-.46,.58)],.05,(0,-2.17,0),'Grille',groups['Trim'],bev=.025)
cube('Front splitter',(0,-2.08,.23),(2.22,.24,.10),'Charcoal',groups['Trim'],.025)
for s in [-1,1]:
 cube('Headlight bezel '+str(s),(s*.79,-2.10,.88),(.42,.13,.22),'GoldEdge',groups['Trim'],.025)
 cube('Headlight '+str(s),(s*.79,-2.175,.89),(.31,.025,.135),'WarmLight',groups['Trim'],.016)
 cube('Fog light socket '+str(s),(s*.82,-2.175,.50),(.31,.026,.20),'Charcoal',groups['Trim'],.022)
 cube('Fog light '+str(s),(s*.82,-2.196,.50),(.21,.015,.11),'WarmLight',groups['Trim'],.014)
crown('Grille crown',(0,-2.205,.65),.22,groups['Trim'])
# Rear wing: broad structural blade and two supports, end plates have relief and texture on actual faces.
for s in [-1,1]:
 p=prism('Wing upright '+str(s),[(1.32,1.02),(1.52,1.02),(1.78,1.76),(1.58,1.76)],.115,(s*.65,0,0),'Charcoal',groups['Wing'],axis='X',bev=.02)
cube('Wing blade',(0,1.70,1.80),(2.19,.49,.12),'Gold',groups['Wing'],.035)
cube('Wing inner face',(0,1.70,1.866),(1.99,.36,.025),'Charcoal',groups['Wing'],.009)
for s in [-1,1]:
 prism('Wing endplate '+str(s),[(1.39,1.65),(2.08,1.65),(2.08,2.0),(1.45,1.97)],.095,(s*1.13,0,0),'GoldEdge',groups['Wing'],axis='X',bev=.027)
 pp=[(1.44,1.70),(1.64,1.70),(2.02,1.70),(2.02,1.95),(1.64,1.935),(1.49,1.92)]
 vs=[(x,y,z) for x in [-.0085,.0085] for y,z in pp]
 fs=[(5,4,1,0),(4,3,2,1),(6,7,10,11),(7,8,9,10)]+[(i,(i+1)%6,(i+1)%6+6,i+6) for i in range(6)]
 o=mesh('Wing endplate inset '+str(s),vs,fs,'Charcoal',groups['Wing'],.008,[(y/.12,z/.12) for x,y,z in vs]);o.location.x=s*1.185;o.data.materials.append(M['Checker'])
 for i in [0,2]:o.data.polygons[i].material_index=1
 crown('Wing crown '+str(s),(s*1.202,1.83,1.73),.16,groups['Wing'],axis='X')
cube('Rear fascia',(0,2.035,.86),(1.81,.12,.39),'Charcoal',groups['Trim'],.045)
cube('Rear diffuser',(0,2.03,.36),(1.84,.20,.22),'Charcoal',groups['Trim'],.035)
for s in [-1,1]:
 for x in [.57,.82]:
  cylinder('Rear lamp lens',(s*x,2.11,.91),.105,.025,'RedLight',groups['Trim'],'Y',32,.006)
  torus('Rear lamp rim',(s*x,2.13,.91),.083,.012,'GoldEdge',groups['Trim'],'Y',32)
 for x in [.58,.82]:
  cylinder('Exhaust shadow',(s*x,2.135,.40),.091,.09,'Charcoal',groups['Trim'],'Y',32,.005)
  torus('Exhaust lip',(s*x,2.185,.40),.089,.016,'GoldEdge',groups['Trim'],'Y',32)
crown('Rear crown',(0,2.12,.77),.17,groups['Trim'])
# Wheels with proper local axle pivots.
for s in [-1,1]:
 for yy,label in [(-1.28,'Front'),(1.30,'Rear')]:
  pivot=empty('Wheel_'+label+('_L' if s<0 else '_R'),roots['Car']);pivot.location=(s*1.00,yy,.54)
  cylinder('Tire',(0,0,0),.54,.32,'Rubber',pivot,'X',48,.07)
  cylinder('Rim barrel',(s*.172,0,0),.418,.028,'Charcoal',pivot,'X',48,.012)
  torus('Gold rim',(s*.19,0,0),.403,.026,'GoldEdge',pivot,'X',48)
  for j in range(5):
   angle=j*2*math.pi/5
   spoke=cube('Broad gold spoke',(s*.193,math.sin(angle)*.245,math.cos(angle)*.245),(.042,.10,.29),'Gold',pivot,.016);spoke.rotation_euler.x=-angle
  cylinder('Wheel hub',(s*.222,0,0),.143,.045,'Charcoal',pivot,'X',32,.008)
  crown('Hub crown',(s*.25,0,-.075),.14,pivot,axis='X')
  torus('Tire shoulder',(s*.145,0,0),.492,.008,'Charcoal',pivot,'X',48)
# DISPLAY
cylinder('Foot',(0,0,.075),3.40,.15,'Charcoal',groups['Base'],verts=96,bev=.04)
cylinder('Lower gold step',(0,0,.17),3.34,.10,'GoldEdge',groups['Base'],verts=96,bev=.025)
cylinder('Fascia core',(0,0,.36),3.18,.34,'Charcoal',groups['Base'],verts=96,bev=.03)
band('Printed checker fascia',3.192,.25,.24,'Checker',groups['Base'],repeats=84)
cylinder('Upper gold lip',(0,0,.56),3.27,.13,'Gold',groups['Base'],verts=96,bev=.026)
cylinder('Recessed deck',(0,0,.625),2.98,.045,'Charcoal',groups['Base'],verts=96,bev=.015)
cylinder('Platter',(0,0,.65),2.58,.06,'Charcoal',groups['TurntablePivot'],verts=96,bev=.012)
torus('Platter gold perimeter',(0,0,.68),2.55,.025,'GoldEdge',groups['TurntablePivot'])
for r,z in [(3.28,.19),(3.23,.59),(2.7,.66)]:torus('Platform light ring',(0,0,z),r,.014,'WarmLight',groups['AccentEmission'],seg=96)
for s in [-1,1]:
 cylinder('Column foot',(s*3.03,0,.72),.31,.24,'Gold',groups['Columns'],verts=8,bev=.035)
 cylinder('Column lower collar',(s*3.03,0,.95),.255,.31,'Charcoal',groups['Columns'],verts=8,bev=.035)
 cylinder('Gold column',(s*3.03,0,2.62),.14,3.20,'Gold',groups['Columns'],verts=12,bev=.02)
 cube('Column warm inset '+str(s),(s*3.03,-.143,2.62),(.046,.01,2.96),'WarmLight',groups['AccentEmission'],.005)
 cylinder('Column upper collar',(s*3.03,0,4.20),.26,.24,'Charcoal',groups['Columns'],verts=8,bev=.03)
 cylinder('Column upper cap',(s*3.03,0,4.34),.29,.10,'GoldEdge',groups['Columns'],verts=8,bev=.025)
cylinder('Canopy underside',(0,0,4.42),3.19,.13,'Gold',groups['Canopy'],verts=96,bev=.028)
band('Canopy fascia',3.17,4.48,.43,'Charcoal',groups['Canopy'],thick=.22)
band('Canopy printed band',3.179,4.55,.28,'Checker',groups['Canopy'],repeats=72,thick=.03)
for z in [4.49,4.91]:torus('Canopy gold piping',(0,0,z),3.20,.055,'GoldEdge',groups['Canopy'],seg=96)
cylinder('Canopy top',(0,0,4.94),3.20,.08,'Gold',groups['Canopy'],verts=96,bev=.02)
cylinder('Central downlight socket',(0,0,4.335),.96,.08,'Charcoal',groups['Canopy'],verts=64)
cylinder('Central diffuser',(0,0,4.285),.81,.025,'WarmLight',groups['AccentEmission'],verts=64,bev=.01)
for r in [1.02,2.54,2.86]:torus('Ceiling light ring',(0,0,4.34),r,.025,'WarmLight',groups['AccentEmission'],seg=96)
for i in range(8):
 a=2*math.pi*i/8;cylinder('Recessed downlight',(2.18*math.cos(a),2.18*math.sin(a),4.335),.13,.025,'WarmLight',groups['AccentEmission'],verts=24,bev=.008)
# Shield profiles front and back; main raised crown only on front.
shield=[(-.84,-.27),(.84,-.27),(1.0,.35),(0,.67),(-1.0,.35)]
for z,label,sz in [(.36,'Base',.75),(4.80,'Canopy',.85)]:
 o=prism(label+' shield rim',shield,.15,(0,-3.18,z),'GoldEdge',groups['CrownPlaques'],bev=.045);o.scale=(sz,sz,sz)
 o=prism(label+' shield inset',[(x*.87,y*.83) for x,y in shield],.04,(0,-3.28,z),'Charcoal',groups['CrownPlaques'],bev=.025);o.scale=(sz,sz,sz)
 crown(label+' crown',(0,-3.325,z-.10),.76*sz,groups['CrownPlaques'])
band('Transparent protective cylinder',2.72,.70,3.55,'Cover',groups['GlassCover'],repeats=1,thick=.012)
for z in [.70,4.25]:torus('Cover rim',(0,0,z),2.72,.012,'GoldEdge',groups['GlassCover'],seg=96)
for s in [-1,1]:
 cube('Spot foot',(s*2.89,-.69,.72),(.33,.36,.12),'Charcoal',groups['SpotlightHousings'],.03)
 spot=cube('Spot housing',(s*2.89,-.69,.95),(.32,.36,.36),'Charcoal',groups['SpotlightHousings'],.06);spot.rotation_euler.y=s*.30
 spot=cube('Spot gold bezel',(s*2.84,-.70,1.12),(.26,.28,.07),'Gold',groups['SpotlightHousings'],.035);spot.rotation_euler.y=s*.30
 spot=cube('Spot lens',(s*2.825,-.70,1.16),(.18,.20,.025),'WarmLight',groups['AccentEmission'],.025);spot.rotation_euler.y=s*.30
# OPTIONAL ACCESSORIES, physical restrained rope curves.
cube('Carpet runner',(0,-3.30,.015),(2.55,3.6,.025),'Fabric',roots['Accessories'],.015)
for s in [-1,1]:
 for yy in [-2.3,-4.3]:
  cylinder('Stanchion foot',(s*2.8,yy,.06),.21,.10,'Gold',roots['Accessories'],verts=32)
  cylinder('Stanchion stem',(s*2.8,yy,.68),.055,1.2,'Gold',roots['Accessories'],verts=16)
  torus('Stanchion collar',(s*2.8,yy,1.21),.08,.017,'GoldEdge',roots['Accessories'],seg=24)
  bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=10,radius=.105,location=(s*2.8,yy,1.33));o=bpy.context.object;o.name='Stanchion finial';o.parent=roots['Accessories'];o.data.materials.append(M['GoldEdge'])
 cu=bpy.data.curves.new('Velvet rope','CURVE');cu.dimensions='3D';cu.resolution_u=12;cu.bevel_depth=.048;cu.bevel_resolution=3
 sp=cu.splines.new('BEZIER');sp.bezier_points.add(2)
 for bp,co in zip(sp.bezier_points,[(s*2.8,-2.3,1.23),(s*2.8,-3.3,.85),(s*2.8,-4.3,1.23)]):bp.co=co;bp.handle_left_type='AUTO';bp.handle_right_type='AUTO'
 ob=bpy.data.objects.new('Red velvet rope',cu);scene.collection.objects.link(ob);ob.parent=roots['Accessories'];cu.materials.append(M['Fabric'])
# Apply modifiers and unwrap real geometry before final metric scale.
for o in list(bpy.data.objects):
 if o.type not in {'MESH','CURVE'}:continue
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
 if o.type=='CURVE':bpy.ops.object.convert(target='MESH')
 for mod in list(o.modifiers):
  try:bpy.ops.object.modifier_apply(modifier=mod.name)
  except Exception:pass
 if not o.data.uv_layers:
  bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.025);bpy.ops.object.mode_set(mode='OBJECT')
 # Correct winding on closed solids, retain the authored thin glass/window surfaces.
 if len(o.data.polygons)>2:
  bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(o.data);bm.free()
# Scale both vertex data and hierarchy translations, leaving all transforms at unit scale.
for o in list(bpy.data.objects):
 if o.type=='MESH':
  # Data shared between crown instances must be scaled only once later.
  pass
seen=set()
for o in list(bpy.data.objects):
 o.location*=S
 if o.type=='MESH' and o.data.name not in seen:
  for v in o.data.vertices:v.co*=S
  seen.add(o.data.name)
bpy.context.view_layer.update()
# Export each independent root. Blender -Y forward converts to Unity +Z via FBX.
def descendants(root):return [root]+list(root.children_recursive)
for key,root in roots.items():
 bpy.ops.object.select_all(action='DESELECT')
 for ob in descendants(root):ob.select_set(True)
 bpy.context.view_layer.objects.active=root
 bpy.ops.export_scene.fbx(filepath=str(A/'Models'/(root.name+'.fbx')),use_selection=True,object_types={'EMPTY','MESH'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False,use_mesh_modifiers=True)
# Counts and exact radial sweep based on complete evaluated mesh including wing/mirrors.
report={}
for key,root in roots.items():
 obs=[o for o in descendants(root) if o.type=='MESH'];points=[o.matrix_world@v.co for o in obs for v in o.data.vertices]
 for o in obs:o.data.calc_loop_triangles()
 report[key]={'triangles':sum(len(o.data.loop_triangles) for o in obs),'mesh_objects':len(obs),'materials':sorted(set(m.name for o in obs for m in o.data.materials)),'min':[min(p[i] for p in points) for i in range(3)],'max':[max(p[i] for p in points) for i in range(3)],'missing_uv':[o.name for o in obs if not o.data.uv_layers]}
 if key=='Car':report['radial_sweep']=max(math.hypot(p.x,p.y) for p in points)
report['cover_inner_radius']=(2.72-.012)*S;report['platter_radius']=2.58*S;report['car_anchor_z']=.68*S
report['radial_clearance']=report['cover_inner_radius']-report['radial_sweep'];report['platter_margin']=report['platter_radius']-report['radial_sweep']
(O/'geometry_audit.json').write_text(json.dumps(report,indent=2))
# Neutral studio setup: area sources and real metallic reflections, no volume/particles/bloom.
floor=material('Studio',(.055,.065,.085),.12,.46)
studio=empty('Preview_Studio')
cube('Studio floor',(0,0,-.008),(200*S,200*S,.01),'Studio',studio,.0)
scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
scene.render.resolution_x=1200;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
scene.world.color=(.22,.22,.22)
for n,loc,power,size in [('Key',(-5,-6,9),1700,6),('Rim',(4,4,7),2200,5),('Fill',(5,-2,4),950,5)]:
 bpy.ops.object.light_add(type='AREA',location=Vector(loc)*S);l=bpy.context.object;l.name=n;l.parent=studio;l.data.energy=power*S*S;l.data.shape='DISK';l.data.size=size*S;l.rotation_euler=(Vector((0,0,1.7))*S-l.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add();cam=bpy.context.object;cam.name='PreviewCamera';cam.parent=studio;scene.camera=cam;cam.data.type='ORTHO';cam.data.lens=50;cam.data.clip_start=.001
roots['Car'].location.z=.68*S
cam.location=Vector((8,-14,3.3))*S;cam.rotation_euler=(Vector((0,0,2.45))*S-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='PERSP';cam.data.lens=55;cam.data.ortho_scale=8.5*S
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':area.spaces.active.region_3d.view_perspective='CAMERA';area.spaces.active.clip_start=.001
try:scene.view_settings.view_transform='AgX'
except:pass
for img in bpy.data.images:
 if img.source=='FILE':img.pack()
bpy.ops.wm.save_as_mainfile(filepath=str(O/'Blender/GoldenReward_Master.blend'))
(O/'PRODUCTION_STATE.json').write_text(json.dumps({'phase':'modeled_uv_exported','next':'render and inspect; Unity import when editor available','source':'Blender/GoldenReward_Master.blend'},indent=2))
print('GOLDEN_REWARD_BUILD_COMPLETE',json.dumps(report))
