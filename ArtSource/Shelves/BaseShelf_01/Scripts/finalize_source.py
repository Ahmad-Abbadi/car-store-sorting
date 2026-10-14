import bpy,json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[4]; SRC=ROOT/'ArtSource/Shelves/BaseShelf_01'
bpy.ops.wm.open_mainfile(filepath=str(SRC/'TR_Shelf_Base_01.blend'))
scene=bpy.context.scene; root=bpy.data.objects['TR_Shelf_Base_01']; cam=scene.camera
objs=list(root.children_recursive)
report={'font_objects':sum(o.type=='FONT' for o in objs),'markers':sum(o.name.startswith('Slot_R') for o in objs),'shared_internal_divider_meshes':len(set(o.data.name for o in objs if o.name.startswith('Divider_'))),'printed_meshes_without_uv':[o.name for o in objs if o.type=='MESH' and any(m and m.name in ['MAT_Header','MAT_SidePanels_Red','MAT_Printed_Left','MAT_Printed_Right'] for m in o.data.materials) and not o.data.uv_layers],'image_files':[Path(bpy.path.abspath(i.filepath)).name for i in bpy.data.images if i.source=='FILE'],'missing_image_files':[i.filepath for i in bpy.data.images if i.source=='FILE' and not Path(bpy.path.abspath(i.filepath)).exists()]}
(SRC/'blender_audit.json').write_text(json.dumps(report,indent=2))
for img in bpy.data.images:
    if img.source=='FILE':img.filepath=bpy.path.relpath(bpy.path.abspath(img.filepath),start=str(SRC))
floor=bpy.data.objects['StudioGround']; floor.hide_render=True
for name,pos in [('A_Front',(0,-10,1.8)),('A_Side',(-10,0,1.8))]:
    cam.location=pos; cam.rotation_euler=(Vector((0,0,1.8))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=4.7
    scene.render.filepath=str(SRC/'Renders'/f'{name}.png');bpy.ops.render.render(write_still=True)
floor.hide_render=False;cam.location=(-6,-9,5.1);cam.rotation_euler=(Vector((0,0,1.8))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=5.1
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'TR_Shelf_Base_01.blend'))
bpy.ops.wm.open_mainfile(filepath=str(SRC/'TR_Shelf_Base_01_Export.blend'))
for img in bpy.data.images:
    if img.source=='FILE':img.filepath=bpy.path.relpath(bpy.path.abspath(img.filepath),start=str(SRC))
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'TR_Shelf_Base_01_Export.blend'))
print('FINAL_SOURCE_AUDIT',json.dumps(report))
