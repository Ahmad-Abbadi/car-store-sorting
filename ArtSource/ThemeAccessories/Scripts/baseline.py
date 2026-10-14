import pathlib,json,hashlib,shutil
P=pathlib.Path(__file__).resolve().parents[3]
out=P/'Assets/TinyRiders/ThemedCollection';prod=P/'TinyRidersProduction/ThemeAccessoryPass';prod.mkdir(parents=True,exist_ok=True)
base=prod/'baseline';base.mkdir(exist_ok=True)
cat=json.loads((out/'Data/Themes/variant_catalog.json').read_text());assert len(cat['entries'])==150
assert all(e['contentRevision']==2 for e in cat['entries'])
prior=json.loads((P/'ArtSource/ThemedCars/Revisions/R02/output_hashes.json').read_text())
conflicts=[f for f,h in prior.items() if not(P/f).exists() or hashlib.sha256((P/f).read_bytes()).hexdigest()!=h]
if not (base/'Collection').exists():shutil.copytree(out,base/'Collection')
hashes={str(p.relative_to(P)).replace('\\','/'):hashlib.sha256(p.read_bytes()).hexdigest() for folder in [out,P/'Assets/ithappy/Modular_Racing_Cars',P/'Assets/TinyRiders/Runtime/Data'] for p in folder.rglob('*') if p.is_file()}
if not (base/'source_hashes.json').exists():(base/'source_hashes.json').write_text(json.dumps(hashes,indent=2))
themes=json.loads((out/'Data/Themes/theme_recipes.json').read_text())['themes']
(prod/'discovered_collection.json').write_text(json.dumps({'variants':cat['entries'],'themes':themes,'manualChangesSinceR02Manifest':conflicts,'unityVersion':(P/'ProjectSettings/ProjectVersion.txt').read_text(),'packages':json.loads((P/'Packages/manifest.json').read_text())},indent=2))
(prod/'production_state.json').write_text(json.dumps({'stage':'baseline_audit','completedKits':[],'updatedVariantIds':[],'pendingThemes':[t['id'] for t in themes],'sourceSnapshot':'baseline/source_hashes.json','conflictsPreserved':conflicts},indent=2))
print(json.dumps({'variants':len(cat['entries']),'themes':{t['id']:len(t['cars']) for t in themes},'changedSinceR02Manifest':conflicts,'snapshotFiles':len(hashes)},indent=2))
