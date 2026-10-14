"""Render the exact approved car for package printing, with genuine transparency."""
from pathlib import Path
import bpy,json,hashlib
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Previews/Packaging';OUT.mkdir(parents=True,exist_ok=True)
source=ROOT/'Blender/Masters/M03_approved.blend'
bpy.ops.wm.open_mainfile(filepath=str(source))
for ob in bpy.context.scene.objects:
    if ob.name.startswith('STUDIO_'):ob.hide_render=True
scene=bpy.context.scene;scene.camera=bpy.data.objects['CAM_three_quarter']
scene.render.film_transparent=True;scene.cycles.samples=80
scene.render.resolution_x=1600;scene.render.resolution_y=1200
scene.render.image_settings.color_mode='RGBA';scene.render.filepath=str(OUT/'M03_exact_hero.png')
bpy.ops.render.render(write_still=True)
(OUT/'hero_provenance.json').write_text(json.dumps({'source':str(source.relative_to(ROOT)),
    'sourceSha256':hashlib.sha256(source.read_bytes()).hexdigest(),
    'method':'Cycles render of the approved geometry and applied artwork; no image generation or redraw',
    'image':'M03_exact_hero.png'},indent=2))
