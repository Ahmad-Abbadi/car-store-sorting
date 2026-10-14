"""Original toy accessories authored with Blender meshes. Execute in live Blender MCP."""
import bpy,math,json,pathlib
from mathutils import Vector,Matrix
P=pathlib.Path(r'C:\Users\User\Documents\Games_Projects\My_Games_Projects\Car Store Sorting')
S=P/'ArtSource/ThemeAccessories';O=P/'Assets/TinyRiders/Art/ThemeAccessories';D=P/'TinyRidersProduction/ThemeAccessoryPass'
THEMES={t['id']:t for t in json.loads((P/'Assets/TinyRiders/ThemedCollection/Data/Themes/theme_recipes.json').read_text())['themes']}
scene=bpy.data.scenes.get('ThemeAccessoryStudio_R03') or bpy.data.scenes.new('ThemeAccessoryStudio_R03')
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
def pilot_space():
    # Foot is short and domed, physically merged visually into the planet's underside.
    lathe('IntegratedFoot',[(0,.18),(.025,.21),(.08,.16),(.16,.12)],1,n=24)
    sphere('BandedPlanet',(0,0,.43),(.32,.32,.32),'sphere',24,12)
    n=48;v=[];tilt=Matrix.Rotation(math.radians(25),4,'Y')
    for z,r in [(-.025,.37),(-.025,.56),(.025,.56),(.025,.37)]:
        for j in range(n):v.append(tilt@Vector((r*math.cos(j*math.tau/n),r*math.sin(j*math.tau/n),z))+Vector((0,0,.43)))
    f=[]
    for ring in range(4):
        for j in range(n):a=ring*n+j;b=ring*n+(j+1)%n;c=((ring+1)%4)*n+(j+1)%n;dd=((ring+1)%4)*n+j;f.append((a,b,c,dd))
    mesh('BroadTiltedOrbit',v,f,2,True)
    for sign in [-1,1]:
        pts=[tilt@Vector((sign*r,0,0))+Vector((0,0,.43)) for r in [.28,.40]]
        tube('ConcealedOrbitBridge',pts,[.035,.035],2,8)
def material(theme):
    m=bpy.data.materials.get('ACC_MAT_'+theme) or bpy.data.materials.new('ACC_MAT_'+theme);m.use_nodes=True
    bs=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');bs.inputs['Roughness'].default_value=1-THEMES[theme]['smoothness'];bs.inputs['Metallic'].default_value=THEMES[theme]['metallic']*.6
    tex=next((n for n in m.node_tree.nodes if n.type=='TEX_IMAGE'),None) or m.node_tree.nodes.new('ShaderNodeTexImage')
    tex.image=bpy.data.images.load(str(S/'Artwork'/theme/'AccessoryAtlas.png'),check_existing=True)
    m.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color']);return m

def brim(name,radius=.52,uv=0,warp=None):
    return lathe(name,[(0,.24),(.025,radius*.92),(.065,radius),(.10,radius*.94),(.12,.27)],uv,n=40,warp=warp)

def pirate():
    def fold(pt,a,z,r):
        # Three lifted corners and three broad folded side walls, not a conical wizard hat.
        k=(r/.58)**2;rr=r*(1+.13*math.cos(3*a))
        return (rr*math.cos(a),rr*math.sin(a),z+k*(.12+.16*(1-math.cos(3*a))))
    lathe('FoldedTricorn',[(0,.22),(.02,.40),(.06,.58),(.11,.59),(.14,.50),(.13,.29)],'front',n=48,warp=fold)
    lathe('LowHatCrown',[(.07,.29),(.22,.30),(.36,.23),(.40,.09),(.40,.005)],6,n=32)
    lathe('GoldFoldEdge',[(.10,.59),(.13,.59),(.15,.57)],2,n=48,warp=fold,closed_section=True)

