import bpy, math, json, sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[4]
SRC=ROOT/'ArtSource/Shelves/BaseShelf_01'
ART=SRC/'Artwork'; RENDER=SRC/'Renders'
ASSET=ROOT/'Assets/TinyRiders/Art/Shelves/BaseShelf_01'
SCALE=3.0
# Dedicated background process, never clears a user's live scene.
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene
scene.unit_settings.system='METRIC'
def mat(name,color,rough=.4,metal=.0,tex=None,overlay=False,emission=0):
    m=bpy.data.materials.new(name); m.use_nodes=True; m.diffuse_color=(*color,1)
    nodes=m.node_tree.nodes; links=m.node_tree.links
    p=next(n for n in nodes if n.type=='BSDF_PRINCIPLED')
    p.inputs['Base Color'].default_value=(*color,1); p.inputs['Roughness'].default_value=rough; p.inputs['Metallic'].default_value=metal
    if tex:
        t=nodes.new('ShaderNodeTexImage'); t.image=bpy.data.images.load(str(ART/(tex+'.png'))); t.interpolation='Linear'
        if overlay:
            mix=nodes.new('ShaderNodeMixRGB'); mix.blend_type='MIX'; mix.inputs[1].default_value=(*color,1)
            links.new(t.outputs['Alpha'],mix.inputs[0]); links.new(t.outputs['Color'],mix.inputs[2]); links.new(mix.outputs[0],p.inputs['Base Color'])
        else: links.new(t.outputs['Color'],p.inputs['Base Color'])
    if emission:
        p.inputs['Emission Color'].default_value=(*color,1); p.inputs['Emission Strength'].default_value=emission
    return m
frame=mat('MAT_Frame_Charcoal',(.055,.066,.09),.43,.18)
red=mat('MAT_SidePanels_Red',(.46,.012,.022),.3,.12, 'TEX_SidePanel',True)
redplain=red
traymat=mat('MAT_Trays_LightGray',(.52,.57,.64),.3,.28)
trim=mat('MAT_Trim_Dark',(.017,.022,.032),.38,.12)
header=mat('MAT_Header',(.9,.9,.9),.42,0,'TEX_Header_Blank')
left=mat('MAT_Printed_Left',(.5,.5,.5),.47,0,'TEX_Printed_Left')
right=mat('MAT_Printed_Right',(.5,.5,.5),.47,0,'TEX_Printed_Right')
accent=mat('MAT_Accent_Warm',(1,.48,.095),.26,0,emission=2.0)
def empty(name,parent=None):
    o=bpy.data.objects.new(name,None); scene.collection.objects.link(o); o.parent=parent; o.empty_display_size=.025; return o
root=empty('TR_Shelf_Base_01'); geo=empty('Geometry',root)
groups={n:empty(n,geo) for n in ['Frame','SidePanels','BackPanel','Header','Row_01','Row_02','Row_03','SolidBase','AccentTrim']}
def finish(o,name,material,parent,bevel=0):
    o.name=name; o.parent=parent; o.data.materials.append(material)
    for poly in o.data.polygons: poly.use_smooth=True
    if material==red:
        while o.data.uv_layers: o.data.uv_layers.remove(o.data.uv_layers[0])
        uv=o.data.uv_layers.new(name='PrintUV')
        for loop in uv.data: loop.uv=(.02,.02)
    if bevel:
        b=o.modifiers.new('Manufactured edge radius','BEVEL'); b.width=bevel; b.segments=4
        b.affect='EDGES'
        n=o.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL'); n.keep_sharp=True; n.weight=50
    return o
def box(name,loc,size,material,parent,bevel=.004):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc); o=bpy.context.object; o.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(o,name,material,parent,bevel)
def prism(name,profile,axis,lo,hi,material,parent,bevel=.004):
    # profile in the two remaining axes, counter-clockwise
    verts=[]
    for a in [lo,hi]:
        for u,v in profile: verts.append((a,u,v) if axis=='X' else (u,a,v))
    n=len(profile); faces=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    me=bpy.data.meshes.new(name); me.from_pydata(verts,[],faces); me.update()
    o=bpy.data.objects.new(name,me); scene.collection.objects.link(o)
    bpy.context.view_layer.objects.active=o; o.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT'); bpy.ops.mesh.normals_make_consistent(inside=False); bpy.ops.object.mode_set(mode='OBJECT'); o.select_set(False)
    return finish(o,name,material,parent,bevel)
