"""Original M01 Pocket recipe: upright hatch, square nose, tall glazing, inset wheels.

This shares low-level geometry operations and the evidence studio only with M03.
"""
import sys,math
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import bpy
from candidate_tools import geo,ROOT,finish_candidate

def body_uv(ob):
    uv=ob.data.uv_layers.new(name='PrintUV')
    rects={'top':(0,0,1024,768),'left':(0,800,1024,1300),'right':(1024,800,2048,1300),
        'front':(0,1350,1024,2000),'back':(1024,1350,2048,2000)}
    for p in ob.data.polygons:
        if p.index==0:zone='front'
        elif p.index==len(ob.data.polygons)-1:zone='back'
        else:
            strip=(p.index-1)%10
            zone='right' if strip in (1,2) else 'left' if strip in (8,9) else 'top'
        x0,y0,x1,y1=rects[zone]
        for li in p.loop_indices:
            x,y,z=ob.data.vertices[ob.data.loops[li].vertex_index].co
            if zone=='top':a=(x+1.67)/3.39;b=(1-y)/2
            elif zone in ('left','right'):
                a=(x+1.67)/3.39;a=1-a if zone=='right' else a;b=(1.55-z)/1.2
            else:a=(-y+1)/2 if zone=='front' else (y+1)/2;b=(1.55-z)/1.2
            uv.data[li].uv=((x0+a*(x1-x0))/2048,1-(y0+b*(y1-y0))/2048)

def cabin(mat):
    verts=[(-.83,-.85,1.35),(-.83,.85,1.35),(1.41,.85,1.35),(1.41,-.85,1.35),
        (-.58,-.75,2.22),(-.58,.75,2.22),(1.21,.75,2.22),(1.21,-.75,2.22)]
    faces=[(0,1,5,4),(3,0,4,7),(1,2,6,5),(2,3,7,6),(4,5,6,7),(3,2,1,0)]
    ob=geo.mesh('CABIN_UprightHatch',verts,faces,mat);uv=ob.data.uv_layers.new(name='WindowPrintUV')
    maps=[{0:(0,1),1:(1,1),5:(1,0),4:(0,0)},{3:(1,1),0:(0,1),4:(0,0),7:(1,0)},
        {1:(1,1),2:(0,1),6:(0,0),5:(1,0)},{2:(0,1),3:(1,1),7:(1,0),6:(0,0)}]
    for p in ob.data.polygons:
        for li in p.loop_indices:
            vi=ob.data.loops[li].vertex_index
            a,b=maps[p.index][vi] if p.index<4 else (.1,.8)
            if p.index in (1,2):
                co=ob.data.vertices[vi].co;a=(co.x+.83)/2.24;b=(2.22-co.z)/.87
                if p.index==2:a=1-a
            if p.index<4:uv.data[li].uv=((p.index*512+a*512)/2048,1-b*.5)
            else:uv.data[li].uv=(.1,.1)
    geo.finish(ob,.055,2)

def wheel(x,y,label,rubber,alloy,hub):
    side=-1 if y<0 else 1
    profile=[(-.20,.32),(-.20,.46),(-.15,.555),(-.07,.585),(.07,.585),(.15,.555),(.20,.46),(.20,.32)]
    geo.lathe_y('WHEEL_'+label+'_Tire',x,y,.585,profile,rubber,28)
    profile=[(side*a,r) for a,r in [(.206,.11),(.206,.41),(.19,.44),(.15,.44),(.09,.32),(.09,.11)]]
    geo.lathe_y('WHEEL_'+label+'_DishRim',x,y,.585,profile,alloy,24)
    profile=[(side*a,r) for a,r in [(.23,.015),(.23,.14),(.21,.17),(.15,.17),(.15,.015)]]
    geo.lathe_y('WHEEL_'+label+'_Hub',x,y,.585,profile,hub,20)

def build():
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    paint=geo.material('M01_Paint_Print',(.86,.035,.022),.32,texture=ROOT/'Textures/Candidates/M01_body.png')
    plain=geo.material('M01_Paint_Solid',(.86,.035,.022),.32)
    window=geo.material('M01_Cabin_Print',(.86,.035,.022),.3,texture=ROOT/'Textures/Candidates/M01_cabin.png')
    cream=geo.material('M01_Ivory_Plastic',(.91,.85,.71),.34)
    rubber=geo.material('Rubber_Satin',(.009,.011,.015),.65)
    alloy=geo.material('M01_SatinAlloy',(.31,.34,.38),.37,.55)
    dark=geo.material('M01_Graphite',(.018,.021,.025),.5)
    sections=[(-1.67,.80,.35,1.35,.035),(-1.57,.92,.35,1.41,.02),(-1.24,.96,.35,1.45,.015),
        (-.83,.96,.35,1.49,.015),(-.25,.94,.35,1.44,.025),(.5,.94,.35,1.44,.02),
        (1.05,.97,.35,1.48,.02),(1.48,.94,.35,1.44,.02),(1.72,.82,.38,1.30,.02)]
    body=geo.section_mesh('BODY_PocketHatchShell',sections,paint);body_uv(body)
    for x in [-.91,.96]:geo.cylinder_cut(body,x,.585,.642,plain)
    geo.finish(body,.05,2);cabin(window)
    roofverts=[]
    for front,back,w,z in [(-.65,1.29,.79,2.18),(-.65,1.29,.79,2.24),(-.57,1.21,.74,2.33)]:
        roofverts.extend([(front,-w,z),(front,w,z),(back,w,z),(back,-w,z)])
    rooffaces=[(3,2,1,0),(8,9,10,11)]
    for ring in range(2):
        for j in range(4):rooffaces.append((ring*4+j,ring*4+(j+1)%4,(ring+1)*4+(j+1)%4,(ring+1)*4+j))
    roof=geo.mesh('ROOF_ContrastingMoldedCap',roofverts,rooffaces,cream);geo.finish(roof,.035,2)
    # Full width bumpers are mounted against closed end caps beyond the wheel envelope.
    front=geo.section_mesh('FRONT_MODULE_ChromeBelt',[(-1.76,.73,.37,.69,0),(-1.69,.84,.34,.72,0),(-1.62,.86,.37,.69,0)],cream)
    geo.finish(front,.045,2)
    rear=geo.section_mesh('REAR_MODULE_ChromeBelt',[(1.67,.86,.37,.69,0),(1.74,.84,.34,.72,0),(1.81,.73,.37,.69,0)],cream)
    geo.finish(rear,.045,2)
    geo.solid('CHASSIS_CentralSpine',(0,0,.31),(2.80,1.24,.20),dark,.05)
    for x,axle in [(-.91,'F'),(.96,'R')]:
        for y,side in [(-.97,'L'),(.97,'R')]:wheel(x,y,axle+side,rubber,alloy,dark)
    finish_candidate('M01','Pocket',
        'Nine-section micro hatch shell with cut arches, upright tapered cabin, molded ivory roof cap, mounted bumper belts, deep dish wheels',
        {'ATTACH_ROOF':(.32,0,2.34),'ATTACH_REAR':(1.72,0,1.0),'AXLE_FRONT':(-.91,0,.585),'AXLE_REAR':(.96,0,.585)})

if __name__=='__main__':build()
