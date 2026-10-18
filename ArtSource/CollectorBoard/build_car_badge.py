"""Author an enamel collector badge in Blender; export evaluated meshes for Unity.

Run with Blender --background --factory-startup --python this_file.
The export explicitly maps Blender X/Z/-Y front view to Unity X/Y/-Z.
"""
import bpy
import math
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent
OUT = ROOT / 'Blender'
OUT.mkdir(exist_ok=True)
for obj in list(bpy.data.objects):
    bpy.data.objects.remove(obj, do_unlink=True)

def material(name, color, metallic, roughness):
    mat=bpy.data.materials.new(name)
    mat.use_nodes=True
    node=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    node.inputs['Base Color'].default_value=(*color,1)
    node.inputs['Metallic'].default_value=metallic
    node.inputs['Roughness'].default_value=roughness
    mat.diffuse_color=(*color,1)
    return mat

gold=material('Brass',(.74,.49,.18),.55,.28)
navy=material('Navy',(.018,.060,.075),.1,.29)
ivory=material('Enamel',(.95,.91,.75),.0,.26)
parts=[]

def finish(obj,name,mat,bevel=.008):
    obj.name=name
    obj.data.materials.append(mat)
    if bevel:
        mod=obj.modifiers.new('Soft machined edges','BEVEL');mod.width=bevel;mod.segments=2
        mod=obj.modifiers.new('Face weighted normals','WEIGHTED_NORMAL');mod.keep_sharp=True
    for face in obj.data.polygons:face.use_smooth=True
    parts.append(obj)
    return obj

def disk(name,x,z,y,r,depth,mat,bevel=.008):
    bpy.ops.mesh.primitive_cylinder_add(vertices=64 if r>.5 else 32 if r>.07 else 16,radius=r,depth=depth,location=(x,y,z),rotation=(math.pi/2,0,0))
    return finish(bpy.context.object,name,mat,bevel)

def profile(name,points,y,depth,mat,bevel=.012):
    # points are counterclockwise in the X/Z artwork plane.
    n=len(points)
    vertices=[(x,y+dy,z) for dy in (-depth/2,depth/2) for x,z in points]
    faces=[tuple(range(n-1,-1,-1)),tuple(range(n,n*2))]
    faces.extend((i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n))
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.update()
    obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj)
    # Recalculate consistent outward normals before beveling.
    import bmesh
    bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(mesh);bm.free()
    return finish(obj,name,mat,bevel)

disk('Cast badge backing',0,0,.024,.86,.055,gold,.012)
disk('Navy enamel field',0,0,-.010,.807,.022,navy,.008)
bpy.ops.mesh.primitive_torus_add(major_segments=96,minor_segments=8,location=(0,-.025,0),rotation=(math.pi/2,0,0),major_radius=.826,minor_radius=.013)
finish(bpy.context.object,'Fine rolled rim',gold,0)
# A coupe silhouette with both wheel arches cut into its lower outline.
outline=[(-.66,-.12),(-.66,.015),(-.60,.09),(-.35,.13),(-.23,.32),(-.12,.38),(.21,.37),(.32,.31),(.46,.12),(.59,.085),(.66,.018),(.66,-.12),(.58,-.12)]
for cx in (.415,-.395):
    if cx<0:outline.append((cx+.165,-.12))
    for i in range(1,19):
        a=math.pi*i/18
        outline.append((cx+.165*math.cos(a),-.12+.165*math.sin(a)))
outline.append((-.66,-.12))
profile('Sculpted coupe body',outline[:-1],-.064,.065,gold,.013)
profile('Rear quarter window',[(-.285,.145),(-.186,.298),(-.102,.329),(-.041,.329),(-.041,.145)],-.103,.013,navy,.012)
profile('Front side window',[(.003,.145),(.003,.329),(.199,.321),(.27,.274),(.367,.145)],-.103,.013,navy,.012)
profile('Door panel seam',[(-.02,-.09),(-.013,-.09),(-.013,.11),(-.02,.11)],-.102,.004,navy,.001)
profile('Door handle',[(.045,.08),(.117,.08),(.117,.092),(.045,.092)],-.108,.013,ivory,.004)
profile('Front lamp',[(.594,.002),(.649,.002),(.637,.045),(.594,.054)],-.105,.018,ivory,.009)
profile('Rear lamp',[(-.649,.005),(-.611,.005),(-.611,.045),(-.649,.035)],-.106,.013,ivory,.006)
profile('Front bumper',[(.558,-.095),(.669,-.095),(.669,-.072),(.558,-.072)],-.11,.023,ivory,.008)
profile('Rear bumper',[(-.673,-.095),(-.581,-.095),(-.581,-.073),(-.673,-.073)],-.11,.023,ivory,.008)
for cx in (-.395,.415):
    disk('Rubber tire',cx,-.12,-.073,.142,.07,navy,.012)
    disk('Wheel rim',cx,-.12,-.115,.091,.018,gold,.007)
    disk('Hub inset',cx,-.12,-.127,.064,.012,navy,.005)
    disk('Polished hubcap',cx,-.12,-.138,.038,.014,ivory,.005)
    for angle in range(0,360,90):
        a=math.radians(angle)
        disk('Wheel fastener',cx+.053*math.cos(a),-.12+.053*math.sin(a),-.139,.006,.008,gold,.001)
# A lower pinstripe gives the badge a composed crest, without adding tiny text.
profile('Lower crest stripe',[(-.43,-.405),(.43,-.405),(.43,-.393),(-.43,-.393)],-.03,.009,gold,.003)
disk('Crest rivet left',-.49,-.399,-.031,.016,.011,gold,.003)
disk('Crest rivet right',.49,-.399,-.031,.016,.011,gold,.003)

bpy.context.view_layer.update()
deps=bpy.context.evaluated_depsgraph_get()
export=[]
for obj in parts:
    evaluated=obj.evaluated_get(deps);mesh=evaluated.to_mesh();mesh.calc_loop_triangles()
    verts=[];normals=[];triangles=[]
    # Preserve split normals on broad faces and curved bevels by exporting triangle corners.
    for tri in mesh.loop_triangles:
        base=len(verts)//3
        for loopid in tri.loops:
            loop=mesh.loops[loopid];v=obj.matrix_world@mesh.vertices[loop.vertex_index].co
            n=obj.matrix_world.to_3x3()@mesh.corner_normals[loopid].vector
            verts.extend((v.x,v.z,v.y));normals.extend((n.x,n.z,n.y))
        triangles.extend((base,base+2,base+1))
    export.append({'name':obj.name,'material':obj.data.materials[0].name,'vertices':verts,'normals':normals,'triangles':triangles})
    evaluated.to_mesh_clear()
(OUT/'CarBadge.mesh.json').write_text(json.dumps({'parts':export},separators=(',',':')),encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'CarBadge.blend'))
print('CAR_BADGE_EXPORTED',len(parts),'parts',sum(len(p['triangles'])//3 for p in export),'triangles')
