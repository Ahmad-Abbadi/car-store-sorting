"""Original M03 coupe: designed section mesh, cut arches, formed tire profiles.

This is an inspectable candidate, never auto-approved by file existence.
Run Blender --background --python this_file.py [-- --views three_quarter,clay].
Modeling dimensions are authored in 25 mm design units then applied to metres.
"""
import argparse
import json
import math
import sys
from pathlib import Path
import bpy
import bmesh
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(Path(__file__).resolve().parent))
OUT=ROOT/'Blender'/'Candidates'; OUT.mkdir(parents=True,exist_ok=True)
PRE=ROOT/'Previews'/'M03'; PRE.mkdir(parents=True,exist_ok=True)
EXPORT=ROOT/'Exports'/'Candidates'; EXPORT.mkdir(parents=True,exist_ok=True)
SCALE=.025

def material(name,color,rough=.33,metal=0,texture=None):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    bs=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    bs.inputs['Base Color'].default_value=(*color,1)
    bs.inputs['Roughness'].default_value=rough
    bs.inputs['Metallic'].default_value=metal
    bs.inputs['Coat Weight'].default_value=.20 if 'Paint' in name else .0
    bs.inputs['Coat Roughness'].default_value=.24
    if texture:
        n=m.node_tree.nodes.new('ShaderNodeTexImage'); n.image=bpy.data.images.load(str(texture))
        m.node_tree.links.new(n.outputs['Color'],bs.inputs['Base Color'])
    return m

def mesh(name,verts,faces,mat):
    data=bpy.data.meshes.new(name); data.from_pydata(verts,[],faces); data.update()
    ob=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(ob)
    ob.data.materials.append(mat)
    bpy.context.view_layer.objects.active=ob; ob.select_set(True)
    # Recalculate all authored solid surface normals consistently.
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.normals_make_consistent(inside=False); bpy.ops.object.mode_set(mode='OBJECT')
    ob.select_set(False)
    return ob

def finish(ob,width=.045,segments=3,smooth=True):
    b=ob.modifiers.new('Controlled molded edge radii','BEVEL'); b.width=width; b.segments=segments
    b.limit_method='ANGLE'; b.angle_limit=.36; b.harden_normals=True
    if smooth:
        for p in ob.data.polygons: p.use_smooth=True
    n=ob.modifiers.new('Broad surface normals','WEIGHTED_NORMAL'); n.keep_sharp=True; n.weight=45
    return ob

def section_mesh(name,sections,mat):
    """Continuous manufactured hull: x, halfwidth, bottom, shoulder, crown."""
    verts=[]
    for x,w,b,t,c in sections:
        lower=min(.10,(t-b)*.22); upper=min(.17,(t-b)*.35)
        verts.extend([(x,-w+.10,b),(x,w-.10,b),(x,w,b+lower),(x,w,t-upper),
                      (x,w-.07,t-.04),(x,w-.23,t+c),(x,-w+.23,t+c),
                      (x,-w+.07,t-.04),(x,-w,t-upper),(x,-w,b+lower)])
    k=10; faces=[tuple(reversed(range(k)))]
    for s in range(len(sections)-1):
        for j in range(k): faces.append((s*k+j,s*k+(j+1)%k,(s+1)*k+(j+1)%k,(s+1)*k+j))
    faces.append(tuple((len(sections)-1)*k+j for j in range(k)))
    return mesh(name,verts,faces,mat)

def cylinder_cut(ob,x,z,r,cut_material,segments=48):
    bpy.ops.mesh.primitive_cylinder_add(vertices=segments,radius=r,depth=3.2,location=(x,0,z),rotation=(math.pi/2,0,0))
    cutter=bpy.context.object; cutter.name='ARCH_TOOL'
    cutter.data.materials.append(cut_material)
    bpy.context.view_layer.objects.active=ob
    m=ob.modifiers.new('Real wheel arch clearance','BOOLEAN'); m.operation='DIFFERENCE'; m.solver='EXACT'; m.object=cutter
    m.material_mode='TRANSFER'
    bpy.ops.object.modifier_apply(modifier=m.name)
    bpy.data.objects.remove(cutter,do_unlink=True)

