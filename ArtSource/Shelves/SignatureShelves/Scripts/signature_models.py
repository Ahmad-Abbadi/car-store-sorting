"""Editable Blender toy sculptures. Coordinates: X right, -Y front, Z up."""
import bpy, bmesh, math
from mathutils import Vector
PARTS=[]
MATERIAL=None
# Four-by-four atlas tiles; the upper-left quadrant is reserved for rendered header art.
TILES=[(2,3),(3,3),(2,2),(3,2),(0,1),(1,1),(2,1),(3,1),(0,0),(1,0),(2,0),(3,0)]

def finish(o,name,color,smooth=True):
    o.name=name;o.data.materials.clear();o.data.materials.append(MATERIAL)
    bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(o.data);bm.free()
    bpy.context.view_layer.objects.active=o
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if smooth:
        for p in o.data.polygons:p.use_smooth=True
    if not o.data.uv_layers.active:
        bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.03);bpy.ops.object.mode_set(mode='OBJECT')
    tx,ty=TILES[color]
    for uv in o.data.uv_layers.active.data:uv.uv=((tx+.08+uv.uv.x*.84)/4,(ty+.08+uv.uv.y*.84)/4)
    PARTS.append(o);return o

def sphere(name,pos,size,color,segments=20,rings=10):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,location=pos)
    o=bpy.context.object;o.scale=size;return finish(o,name,color)

def box(name,pos,size,color,bevel=.06,rot=(0,0,0)):
    bpy.ops.mesh.primitive_cube_add(size=1,location=pos,rotation=rot);o=bpy.context.object;o.scale=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=o.modifiers.new('Soft toy edges','BEVEL');mod.width=min(bevel,min(size)*.3);mod.segments=2
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(o,name,color,False)

def cylinder(name,pos,radius,depth,color,rot=(0,0,0),vertices=24):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=depth,location=pos,rotation=rot)
    return finish(bpy.context.object,name,color)

def cone(name,pos,r1,r2,depth,color,rot=(0,0,0),vertices=24):
    bpy.ops.mesh.primitive_cone_add(vertices=vertices,radius1=r1,radius2=r2,depth=depth,location=pos,rotation=rot)
    return finish(bpy.context.object,name,color)

def rod(name,a,b,r,color,vertices=12):
    a,b=Vector(a),Vector(b);o=cylinder(name,(a+b)/2,r,(b-a).length,color,vertices=vertices)
    o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();return o

def torus(name,pos,major,minor,color,rot=(0,0,0),scale=(1,1,1)):
    bpy.ops.mesh.primitive_torus_add(major_segments=28,minor_segments=8,location=pos,rotation=rot,major_radius=major,minor_radius=minor)
    o=bpy.context.object;o.scale=scale;return finish(o,name,color)

def tube(name,points,r,color):
    # Segment joints use smooth spheres only where bends are visible.
    for i in range(len(points)-1):rod(name+' stem',points[i],points[i+1],r,color)
    for p in points[1:-1]:sphere(name+' joint',p,(r,r,r),color,12,6)

def profile(name,levels,color,segments=32,ellipse=1):
    verts=[];faces=[]
    for z,r in levels:
        verts.extend([(r*math.cos(a*math.tau/segments),r*math.sin(a*math.tau/segments)*ellipse,z) for a in range(segments)])
    for j in range(len(levels)-1):
        for i in range(segments):a=j*segments+i;b=j*segments+(i+1)%segments;faces.append((a,b,b+segments,a+segments))
    faces.extend([tuple(range(segments-1,-1,-1)),tuple((len(levels)-1)*segments+i for i in range(segments))])
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o)
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
    return finish(o,name,color)