def royal():
    n=40;v=[]
    for ring in range(4):
        for j in range(n):
            a=j*math.tau/n;r=.39 if ring in (0,1) else .32
            z=.04 if ring in (0,3) else .33+.19*(.5+.5*math.cos(5*a))**3
            v.append((r*math.cos(a),r*math.sin(a),z))
    f=[]
    for k in range(4):
        for j in range(n):f.append((k*n+j,k*n+(j+1)%n,((k+1)%4)*n+(j+1)%n,((k+1)%4)*n+j))
    mesh('FivePointThickCrown',v,f,2,False,.012)
    lathe('RoundedRoyalBand',[(0,.33),(.035,.42),(.10,.42),(.14,.38)],'front',n=32)

def alien():
    lathe('LensSaucer',[(0,.19),(.06,.25),(.11,.46),(.16,.57),(.22,.52),(.29,.31),(.31,.20)],1,n=32)
    lathe('BrightSaucerRim',[(.15,.56),(.17,.585),(.20,.57)],2,n=32)
    sphere('OpaqueAlienDome',(0,0,.28),(.28,.28,.20),0,24,10)
    for x in [-.13,.13]:
        tube('SturdyEyeStalk',[(x,0,.39),(x*1.25,0,.56)],[.045,.05],1,8)
        sphere('EyePod',(x*1.25,-.01,.57),(.085,.08,.085),3,12,6)

def beach():
    # Closed, gently rockered surfboard with a rounded rail and pointed nose.
    n=32;v=[]
    for side in [-1,1]:
        for j in range(n):
            a=j*math.tau/n;y=.64*math.sin(a);x=.24*math.cos(a)*(1-.2*math.sin(a))
            v.append((x,y,.14+.075*(y/.64)**2+side*.035))
    f=[tuple(range(n-1,-1,-1)),tuple(n+j for j in range(n))]
    f += [(j,(j+1)%n,n+(j+1)%n,n+j) for j in range(n)]
    o=mesh('RockeredSurfboard',v,f,'top',False,.018);o.rotation_euler.z=.25
    for y in [-.24,.24]:box('SecuredRoofRail',(0,y,.06),(.34,.10,.12),1,.03)

def leaf(name,start,end,width,uv):
    # Thick lenticular cross sections form a curved leaf with a strong midrib silhouette.
    a=Vector(start);b=Vector(end);axis=(b-a).normalized();side=axis.cross(Vector((0,0,1))).normalized();up=side.cross(axis).normalized()
    v=[];steps=9;n=8
    for j in range(steps):
        t=j/(steps-1);c=a.lerp(b,t)+Vector((0,0,.18*math.sin(math.pi*t)));w=max(.018,width*math.sin(math.pi*t)**.7)
        for k in range(n):ang=k*math.tau/n;v.append(c+side*w*math.cos(ang)+up*.055*math.sin(ang))
    f=[]
    for j in range(steps-1):
        for k in range(n):f.append((j*n+k,j*n+(k+1)%n,(j+1)*n+(k+1)%n,(j+1)*n+k))
    f.extend([tuple(range(n-1,-1,-1)),tuple((steps-1)*n+k for k in range(n))]);return mesh(name,v,f,uv)

def jungle():
    lathe('LeafRoot',[(0,.12),(.10,.16),(.19,.09)],0,n=16)
    for j,a in enumerate([-.25,1.9,3.8]):leaf('BroadTropicalLeaf',(0,0,.12),(.57*math.cos(a),.57*math.sin(a),.18),.19,'top')

def spooky():
    o=sphere('EightRibPumpkin',(0,0,.31),(.40,.36,.32),'front',32,14)
    for v in o.data.vertices:
        a=math.atan2(v.co.y,v.co.x);k=1+.075*math.cos(8*a);v.co.x*=k;v.co.y*=k
    tube('CrookedStem',[(0,0,.57),(.01,0,.68),(.085,0,.73)],[.075,.065,.045],1,10)

