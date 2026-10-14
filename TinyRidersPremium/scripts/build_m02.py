"""M02 Parcel: cab-forward delivery foundation with a raised, shaped cargo shell.

Independent silhouette recipe; shares mesh operations and review studio with M01/M03.
The short sloped cab and taller cargo module provide the later microbus conversion sockets.
"""
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import bpy
from candidate_tools import geo,ROOT,finish_candidate

def body_uv(ob,front_height=1.45):
    uv=ob.data.uv_layers.new(name='PrintUV')
    for p in ob.data.polygons:
        n=p.normal
        zone='front' if n.x<-.7 else 'back' if n.x>.7 else 'left' if n.y<-.6 else 'right' if n.y>.6 else 'top'
        for li in p.loop_indices:
            x,y,z=ob.data.vertices[ob.data.loops[li].vertex_index].co
            if zone=='top':u=(x+1.8)/3.65*1024;v=(1.02-y)/2.04*768
            elif zone in ('left','right'):
                a=(x+1.8)/3.65
                if zone=='right':a=1-a
                u=(1024 if zone=='right' else 0)+a*1024;v=800+(2.7-z)/2.45*500
            elif zone=='front':u=(1.02-y)/2.04*1024;v=1350+(front_height-z)/(front_height-.25)*650
            else:u=1024+(y+1.02)/2.04*1024;v=1350+(2.7-z)/2.45*650
            uv.data[li].uv=(u/2048,1-v/2048)

def cab(mat):
    verts=[(-1.72,-.87,1.23),(-1.72,.87,1.23),(-.20,.93,1.23),(-.20,-.93,1.23),
           (-1.38,-.79,2.48),(-1.38,.79,2.48),(-.20,.86,2.52),(-.20,-.86,2.52)]
    faces=[(0,1,5,4),(3,0,4,7),(1,2,6,5),(2,3,7,6),(4,5,6,7),(3,2,1,0)]
    ob=geo.mesh('CABIN_ForwardControl',verts,faces,mat);uv=ob.data.uv_layers.new(name='WindowPrintUV')
    front={0:(0,1),1:(1,1),5:(1,0),4:(0,0)}
    for p in ob.data.polygons:
        for li in p.loop_indices:
            vi=ob.data.loops[li].vertex_index;x,y,z=ob.data.vertices[vi].co
            if p.index==0:a,b=front[vi];u=a*512;v=b*512
            elif p.index in (1,2):
                a=(x+1.72)/1.52;b=(2.52-z)/1.29
                if p.index==2:a=1-a
                u=p.index*512+a*512;v=b*512
            else:u=1850;v=800
            uv.data[li].uv=(u/2048,1-v/1024)
    geo.finish(ob,.055,3)

def utility_wheel(x,y,label,rubber,gold,dark):
    s=-1 if y<0 else 1
    geo.lathe_y('WHEEL_'+label+'_Tire',x,y,.55,[(-.19,.27),(-.19,.42),(-.14,.51),(-.07,.55),(.07,.55),(.14,.51),(.19,.42),(.19,.27)],rubber,24)
    geo.lathe_y('WHEEL_'+label+'_UtilityDish',x,y,.55,[(s*a,r) for a,r in [(.195,.08),(.195,.35),(.17,.39),(.125,.39),(.115,.31),(.115,.08)]],dark,24)
    geo.lathe_y('WHEEL_'+label+'_PressedCap',x,y,.55,[(s*a,r) for a,r in [(.223,.015),(.223,.23),(.21,.28),(.17,.30),(.16,.28),(.16,.015)]],gold,20)

def build():
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    paint=geo.material('M02_Paint_Print',(1,.565,.027),.34,texture=ROOT/'Textures/Candidates/M02_body.png')
    plain=geo.material('M02_Paint_Solid',(1,.565,.027),.34)
    windows=geo.material('M02_Cabin_Print',(1,.565,.027),.3,texture=ROOT/'Textures/Candidates/M02_cabin.png')
    rubber=geo.material('Rubber_Satin',(.009,.011,.015),.65)
    dark=geo.material('M02_Graphite',(.018,.025,.031),.48)
    gold=geo.material('M02_WheelGold',(.84,.43,.022),.37,.3)
    # The lower hull has a broad vertical nose, rounded plan corners and straight utility sill.
    sections=[(-1.80,.78,.30,1.38,.015),(-1.72,.91,.30,1.44,.012),(-1.34,.98,.30,1.45,.01),
              (-.70,.98,.30,1.45,.01),(-.22,.985,.30,1.44,.02),(.35,.985,.30,1.44,.02),
              (1.23,.985,.30,1.43,.02),(1.70,.93,.30,1.41,.02),(1.82,.80,.32,1.36,.01)]
    body=geo.section_mesh('BODY_ParcelLowerHull',sections,paint);body_uv(body)
    for x in [-1.10,1.10]:geo.cylinder_cut(body,x,.55,.615,dark,32)
    geo.finish(body,.05,2)
    cab(windows)
    # Separate cargo shell has chamfered shoulders and a raised crown, overlapping the hull.
    cargo=geo.section_mesh('BODY_RaisedCargoShell',[(-.24,.91,1.24,2.55,.05),(-.10,.97,1.24,2.62,.055),
        (1.43,.965,1.24,2.62,.05),(1.70,.92,1.24,2.57,.05),(1.825,.80,1.24,2.46,.04)],paint)
    body_uv(cargo,2.7);geo.finish(cargo,.045,3)
    # Sturdy physical bumper belts sit against the hull. End returns have cut wheel clearance.
    front=geo.section_mesh('FRONT_MODULE_UtilityBumper',[(-1.86,.77,.31,.63,0),(-1.80,.88,.30,.66,0),(-1.70,.91,.31,.63,0)],plain)
    rear=geo.section_mesh('REAR_MODULE_UtilityBumper',[(1.71,.91,.31,.61,0),(1.83,.86,.30,.64,0),(1.89,.76,.32,.61,0)],dark)
    for ob,x in [(front,-1.1),(rear,1.1)]:geo.cylinder_cut(ob,x,.55,.615,dark,32);geo.finish(ob,.035,2)
    geo.solid('CHASSIS_UtilitySpine',(0,0,.29),(2.9,1.3,.20),dark,.04)
    for x,axle in [(-1.10,'F'),(1.10,'R')]:
        for y,side in [(-1.0,'L'),(1.0,'R')]:utility_wheel(x,y,axle+side,rubber,gold,dark)
    finish_candidate('M02','Parcel','Short forward-control cab, shaped raised cargo shell, straight utility sill, cut arches, mounted bumper belts and pressed utility wheels',
        {'ATTACH_ROOF':(.70,0,2.68),'ATTACH_CAB':(-.95,0,2.51),'ATTACH_CARGO':(.70,0,1.42),
         'ATTACH_REAR':(1.83,0,.5),'AXLE_FRONT':(-1.10,0,.55),'AXLE_REAR':(1.10,0,.55)},review_height=1.34)

if __name__=='__main__':build()
