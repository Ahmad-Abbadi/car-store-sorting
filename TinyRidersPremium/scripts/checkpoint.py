"""Truthful premium-production ledger. Prototype assets never count as finished."""
import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageOps

ROOT=Path(__file__).resolve().parents[1]; PROJECT=ROOT.parent
KIT=PROJECT/'Tiny_Riders_25_Master_Art_Production_Kit'/'Tiny_Riders_25_Master_Art_Production_Kit'
REPORT=ROOT/'Reports'; REPORT.mkdir(parents=True,exist_ok=True)
CAT=ROOT/'Catalog'; CAT.mkdir(parents=True,exist_ok=True)
def write(name,data): (REPORT/name).write_text(json.dumps(data,indent=2),encoding='utf8')
def checksum(p): return hashlib.sha256(p.read_bytes()).hexdigest()
masters=json.loads((KIT/'catalog/masters_25.json').read_text())
products=json.loads((KIT/'catalog/collectibles_500_v2.json').read_text())
for f in ['masters_25.json','body_to_master_50.json','bodies_50_v2.json','builds_100.json','collectibles_500_v2.json']:
    (CAT/f).write_bytes((KIT/'catalog'/f).read_bytes())

# These exact images have been opened and inspected during this production run.
refs=list(sorted((KIT/'references/original_boards').glob('*.png')))
refs+=list(sorted((KIT/'references/featured_cars').glob('*.png')))
refs+=[KIT/'references/ALL_TEN_SHEETS_GALLERY.png',KIT/'design/packaging/MASTER_FRONT_LAYOUT_GUIDE.png',
       PROJECT/'TinyRidersArt/previews/contact_sheets/all_50_bases.png']
write('REFERENCE_MANIFEST.json',{'inspected':[{'path':str(p),'sha256':checksum(p),
       'role':'negative_prototype_reference' if 'TinyRidersArt' in p.parts else 'user_supplied_art_direction'} for p in refs]})

entries=[]; completed=[]; failures=[]
for master in masters:
    mid=master['masterId']; review_path=ROOT/'Previews'/mid/'review.json'
    review=json.loads(review_path.read_text()) if review_path.exists() else {}
    candidate=list((ROOT/'Blender/Candidates').glob(mid+'*.blend'))
    status=review.get('status','CANDIDATE_UNDER_REVIEW' if candidate else 'NOT_YET_MODELED')
    evidence=[str(p.relative_to(ROOT)) for p in sorted((ROOT/'Previews'/mid).glob('*.png'))]
    entry={'masterId':mid,'namedIdentities':master['bodyIds'],'names':master['bodyNames'],
           'status':status,'sourceMethod':'Original local Blender modeling only',
           'candidateFiles':[str(p.relative_to(ROOT)) for p in candidate],
           'reviewEvidence':evidence,'dependentSkus':[p['sku'] for p in products if p['masterId']==mid],
           'review':review}
    approved=list((ROOT/'Blender/Masters').glob(mid+'*.blend'))+list((ROOT/'Exports/Masters').glob(mid+'*.fbx'))
    entry['approvedSourceFiles']=[{'path':str(p.relative_to(ROOT)),'sha256':checksum(p)} for p in approved]
    if status=='MASTER_APPROVED' and len(approved)<2:
        raise RuntimeError(mid+': approval has no saved Blender and FBX master')
    entries.append(entry)
    if status=='MASTER_APPROVED': completed.append(entry)
    if status=='NEEDS_REWORK': failures.append(entry)
