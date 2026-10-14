"""Twelve original physical wheel designs, reusable at a centred +Y axle socket.

Rims, spokes and tread are physical shapes. No logos, numbers or decorative shine meshes.
The first three preserve the reviewed M01/M03/M02 turned profiles. New designs have
different spoke structures, profiles and tire categories. All labels are metadata/2D only.
"""
from pathlib import Path
import sys,math,json
sys.path.insert(0,str(Path(__file__).resolve().parent))
import bpy,bmesh
from mathutils import Vector
import build_m03 as geo
import build_m01,build_m02
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Blender/Shared';EXP=ROOT/'Exports/Shared/Wheels';PRE=ROOT/'Previews/Wheels'
for path in [OUT,EXP,PRE]:path.mkdir(parents=True,exist_ok=True)
S=.025
DESIGNS=[
 ('W01','City deep dish','city',.585,.40),
 ('W02','Track inset rim','sport',.590,.46),
 ('W03','Utility pressed cap','utility',.550,.38),
 ('W04','Five spoke sport','sport',.620,.44),
 ('W05','Six arm trail','allterrain',.700,.52),
 ('W06','Vented aero disc','sport',.610,.44),
 ('W07','Vintage tri bar','classic',.620,.42),
 ('W08','Swept turbine','sport',.630,.46),
 ('W09','Rally multi spoke','rally',.660,.50),
 ('W10','Utility beadlock','allterrain',.720,.55),
 ('W11','Monster cross hub','monster',.940,.74),
 ('W12','Future split triad','future',.650,.46),
]

def ring(name,profile,mat,n=24):return geo.lathe_y(name,0,0,0,profile,mat,n)

def radial_prism(name,points,back,front,mat,bevel=.01):
    # points are 2D (x,z); axial thickness is real physical geometry.
    k=len(points);verts=[(x,y,z) for y in (back,front) for x,z in points]
    faces=[tuple(reversed(range(k))),tuple(range(k,2*k))]
    faces.extend((j,(j+1)%k,(j+1)%k+k,j+k) for j in range(k))
    ob=geo.mesh(name,verts,faces,mat);geo.finish(ob,bevel,2);return ob

def rotated(points,a):
    return [(x*math.cos(a)-z*math.sin(a),x*math.sin(a)+z*math.cos(a)) for x,z in points]

def vented_disc(front,radius):
    """Six true annular vent openings from a shared-vertex surface, without booleans."""
    n=48;rings=[radius*.08,radius*.54,radius*.77,radius*.90]
    verts=[(r*math.cos(j*2*math.pi/n),y,r*math.sin(j*2*math.pi/n))
           for y in (front-.09,front-.024) for r in rings for j in range(n)]
    k=len(rings)*n;surface=[]
    for ring_index in range(3):
        for j in range(n):
            if ring_index==1 and j%8 in (2,3,4):continue
            surface.append((ring_index*n+j,ring_index*n+(j+1)%n,(ring_index+1)*n+(j+1)%n,(ring_index+1)*n+j))
    edges={};faces=[]
    for face in surface:
        faces.append(tuple(reversed(face)));faces.append(tuple(i+k for i in face))
        for i,a in enumerate(face):
            b=face[(i+1)%4];key=tuple(sorted((a,b)));edges.setdefault(key,[]).append((a,b))
    for pair in edges.values():
        if len(pair)==1:
            a,b=pair[0];faces.append((a,b,b+k,a+k))
    ob=geo.mesh('Aero_vented_disc',verts,faces,ALLOY);geo.finish(ob,.004,1)

def spoke_set(name,count,inner,outer,width,back,front,mat,sweep=0,split=False):
    for j in range(count):
        a=2*math.pi*j/count
        if split:
            for side in [-1,1]:
                pts=[(inner,-width*.34),(outer,-width*.24+side*.08),
                     (outer,width*.24+side*.08),(inner,width*.34)]
                radial_prism(name+f'_{j}_{side}',rotated(pts,a),back,front,mat,.008)
        else:
            pts=[(inner,-width*.50),(outer,-width*.38+sweep),(outer,width*.38+sweep),(inner,width*.50)]
            radial_prism(name+f'_{j}',rotated(pts,a),back,front,mat,.01)

def tire(radius,width,kind):
    half=width/2;hole=radius*.57
    profile=[(-half,hole),(-half,radius*.79),(-half*.72,radius*.94),(-half*.40,radius),
             (half*.40,radius),(half*.72,radius*.94),(half,radius*.79),(half,hole)]
    ring('Tire',profile,RUBBER,28 if kind in ('classic','city') else 24)
    if kind in ('allterrain','monster','rally'):
        # Actual raised tread lugs, embedded into the tire shoulders.
        count=16 if kind=='monster' else 20
        depth=.045 if kind=='monster' else .025
        for j in range(count):
            a=2*math.pi*j/count
            for side in [-1,1]:
                # Chunky chevrons span tread and have alternating axial offsets.
                ob=geo.solid(f'Tread_{j}_{side}',(0,0,0),(.12 if kind=='monster' else .075,width*.44,depth),RUBBER,.008)
                next(m for m in ob.modifiers if m.type=='BEVEL').segments=1
                # Rotate authored vertices, not object transforms, so axle pivots stay zero.
                for v in ob.data.vertices:
                    x,y,z=v.co;x+=side*.016;y+=side*width*.23;z+=radius-depth*.20
                    v.co=(x*math.cos(a)+z*math.sin(a),y,-x*math.sin(a)+z*math.cos(a))