def candy():
    sphere('SweetBody',(0,0,.25),(.34,.25,.25),'front',24,12)
    for sign in [-1,1]:
        points=[(sign*.28,0,.25),(sign*.36,0,.25),(sign*.48,0,.25),(sign*.58,0,.25)]
        o=tube('PleatedWrapper',points,[.12,.085,.20,.23],2,12)
        for v in o.data.vertices:
            a=math.atan2(v.co.z-.25,v.co.y);k=1+.18*math.cos(6*a);v.co.y*=k;v.co.z=.25+(v.co.z-.25)*k

def food():
    lathe('LowerBun',[(0,.22),(.035,.36),(.12,.40),(.18,.37)],7,n=32)
    lathe('BroadPatty',[(.15,.37),(.18,.40),(.25,.39),(.28,.36)],1,n=32)
    lathe('WavyLettuce',[(.25,.32),(.29,.43),(.33,.39)],2,n=32,warp=lambda p,a,z,r:(p[0],p[1],z+.022*math.cos(8*a)))
    lathe('SesameBun',[(.32,.38),(.38,.41),(.48,.36),(.56,.24),(.59,.06),(.59,.001)],'top',n=32)

def fire():
    for j,(pts,rs) in enumerate([
        ([(0,0,.02),(0,0,.17),(-.04,0,.37),(.02,0,.58),(.13,0,.77)],[.17,.24,.20,.11,.008]),
        ([(-.12,0,.06),(-.26,0,.22),(-.30,0,.39),(-.22,0,.51)],[.13,.14,.08,.008]),
        ([(.12,0,.05),(.28,0,.18),(.32,0,.35),(.25,0,.43)],[.12,.14,.07,.008])]):
        o=tube('SweptFlameLobe',pts,rs,[0,2,7][j],16)
        for v in o.data.vertices:v.co.y*=.65

def ice():
    for j,(x,y,h,r,tilt) in enumerate([(0,0,.77,.17,-.10),(-.23,.01,.46,.14,-.35),(.22,.04,.55,.14,.3)]):
        o=lathe('OpaqueFacetedCrystal',[(0,r*.8),(.04,r),(h*.74,r),(h,.015)],j%3,n=6,smooth=False)
        o.rotation_euler.y=tilt;o.location=(x,y,.045 if j else .02)
    # A compact irregular crystalline root, not a separate display plinth.
    lathe('CrystalRoot',[(0,.19),(.055,.30),(.12,.22)],3,n=10,smooth=False)

def ocean():
    # Foil sections in X/Z; the swept profile lives along vehicle length Y.
    rings=[(0,0,.31,.13),(.08,-.02,.34,.14),(.27,.01,.22,.11),(.48,.14,.12,.065),(.67,.28,.018,.018)]
    v=[];n=16
    for z,y,length,thick in rings:
        for j in range(n):a=j*math.tau/n;v.append((thick*math.cos(a),y+length*math.sin(a),z))
    f=[]
    for k in range(len(rings)-1):
        for j in range(n):f.append((k*n+j,k*n+(j+1)%n,(k+1)*n+(j+1)%n,(k+1)*n+j))
    f += [tuple(range(n-1,-1,-1)),tuple((len(rings)-1)*n+j for j in range(n))]
    mesh('SweptDorsalFoil',v,f,1)

def construction():
    brim('HardHatBrim',.49,2,lambda p,a,z,r:(p[0],p[1]*1.12,z))
    lathe('SafetyCrown',[(.07,.34),(.18,.37),(.34,.32),(.45,.20),(.49,.03)],0,n=32)
    tube('BroadReinforcementRidge',[(0,-.31,.17),(0,-.27,.35),(0,-.12,.49),(0,.12,.49),(0,.29,.32),(0,.33,.17)],[.05]*6,2,8)