def uv_project_body(ob):
    """Face-directed, non-mirrored artwork on the physical shell, no decal mesh."""
    uv=ob.data.uv_layers.new(name='PrintUV')
    rects={'top':(0,0,1024,768),'left':(0,800,1024,1300),'right':(1024,800,2048,1300),
           'front':(0,1350,1024,2000),'back':(1024,1350,2048,2000)}
    for p in ob.data.polygons:
        if p.index==0: zone='front'
        elif p.index==len(ob.data.polygons)-1: zone='back'
        else:
            strip=(p.index-1)%10
            zone='right' if strip in (1,2) else 'left' if strip in (8,9) else 'top'
        x0,y0,x1,y1=rects[zone]
        for li in p.loop_indices:
            x,y,z=ob.data.vertices[ob.data.loops[li].vertex_index].co
            if zone=='top': a=(x+1.83)/3.68; b=(1.03-y)/2.06
            elif zone in ('left','right'):
                a=(x+1.83)/3.68; a=1-a if zone=='right' else a; b=(1.30-z)/.95
            else: a=(-y+1.03)/2.06 if zone=='front' else (y+1.03)/2.06; b=(1.30-z)/.95
            a=max(.002,min(.998,a)); b=max(.002,min(.998,b))
            uv.data[li].uv=((x0+a*(x1-x0))/2048,1-(y0+b*(y1-y0))/2048)

def front_recess(ob,y,z,width,height):
    """Shallow molded fascia pocket, UV artwork transferred onto its real surface."""
    cutter=solid('FASCIA_RECESS_TOOL',(-1.825,y,z),(.13,width,height),PAINT,.025)
    bpy.context.view_layer.objects.active=cutter
    for mod in list(cutter.modifiers): bpy.ops.object.modifier_apply(modifier=mod.name)
    uv=cutter.data.uv_layers.new(name='PrintUV')
    for poly in cutter.data.polygons:
        for li in poly.loop_indices:
            co=cutter.data.vertices[cutter.data.loops[li].vertex_index].co
            a=(-co.y+1.03)/2.06; b=(1.30-co.z)/.95
            uv.data[li].uv=(a*.5,1-(1350+b*650)/2048)
    bpy.context.view_layer.objects.active=ob
    mod=ob.modifiers.new('Integrated fascia recess','BOOLEAN'); mod.operation='DIFFERENCE'; mod.solver='EXACT'; mod.object=cutter
    mod.material_mode='TRANSFER'; bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.data.objects.remove(cutter,do_unlink=True)

def solid(name,loc,size,mat,bevel=.04):
    x,y,z=loc; a,b,c=[v/2 for v in size]
    verts=[(x+sx*a,y+sy*b,z+sz*c) for sz in (-1,1) for sy in (-1,1) for sx in (-1,1)]
    ob=mesh(name,verts,[(0,2,3,1),(4,5,7,6),(0,1,5,4),(2,6,7,3),(0,4,6,2),(1,3,7,5)],mat)
    return finish(ob,bevel)

def lathe_y(name,xc,yc,zc,profile,mat,segments=40):
    """Axial/radial section creates a thick molded ring with rounded shoulders."""
    verts=[]
    for axial,radius in profile:
        for j in range(segments):
            a=2*math.pi*j/segments
            verts.append((xc+radius*math.cos(a),yc+axial,zc+radius*math.sin(a)))
    faces=[]
    for i in range(len(profile)):
        for j in range(segments): faces.append((i*segments+j,i*segments+(j+1)%segments,
                    ((i+1)%len(profile))*segments+(j+1)%segments,((i+1)%len(profile))*segments+j))
    ob=mesh(name,verts,faces,mat)
    for p in ob.data.polygons: p.use_smooth=True
    if 'Tire' not in name: ob.data.set_sharp_from_angle(angle=.65)
    return ob

def make_wheel(x,y,label):
    tire_profile=[(-.23,.35),(-.23,.47),(-.16,.56),(-.08,.590),
                  (.08,.590),(.16,.56),(.23,.47),(.23,.35)]
    lathe_y('WHEEL_'+label+'_Tire',x,y,.59,tire_profile,RUBBER,24)
    side=-1 if y<0 else 1
    # Deep dish with a modest silver outer lip and clean dark central hub.
    prof=[(side*a,r) for a,r in [(.235,.34),(.239,.43),(.22,.455),(.196,.45),(.183,.42),(.19,.34)]]
    lathe_y('WHEEL_'+label+'_RimLip',x,y,.59,prof,ALLOY,20)
    prof=[(side*a,r) for a,r in [(.215,.02),(.215,.35),(.15,.35),(.15,.02)]]
    lathe_y('WHEEL_'+label+'_InsetHub',x,y,.59,prof,HUB,20)
    prof=[(side*a,r) for a,r in [(.22,.02),(.22,.11),(.205,.125),(.18,.11),(.18,.02)]]
    lathe_y('WHEEL_'+label+'_AxleCap',x,y,.59,prof,ALLOY,16)

