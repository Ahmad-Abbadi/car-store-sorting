"""Shared geometry utilities and evidence studio for ORIGINAL premium candidates.

Vehicle recipes stay in individual master scripts. No prototype generator imports.
"""
import sys
from pathlib import Path
import bpy
import bmesh
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
import build_m03 as geo

ROOT=Path(__file__).resolve().parents[1]

def finish_candidate(mid,label,recipe,attachments,review_height=1.05):
    pre=ROOT/'Previews'/mid; pre.mkdir(parents=True,exist_ok=True)
    objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
    for ob in objects:
        for i,mat in enumerate(ob.data.materials):
            if mat is None: ob.data.materials[i]=ob.data.materials[0]
        geo.generic_uv(ob)
        # Boolean cutters may prepend an unused UVMap. Unity samples UV0, while
        # Blender samples the active render layer; preserve that exact layer as UV0.
        print_uv=next((u for u in ob.data.uv_layers if u.active_render),ob.data.uv_layers[0])
        for uv in list(ob.data.uv_layers):
            if uv.name!=print_uv.name:ob.data.uv_layers.remove(uv)
        for vertex in ob.data.vertices: vertex.co*=geo.SCALE
        for modifier in ob.modifiers:
            if modifier.type=='BEVEL': modifier.width*=geo.SCALE
    # Tight bevels around boolean intersections can produce microscopic collapsed
    # faces. Repair only meshes where the evaluated result actually contains them.
    bpy.context.view_layer.update();deps=bpy.context.evaluated_depsgraph_get()
    for ob in objects:
        evaluated=ob.evaluated_get(deps);data=evaluated.to_mesh()
        bm=bmesh.new();bm.from_mesh(data)
        collapsed=sum(f.calc_area()<1e-13 for f in bm.faces)
        evaluated.to_mesh_clear()
        if collapsed:
            bmesh.ops.dissolve_degenerate(bm,dist=0.0000005,edges=list(bm.edges))
            bm.normal_update();ob.modifiers.clear();bm.to_mesh(ob.data);ob.data.update()
            normals=ob.modifiers.new('Normals after collapsed-face repair','WEIGHTED_NORMAL')
            normals.keep_sharp=True;normals.weight=45
            print('Repaired collapsed evaluated faces',ob.name,collapsed,flush=True)
        bm.free()
    # Keep original editable polygons but define the export triangulation explicitly.
    # FBX/Unity otherwise tessellates concave Boolean faces differently and can drop faces.
    for ob in objects:
        ob.modifiers.new('Explicit export triangulation','TRIANGULATE')
    root=bpy.data.objects.new(mid+'_CAR_ROOT',None); bpy.context.collection.objects.link(root)
    for ob in objects: ob.parent=root
    for name,location in attachments.items():
        ob=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(ob)
        ob.location=Vector(location)*geo.SCALE;ob.parent=root
    bpy.context.view_layer.update()
    geo.PRE=pre; geo.audit(objects,mid,recipe,'scripts/build_'+mid.lower()+'.py')
    bpy.ops.object.select_all(action='DESELECT')
    for ob in [root,*objects,*root.children]: ob.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(ROOT/'Exports/Candidates'/f'{mid}_candidate.fbx'),use_selection=True,
        object_types={'MESH','EMPTY'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',
        use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False,path_mode='COPY',embed_textures=False)
    bpy.ops.object.select_all(action='DESELECT')
    scene=bpy.context.scene; scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
    scene.render.engine='CYCLES';scene.cycles.samples=48;scene.cycles.use_denoising=True
    scene.render.resolution_x=1100;scene.render.resolution_y=825;scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA'
    scene.view_settings.view_transform='AgX';scene.view_settings.look='AgX - Medium High Contrast'
    scene.world.use_nodes=True
    world=next(n for n in scene.world.node_tree.nodes if n.type=='BACKGROUND')
    world.inputs[0].default_value=(.22,.26,.34,1);world.inputs[1].default_value=.35
    ground=geo.material('Studio_Matte',(.63,.67,.73),.82)
    bpy.ops.mesh.primitive_plane_add(size=200*geo.SCALE,location=(0,0,-.00035))
    bpy.context.object.name='STUDIO_Ground';bpy.context.object.data.materials.append(ground)
    geo.area('KEY_LargeSoftbox',(-3,-4,7),.36,5,(1,.92,.83))
    geo.area('FILL_Softbox',(-1,4,4),.20,4,(.72,.84,1))
    geo.area('RIM_Softbox',(4,1,6),.46,3,(.93,.97,1))
    views={'three_quarter':((-6,-5,3.8),(0,0,1.05),5.4),
        'front':((-7,0,2.6),(0,0,1.05),4.05),'side':((0,-8,2),(0,0,1.05),4.6),
        'rear':((6,-5,3.4),(.1,0,1.05),5.35),'top':((0,0,8),(0,0,0),4.9)}
    if review_height!=1.05:
        for name,(position,target,ortho) in list(views.items()):
            if name!='top':
                shift=review_height-1.05
                views[name]=((position[0],position[1],position[2]+shift),
                    (target[0],target[1],review_height),max(ortho,4.55))
    cameras={n:geo.camera('CAM_'+n,*args) for n,args in views.items()}
    scene.camera=cameras['three_quarter']
    source=ROOT/'Blender/Candidates'/f'{mid}_{label}_candidate.blend'
    bpy.ops.wm.save_as_mainfile(filepath=str(source))
    clay=geo.material('QA_Clay',(.44,.48,.55),.52)
    for view in ['three_quarter','clay','front','side','rear','top','wireframe']:
        scene.camera=cameras.get(view,cameras['three_quarter'])
        scene.view_layers[0].material_override=clay if view in ('clay','wireframe') else None
        if view=='wireframe':
            scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO'
            scene.display.shading.color_type='SINGLE';scene.display.shading.single_color=(.55,.6,.68)
            scene.display.shading.show_cavity=True;scene.display.shading.show_shadows=True
            for ob in objects: ob.show_wire=True;ob.show_all_edges=True
        else: scene.render.engine='CYCLES'
        scene.render.filepath=str(pre/(view+'.png'));bpy.ops.render.render(write_still=True)
        print(mid+'_RENDER_DONE',view,flush=True)
    scene.view_layers[0].material_override=None;scene.render.engine='CYCLES';scene.camera=cameras['three_quarter']
    for ob in objects: ob.show_wire=False
    bpy.ops.wm.save_as_mainfile(filepath=str(source))