def rescue():
    box('RoundedBeaconHousing',(0,0,.09),(.90,.28,.18),1,.06)
    for x in [-.33,.33]:sphere('DomedBeacon',(x,0,.19),(.17,.16,.17),2 if x<0 else 3,16,8)
    # Heart is a three-dimensional molded central housing, with a printed pulse.
    n=32;v=[]
    for y in [-.10,.10]:
        for j in range(n):
            a=j*math.tau/n;x=.014*16*math.sin(a)**3;z=.29+.014*(13*math.cos(a)-5*math.cos(2*a)-2*math.cos(3*a)-math.cos(4*a));v.append((x,y,z))
    f=[tuple(range(n-1,-1,-1)),tuple(n+j for j in range(n))]+[(j,(j+1)%n,n+(j+1)%n,n+j) for j in range(n)]
    mesh('MoldedHeartHousing',v,f,'front',False,.025)

def wizard():
    brim('BroadWizardBrim',.46,1)
    tube('BentPointedCrown',[(0,0,.08),(0,0,.24),(-.035,0,.43),(-.04,0,.63),(.04,0,.80),(.20,0,.84)],[.29,.25,.18,.11,.055,.008],'front',24)
    lathe('HatBand',[(.13,.28),(.20,.265),(.22,.25)],2,n=32)

def robot():
    box('ShortNeck',(0,0,.07),(.29,.25,.14),1,.035)
    box('ChamferedSensorHousing',(0,0,.32),(.63,.40,.46),0,.07)
    box('InsetVisor',(0,-.205,.34),(.52,.045,.29),'front',.035)
    for x in [-.36,.36]:box('SideSensorModule',(x,0,.31),(.13,.25,.25),2,.035)
    tube('StoutAntenna',[(.18,0,.51),(.21,0,.65)],[.035,.035],1,8)
    sphere('AntennaCap',(.21,0,.66),(.065,.065,.065),2,12,6)

def music():
    pts=[(.36*math.cos(a),0,.27+.36*math.sin(a)) for a in [j*math.pi/12 for j in range(13)]]
    tube('PaddedHeadphoneArch',pts,[.075]*13,1,10)
    for x in [-.35,.35]:
        box('RoundedEarCup',(x,0,.21),(.22,.31,.39),2,.08)
        box('EarCushion',(x*.75,0,.21),(.08,.25,.29),6,.035)
    box('LowHeadphoneBridge',(0,0,.045),(.60,.15,.09),1,.03)

def sport():
    lathe('BallCradle',[(0,.19),(.07,.22),(.14,.17)],1,n=24)
    sphere('PrintedFootball',(0,0,.38),(.32,.32,.32),'sphere',24,12)

def farm():
    brim('CurvedStrawBrim',.55,'top',lambda p,a,z,r:(p[0],p[1]*.86,z+.035*math.cos(2*a)*(r/.55)**2))
    lathe('PinchedFarmCrown',[(.08,.29),(.16,.30),(.34,.25),(.40,.19),(.41,.005)],'front',n=32,warp=lambda p,a,z,r:(p[0]*(1-.10*math.cos(2*a)),p[1],z))
    lathe('FarmHatBand',[(.13,.303),(.20,.287),(.23,.275)],1,n=32)

def party():
    for j,(x,y,z) in enumerate([(-.22,0,.39),(.23,.03,.43),(0,.15,.59)]):
        tube('ShortIntegratedBalloonSupport',[(x*.3,y*.3,0),(x,y,z-.18)],[.055,.04],1,8)
        sphere('ConfettiBalloon',(x,y,z),(.22,.20,.27),'sphere' if j==2 else j,20,10)
        lathe('BalloonTie',[(0,.06),(.06,.03)],2,n=8).location=(x,y,z-.26)

RECIPES={'PIRATE':pirate,'ROYAL':royal,'SPACE':pilot_space,'ALIEN':alien,'BEACH':beach,'JUNGLE':jungle,'SPOOKY':spooky,'CANDY':candy,'FOOD':food,'FIRE':fire,'ICE':ice,'OCEAN':ocean,'CONSTRUCTION':construction,'RESCUE':rescue,'WIZARD':wizard,'ROBOT':robot,'MUSIC':music,'SPORT':sport,'FARM':farm,'PARTY':party}