def new_wheel(wid,kind,r,width):
    tire(r,width,kind);f=width/2+.012;R=r*.70
    ring('Rim_outer_lip',[(f-.02,R*.73),(f+.008,R*.95),(f-.007,R),(f-.055,R),
                        (f-.065,R*.83),(f-.045,R*.73)],ALLOY)
    ring('Rim_barrel',[(f-.075,R*.78),(f-.075,R*.94),(-f*.65,R*.94),(-f*.65,R*.78)],DARK)
    if wid=='W04':spoke_set('Five_spoke',5,R*.20,R*.90,.13,f-.09,f-.027,ALLOY)
    elif wid=='W05':spoke_set('Six_arm',6,R*.18,R*.90,.17,f-.11,f-.025,GOLD)
    elif wid=='W06':
        vented_disc(f,R)
    elif wid=='W07':
        ring('Vintage_dish',[(f-.055,.015),(f-.055,R*.83),(f-.09,R*.90),(f-.12,R*.85),(f-.12,.015)],DARK)
        spoke_set('Tri_bar',3,R*.08,R*.74,.14,f-.053,f+.016,ALLOY)
    elif wid=='W08':spoke_set('Swept_blade',8,R*.22,R*.89,.085,f-.11,f-.024,ALLOY,sweep=.095)
    elif wid=='W09':spoke_set('Rally_spoke',12,R*.19,R*.90,.055,f-.10,f-.03,CREAM)
    elif wid=='W10':
        spoke_set('Beadlock_arm',8,R*.17,R*.88,.115,f-.10,f-.03,DARK)
        ring('Beadlock_retaining_ring',[(f+.014,R*.75),(f+.014,R*.96),(f-.005,R*.98),(f-.015,R*.75)],GOLD)
        for j in range(8):
            a=j*math.pi/4
            geo.lathe_y(f'Beadlock_bolt_{j}',R*.86*math.cos(a),0,R*.86*math.sin(a),
                [(f+.028,.006),(f+.028,.024),(f+.008,.024),(f+.008,.006)],ALLOY,6)
    elif wid=='W11':
        spoke_set('Monster_heavy_cross',5,R*.18,R*.90,.23,f-.15,f-.027,ALLOY)
        ring('Monster_inner_ring',[(f-.016,R*.40),(f-.016,R*.48),(f-.09,R*.48),(f-.09,R*.40)],GOLD)
    elif wid=='W12':
        spoke_set('Split_triad',3,R*.18,R*.91,.08,f-.10,f-.024,ALLOY,split=True)
        radial_prism('Triangular_hub',[(R*.32,0),(-R*.16,R*.277),(-R*.16,-R*.277)],f-.09,f+.014,GOLD,.022)
    ring('Axle_cap',[(f+.012,.008),(f+.012,R*.19),(f-.013,R*.245),(f-.07,R*.245),(f-.07,.008)],GOLD if wid in ('W05','W08','W11') else ALLOY,20)