def uv_project(o,axes,limits,flip=False):
    while o.data.uv_layers: o.data.uv_layers.remove(o.data.uv_layers[0])
    uv=o.data.uv_layers.new(name='PrintUV')
    a,b=axes; (a0,a1),(b0,b1)=limits
    for p in o.data.polygons:
        for li in p.loop_indices:
            v=o.data.vertices[o.data.loops[li].vertex_index].co
            u=(v[a]-a0)/(a1-a0); w=(v[b]-b0)/(b1-b0)
            uv.data[li].uv=(1-u if flip else u,w)
def rounded_rect(w,h,r,steps=7):
    pts=[]
    for cx,cy,start in [(w/2-r,h/2-r,0),(-w/2+r,h/2-r,90),(-w/2+r,-h/2+r,180),(w/2-r,-h/2+r,270)]:
        for j in range(steps+1):
            a=math.radians(start+j*90/steps); pts.append((cx+r*math.cos(a),cy+r*math.sin(a)))
    return pts
# Side walls: front toe steps forward near lower section; rounded crown.
profile=[(-.15,.018),(.15,.018),(.15,1.135),(.143,1.16),(.122,1.18),(-.055,1.18),(-.078,1.172),(-.095,1.15),(-.10,1.12),(-.10,.47),(-.108,.44),(-.14,.395),(-.15,.365)]
for side,sgn in [('Left',-1),('Right',1)]:
    shell=prism(side+'_StructuralWall',profile,'X',sgn*.55-.035,sgn*.55+.035,frame,groups['SidePanels'],.012)
    # Outer colored physical skin, sunk into the structural wall.
    panel=prism(side+'_PaintedPanel',profile,'X',sgn*.586-.014,sgn*.586+.014,red,groups['SidePanels'],.01)
    uv_project(panel,(1,2),((-.15,.15),(0,1.20)),flip=sgn<0)
    for poly in panel.data.polygons:
        if abs(poly.normal.x)<.9:
            for li in poly.loop_indices: panel.data.uv_layers.active.data[li].uv=(.02,.02)
    # Draw trim along front-facing inner edge. Real diffuser sweep, not a decal.
    path=[(sgn*.535,-.145,.04),(sgn*.535,-.145,.355),(sgn*.535,-.14,.385),(sgn*.535,-.105,.435),(sgn*.535,-.095,.47),(sgn*.535,-.095,1.105),(sgn*.535,-.092,1.135),(sgn*.535,-.08,1.155),(sgn*.535,-.06,1.166)]
    for nm,r,mt in [('Housing',.012,redplain),('Diffuser',.0045,accent)]:
        cr=bpy.data.curves.new(side+nm,'CURVE'); cr.dimensions='3D'; cr.resolution_u=2; cr.bevel_depth=r; cr.bevel_resolution=3
        sp=cr.splines.new('BEZIER'); sp.bezier_points.add(len(path)-1)
        for p,co in zip(sp.bezier_points,path): p.co=(co[0],co[1]-(.008 if nm=='Diffuser' else 0),co[2]); p.handle_left_type='AUTO'; p.handle_right_type='AUTO'
        ob=bpy.data.objects.new(side+nm,cr); scene.collection.objects.link(ob); ob.parent=groups['AccentTrim']; cr.materials.append(mt)
    # Lower sign is a thick inset panel integrated into the outside wall.
    inset=box(side+'_LowerInset',(sgn*.601,-.005,.178),(.009,.225,.285),left if sgn<0 else right,groups['SidePanels'],.012)
    uv_project(inset,(1,2),((-.1125,.1125),(-.1425,.1425)),flip=sgn<0)
