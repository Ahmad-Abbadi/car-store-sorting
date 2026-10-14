"""Delivery checks against the requested R03 scope, not a count of planned entries."""
from pathlib import Path
import json,hashlib
P=Path(__file__).resolve().parents[3];D=P/'TinyRidersProduction/ThemeAccessoryPass';A=P/'Assets/TinyRiders/Art/ThemeAccessories';C=P/'Assets/TinyRiders/ThemedCollection'
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
lib=read(D/'theme_accessory_library.json');assign=read(D/'variant_assignments.json');cat=read(C/'Data/Themes/variant_catalog.json')['entries'];checks={}
checks['actual_existing_counts']=len(lib)==20 and len(cat)==150 and len(assign)==150 and len({e['collectibleId'] for e in cat})==150
checks['source_files_and_exports']=all((P/r['sourceBlend']).is_file() and (P/r['exportPath']).is_file() and (P/r['atlasPath']).is_file() and (P/r['kitPrefabPath']).is_file() for r in lib)
checks['one_primary_per_theme_no_new_products']=all(r['role']=='primary' for r in lib) and {e['collectibleId'] for e in cat}==set(read(C/'Data/Themes/id_registry.json')['immutableIds'])
checks['revision_and_local_fits']=all(e['contentRevision']==3 for e in cat) and all(r['newRevision']==3 and len(r['localPosition'])==3 and len(set(round(x,5) for x in r['localScale']))==1 for r in assign)
checks['every_required_view_exists']=all((D/'before_after/before'/(r['variantId']+'.png')).is_file() and all((P/v).is_file() for v in r['evidence'].values()) and (D/'thumbnails/128'/(r['variantId']+'.png')).is_file() for r in assign)
checks['before_after_and_mixed_piles']=all((D/'before_after'/('pile_%d_%s.png'%(g,rev))).is_file() for g in range(2) for rev in ['before','after']) and len(list((D/'before_after/mixed/after').glob('*.png')))==15
checks['camera_tests']=len(list((D/'views/ActualCamera/after').glob('*.png')))==20 and (D/'actual_camera_test.json').is_file()
checks['no_flagged_geometry']=not any(x['flags'] for x in read(D/'geometric_fit_checks.json'))
checks['blender_topology']=len(read(D/'blender_mesh_validation.json'))==20 and all(x['nonManifoldEdges']==0 and x['boundaryEdges']==0 and x['uvLayers']==1 and x['materialSlots']==1 for x in read(D/'blender_mesh_validation.json'))
checks['unity_and_regeneration']=read(D/'regeneration_validation.json')['status']=='passed' and not read(D/'unity_validation_second.json')['errors']
checks['source_protection']=all(not v['changed'] for v in read(D/'source_protection.json').values())
checks['handoff']=all((D/f).is_file() for f in ['validation_report.md','integration_notes.md','RESUME_HERE.md','production_state.json','output_hashes.json'])
checks['published_previews']=all(hashlib.sha256((P/e['previewPath']).read_bytes()).digest()==hashlib.sha256((D/'before_after/after'/(e['collectibleId']+'.png')).read_bytes()).digest() for e in cat)
result={'status':'passed' if all(checks.values()) else 'failed','checks':checks,'unsupportedDisplayConstraint':'All 150 raw full-size themed prefabs remain incompatible with the separate existing small shelf-cell scale; exact IDs/bounds recorded.','recognitionLimit':'Visual review only; no player recognition claim.'}
(D/'completion_audit.json').write_text(json.dumps(result,indent=2));print(json.dumps(result,indent=2));assert all(checks.values())
