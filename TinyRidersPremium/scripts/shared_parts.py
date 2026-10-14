"""Attach the reviewed physical library at explicit vehicle sockets."""
from pathlib import Path
import bpy,bmesh
ROOT=Path(__file__).resolve().parents[1]

def wheel(wheel_id,x,y,z,label):
    path=ROOT/'Blender/Shared/Wheels_12.blend'
    known={m.name:m for m in bpy.data.materials}
    with bpy.data.libraries.load(str(path),link=False) as (source,target):
        target.objects=[name for name in source.objects if name.startswith(wheel_id+'_')]
    for ob in target.objects:
        if ob.type!='MESH':continue
        if not ob.users_collection:bpy.context.collection.objects.link(ob)
        ob.parent=None;ob.location=(0,0,0)
        suffix=ob.name.removeprefix(wheel_id+'_').split('.')[0]
        ob.name='WHEEL_'+label+'_'+suffix
        side=-1 if y<0 else 1
        for v in ob.data.vertices:v.co=(v.co.x*40+x,v.co.y*40*side+y,v.co.z*40+z)
        for modifier in list(ob.modifiers):
            if modifier.type=='BEVEL':modifier.width*=40
            if modifier.type=='TRIANGULATE':ob.modifiers.remove(modifier)
        bm=bmesh.new();bm.from_mesh(ob.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(ob.data);bm.free()
        for i,m in enumerate(ob.data.materials):
            base=m.name.split('.')[0]
            if base in known:ob.data.materials[i]=known[base]
            else:m.name=base;known[base]=m
    for ob in target.objects:
        if ob.type!='MESH':bpy.data.objects.remove(ob,do_unlink=True)
