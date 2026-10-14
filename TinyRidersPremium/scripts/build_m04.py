"""M04 low wedge: folded fenders, falling nose, narrow canopy, integrated ducktail.

The main hull has independent shoulder and hood heights, not a recolored coupe shell.
"""
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import bpy
from candidate_tools import geo,ROOT,finish_candidate
from shared_parts import wheel

def hull(paint):
    sections=[(-2.04,.88,.66,.60),(-1.82,1.00,.93,.75),(-1.50,1.08,1.18,.90),
        (-1.18,1.09,1.37,1.02),(-.72,1.02,1.25,1.16),(-.26,.93,1.16,1.15),
        (.32,.94,1.14,1.14),(.91,1.04,1.31,1.24),(1.30,1.06,1.37,1.22),
        (1.77,1.01,1.20,1.14),(1.96,.94,1.17,1.15)]
    verts=[]
    for x,w,shoulder,hood in sections:
        verts.extend([(x,-w+.08,.28),(x,w-.08,.28),(x,w,.38),(x,w,shoulder-.14),
            (x,w-.07,shoulder-.02),(x,w-.22,shoulder),(x,.48,hood),(x,-.48,hood),
            (x,-w+.22,shoulder),(x,-w+.07,shoulder-.02),(x,-w,shoulder-.14),(x,-w,.38)])
    n=12;faces=[tuple(reversed(range(n)))]
    for ring in range(len(sections)-1):
        for j in range(n):faces.append((ring*n+j,ring*n+(j+1)%n,(ring+1)*n+(j+1)%n,(ring+1)*n+j))
    faces.append(tuple((len(sections)-1)*n+j for j in range(n)))
    ob=geo.mesh('BODY_FoldedWedgeHull',verts,faces,paint);uv=ob.data.uv_layers.new(name='PrintUV')
    for p in ob.data.polygons:
        if p.index==0:zone='front'
        elif p.index==len(faces)-1:zone='rear'
        else:
            edge=(p.index-1)%n;zone='right' if edge in (1,2) else 'left' if edge in (10,11) else 'top'
        for li in p.loop_indices:
            x,y,z=ob.data.vertices[ob.data.loops[li].vertex_index].co
            if zone=='top':u=(x+2.04)/4*1024;v=(1.1-y)/2.2*768
            elif zone in ('left','right'):
                a=(x+2.04)/4
                if zone=='right':a=1-a
                u=(1024 if zone=='right' else 0)+a*1024;v=800+(1.38-z)/1.10*500
            elif zone=='front':u=(1.1-y)/2.2*1024;v=1350+(.68-z)/.4*650
            else:u=1024+(y+1.1)/2.2*1024;v=1350+(1.38-z)/1.10*650
            uv.data[li].uv=(u/2048,1-v/2048)
    return ob

def cabin(mat):
    v=[(-.48,-.72,1.10),(-.48,.72,1.10),(1.18,.70,1.10),(1.18,-.70,1.10),
       (.14,-.53,1.73),(.14,.53,1.73),(.80,.51,1.73),(.80,-.51,1.73)]
    faces=[(0,1,5,4),(3,0,4,7),(1,2,6,5),(2,3,7,6),(4,5,6,7),(3,2,1,0)]
    ob=geo.mesh('CABIN_NarrowFastbackCanopy',v,faces,mat);uv=ob.data.uv_layers.new(name='WindowPrintUV')
    maps={0:{0:(0,1),1:(1,1),5:(1,0),4:(0,0)},3:{2:(0,1),3:(1,1),7:(1,0),6:(0,0)}}
    for p in ob.data.polygons:
        for li in p.loop_indices:
            vi=ob.data.loops[li].vertex_index;x,y,z=ob.data.vertices[vi].co
            if p.index in (0,3):a,b=maps[p.index][vi];u=p.index*512+a*512;v=b*512
            elif p.index in (1,2):
                a=(x+.48)/1.66
                if p.index==2:a=1-a
                u=p.index*512+a*512;v=(1.73-z)/.63*512
            elif p.index==4:u=(x-.14)/.66*1024;v=512+(.53-y)/1.06*512
            else:u=1800;v=900
            uv.data[li].uv=(u/2048,1-v/1024)
    geo.finish(ob,.04,2)

def build():
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    paint=geo.material('M04_Paint_Print',(.94,.188,.008),.32,texture=ROOT/'Textures/Candidates/M04_body.png')
    plain=geo.material('M04_Paint_Solid',(.94,.188,.008),.32)
    windows=geo.material('M04_Cabin_Print',(.94,.188,.008),.30,texture=ROOT/'Textures/Candidates/M04_cabin.png')
    dark=geo.material('M04_Graphite',(.015,.022,.03),.47)
    body=hull(paint)
    for x in [-1.18,1.23]:geo.cylinder_cut(body,x,.62,.685,plain,32)
    geo.finish(body,.035,2);cabin(windows)
    geo.solid('CHASSIS_WedgeSpine',(0,0,.26),(3.2,1.35,.18),dark,.045)
    front=geo.section_mesh('FRONT_MODULE_ChinSplitter',[(-2.105,.79,.24,.34,0),(-2.04,.91,.23,.35,0),(-1.90,.94,.25,.34,0)],dark)
    geo.finish(front,.025,2)
    rear=geo.section_mesh('REAR_MODULE_DiffuserBelt',[(1.81,.94,.25,.49,0),(1.97,.91,.25,.47,0),(2.015,.81,.27,.44,0)],dark)
    geo.cylinder_cut(rear,1.23,.62,.685,dark,32);geo.finish(rear,.028,2)
    # The standard ducktail grows from the deck; tall supported wing belongs to custom build.
    lip=geo.section_mesh('SPOILER_IntegratedDucktail',[(1.66,.94,1.11,1.23,0),(1.87,.96,1.10,1.46,0),(1.99,.86,1.11,1.46,0)],plain)
    geo.finish(lip,.026,2)
    for x,ax in [(-1.18,'F'),(1.23,'R')]:
        for y,side in [(-1.04,'L'),(1.04,'R')]:wheel('W04',x,y,.62,ax+side)
    finish_candidate('M04','Wedge','Independent folded-fender hull, sloping long nose, low narrow fastback canopy, integrated ducktail, cut arches, shared five-spoke sport wheels',
        {'ATTACH_ROOF':(.48,0,1.75),'ATTACH_REAR':(1.76,0,1.22),'ATTACH_FRONT':(-1.93,0,.38),
         'AXLE_FRONT':(-1.18,0,.62),'AXLE_REAR':(1.23,0,.62)},review_height=.95)

if __name__=='__main__':build()
