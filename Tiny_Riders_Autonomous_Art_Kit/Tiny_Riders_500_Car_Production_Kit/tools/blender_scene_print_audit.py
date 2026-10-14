"""Blender-side audit. Run AFTER actual 3D assets exist:
blender -b production/Blender/.../source.blend --python tools/blender_scene_print_audit.py -- output.json
It checks visible FONT objects, suspicious printable mesh names and rough triangle counts.
This is a heuristic aid; owner's clay/wireframe visual audit remains mandatory.
"""
import bpy,json,sys
from pathlib import Path
out_path=Path(sys.argv[sys.argv.index('--')+1]) if '--' in sys.argv and sys.argv.index('--')+1 <len(sys.argv) else Path('blender_print_audit.json')
forbidden_names=['extruded_text','letter_mesh','logo_mesh','decal_mesh','stripe_mesh','floating_logo','number_mesh','modeled_letter','word_mesh']
violations=[];stats={};mesh_count=0
for ob in bpy.data.objects:
    if ob.hide_render:continue
    if ob.type=='FONT':violations.append({'name':ob.name,'reason':'Visible Blender FONT: printed text must use mapped 2D art'})
    if ob.type=='MESH':
        mesh_count+=1
        name=ob.name.lower()
        for token in forbidden_names:
            if token in name:violations.append({'name':ob.name,'reason':'Suspicious mesh asset name: '+token})
        try:
            ob.data.calc_loop_triangles()
            stats[ob.name]={'triangles':len(ob.data.loop_triangles),'materials':[m.name for m in ob.data.materials if m]}
        except Exception as e:stats[ob.name]={'error':str(e)}
report={'scene':bpy.data.filepath,'visibleMeshes':mesh_count,'visibleTrianglesByObject':stats,'visibleFontOrSuspiciousMeshViolations':violations,'passedHeuristic':not violations,'limitations':'Cannot prove printable numbers were not sculpted into arbitrary mesh names; visually inspect clay and wireframe.'}
out_path.parent.mkdir(parents=True,exist_ok=True);out_path.write_text(json.dumps(report,indent=2))
print(json.dumps({'passedHeuristic':not violations,'violations':violations,'meshCount':mesh_count},indent=2))
if violations:raise SystemExit(1)
