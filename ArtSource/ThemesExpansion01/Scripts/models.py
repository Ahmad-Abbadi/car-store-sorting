"""Original toy accessories authored with Blender meshes. Execute in live Blender MCP."""
import bpy,math,json,pathlib
from mathutils import Vector,Matrix
P=pathlib.Path(r'C:\Users\User\Documents\Games_Projects\My_Games_Projects\Car Store Sorting')
S=P/'ArtSource/ThemesExpansion01';O=P/'Assets/TinyRiders/Art/ThemeAccessories';D=P/'TinyRidersProduction/ThemesExpansion01'
THEMES={t['id']:t for t in json.loads((D/'texture_recipes.json').read_text())['themes']}
scene=bpy.data.scenes.get('ThemesExpansion01Studio') or bpy.data.scenes.new('ThemesExpansion01Studio')
bpy.context.window.scene=scene
PARTS=[];MAT=None;COL=None
def swatch(j):return ((768+(j%2)*128+64)/1024,1-((j//2)*128+64)/1024)
def setuv(o,mode=0):
    uv=o.data.uv_layers.active or o.data.uv_layers.new(name='AccessoryUV')
    uv.name='AccessoryUV'
    if isinstance(mode,int):
        for l in uv.data:l.uv=swatch(mode)
    elif mode=='sphere':
        for l in uv.data:l.uv=(l.uv.x*.75,.25+l.uv.y*.75)
    elif mode=='stripe':
        vs=[v.co for v in o.data.vertices];mn=min(v.x for v in vs);mx=max(v.x for v in vs)
        for poly in o.data.polygons:
            for li in poly.loop_indices:
                v=o.data.vertices[o.data.loops[li].vertex_index].co;uv.data[li].uv=(.03+.69*(v.x-mn)/max(.001,mx-mn),.06+.12*(v.z-min(w.z for w in vs))/max(.001,max(w.z for w in vs)-min(w.z for w in vs)))
    else:
        vs=[v.co for v in o.data.vertices];mn=[min(v[i] for v in vs) for i in range(3)];mx=[max(v[i] for v in vs) for i in range(3)]
        ax=(0,2) if mode=='front' else (0,1)
        for poly in o.data.polygons:
            for li in poly.loop_indices:
                v=o.data.vertices[o.data.loops[li].vertex_index].co
                uv.data[li].uv=(.035+.68*(v[ax[0]]-mn[ax[0]])/max(.001,mx[ax[0]]-mn[ax[0]]),.285+.68*(v[ax[1]]-mn[ax[1]])/max(.001,mx[ax[1]]-mn[ax[1]]))
def finish(o,name,uv=0,smooth=True,bevel=0):
    o.name=name
    for c in list(o.users_collection):c.objects.unlink(o)
    COL.objects.link(o)
    bpy.context.view_layer.objects.active=o;o.select_set(True)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=o.modifiers.new('Rounded toy edges','BEVEL');mod.width=bevel;mod.segments=2
        bpy.ops.object.modifier_apply(modifier=mod.name)
    o.data.materials.clear();o.data.materials.append(MAT);setuv(o,uv)
    for p in o.data.polygons:p.use_smooth=smooth
    PARTS.append(o);o.select_set(False);return o
def mesh(name,v,f,uv=0,smooth=True,bevel=0):
    m=bpy.data.meshes.new(name);m.from_pydata(v,[],f);m.update();o=bpy.data.objects.new(name,m);COL.objects.link(o)
    return finish(o,name,uv,smooth,bevel)
def sphere(name,loc,scale,uv=0,segments=24,rings=12):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,location=loc);o=bpy.context.object;o.scale=scale
    return finish(o,name,uv)
def box(name,loc,size,uv=0,bevel=.04):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.scale=size
    return finish(o,name,uv,False,bevel)
def cylinder(name,loc,radius,depth,uv=0,vertices=24,rotation=(0,0,0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=depth,location=loc,rotation=rotation)
    return finish(bpy.context.object,name,uv,True,.015)
def lathe(name,rings,uv=0,n=32,warp=None,smooth=True,closed_section=False):
    if closed_section and len(rings)==2:
        rings=list(rings)+[(z,max(.001,r-.018)) for z,r in reversed(rings)]
    v=[]
    for z,r in rings:
        for j in range(n):
            a=j*math.tau/n;pt=(r*math.cos(a),r*math.sin(a),z);v.append(warp(pt,a,z,r) if warp else pt)
    f=[]
    for k in range(len(rings)-1):
        for j in range(n):a=k*n+j;b=k*n+(j+1)%n;f.append((a,b,b+n,a+n))
    if closed_section:
        for j in range(n):f.append(((len(rings)-1)*n+j,(len(rings)-1)*n+(j+1)%n,(j+1)%n,j))
    else:f.extend([tuple(range(n-1,-1,-1)),tuple((len(rings)-1)*n+j for j in range(n))])
    return mesh(name,v,f,uv,smooth)
def tube(name,points,radii,uv=0,n=12):
    v=[]
    for j,pt in enumerate(points):
        tangent=Vector(points[min(j+1,len(points)-1)])-Vector(points[max(0,j-1)])
        tangent.normalize();a=tangent.cross(Vector((0,1,0)))
        if a.length<.01:a=tangent.cross(Vector((1,0,0)))
        a.normalize();b=tangent.cross(a).normalized()
        for k in range(n):v.append(Vector(pt)+radii[j]*(a*math.cos(k*math.tau/n)+b*math.sin(k*math.tau/n)))
    f=[]
    for j in range(len(points)-1):
        for k in range(n):a=j*n+k;b=j*n+(k+1)%n;f.append((a,b,b+n,a+n))
    f.extend([tuple(range(n-1,-1,-1)),tuple((len(points)-1)*n+k for k in range(n))]);return mesh(name,v,f,uv)

def slab(name,outline,thick=.08,uv=0):
    n=len(outline);v=[(x,y,z) for z in [-thick/2,thick/2] for x,y in outline]
    faces=[tuple(range(n-1,-1,-1)),tuple(range(n,n*2))]+[(j,(j+1)%n,n+(j+1)%n,n+j) for j in range(n)]
    return mesh(name,v,faces,uv,False,.018)

def cowboy():
    def fold(p,a,z,r):return (p[0],p[1]*.78,z+.16*(abs(math.cos(a))**4)*(r/.62)**2)
    lathe('RolledOvalCowboyBrim',[(0,.22),(.025,.49),(.055,.63),(.095,.64),(.125,.60),(.11,.29)],0,n=40,warp=fold)
    def pinch(p,a,z,r):return (p[0]*(1-.20*max(0,z-.20)/.34),p[1]*.78,z-.055*abs(math.cos(a)) if z>.45 else z)
    lathe('PinchedCrown',[(.08,.30),(.20,.31),(.40,.28),(.53,.22),(.55,.12),(.52,.005)],0,n=32,warp=pinch)
    lathe('DenimHatband',[(.15,.309),(.22,.30)],2,n=32,warp=lambda p,a,z,r:(p[0],p[1]*.78,z),closed_section=True)

def science():
    lathe('OpaqueConicalFlask',[(0,.25),(.025,.30),(.09,.31),(.37,.19),(.53,.10),(.69,.10),(.72,.12),(.76,.12),(.78,.085)],3,n=32)
    lathe('OpaqueLiquidBody',[(.035,.303),(.09,.318),(.32,.219),(.34,.207)],1,n=32,closed_section=True)
    lathe('LimeNeckLip',[(.70,.123),(.735,.13),(.77,.121)],2,n=32,closed_section=True)

def barber():
    lathe('BroadPedestal',[(0,.24),(.055,.26),(.10,.20)],3,n=24)
    lathe('LowerRoundedCap',[(.08,.17),(.12,.23),(.18,.24),(.23,.21)],1,n=24)
    o=lathe('PrintedHelixCylinder',[(.18,.20),(.65,.20)],0,n=32)
    uv=o.data.uv_layers.active
    for poly in o.data.polygons:
        for li in poly.loop_indices:
            vi=o.data.loops[li].vertex_index;ring,j=divmod(vi,32);jj=32 if j==0 and any(o.data.loops[q].vertex_index%32==31 for q in poly.loop_indices) else j
            uv.data[li].uv=(jj/32*.75,.25+ring*.75)
    lathe('UpperRoundedCap',[(.62,.21),(.67,.24),(.74,.22),(.79,.13),(.80,.005)],2,n=24)

def detective():
    sphere('FittedCapCrown',(0,0,.15),(.37,.34,.27),'top',24,10)
    for y in [-.34,.34]:
        o=sphere('DoubleVisor',(0,y,.07),(.32,.25,.065),'top',20,8)
    for x in [-.33,.33]:box('FoldedEarFlap',(x,0,.17),(.08,.29,.22),'front',.05)
    sphere('TopButton',(0,0,.42),(.055,.055,.035),3,12,6)

def ninja():
    lathe('ClothRoofWrap',[(0,.25),(.05,.37),(.16,.35),(.22,.27),(.20,.20)],1,n=32,warp=lambda p,a,z,r:(p[0],p[1]*.70,z),closed_section=True)
    sphere('TiedKnot',(0,.25,.18),(.14,.12,.11),1,16,8)
    for side in [-1,1]:
        o=slab('ThickSweptTail',[(0,.20),(side*.15,.27),(side*.29,.56),(side*.15,.52),(side*.08,.61),(-side*.05,.30)],.075,1);o.location.z=.12

def viking():
    lathe('DomeHelmet',[(0,.32),(.10,.36),(.26,.32),(.43,.19),(.48,.005)],0,n=28)
    lathe('BronzeRim',[(.02,.34),(.10,.37),(.13,.35)],2,n=28,closed_section=True)
    for sign in [-1,1]:
        tube('SweptBluntHorn',[(sign*.27,0,.28),(sign*.43,.03,.34),(sign*.49,.18,.46),(sign*.44,.30,.54)],[.10,.10,.07,.025],1,12)
        box('BroadCheekGuard',(sign*.27,-.12,.09),(.13,.23,.27),2,.035)

def egypt():
    lathe('RoundedNemesCrown',[(0,.25),(.12,.33),(.30,.29),(.43,.18),(.44,.005)],'front',n=28,warp=lambda p,a,z,r:(p[0],p[1]*.84,z))
    for sign in [-1,1]:
        outline=[(-.23,.30),(.10,.34),(.34,.02),(-.27,0)];v=[(sign*(.29+th),y,z) for th in [-.065,.065] for y,z in outline]
        mesh('ChunkyDrapedNemesLappet',v,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],'front',False,.025)
    lathe('GoldBrowBand',[(.13,.332),(.18,.327)],3,n=28,closed_section=True,warp=lambda p,a,z,r:(p[0],p[1]*.84,z))

def gear(name,loc,r=.25,uv=1):
    pts=[]
    for j in range(48):a=j*math.tau/48;rr=r*(1 if j%4 in [1,2] else .78);pts.append((rr*math.cos(a),rr*math.sin(a)))
    n=len(pts);v=[(x,y,z) for z in [-.06,.06] for x,y in pts];f=[tuple(range(n-1,-1,-1)),tuple(range(n,n*2))]+[(j,(j+1)%n,n+(j+1)%n,n+j) for j in range(n)]
    o=mesh(name,v,f,uv,False,0);o.rotation_euler.x=math.pi/2;o.location=loc;return o

def steampunk():
    box('LowClockworkHousing',(0,0,.075),(.76,.49,.15),2,.045)
    gear('LargeWorkingGear',(-.17,-.08,.32),.27,1);gear('SmallWorkingGear',(.22,-.05,.27),.19,0)
    sphere('PressureDome',(.18,.12,.19),(.15,.16,.17),1,20,8)

def prehistoric():
    box('BodyHuggingRidge',(0,0,.045),(.25,.98,.09),1,.04)
    for y,h,w in [(-.30,.32,.21),(0,.47,.26),(.32,.37,.23)]:
        pts=[(-w*.7,0),(-w,h*.50),(-w*.40,h),(w*.40,h),(w,h*.50),(w*.7,0)]
        o=slab('BroadDorsalPlate',pts,.10,0);o.rotation_euler.x=math.pi/2;o.location=(0,y,.07)

def bee():
    sphere('StripedBeePod',(0,0,.23),(.27,.40,.23),'sphere',24,12)
    for sign in [-1,1]:
        o=sphere('ThickIvoryWing',(sign*.27,.08,.31),(.21,.25,.065),1,20,8);o.rotation_euler.y=sign*-.20
        tube('ShortAntenna',[(sign*.11,-.23,.35),(sign*.16,-.34,.49)],[.025,.025],2,8);sphere('AntennaTip',(sign*.16,-.34,.49),(.045,.045,.045),2,12,6)

def butterfly():
    box('LowWingSaddle',(0,0,.06),(.28,.50,.12),1,.04)
    for sign in [-1,1]:
        pts=[(0,-.30),(sign*.13,-.48),(sign*.43,-.42),(sign*.54,-.20),(sign*.47,0),(sign*.50,.21),(sign*.32,.38),(sign*.11,.30),(0,.13)]
        o=slab('BroadPrintedButterflyWing',pts,.08,'top');o.rotation_euler.y=-sign*.40;o.location.z=.13
    sphere('ButterflyBody',(0,0,.17),(.09,.35,.085),3,16,8)

def mushroom():
    lathe('ShortThickStem',[(0,.16),(.04,.20),(.14,.15),(.29,.16)],1,n=24)
    lathe('GilledCapUnderside',[(.22,.16),(.26,.42),(.30,.48)],1,n=32)
    lathe('SpottedRoundedMushroomCap',[(.27,.47),(.32,.49),(.43,.42),(.55,.28),(.60,.10),(.61,.005)],'top',n=32)

def storm():
    box('CloudFoot',(0,0,.04),(.33,.23,.08),0,.035)
    for x,z,r in [(-.25,.22,.20),(0,.30,.28),(.26,.23,.20)]:sphere('SolidCloudLobe',(x,0,z),(r,.19,r),1,20,10)
    o=slab('SturdyLightningPendant',[(-.04,.40),(.14,.40),(.03,.23),(.15,.23),(-.13,.01),(-.06,.19),(-.17,.19)],.10,2);o.rotation_euler.x=math.pi/2;o.location.y=-.18

def holidays():
    box('LowGiftRack',(0,0,.035),(.63,.51,.07),1,.025);box('WrappedGift',(0,0,.25),(.57,.45,.40),'front',.04)
    box('RibbonBelt',(0,0,.26),(.12,.46,.41),3,.01)
    for sign in [-1,1]:tube('ThickBowLoop',[(0,0,.46),(sign*.23,0,.58),(sign*.25,0,.70),(sign*.11,0,.68),(0,0,.46)],[.045]*5,3,10)
    sphere('BowKnot',(0,0,.48),(.08,.07,.07),3,16,8)

def railway():
    box('LowLocomotiveHousing',(0,0,.10),(.55,.68,.20),0,.05)
    o=cylinder('RoundSmokebox',(0,-.16,.25),.23,.35,3,24,(math.pi/2,0,0))
    lathe('StoutChimney',[(.26,.12),(.46,.10),(.55,.18),(.59,.18)],3,n=24);PARTS[-1].location.y=-.18
    sphere('BrassSteamDome',(0,.23,.27),(.16,.18,.21),2,20,10)

def aviation():
    o=slab('ShortSweptWing',[(-.58,.17),(-.58,.0),(-.12,-.19),(.12,-.19),(.58,.0),(.58,.17),(.13,.08),(-.13,.08)],.09,1);o.location.z=.16
    box('WingRoot',(0,0,.08),(.27,.32,.16),0,.035)
    tube('CompactTailBoom',[(0,.02,.12),(0,.43,.18)],[.07,.055],2,12)
    o=slab('CompactTailFin',[(.20,.13),(.42,.16),(.40,.43),(.28,.37)],.07,3);o.rotation_euler=(math.pi/2,0,math.pi/2)

def cinema():
    box('LowClapperMount',(0,0,.04),(.45,.22,.08),2,.025)
    o=box('ThickClapperBoard',(0,0,.24),(.71,.15,.39),'front',.035);o.rotation_euler.x=math.radians(10)
    o=box('OpenedClapperBar',(-.01,0,.51),(.75,.15,.11),'stripe',.02);o.rotation_euler.y=-.13
    cylinder('Hinge',(-.33,0,.43),.055,.19,3,16,(math.pi/2,0,0))

def photography():
    box('RoundedCameraHousing',(0,0,.22),(.69,.34,.43),0,.065)
    box('SteppedViewfinder',(0,.015,.45),(.28,.27,.12),1,.03)
    for y,r,depth,col in [(-.18,.19,.10,1),(-.26,.16,.12,3),(-.325,.128,.045,0)]:cylinder('SteppedLensBarrel',(0,y,.23),r,depth,col,28,(math.pi/2,0,0))
    cylinder('ShutterButton',(.23,0,.46),.06,.07,1,16)

def artist():
    # Ring between a thumb opening and an asymmetric organic outside: a real through-hole.
    n=40;v=[]
    for z in [.02,.12]:
        for ring in [0,1]:
            for j in range(n):
                a=j*math.tau/n
                if ring==0:r=.44*(1+.12*math.sin(3*a));x=r*math.cos(a);y=r*.82*math.sin(a)
                else:x=-.10+.09*math.cos(a);y=-.12+.09*math.sin(a)
                v.append((x,y,z))
    f=[]
    for j in range(n):
        q=(j+1)%n;f.extend([(j,q,n+q,n+j),(2*n+j,3*n+j,3*n+q,2*n+q),(j,2*n+j,2*n+q,q),(n+j,n+q,3*n+q,3*n+j)])
    mesh('PaletteWithThumbOpening',v,f,'top',False,.012)
    tube('ShortBrushHandle',[(-.26,.22,.15),(.25,-.20,.16)],[.035,.025],2,12)
    tube('BrushFerrule',[(.18,-.14,.16),(.28,-.22,.16)],[.044,.044],1,12)
    tube('ShapedBristleMass',[(.27,-.21,.16),(.36,-.30,.16),(.42,-.37,.16)],[.043,.038,.006],3,12)

def post():
    # Extruded rounded mailbox tunnel with closed front and back, resting on a low base.
    outline=[(-.26,.08),(.26,.08),(.26,.37)]+[(.26*math.cos(j*math.pi/16),.37+.26*math.sin(j*math.pi/16)) for j in range(1,17)]
    o=slab('RoundedMailboxTunnel',outline,.55,0);o.rotation_euler.x=math.pi/2
    box('MailboxFoot',(0,0,.045),(.42,.43,.09),1,.025)
    box('ThickFlagStem',(.29,.08,.39),(.065,.055,.34),1,.015);box('RaisedPostalFlag',(.34,.08,.53),(.17,.07,.14),0,.02)

RECIPES={'WILD_WEST':cowboy,'DETECTIVE':detective,'NINJA':ninja,'VIKING_LEGENDS':viking,'ANCIENT_EGYPT':egypt,'STEAMPUNK':steampunk,'PREHISTORIC':prehistoric,'HONEY_BEE':bee,'BUTTERFLY_GARDEN':butterfly,'MUSHROOM_GROVE':mushroom,'THUNDERSTORM':storm,'WINTER_HOLIDAYS':holidays,'RAILWAY':railway,'AVIATION':aviation,'CINEMA':cinema,'PHOTOGRAPHY':photography,'ART_STUDIO':artist,'POST_OFFICE':post,'BARBERSHOP':barber,'SCIENCE_LAB':science}

def setup():
    if scene.camera:
        cam=scene.camera;cam.data.ortho_scale=1.6;cam.location=(1.6,-2.4,1.6);cam.rotation_euler=(Vector((0,0,.30))-cam.location).to_track_quat('-Z','Y').to_euler()
        for name,loc,power,size in [('ExpansionKey',(2,-3,4),400,4),('ExpansionFill',(-3,1,2),260,3)]:
            light=scene.objects.get(name)
            if light:light.location=loc;light.data.energy=power;light.data.size=size;light.rotation_euler=(-light.location).to_track_quat('-Z','Y').to_euler()
        scene.render.resolution_x=512;scene.render.resolution_y=512
        return
    scene.world=bpy.data.worlds.new('ExpansionNeutralWorld');scene.world.use_nodes=True
    bg=next(n for n in scene.world.node_tree.nodes if n.type=='BACKGROUND');bg.inputs['Color'].default_value=(.7,.75,.8,1);bg.inputs['Strength'].default_value=.5
    try:scene.render.engine='BLENDER_EEVEE'
    except TypeError:pass
    cam=bpy.data.objects.new('ExpansionReviewCamera',bpy.data.cameras.new('ExpansionReviewCamera'));scene.collection.objects.link(cam);scene.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=1.6;cam.location=(1.6,-2.4,1.6);cam.rotation_euler=(Vector((0,0,.30))-cam.location).to_track_quat('-Z','Y').to_euler()
    for name,loc,power,size in [('ExpansionKey',(2,-3,4),400,4),('ExpansionFill',(-3,1,2),260,3)]:
        light=bpy.data.objects.new(name,bpy.data.lights.new(name,'AREA'));scene.collection.objects.link(light);light.location=loc;light.data.energy=power;light.data.shape='DISK';light.data.size=size;light.rotation_euler=(-light.location).to_track_quat('-Z','Y').to_euler()
    scene.render.resolution_x=512;scene.render.resolution_y=512;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG'

def build(theme):
    global PARTS,MAT,COL
    setup();existing=bpy.data.collections.get(theme)
    if existing:
        for o in list(existing.objects):bpy.data.objects.remove(o,do_unlink=True)
        bpy.data.collections.remove(existing)
    COL=bpy.data.collections.new(theme);scene.collection.children.link(COL);PARTS=[]
    MAT=bpy.data.materials.get('EXP_'+theme) or bpy.data.materials.new('EXP_'+theme);MAT.use_nodes=True
    bs=next(n for n in MAT.node_tree.nodes if n.type=='BSDF_PRINCIPLED');bs.inputs['Roughness'].default_value=.65;bs.inputs['Metallic'].default_value=.04
    tex=next((n for n in MAT.node_tree.nodes if n.type=='TEX_IMAGE'),None) or MAT.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(S/'Artwork'/theme/'AccessoryAtlas.png'),check_existing=True);tex.image.reload();tex.image.pack();MAT.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
    bpy.ops.object.select_all(action='DESELECT');RECIPES[theme[3:]]()
    bpy.ops.object.select_all(action='DESELECT')
    for o in PARTS:o.select_set(True)
    bpy.context.view_layer.objects.active=PARTS[0]
    if len(PARTS)>1:bpy.ops.object.join()
    obj=bpy.context.object;obj.name='ACC_'+theme+'_Primary'
    import bmesh
    bm=bmesh.new();bm.from_mesh(obj.data);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(obj.data);bm.free()
    scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    low=min(v.co.z for v in obj.data.vertices)
    for v in obj.data.vertices:v.co.z-=low
    obj.data.calc_loop_triangles();triangles=len(obj.data.loop_triangles)
    target=O/'Meshes'/theme;target.mkdir(parents=True,exist_ok=True);export=target/(obj.name+'.fbx')
    bpy.ops.export_scene.fbx(filepath=str(export),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=False,use_mesh_modifiers=True,add_leaf_bones=False,path_mode='STRIP')
    for o in scene.objects:
        if o.type=='MESH':o.hide_render=o!=obj
    source=S/'Blender'/theme;source.mkdir(parents=True,exist_ok=True)
    ss=bpy.data.scenes.new(theme+'_Source');ss.collection.objects.link(obj);ss.world=scene.world;ss.camera=scene.camera;ss.render.engine=scene.render.engine
    for o in scene.objects:
        if o.type in ['CAMERA','LIGHT']:ss.collection.objects.link(o)
    ss.render.resolution_x=512;ss.render.resolution_y=512
    bpy.data.libraries.write(str(source/(theme+'.blend')),{ss},fake_user=True);bpy.data.scenes.remove(ss)
    dest=S/'Renders';dest.mkdir(parents=True,exist_ok=True)
    scene.render.filepath=str(dest/(theme+'_Color.png'));bpy.ops.render.render(write_still=True)
    gray=bpy.data.materials.get('ExpansionClay') or bpy.data.materials.new('ExpansionClay');gray.diffuse_color=(.5,.53,.57,1);oldmats=list(obj.data.materials)
    for i in range(len(obj.data.materials)):obj.data.materials[i]=gray
    scene.render.filepath=str(dest/(theme+'_Clay.png'));bpy.ops.render.render(write_still=True)
    wire=obj.modifiers.new('Topology proof','WIREFRAME');wire.thickness=.003;wire.use_replace=True
    scene.render.filepath=str(dest/(theme+'_Wire.png'));bpy.ops.render.render(write_still=True);obj.modifiers.remove(wire)
    for i,m in enumerate(oldmats):obj.data.materials[i]=m
    record={'themeId':theme,'accessoryId':obj.name,'sourceBlend':str((source/(theme+'.blend')).relative_to(P)),'exportPath':str(export.relative_to(P)),'triangles':triangles,'uvLayers':[u.name for u in obj.data.uv_layers],'revision':1,'qaStatus':'exported_pending_Unity','triangleBudgetException':('Deliberate curved volumes and stout joining parts; verify target-device performance separately.' if triangles>1500 else None)}
    file=D/'accessory_library.json';records=json.loads(file.read_text()) if file.exists() else [];records=[r for r in records if r['themeId']!=theme]+[record];file.write_text(json.dumps(records,indent=2))
    bpy.ops.wm.save_as_mainfile(filepath=str(S/'Blender/ExpansionStudio.blend'));print(json.dumps(record));return obj