def shape(name,points,depth,color,y=0,bevel=.04):
    n=len(points);v=[(x,y-depth/2,z) for x,z in points]+[(x,y+depth/2,z) for x,z in points]
    f=[tuple(range(n)),tuple(range(2*n-1,n-1,-1))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    me=bpy.data.meshes.new(name);me.from_pydata(v,[],f);me.update();o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o)
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
    if bevel:
        m=o.modifiers.new('Rounded silhouette','BEVEL');m.width=bevel;m.segments=2;bpy.ops.object.modifier_apply(modifier=m.name)
    return finish(o,name,color,False)

def star(name,pos,r,color,depth=.1):
    x,y,z=pos
    return shape(name,[(x+math.cos(math.pi/2+i*math.pi/5)*r*(1 if i%2==0 else .46),z+math.sin(math.pi/2+i*math.pi/5)*r*(1 if i%2==0 else .46)) for i in range(10)],depth,color,y,.025)

def eyes(x,z,y=-.43,r=.12):
    for xx in (-x,x):
        sphere('Eye socket',(xx,y,z),(r,r*.35,r*1.15),3)
        sphere('Eye glint',(xx-r*.25,y-r*.34,z+r*.3),(r*.18,r*.12,r*.18),4,12,6)

def leaf(name,pos,size,color):
    x,y,z=pos;w,h=size
    o=sphere(name,(x,y,z),(w,.1,h),color);o.rotation_euler.y=-.4
    rod(name+' vein',(x-w*.35,y-.095,z-h*.65),(x+w*.35,y-.095,z+h*.65),.018,2)

def hat(kind):
    if kind=='cowboy':
        # A shaped, upturned brim and pinched crown, with a stitched leather band.
        levels=[(-.1,.58),(-.04,.8),(.025,.85),(.075,.76),(.09,.48)]
        brim=profile('Upturned leather brim',levels,0,40,.65)
        for v in brim.data.vertices:v.co.z+=.18*(abs(v.co.x)/.85)**3
        profile('Pinched hat crown',[(.04,.45),(.14,.46),(.62,.38),(.72,.25),(.7,.05)],0,32,.7)
        profile('Chocolate hat band',[(.12,.465),(.26,.44)],1,32,.71)
        box('Brass buckle',(0,-.33,.2),(.2,.06,.14),2,.025)
        box('Buckle center',(0,-.365,.2),(.11,.015,.065),1,.01)
        for i in range(12):
            a=math.pi+i*math.pi/11
            sphere('Band stitch',(.445*math.cos(a),.318*math.sin(a),.285),(.022,.016,.012),4,8,4)
    elif kind=='detective':
        sphere('Tweed cap',(0,0,.2),(.55,.4,.4),0)
        sphere('Front visor',(0,-.42,.02),(.47,.32,.065),1)
        sphere('Rear visor',(0,.4,.02),(.42,.23,.06),1)
        for x in (-.43,.43):sphere('Ear flap',(x,0,.06),(.16,.27,.27),0)
        torus('Crown piping',(0,0,.19),.41,.028,2,scale=(1.27,1,.8))
        sphere('Top button',(0,0,.61),(.07,.07,.04),2)

def wheel(name,x,y,z,r=.16):
    cylinder(name,(x,y,z),r,.11,3,(math.pi/2,0,0),20)
    cylinder(name+' hub',(x,y-.065,z),r*.45,.04,2,(math.pi/2,0,0),16)