def save_editable_source(obj,theme,path):
    # A real scene opens directly in Blender, with the original atlas packed for portability.
    for mat in obj.data.materials:
        for node in mat.node_tree.nodes:
            if node.type=='TEX_IMAGE' and node.image:node.image.pack()
    source_scene=bpy.data.scenes.new(theme+'_EditableSource');source_scene.collection.objects.link(obj);source_scene.world=scene.world
    camera=bpy.data.objects.new(theme+'_ReviewCamera',scene.camera.data.copy());source_scene.collection.objects.link(camera)
    camera.location=(1.7,-2.4,1.6);camera.rotation_euler=(Vector((0,0,.35))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=1.7;source_scene.camera=camera
    for light in scene.objects:
        if light.type=='LIGHT':source_scene.collection.objects.link(light)
    source_scene.render.engine=scene.render.engine;source_scene.render.resolution_x=640;source_scene.render.resolution_y=640
    bpy.data.libraries.write(str(path),{source_scene},fake_user=True)
    bpy.data.scenes.remove(source_scene);bpy.data.objects.remove(camera,do_unlink=True)
def build(theme):
    global PARTS,MAT,COL
    existing=bpy.data.collections.get(theme)
    if existing:
        for o in list(existing.objects):bpy.data.objects.remove(o,do_unlink=True)
        bpy.data.collections.remove(existing)
    COL=bpy.data.collections.new(theme);scene.collection.children.link(COL);PARTS=[];MAT=material(theme)
    RECIPES[theme[3:]]()
    # One shared accessory mesh per kit, retaining real volume and printed UV detail.
    bpy.ops.object.select_all(action='DESELECT')
    for o in PARTS:o.select_set(True)
    bpy.context.view_layer.objects.active=PARTS[0]
    if len(PARTS)>1:bpy.ops.object.join()
    obj=bpy.context.object;obj.name='ACC_'+theme+'_Primary'
    import bmesh
    bm=bmesh.new();bm.from_mesh(obj.data);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(obj.data);bm.free()
    scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    obj.data.calc_loop_triangles();triangles=len(obj.data.loop_triangles)
    target=O/'Meshes'/theme;target.mkdir(parents=True,exist_ok=True)
    export=target/(obj.name+'.fbx')
    bpy.ops.export_scene.fbx(filepath=str(export),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=False,use_mesh_modifiers=True,add_leaf_bones=False,path_mode='STRIP')
    source=S/'Blender';source.mkdir(parents=True,exist_ok=True)
    # Store editable sources independently of the studio scene's later presentation offsets.
    save_editable_source(obj,theme,source/(theme+'.blend'))
    bounds=[list(v) for v in obj.bound_box];record={'accessoryId':'ACC-'+theme+'-PRIMARY','themeId':theme,'role':'primary','sourceBlend':str((source/(theme+'.blend')).relative_to(P)).replace('\\','/'),'exportPath':str(export.relative_to(P)).replace('\\','/'),'triangles':triangles,'boundsMin':[min(v[i] for v in bounds) for i in range(3)],'boundsMax':[max(v[i] for v in bounds) for i in range(3)],'pivot':'bottom-center; Blender Z up; FBX Y up / -Z forward','atlasPath':str((O/'Textures'/theme/'AccessoryAtlas.png').relative_to(P)).replace('\\','/'),'contentRevision':1,'validationStatus':'modeled_exported_pending_Unity_fit'}
    path=D/'theme_accessory_library.json';records=json.loads(path.read_text()) if path.exists() else []
    records=[r for r in records if r['themeId']!=theme]+[record];path.write_text(json.dumps(records,indent=2))
    bpy.ops.wm.save_as_mainfile(filepath=str(source/'ThemeAccessories_Studio.blend'))
    print(json.dumps(record));return obj