def build():
    global RUBBER,ALLOY,DARK,GOLD,CREAM
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    RUBBER=geo.material('Wheel_Rubber',(.009,.012,.017),.65)
    ALLOY=geo.material('Wheel_Alloy',(.34,.39,.46),.34,.65)
    DARK=geo.material('Wheel_Graphite',(.022,.03,.045),.46,.2)
    GOLD=geo.material('Wheel_Gold',(.80,.42,.028),.36,.45)
    CREAM=geo.material('Wheel_Ivory',(.9,.84,.69),.40,.2)
    roots=[];records=[]
    for wid,label,kind,radius,width in DESIGNS:
        old=set(bpy.context.scene.objects)
        if wid=='W01':build_m01.wheel(0,0,'Library',RUBBER,ALLOY,DARK)
        elif wid=='W02':
            geo.RUBBER=RUBBER;geo.ALLOY=ALLOY;geo.HUB=DARK;geo.make_wheel(0,0,'Library')
        elif wid=='W03':build_m02.utility_wheel(0,0,'Library',RUBBER,GOLD,DARK)
        else:new_wheel(wid,kind,radius,width)
        parts=[o for o in bpy.context.scene.objects if o not in old and o.type=='MESH']
        root=bpy.data.objects.new(wid+'_AXLE_ROOT',None);bpy.context.collection.objects.link(root);roots.append(root)
        root['wheelId']=wid;root['label']=label;root['axleDirection']='+Y';root['radiusMetres']=radius*S
        rows=[];failures=[];min_z=1;max_radial=0
        for ob in parts:
            ob.name=wid+'_'+ob.name;ob.parent=root;geo.generic_uv(ob)
            for v in ob.data.vertices:
                if wid in ('W01','W02','W03'):v.co.z-=radius
                v.co*=S
            for mod in ob.modifiers:
                if mod.type=='BEVEL':mod.width*=S
            ob.modifiers.new('Explicit export triangulation','TRIANGULATE')
        bpy.context.view_layer.update();deps=bpy.context.evaluated_depsgraph_get()
        for ob in parts:
            ev=ob.evaluated_get(deps);data=ev.to_mesh();data.calc_loop_triangles();bm=bmesh.new();bm.from_mesh(data)
            bad=sum(not e.is_manifold for e in bm.edges);deg=sum(f.calc_area()<1e-13 for f in bm.faces);vol=bm.calc_volume(signed=True)
            min_z=min(min_z,min(v.co.z for v in data.vertices))
            max_radial=max(max_radial,max(math.hypot(v.co.x,v.co.z) for v in data.vertices))
            if bad or deg or vol<=0:failures.append({'part':ob.name,'nonManifold':bad,'degenerate':deg,'volume':vol})
            rows.append({'name':ob.name,'triangles':len(data.loop_triangles),'uvLayers':len(data.uv_layers)})
            bm.free();ev.to_mesh_clear()
        bpy.ops.object.select_all(action='DESELECT')
        for ob in [root,*parts]:ob.select_set(True)
        bpy.ops.export_scene.fbx(filepath=str(EXP/(wid+'.fbx')),use_selection=True,object_types={'MESH','EMPTY'},
            axis_forward='-Z',axis_up='Y',use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False)
        records.append({'wheelId':wid,'name':label,'tireCategory':kind,'radiusMetres':radius*S,'widthMetres':width*S,
            'axleHeightForGroundM':-min_z,'maximumRadialEnvelopeM':max_radial,
            'axle':'local +Y; mount at origin; mirror on the opposite side','triangles':sum(x['triangles'] for x in rows),
            'parts':rows,'technicalFailures':failures,'source':'Blender/Shared/Wheels_12.blend','export':'Exports/Shared/Wheels/'+wid+'.fbx',
            'status':'COMPONENT_CANDIDATE_REQUIRES_VISUAL_REVIEW'})
    # Real meshes are arranged in a source library; FBX exports retain zero-centred axle pivots.
    for i,root in enumerate(roots):root.location=((3-i%4)*.055,0,-(i//4)*.060)
    scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.render.engine='CYCLES';scene.cycles.samples=48;scene.cycles.use_denoising=True
    scene.render.resolution_x=1600;scene.render.resolution_y=1250;scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA';scene.render.film_transparent=True
    scene.view_settings.view_transform='AgX';scene.view_settings.look='AgX - Medium High Contrast'
    scene.world.use_nodes=True;bg=next(n for n in scene.world.node_tree.nodes if n.type=='BACKGROUND')
    bg.inputs[0].default_value=(.3,.36,.45,1);bg.inputs[1].default_value=.45
    for name,loc,energy,size in [('Key',(-.1,.16,.2),2.5,.20),('Fill',(.22,.10,.03),1.5,.15)]:
        data=bpy.data.lights.new(name,'AREA');data.energy=energy;data.shape='DISK';data.size=size
        ob=bpy.data.objects.new(name,data);scene.collection.objects.link(ob);ob.location=loc
        ob.rotation_euler=(Vector((.08,0,-.05))-ob.location).to_track_quat('-Z','Y').to_euler()
    camera_data=bpy.data.cameras.new('WheelLibraryCamera');camera=bpy.data.objects.new('WheelLibraryCamera',camera_data);scene.collection.objects.link(camera)
    camera.location=(.10,.5,-.04);camera.rotation_euler=(Vector((.0825,0,-.06))-camera.location).to_track_quat('-Z','Y').to_euler()
    camera_data.type='ORTHO';camera_data.ortho_scale=.235;camera_data.clip_start=.001;scene.camera=camera
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Wheels_12.blend'))
    (PRE/'technical_audit.json').write_text(json.dumps({'componentCount':12,'components':records,'allTechnicalPass':all(not x['technicalFailures'] for x in records)},indent=2))
    scene.render.filepath=str(PRE/'library_render.png');bpy.ops.render.render(write_still=True)
    # Three-quarter evidence reveals actual spoke depth, tread and axle orientation.
    camera.location=(.26,.40,.08);camera.rotation_euler=(Vector((.0825,0,-.06))-camera.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=str(PRE/'library_depth.png');bpy.ops.render.render(write_still=True)
    print('WHEEL_LIBRARY_DONE',[(r['wheelId'],r['triangles'],len(r['technicalFailures'])) for r in records],flush=True)

if __name__=='__main__':build()