def build(key,material):
    global MATERIAL,PARTS
    MATERIAL=material;PARTS=[]
    if key=='WILD_WEST':hat('cowboy')
    elif key=='PIRATE':
        sphere('Ivory skull',(0,0,.24),(.47,.31,.48),4,28,16)
        sphere('Jaw',(0,-.015,-.12),(.29,.25,.2),4)
        for x in (-.19,.19):sphere('Deep eye hollow',(x,-.275,.28),(.16,.095,.18),3)
        shape('Nose cavity',[(-.07,.1),(.07,.1),(0,-.025)],.035,3,-.307,.01)
        for x in (-.19,-.095,0,.095,.19):box('Individual tooth',(x,-.245,-.18),(.075,.09,.11),4,.024)
        for flip in (-1,1):
            a=(-.6,-.02,-.47*flip);b=(.6,-.02,.47*flip)
            rod('Crossbone',a,b,.06,4)
            for p in (a,b):
                for dz in (-.045,.045):sphere('Bone end',(p[0],p[1],p[2]+dz),(.09,.08,.07),4,12,6)
    elif key=='CANDY':
        sphere('Glossy candy center',(0,0,0),(.4,.29,.32),0,28,16)
        for x in (-1,1):
            cone('Twisted wrapper',(x*.52,0,0),.27,.07,.33,1,(0,-x*math.pi/2,0))
            torus('Wrapper tie',(x*.39,0,0),.09,.035,2,(0,math.pi/2,0))
            for z in (-.12,0,.12):rod('Wrapper fold',(x*.4,-.075,0),(x*.68,-.17,z),.018,4)
        for x in (-.2,0,.2):
            o=torus('Candy stripe',(x,0,0),.28,.025,4,(0,math.pi/2,0));o.scale.z=1.06
    elif key=='SCIENCE_LAB':
        profile('Ceramic flask',[(-.56,.29),(-.53,.43),(-.4,.42),(.16,.13),(.55,.13),(.58,.18),(.65,.18),(.67,.12)],1,32)
        profile('Bright chemical enamel',[(-.5,.427),(-.44,.435),(-.16,.29)],2,32)
        torus('Flask lip',(0,0,.62),.16,.035,4)
        for x,z,r in [(-.15,-.3,.07),(.12,-.38,.045),(0,-.1,.045)]:sphere('Bubble',(x,-math.sqrt(max(.03,.35**2-x*x)),z),(r,.024,r),4,16,8)
        for z in (-.04,.08,.2):box('Measurement tick',(.1,-.15,z),(.07,.018,.018),4,.004)
        sphere('Rising bubble',(.05,0,.83),(.075,.075,.075),2)
    elif key=='ROYAL':
        profile('Crown band',[(-.35,.45),(-.22,.5),(.1,.47)],2,32)
        for i in range(5):
            a=i*math.tau/5; x,y=math.cos(a)*.43,math.sin(a)*.43
            cone('Crown point',(x,y,.22),.16,.03,.48,2)
            sphere('Pearl finial',(x,y,.48),(.06,.06,.06),4,12,6)
            sphere('Jewel',(x*1.15,y*1.15,-.1),(.08,.08,.1),1,16,8)
        torus('Rolled lower rim',(0,0,-.3),.47,.04,2)
    elif key=='SPACE':
        sphere('Ringed planet',(0,0,0),(.45,.45,.45),0,28,16)
        torus('Saturn rings',(0,0,0),.68,.075,2,(.23,.35,0),scale=(1,1,.35))
        torus('Equatorial cloud band',(0,0,.12),.432,.024,1)
        sphere('Small moon',(.61,0,.48),(.11,.11,.11),4)
    elif key=='ALIEN':
        sphere('Alien head',(0,0,.08),(.47,.3,.59),0,28,16)
        for x in (-.22,.22):
            o=sphere('Obsidian alien eye',(x,-.269,.16),(.16,.055,.24),3);o.rotation_euler.y=-x*1.2
            sphere('Eye highlight',(x-.03,-.32,.24),(.035,.015,.055),4,12,6)
        box('Friendly mouth',(0,-.27,-.24),(.19,.025,.035),1,.01)
    elif key=='BEACH':
        sphere('Sunny face',(0,0,0),(.43,.2,.43),0,24,12)
        for i in range(12):
            a=i*math.tau/12;rod('Sun ray',(.49*math.cos(a),0,.49*math.sin(a)),(.67*math.cos(a),0,.67*math.sin(a)),.045,2)
        eyes(.15,.08,-.2,.06)
        tube('Sunny smile',[(-.15,-.21,-.1),(0,-.23,-.17),(.15,-.21,-.1)],.025,1)
    elif key=='JUNGLE':
        leaf('Sculpted jungle leaf',(0,0,0),(.32,.67),0)
        for z in (-.35,-.12,.12,.35):
            rod('Leaf vein',(-.01,-.11,z-.1),(.24,-.08,z+.09),.015,2)
            rod('Leaf vein',(-.01,-.11,z-.1),(-.2,-.08,z+.04),.015,2)
        tube('Curving leaf stem',[(0,0,-.7),(-.1,0,-.86),(-.04,0,-.99)],.035,1)
    elif key=='SPOOKY':
        sphere('Friendly ghost',(0,0,.17),(.43,.29,.48),4,24,12)
        profile('Waving ghost hem',[(-.43,.46),(-.19,.42),(.13,.4)],4,24,.7)
        for x in (-.28,0,.28):sphere('Hem curl',(x,-.025,-.4),(.17,.23,.13),4)
        eyes(.15,.23,-.285,.08);sphere('Round ghost mouth',(0,-.29,-.02),(.055,.02,.08),3)
    elif key=='FOOD':
        sphere('Toasted top bun',(0,0,.24),(.52,.38,.23),0,28,12)
        sphere('Lower bun',(0,0,-.24),(.51,.38,.12),0)
        cylinder('Burger patty',(0,0,-.08),.47,.17,1);box('Cheese slice',(0,0,.025),(.79,.65,.035),2,.04,rot=(0,0,.15))
        for i in range(10):
            a=i*math.tau/10;sphere('Lettuce ruffle',(.43*math.cos(a),.32*math.sin(a),.09),(.14,.1,.04),8,12,6)
        for i in range(9):
            a=i*2.4;r=.12+.2*(i%3)/2
            sphere('Sesame seed',(math.cos(a)*r,math.sin(a)*r,.44-r*.2),(.035,.017,.013),4,8,4)
    elif key=='FIRE':
        for x,y,z,s in [(0,0,0,1),(-.23,0,-.14,.7),(.23,.02,-.2,.65)]:
            o=profile('Dancing flame',[(-.48,.1),(-.3,.3),(.05,.23),(.42,.1),(.7,.005)],0,24,.65);o.location=(x,y,z);o.scale=(s,s,s)
        sphere('Golden flame core',(0,-.18,-.14),(.2,.13,.32),2)
    elif key=='ICE':
        sphere('Snow crystal center',(0,0,0),(.13,.1,.13),4)
        for i in range(6):
            a=i*math.tau/6;dx,dz=math.cos(a),math.sin(a);rod('Crystal arm',(0,0,0),(.68*dx,0,.68*dz),.045,1)
            for r in (.35,.52):
                for side in (-1,1):rod('Crystal fork',(r*dx,0,r*dz),((r-.15)*dx-side*.14*dz,0,(r-.15)*dz+side*.14*dx),.026,2)
    elif key=='OCEAN':
        sphere('Reef fish',(0,0,0),(.48,.25,.32),0,24,12)
        shape('Fishtail',[(.35,0),(.76,.31),(.76,-.31)],.16,1)
        shape('Dorsal fin',[(-.25,.18),(.15,.52),(.31,.17)],.1,2)
        sphere('Fish eye',(-.25,-.232,.09),(.09,.04,.09),4);sphere('Fish pupil',(-.25,-.27,.09),(.045,.02,.05),3)
        for x in (-.04,.12):tube('Gill curve',[(x,-.24,.16),(x+.04,-.27,0),(x,-.24,-.15)],.018,2)
    elif key=='CONSTRUCTION':
        sphere('Hardhat shell',(0,0,.15),(.5,.39,.36),0,28,12)
        sphere('Hardhat brim',(0,-.04,-.06),(.6,.49,.055),2)
        box('Safety center rib',(0,0,.4),(.1,.64,.11),2,.05)
        for x in (-.25,.25):box('Safety side rib',(x,0,.35),(.06,.5,.08),2,.03)
        box('Safety badge',(0,-.39,.18),(.2,.035,.14),4,.025)
    elif key=='RESCUE':
        shape('Rounded heart',[(0,-.58),(-.55,-.04),(-.61,.3),(-.37,.55),(-.12,.51),(0,.34),(.12,.51),(.37,.55),(.61,.3),(.55,-.04)],.34,0,0,.08)
        box('Medical cross vertical',(0,-.2,.06),(.13,.06,.45),4,.025);box('Medical cross horizontal',(0,-.2,.06),(.42,.06,.13),4,.025)
    elif key=='WIZARD':
        sphere('Wizard brim',(0,0,-.45),(.65,.43,.065),1)
        profile('Curving wizard crown',[(-.43,.4),(-.13,.3),(.23,.21),(.56,.13),(.79,.02)],0,28,.85)
        torus('Wizard hat band',(0,0,-.28),.35,.055,2,scale=(1,.85,1))
        star('Golden star',(.03,-.245,.09),.16,2,.045)
        sphere('Tip charm',(0,0,.84),(.06,.06,.06),2)
    elif key=='ROBOT':
        box('Robot head',(0,0,0),(.88,.5,.72),0,.13)
        box('Face panel',(0,-.28,.07),(.7,.06,.4),3,.05)
        for x in (-.2,.2):sphere('Robot eye',(x,-.33,.12),(.09,.03,.1),2)
        box('Robot smile',(0,-.32,-.09),(.28,.03,.035),4,.01)
        for x in (-.49,.49):cylinder('Ear dial',(x,0,0),.14,.14,1,(0,math.pi/2,0),16)
        rod('Antenna',(0,0,.35),(0,0,.61),.035,2);sphere('Antenna light',(0,0,.68),(.095,.095,.095),1)
    elif key=='MUSIC':
        for x,z in [(-.3,-.32),(.3,-.2)]:
            sphere('Musical note head',(x-.07,0,z),(.21,.13,.14),0)
            box('Musical note stem',(x+.08,0,z+.43),(.09,.15,.83),2,.04)
        box('Musical note beam',(.08,0,.55),(.7,.17,.18),1,.045,rot=(0,-.16,0))
    elif key=='SPORT':
        sphere('Sports ball',(0,0,0),(.5,.5,.5),4,28,16)
        for rot in [(0,0,0),(math.pi/2,0,0),(0,math.pi/2,0)]:torus('Ball seam',(0,0,0),.499,.015,1,rot)
        for x,z in [(0,0),(-.3,.27),(.31,.26),(-.3,-.27),(.3,-.27)]:
            sphere('Ball patch',(x,-math.sqrt(.25-x*x-z*z),z),(.12,.019,.12),0,10,6)
    elif key=='FARM':
        sphere('Orchard apple',(0,0,0),(.45,.36,.48),0,28,16)
        tube('Apple stalk',[(0,0,.39),(.04,0,.57),(.11,0,.65)],.05,1)
        leaf('Apple leaf',(.23,0,.55),(.19,.12),8)
        sphere('Apple shine',(-.17,-.33,.18),(.04,.018,.1),4)
    elif key=='PARTY':
        for x,y,z,c in [(-.25,0,.15,0),(.27,.05,.2,1),(0,-.12,.4,2)]:
            sphere('Party balloon',(x,y,z),(.23,.19,.3),c)
            cone('Balloon knot',(x,y,z-.31),.025,.05,.07,c)
            rod('Balloon string',(x,y,z-.34),(0,.02,-.62),.012,4,8)
    elif key=='DETECTIVE':hat('detective')
    elif key=='NINJA':
        sphere('Ninja hood',(0,0,0),(.43,.32,.48),0,24,12)
        box('Eye opening',(0,-.3,.12),(.63,.09,.18),4,.06)
        eyes(.16,.13,-.36,.052)
        for z in (-.28,-.15,.29):torus('Fabric wrap seam',(0,0,z),math.sqrt(.43**2-min(.4,z)**2),.022,1,scale=(1,.76,1))
        shape('Trailing headband',[(.31,.15),(.72,.29),(.63,-.04),(.35,-.02)],.1,2,.1)
    elif key=='VIKING_LEGENDS':
        sphere('Viking helmet',(0,0,0),(.45,.34,.4),0,24,12)
        torus('Helmet rim',(0,0,-.12),.43,.055,2,scale=(1,.8,1))
        box('Nasal guard',(0,-.35,-.02),(.11,.075,.54),2,.03)
        for side in (-1,1):
            tube('Ivory horn',[(side*.35,0,.12),(side*.61,0,.26),(side*.71,0,.48)],.085,4)
            cone('Horn tip',(side*.73,0,.57),.08,0,.23,4,rot=(0,side*.15,0),vertices=16)
    elif key=='ANCIENT_EGYPT':
        sphere('Pharaoh face',(0,-.09,.05),(.25,.24,.4),2,24,12)
        shape('Nemes headdress',[(-.29,.47),(-.51,.3),(-.55,-.55),(-.24,-.48),(-.2,.23),(.2,.23),(.24,-.48),(.55,-.55),(.51,.3),(.29,.47)],.35,1,.08,.05)
        for x in (-.4,.4):
            for z in (-.4,-.23,-.06,.11):box('Gold nemes band',(x,-.13,z),(.23,.045,.055),2,.012)
        eyes(.09,.14,-.31,.034)
        cone('Royal beard',(0,-.23,-.43),.055,.09,.26,2)
        tube('Cobra crest',[(0,-.2,.38),(0,-.3,.55),(0,-.26,.68)],.04,2)
    elif key=='STEAMPUNK':
        for x,z,r,c in [(-.2,.06,.43,2),(.32,-.22,.29,1)]:
            cylinder('Gear hub',(x,0,z),r*.83,.16,c,(math.pi/2,0,0))
            torus('Gear ring',(x,-.1,z),r*.5,.05,0,(math.pi/2,0,0))
            cylinder('Gear axle',(x,-.13,z),r*.18,.05,3,(math.pi/2,0,0))
            for i in range(12):
                a=i*math.tau/12;box('Gear tooth',(x+r*math.cos(a),0,z+r*math.sin(a)),(r*.3,.18,r*.2),c,.015,rot=(0,-a,0))
    elif key=='PREHISTORIC':
        sphere('Dinosaur ridge',(0,0,-.27),(.56,.23,.16),0)
        for x,z,s in [(-.38,.01,.23),(0,.18,.35),(.38,.02,.24)]:
            shape('Dinosaur plate',[(x-s,z-.2),(x-s*.7,z+s*.7),(x,z+s),(x+s*.7,z+s*.5),(x+s,z-.2)],.19,1,0,.04)
            rod('Plate rib',(x,-.12,z-.13),(x,-.12,z+s*.62),.022,2)
    elif key=='HONEY_BEE':
        sphere('Bee body',(0,0,0),(.33,.27,.47),0,24,12)
        for z in (-.22,.02,.22):torus('Bee stripe',(0,0,z),math.sqrt(.33**2-(z*.7)**2),.05,3,scale=(1,.82,1))
        sphere('Bee head',(0,-.04,.44),(.25,.23,.23),0);eyes(.1,.47,-.255,.05)
        for x in (-.37,.37):
            o=sphere('Pearly bee wing',(x,.13,.18),(.28,.06,.36),4);o.rotation_euler.y=-x*.8
            tube('Antenna',[(x*.35,-.04,.61),(x*.5,-.03,.8)],.02,3)
    elif key=='BUTTERFLY_GARDEN':
        for s in (-1,1):
            o=sphere('Upper butterfly wing',(s*.32,.04,.24),(.32,.09,.4),0);o.rotation_euler.y=s*.4
            sphere('Lower butterfly wing',(s*.28,.04,-.25),(.25,.09,.27),1)
            sphere('Wing eye',(s*.4,-.06,.3),(.11,.025,.16),2)
            sphere('Wing eye center',(s*.4,-.087,.3),(.05,.018,.08),3)
            tube('Butterfly antenna',[(s*.04,-.02,.4),(s*.12,-.02,.61)],.018,3)
        sphere('Butterfly body',(0,-.06,0),(.09,.1,.45),3)
    elif key=='MUSHROOM_GROVE':
        profile('Mushroom stem',[(-.58,.2),(-.5,.22),(-.15,.16),(.1,.18)],4,24)
        sphere('Velvet mushroom cap',(0,0,.15),(.57,.42,.27),0,28,14)
        for x,y in [(-.3,-.19),(.21,-.24),(0,-.04),(.3,.15),(-.27,.17)]:sphere('Ivory cap spot',(x,y,.35-(x*x+y*y)*.5),(.09,.07,.022),4,12,6)
        torus('Cap rolled edge',(0,0,.1),.5,.035,1,scale=(1,.75,1))
    elif key=='THUNDERSTORM':
        for x,z,s in [(-.34,.2,.26),(0,.31,.34),(.33,.16,.27),(0,.06,.3)]:sphere('Cloud billow',(x,0,z),(s,s*.7,s*.8),0,20,10)
        shape('Lightning bolt',[(.06,.08),(-.24,-.3),(-.02,-.29),(-.13,-.67),(.32,-.16),(.1,-.18)],.13,2,-.07,.025)
    elif key=='WINTER_HOLIDAYS':
        for s in (-1,1):
            o=torus('Gift bow loop',(s*.25,0,.14),.22,.075,0,(math.pi/2,0,0),scale=(1,.9,1.3));o.rotation_euler.y=s*.3
            shape('Satin ribbon tail',[(s*.04,-.02),(s*.18,-.56),(s*.34,-.39),(s*.45,-.45),(s*.3,.03)],.08,1,.04,.025)
        sphere('Bow knot',(0,-.06,.08),(.15,.13,.15),2)
    elif key=='RAILWAY':
        box('Locomotive chassis',(0,0,-.26),(1.15,.53,.14),2,.04)
        cylinder('Boiler',(-.18,0,.01),.23,.7,0,(0,math.pi/2,0))
        box('Driver cabin',(.36,0,.15),(.4,.49,.58),0,.04)
        box('Cabin roof',(.35,0,.48),(.51,.61,.09),1,.035)
        box('Cabin window',(.36,-.257,.23),(.22,.025,.22),3,.03)
        cone('Chimney',(-.39,0,.35),.07,.12,.28,1)
        for x in (-.4,0,.4):
            for y in (-.3,.3):wheel('Train wheel',x,y,-.28)
        rod('Wheel connecting bar',(-.4,-.38,-.28),(.4,-.38,-.28),.025,2)
    elif key=='AVIATION':
        sphere('Aircraft fuselage',(0,0,0),(.15,.63,.15),0,24,12)
        shape('Aircraft wings',[(-.76,-.04),(0,.1),(.76,-.04),(.65,-.19),(0,-.07),(-.65,-.19)],.42,1,0,.025)
        box('Tail plane',(0,.46,.06),(.55,.19,.055),2,.025)
        shape('Tail fin',[(-.02,0),(.02,.35),(.18,.04)],.08,1,.46,.02)
        sphere('Cockpit canopy',(0,-.16,.13),(.11,.22,.09),3)
        rod('Propeller',(-.3,-.62,0),(.3,-.62,0),.035,2)
    elif key=='CINEMA':
        box('Clapper slate',(0,0,-.08),(1,.17,.66),3,.045)
        box('Clapper hinge',(0,0,.32),(1,.2,.13),4,.02)
        top=box('Clapper raised arm',(0,0,.53),(1,.19,.13),3,.02,rot=(0,-.18,0))
        for x in (-.34,-.1,.14,.38):box('Clapper stripe',(x,-.108,.53+x*.18),(.11,.02,.125),4,.008,rot=(0,-.45,0))
        for z in (-.02,-.17):box('Slate writing rule',(0,-.092,z),(.75,.02,.018),4,.004)
        box('Slate title',(-.1,-.092,.13),(.55,.02,.045),2,.005)
    elif key=='PHOTOGRAPHY':
        box('Camera body',(0,0,0),(.96,.35,.65),0,.08)
        cylinder('Camera lens',(0,-.27,-.02),.28,.25,3,(math.pi/2,0,0),32)
        torus('Lens chrome rim',(0,-.41,-.02),.23,.035,2,(math.pi/2,0,0))
        cylinder('Lens glass',(0,-.425,-.02),.19,.025,1,(math.pi/2,0,0),32)
        sphere('Lens reflection',(-.05,-.445,.05),(.05,.015,.06),4)
        box('Viewfinder',(-.25,-.19,.22),(.16,.04,.1),3,.02)
        cylinder('Shutter button',(.32,0,.36),.08,.08,2)
    elif key=='ART_STUDIO':
        sphere('Wooden palette',(0,0,0),(.5,.13,.62),0,28,16)
        sphere('Thumb recess',(.22,-.13,.26),(.1,.025,.13),3)
        for i,c in enumerate((1,2,8,9,10)):
            a=math.pi*.55+i*.48;sphere('Paint daub',(.34*math.cos(a),-.13,.43*math.sin(a)),(.085,.045,.08),c)
        rod('Paintbrush handle',(-.25,-.2,-.65),(.36,-.2,.55),.035,1)
        rod('Metal ferrule',(.28,-.2,.39),(.38,-.2,.59),.048,2)
        cone('Brush bristles',(.42,-.2,.67),.07,.015,.22,3,rot=(0,.45,0),vertices=16)
    elif key=='POST_OFFICE':
        box('Mailbox',(0,0,.02),(.82,.48,.56),0,.12)
        sphere('Mailbox roof',(0,0,.29),(.41,.24,.19),0)
        box('Letter slot',(0,-.255,.15),(.51,.025,.06),3,.02)
        box('Envelope',(0,-.28,-.14),(.35,.04,.21),4,.02)
        tube('Envelope crease',[(-.16,-.31,-.05),(0,-.31,-.18),(.16,-.31,-.05)],.01,1)
        rod('Mailbox flagpole',(.48,0,-.16),(.48,0,.56),.025,2)
        box('Raised flag',(.38,0,.5),(.24,.08,.18),1,.025)
    elif key=='BARBERSHOP':
        cylinder('Barber porcelain cylinder',(0,0,0),.23,.95,4)
        for c,phase in [(0,0),(1,math.pi)]:
            points=[(.234*math.cos(i*.25+phase),.234*math.sin(i*.25+phase),-.44+i*.88/48) for i in range(49)]
            # Thin ribbon winds continuously around the cylinder.
            verts=[]
            for x,y,z in points:verts.extend([(x,y,z-.045),(x,y,z+.045)])
            me=bpy.data.meshes.new('Spiral stripe');me.from_pydata(verts,[],[(2*i,2*i+2,2*i+3,2*i+1) for i in range(48)]);me.update()
            o=bpy.data.objects.new('Spiral barber stripe',me);bpy.context.collection.objects.link(o);bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;finish(o,'Spiral barber stripe',c)
        for z in (-.53,.53):sphere('Chrome barber cap',(0,0,z),(.29,.29,.1),2)
    else:raise ValueError('No dimensional theme model: '+key)
    return PARTS