box('BackPanel',(0,.137,.686),(1.02,.026,.94),frame,groups['BackPanel'],.008)
box('TopCrossRail',(0,.08,1.14),(1.04,.10,.06),frame,groups['Frame'],.008)
box('ClosedSolidBase',(0,.0,.151),(1.02,.285,.238),frame,groups['SolidBase'],.008)
box('LowerPlinth',(0,0,.025),(1.04,.29,.05),trim,groups['SolidBase'],.004)
box('BaseFrontInset',(0,-.145,.166),(1.00,.012,.195),frame,groups['SolidBase'],.003)
# Header geometry in local XZ plane, shallow slope backward.
hgroup=groups['Header']; hgroup.location=(0,-.066,1.107); hgroup.rotation_euler[0]=math.radians(-10)
outer=prism('HeaderBorder',rounded_rect(1.02,.182,.035),'Y',-.019,.025,redplain,hgroup,.005)
face=prism('HeaderFace',rounded_rect(.955,.126,.018),'Y',-.024,-.015,header,hgroup,.002)
uv_project(face,(0,2),((-.4775,.4775),(-.063,.063)))
theta=math.radians(3)
markers=empty('PlacementMarkers',root)
shared_div=None; shared_tray=None
for r,z in enumerate([.82,.565,.31],1):
    g=groups[f'Row_{r:02}']; g.location=(0,0,z); g.rotation_euler[0]=theta
    deck=box(f'Tray_{r:02}',(0,.005,-.012),(1.016,.252,.024),traymat,g,.004)
    if shared_tray: deck.data=shared_tray
    else: shared_tray=deck.data
    box(f'FrontLip_{r:02}',(0,-.117,.007),(1.025,.017,.031),traymat,g,.003)
    box(f'LipShadowReveal_{r:02}',(0,-.126,-.014),(1.01,.002,.005),trim,g,.0008)
    for c,x in enumerate([-.50+i/6 for i in range(7)]):
        p=[(-.108,0),(.123,0),(.123,.075),(.048,.075),(-.108,.024)]
        div=prism(f'{"End" if c in [0,6] else "Divider"}_{r:02}_{c:02}',p,'X',x-.006,x+.006,traymat,g,.003)
        # localize mesh so repeated internal elements share one mesh datablock.
        for v in div.data.vertices: v.co.x-=x
        div.location.x=x
        if shared_div: div.data=shared_div
        else: shared_div=div.data
    box(f'RowLightHousing_{r:02}',(0,-.087,-.029),(.97,.022,.014),trim,g,.003)
    box(f'RowDiffuser_{r:02}',(0,-.093,-.037),(.945,.012,.004),accent,g,.001)
    mr=empty(f'Row_{r:02}',markers)
    for c in range(6):
        mk=empty(f'Slot_R{r:02}_C{c+1:02}',mr)
        mk.location=(-.5+(c+.5)/6,.005,z+math.tan(theta)*.005)
        mk.rotation_euler[0]=math.pi/2+theta
box('HeaderDiffuser',(0,-.065,1.015),(.974,.012,.005),accent,groups['AccentTrim'],.002)
# Apply one uniform shelf scale to mesh coordinates/translations, leaving unit root transforms.
for o in list(root.children_recursive):
    o.location*=SCALE
    if o.type=='MESH':
        if not o.data.get('scaled'):
            for v in o.data.vertices: v.co*=SCALE
            o.data['scaled']=True
        for mod in o.modifiers:
            if mod.type=='BEVEL': mod.width*=SCALE
    elif o.type=='CURVE':
        for sp in o.data.splines:
            for p in sp.bezier_points: p.co*=SCALE
        o.data.bevel_depth*=SCALE
# Studio stage lives outside the shelf hierarchy and export selection.
groundmat=mat('Preview_Ground',(.19,.22,.27),.62)
floor=box('StudioGround',(0,0,-.045),(200,200,.08),groundmat,None,.01)
world=bpy.data.worlds.new('Neutral studio') if not scene.world else scene.world; scene.world=world; world.use_nodes=True
bg=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND'); bg.inputs[0].default_value=(.24,.29,.38,1); bg.inputs[1].default_value=.35
def aim(o,p): o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
def area(name,pos,power,size,color):
    d=bpy.data.lights.new(name,'AREA'); d.energy=power; d.shape='DISK'; d.size=size; d.color=color
    o=bpy.data.objects.new(name,d); scene.collection.objects.link(o); o.location=pos; aim(o,(0,0,1.7))