package_review_path=ROOT/'Previews/Packaging/review.json'
package_review=json.loads(package_review_path.read_text()) if package_review_path.exists() else {}
wheel_review_path=ROOT/'Previews/Wheels/review.json'
wheel_review=json.loads(wheel_review_path.read_text()) if wheel_review_path.exists() else {}
files=[p for folder in ('Blender','Exports','Textures','Previews','scripts') for p in (ROOT/folder).rglob('*') if p.is_file() and p.suffix not in ('.pyc','.blend1')]
now=datetime.now(timezone.utc).isoformat()
state={'objectiveVersion':'Tiny Riders 3.1','generatedAtUtc':now,'phase':'Original master geometry and quality refinement',
       'toolPolicy':'Codex, local Python, Blender Python/MCP, Unity only; no external art generation or downloaded meshes',
       'counts':{'approvedMasters':len(completed),'candidateMasters':sum(bool(x['candidateFiles']) for x in entries),
                 'approvedWheelDesigns':len(wheel_review.get('approvedWheelIds',[])),
                 'approvedIdentities':0,'approvedBuilds':0,'finishedCollectibles':0,'finishedPackages':0,
                 'unityCarProducts':0,'unityPackagedProducts':0},
       'priorPrototypeFolder':'TinyRidersArt (rejected as final art, excluded from all premium counts)',
       'masters':entries,'packagingTemplate':package_review,'wheelLibrary':wheel_review,'completedProductIds':[],
       'unfinishedProductIds':[p['sku'] for p in products],
       'generatedFiles':[{'path':str(p.relative_to(ROOT)),'bytes':p.stat().st_size,'sha256':checksum(p)} for p in files],
       'remainingWork':['Complete final family lineup artwork and per-build package fit checks using the validated shared template',
                        'Develop the approved M03 foundation into Blue Racer/Apex and their physical builds',
                        'Develop the approved M01 foundation into Pocket/Bean and their physical builds',
                        f'Finish approval of the remaining {25-len(completed)} original master recipes and meshes',
                        'Create 50 distinct identities and 100 standard/custom builds',
                        'Create 500 curated liveries and exact matching packages',
                        'Import and verify 1000 Unity product configurations and preview scenes',
                        'Build modular environment after vehicle collection',
                        'Complete the requirement-by-requirement visual and technical audit']}
write('PRODUCTION_STATE.json',state); write('COMPLETED_ASSETS.json',completed); write('FAILED_ASSETS.json',failures)
(REPORT/'PROGRESS.md').write_text(f'''# Tiny Riders 3.1 production progress

Updated {now}

New production root: `TinyRidersPremium`. The previous `TinyRidersArt` collection is retained as prototype evidence and does not count toward this goal.

- Approved premium masters: {len(completed)} / 25
- Master candidates saved: {state['counts']['candidateMasters']}
- Shared wheel designs approved and imported: {state['counts']['approvedWheelDesigns']} / 12 minimum
- Finished identities/builds/products/packages: 0 / 0 / 0 / 0
- Unity products verified for this new collection: 0

M01 Pocket is approved as a foundation mesh after seven Blender evidence views and three Unity captures. It has 6672 triangles and passes topology, wheel-clearance, ground-contact, UV0 and imported-scale checks. Its alternate identity, builds and SKU assembly remain unfinished.

M03 Blue Racer is approved as a foundation at 9310 triangles, reduced from 17584. Its wheel/bumper clearance, closed rear cap, supported wing, printed UV0 artwork, attachment hierarchy and evaluated topology pass review. The mesh and scale match in Unity. Its alternate identity, physical builds, livery set and product packaging remain unfinished.

M02 Parcel is tracked in its individual review and technical audit. Its independent recipe has a short forward-control cab, shaped taller cargo shell, utility bumper belts and pressed utility wheels. Its Loop conversion and standard/custom builds remain pending.

The shared packaging template is {package_review.get('status','NOT_YET_REVIEWED')}. It has a real hanger cutout, a thin clear shell fitted to M03, exact approved-car hero artwork, and Blender/Unity front, back and three-quarter evidence. The Unity hanger triangulation and orientation defects were repaired. Back-card family lineup images and final SKU packages remain unfinished; the template does not count as a finished product.

The shared wheel library has 12 designs and eight tire categories, 912–4264 triangles per wheel. Blender sources preserve meaningful editable parts. Unity prefabs reference one combined mesh renderer per wheel with submeshes by material, and all triangle counts and axle-ground heights match. Crowded scenes still need lower detail levels. Other shared accessories are pending.

Unity candidate assets are under `Assets/TinyRidersPremium`, with separate M01 and M03 review scenes. `ArtPreview_URP.asset` is the active quality pipeline for these art previews; the original `Performance_URP.asset` is preserved. Candidate review scenes enlarge the metre-scale prefab by 20 for reliable realtime shadows. Saved prefabs retain physical scale.

All ten family boards and all ten featured-car crops have been inspected. The canonical 25/50/100/500 catalog passes validation and preserves all original SKUs. The pending external-service login was cancelled when the 3.1 tool restriction arrived; no generated or downloaded external asset is used.
''',encoding='utf8')
(REPORT/'RESUME_HERE.md').write_text('''# Resume Tiny Riders 3.1

The goal is incomplete. Do not reuse prior prototype counts.

Approved masters are foundations only, saved under `Blender/Masters` and `Exports/Masters`. Review each `Previews/<master>/review.json` and the ledger for actual counts. The shared packaging template has been visually validated; final SKU back artwork remains pending. Continue original masters and the shared component library, then alternate identities/builds. Do not regenerate approved geometry unnecessarily. To rebuild the latest Parcel candidate after an intentional source change:

```powershell
python TinyRidersPremium/scripts/make_m02_textures.py
& 'C:\\Program Files\\Blender Foundation\\Blender 5.1\\blender.exe' --background --python TinyRidersPremium/scripts/build_m02.py
& 'C:\\Program Files\\Blender Foundation\\Blender 5.1\\blender.exe' --background --python TinyRidersPremium/scripts/render_wire_review.py -- M02
unity command run_script --file TinyRidersPremium/scripts/UnityCandidateImport.cs --entry UnityCandidateImport.Main --args '["M02"]' --caller plugin --skill unity-cli --format json
python TinyRidersPremium/scripts/checkpoint.py
```

Packaging source commands, only when a dependency changes: Blender `render_package_hero.py`, local Python `compose_package_master.py`, Blender `build_package_master.py`, then Unity `UnityPackageImport.cs` entry `UnityPackageImport.Main`. Preserve the canonical logo/layout. Review `Previews/Packaging/review.json` for scope and limitations. Continue remaining masters, 12 wheel designs, named identities, physical builds and the full 25/50/100/500 objective. `PRODUCTION_STATE.json` lists every unfinished SKU. No generation process is implied to remain running by this file; inspect the current process or session before resuming.
''',encoding='utf8')