def make_cabin():
    # The roof narrows toward the rear, and the windshield is purposefully raked.
    v=[(-.57,-.79,1.15),(-.57,.79,1.15),(1.04,.79,1.15),(1.04,-.79,1.15),
       (-.13,-.64,1.87),(-.13,.64,1.87),(.63,.64,1.87),(.63,-.64,1.87)]
    faces=[(0,1,5,4),(3,0,4,7),(1,2,6,5),(2,3,7,6),(4,5,6,7),(3,2,1,0)]
    ob=mesh('CABIN_RakedCoupe',v,faces,CABIN)
    uv=ob.data.uv_layers.new(name='PrintedWindowUV')
    # Map orientation explicitly by vertex to prevent mirrored faces after normal repair.
    maps=[{0:(0,1),1:(1,1),5:(1,0),4:(0,0)},
          {3:(1,1),0:(0,1),4:(0,0),7:(1,0)},
          {1:(1,1),2:(0,1),6:(0,0),5:(1,0)},
          {2:(0,1),3:(1,1),7:(1,0),6:(0,0)},
          {4:(0,1),5:(0,0),6:(1,0),7:(1,1)}]
    for p in ob.data.polygons:
        i=p.index; rect=(i*512,0,(i+1)*512,512) if i<4 else (0,512,1024,1024)
        if i==5: rect=(1700,800,1750,850)
        for li in p.loop_indices:
            vi=ob.data.loops[li].vertex_index; a,b=maps[i].get(vi,(0,0)) if i<5 else (0,0)
            if i in (1,2):
                co=ob.data.vertices[vi].co; a=(co.x+.57)/1.61; b=(1.87-co.z)/.72
                if i==2: a=1-a
            x0,y0,x1,y1=rect; uv.data[li].uv=((x0+a*(x1-x0))/2048,1-(y0+b*(y1-y0))/1024)
    finish(ob,.055,3)
    return ob

def generic_uv(ob):
    if ob.data.uv_layers: return
    uv=ob.data.uv_layers.new(name='SurfaceUV')
    for p in ob.data.polygons:
        n=p.normal; axes=(0,2) if abs(n.y)>.5 else (1,2) if abs(n.x)>.5 else (0,1)
        for li in p.loop_indices:
            co=ob.data.vertices[ob.data.loops[li].vertex_index].co
            uv.data[li].uv=((co[axes[0]]+2)/4,(co[axes[1]]+2)/4)

def camera(name,position,target,ortho):
    data=bpy.data.cameras.new(name); ob=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(ob)
    ob.location=Vector(position)*SCALE; ob.rotation_euler=(Vector(target)*SCALE-ob.location).to_track_quat('-Z','Y').to_euler()
    data.type='ORTHO'; data.ortho_scale=ortho*SCALE; data.lens=55; data.clip_start=.001; data.clip_end=10
    return ob

def area(name,location,energy,size,color):
    data=bpy.data.lights.new(name,'AREA'); data.energy=energy; data.shape='DISK'; data.size=size*SCALE; data.color=color
    ob=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(ob)
    ob.location=Vector(location)*SCALE; ob.rotation_euler=(Vector((0,0,.8))*SCALE-ob.location).to_track_quat('-Z','Y').to_euler()

def audit(objects,master_id='M03',recipe=None,source_script=None):
    deps=bpy.context.evaluated_depsgraph_get(); rows=[]; trees={}; failures=[]
    for ob in objects:
        evaluated=ob.evaluated_get(deps); data=evaluated.to_mesh(); data.calc_loop_triangles()
        bm=bmesh.new(); bm.from_mesh(data)
        bad_edges=sum(not e.is_manifold for e in bm.edges)
        degenerate=sum(f.calc_area()<1e-13 for f in bm.faces)
        volume=bm.calc_volume(signed=True)
        bm.free()
        trees[ob.name]=BVHTree.FromPolygons([ob.matrix_world@v.co for v in data.vertices],
                                          [list(p.vertices) for p in data.polygons])
        if bad_edges: failures.append(ob.name+': nonmanifold evaluated edges '+str(bad_edges))
        if degenerate: failures.append(ob.name+': zero-area faces '+str(degenerate))
        if volume<=0: failures.append(ob.name+': inverted or empty signed volume')
        rows.append({'name':ob.name,'triangles':len(data.loop_triangles),'uvLayers':len(data.uv_layers),
                     'materials':[m.name for m in ob.data.materials],'dimensionsM':list(ob.dimensions),
                     'nonManifoldEdges':bad_edges,'degenerateFaces':degenerate,'signedVolumeM3':volume})
        evaluated.to_mesh_clear()
    for wheel in [o for o in objects if o.name.endswith('_Tire')]:
        for body in [o for o in objects if o.name.startswith(('BODY_','FRONT_MODULE_','REAR_MODULE_','CHASSIS_'))]:
            if trees[wheel.name].overlap(trees[body.name]): failures.append(wheel.name+' intersects '+body.name)
    result={'masterId':master_id,'status':'CANDIDATE_REQUIRES_VISUAL_REVIEW','source':'original Blender custom mesh construction',
            'sourceScript':source_script or str(Path(__file__).relative_to(ROOT)), 'blenderVersion':bpy.app.version_string,
            'coordinateSystem':'metres, front -X, up +Z; FBX export forward -Z up Y',
            'recipe':recipe or 'sectioned broad coupe shell, Boolean-cut semicircular arches, raked narrowing cabin, sculpted bumper belts, mounted wing, turned tire/rim sections',
            'objects':rows,'triangles':sum(r['triangles'] for r in rows),'fontObjects':[o.name for o in bpy.data.objects if o.type=='FONT'],
            'wheelGroundM':min((o.matrix_world@Vector(v)).z for o in objects if o.name.endswith('_Tire') for v in o.bound_box),
            'technicalFailures':failures,'technicalPass':not failures,'completedMasterCount':0}
    (PRE/'technical_audit.json').write_text(json.dumps(result,indent=2),encoding='utf8')