area('Key softbox',(-3,-4,7),950,5,(1,.88,.78)); area('Fill softbox',(4,-2,4.5),700,4,(.78,.87,1)); area('Rim softbox',(1,3,6),1100,3,(1,1,1))
camd=bpy.data.cameras.new('ArtPreview'); cam=bpy.data.objects.new('ArtPreview',camd); scene.collection.objects.link(cam); scene.camera=cam
camd.type='ORTHO'; camd.ortho_scale=5.3
try: scene.render.engine='CYCLES'
except TypeError: pass
scene.cycles.samples=40
scene.cycles.use_denoising=True
scene.render.resolution_x=1200; scene.render.resolution_y=1200; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.view_settings.view_transform='AgX'
def view(name,pos,target=(0,0,1.8),scale=5.1):
    cam.location=pos; aim(cam,target); camd.ortho_scale=scale
    scene.render.filepath=str(RENDER/(name+'.png')); bpy.ops.render.render(write_still=True)
# Export only the asset; duplicate into an export-ready file with evaluated meshes.
bpy.ops.object.select_all(action='DESELECT')
for o in [root]+list(root.children_recursive): o.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.fbx(filepath=str(ASSET/'Models/TR_Shelf_Base_01.fbx'),use_selection=True,object_types={'EMPTY','MESH','OTHER'},use_mesh_modifiers=True,add_leaf_bones=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',bake_space_transform=False)
deps=bpy.context.evaluated_depsgraph_get(); tris=0
for o in root.children_recursive:
    if o.type in {'MESH','CURVE'}:
        ev=o.evaluated_get(deps); me=ev.to_mesh(); me.calc_loop_triangles(); tris+=len(me.loop_triangles); ev.to_mesh_clear()
report={'uniform_scale':SCALE,'reference_envelope_m':[1.2,1.2,.3],'nominal_dimensions_m':[3.6,3.6,.9],'triangle_count':tris,'rows':3,'slots_per_row':6,'markers':18,'clear_slot_width_m':(.1666667-.012)*SCALE,'usable_depth_m':.216*SCALE,'minimum_clear_height_m':.18*SCALE,'tray_angle_degrees':3}
(SRC/'geometry_report.json').write_text(json.dumps(report,indent=2))
cam.location=(-6,-9,5.1); aim(cam,(0,0,1.8))
for img in bpy.data.images:
    if img.source=='FILE': img.filepath=bpy.path.relpath(img.filepath,start=str(SRC))
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'TR_Shelf_Base_01.blend'))
floor.hide_render=True
view('A_Front',(0,-10,1.8))
view('A_Side',(-10,0,1.8))
floor.hide_render=False
view('B_Hero_EmissionOn',(-6,-9,5.1))
clay=mat('QA_Clay',(.45,.45,.45),.65)
scene.view_layers[0].material_override=clay
view('A_Clay',(-6,-9,5.1))
scene.view_layers[0].material_override=None
ep=next(n for n in accent.node_tree.nodes if n.type=='BSDF_PRINCIPLED'); ep.inputs['Emission Strength'].default_value=0
view('B_Hero_EmissionOff',(-6,-9,5.1)); ep.inputs['Emission Strength'].default_value=2
view('B_Header',(0,-6,4.3),(0,-.12,3.3),3.5)
view('B_Dividers',(-3,-6,3.8),(0,-.02,1.8),2.5)
view('B_SideDetail',(-5,-4,2.8),(-1.55,0,1.8),3.7)
cam.location=(-6,-9,5.1); aim(cam,(0,0,1.8)); camd.ortho_scale=5.1
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'TR_Shelf_Base_01.blend'))
for o in list(root.children_recursive):
    if o.type in {'MESH','CURVE'}:
        bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active=o; bpy.ops.object.convert(target='MESH')
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'TR_Shelf_Base_01_Export.blend'))
print('SHELF_COMPLETE',json.dumps(report))
