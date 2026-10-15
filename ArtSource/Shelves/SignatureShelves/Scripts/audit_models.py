"""Fast construction/budget check before rendering the entire collection."""
import bpy,json,sys
from pathlib import Path
HERE=Path(__file__).resolve().parent;sys.path.insert(0,str(HERE))
import signature_models as models
ROOT=HERE.parents[3]
themes=json.loads((ROOT/'Assets/TinyRiders/ThemedCollection/Data/Themes/theme_recipes.json').read_text(encoding='utf-8-sig'))['themes']
reports=[]
for theme in themes:
    # Dedicated background process, never the user's interactive Blender session.
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    material=bpy.data.materials.new('Audit material')
    parts=models.build(theme['id'][3:],material)
    triangles=0
    for o in parts:o.data.calc_loop_triangles();triangles+=len(o.data.loop_triangles)
    # Reserve 1,000 triangles for the existing side details, plaques, and mounting hardware.
    if triangles>7000:raise RuntimeError(f'{theme["id"]}: {triangles} topper triangles exceeds reserved budget')
    reports.append({'theme':theme['id'],'sculptureTriangles':triangles})
(ROOT/'IntegrationEvidence/SignatureShelves/model_construction_audit.json').write_text(json.dumps(reports,indent=2))
print('MODEL_AUDIT_COMPLETE',len(reports),'maximum',max(r['sculptureTriangles'] for r in reports))