def build():
    global PAINT,CABIN,RUBBER,ALLOY,HUB
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
    PAINT=material('M03_Paint_Print',(.006,.12,.86),.29,texture=ROOT/'Textures/Candidates/M03_body.png')
    CABIN=material('M03_Cabin_Print',(.006,.12,.86),.3,texture=ROOT/'Textures/Candidates/M03_cabin.png')
    plain=material('M03_Paint_Solid',(.007,.12,.86),.29)
    RUBBER=material('Rubber_Satin',(.009,.011,.015),.62)
    ALLOY=material('Wheel_Aluminum',(.36,.42,.50),.28,.72)
    HUB=material('Wheel_DeepGraphite',(.027,.035,.050),.34,.30)
    chassis_mat=material('Underbody_Graphite',(.023,.028,.038),.5)
    sections=[(-1.83,.85,.36,1.08,.02),(-1.74,.92,.35,1.18,.02),(-1.54,.99,.35,1.29,.025),
              (-1.17,1.015,.35,1.35,.015),(-.79,.985,.35,1.29,.02),(-.48,.92,.35,1.18,.02),
              (.03,.91,.35,1.17,.02),(.63,.96,.35,1.28,.015),(1.12,1.01,.35,1.35,.015),
              (1.51,.975,.35,1.27,.01),(1.85,.87,.38,1.16,.01)]
    body=section_mesh('BODY_ArchSculptedCoupe',sections,PAINT)
    uv_project_body(body)
    for x in (-1.13,1.11): cylinder_cut(body,x,.59,.661,plain,32)
    for y in (-.605,.605): front_recess(body,y,.885,.36,.31)
    front_recess(body,0,.68,.80,.22)
    finish(body,.045,2)
    make_cabin()
    # These belt shapes follow the hull, rather than protruding rectangular bumpers.
    front=section_mesh('FRONT_MODULE_FormedBumper',[(-1.865,.79,.32,.57,0),(-1.80,.88,.30,.58,0),(-1.62,.90,.32,.59,0)],plain)
    cylinder_cut(front,-1.13,.59,.661,plain,32)
    finish(front,.042,2)
    rear=section_mesh('REAR_MODULE_FormedBumper',[(1.64,.92,.34,.61,0),(1.84,.88,.33,.61,0),(1.92,.78,.37,.58,0)],plain)
    cylinder_cut(rear,1.11,.59,.661,plain,32)
    finish(rear,.04,2)
    solid('CHASSIS_CentralSpine',(0,0,.30),(2.9,1.37,.22),chassis_mat,.07)
    # Stout supports are embedded into the rear deck and wing.
    for y in (-.64,.64):
        solid('WING_Mount_L' if y<0 else 'WING_Mount_R',(1.40,y,1.46),(.20,.18,.62),plain,.035)
    wing=mesh('WING_CurvedBlade',[(1.23,-1.03,1.72),(1.56,-1.03,1.72),(1.56,1.03,1.72),(1.23,1.03,1.72),
               (1.21,-1.03,1.85),(1.58,-1.03,1.82),(1.58,1.03,1.82),(1.21,1.03,1.85)],
               [(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],plain)
    finish(wing,.04,3)
    for y in (-1.055,1.055): solid('WING_EndPlate_L' if y<0 else 'WING_EndPlate_R',(1.39,y,1.78),(.43,.105,.20),plain,.035)
    for x,ax in [(-1.13,'F'),(1.11,'R')]:
        for y,side in [(-1.02,'L'),(1.02,'R')]: make_wheel(x,y,ax+side)
    from candidate_tools import finish_candidate
    finish_candidate('M03','TrackCoupe',
        'Sectioned broad coupe shell, cut arches, raked cabin, formed fascia and bumper belts, mounted wing and sport wheels',
        {'ATTACH_ROOF':(.30,0,1.9),'ATTACH_REAR':(1.4,0,1.2),
         'AXLE_FRONT':(-1.13,0,.59),'AXLE_REAR':(1.11,0,.59)})

if __name__=='__main__': build()