# Comparison images are produced before checkpointing, so recorded hashes stay valid.
lines=['# Tiny Riders production validation','',f'Updated {now}','','The full production goal remains incomplete. Prior prototype outputs are excluded.','',
       '| Master | Review state | Blender triangles | Technical failures | Unity triangles |','|---|---|---:|---|---:|']
for entry in entries:
    mid=entry['masterId'];p=ROOT/'Previews'/mid
    if not (p/'technical_audit.json').exists():continue
    audit=json.loads((p/'technical_audit.json').read_text())
    unity=json.loads((p/'unity_audit.json').read_text()) if (p/'unity_audit.json').exists() else {}
    lines.append(f"| {mid} | {entry['status']} | {audit['triangles']} | {', '.join(audit['technicalFailures']) or 'None'} | {unity.get('triangles','Not imported')} |")
lines+=['','## Verified repairs','',
    '- M01: roof profile, front lamp placement, side-window UVs, bumper topology, UV0 assignment, FBX root axis and scale preservation, rim shading.',
    '- M03: closed rear cap, readable number orientation, wheel/bumper clearance, supported wing, 9310-triangle optimized geometry, preserved hierarchy, UV0 and collapsed bevel repairs verified in Blender and Unity.',
    '- Packaging: real cutout, shell/car clearance, exact-car hero artwork, imported orientation, explicit hanger triangulation and compiled clear PET shader reviewed in actual renders. See scoped template review.',
    '- Unity studio: dedicated pipeline enables shadows and preserves the original performance asset. No gameplay added.',
    '', '## Remaining gates','',
    '- Finish and visually approve the remaining masters; create both named identities and physical builds.',
    '- Curate and render all 500 SKU liveries, then render the same assemblies for their locked package artwork.',
    '- Populate the final family back-card lineups and check shared blister fit against every final physical build.',
    '- Create and validate the 1000 final product prefabs; then environment art and collection-wide galleries.',
    '- Add game-scale LODs and mesh/material batching, preserving the reviewed appearance.']
(REPORT/'VALIDATION_REPORT.md').write_text('\n'.join(lines)+'\n',encoding='utf8')
print(json.dumps(state['counts']))
