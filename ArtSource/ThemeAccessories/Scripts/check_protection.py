from pathlib import Path
import hashlib,json
P=Path(__file__).resolve().parents[3];D=P/'TinyRidersProduction/ThemeAccessoryPass'
old=json.loads((D/'baseline/source_hashes.json').read_text());sets={}
for label,select in {'supplier':lambda x:x.startswith('Assets/ithappy/'),'body_materials_textures':lambda x:x.startswith('Assets/TinyRiders/ThemedCollection/Materials/') or x.startswith('Assets/TinyRiders/ThemedCollection/Textures/'),'existing_meta_guids':lambda x:x.endswith('.meta'),'runtime_code':lambda x:x.startswith('Assets/TinyRiders/Runtime/')}.items():
    files=[x for x in old if select(x)];bad=[x for x in files if not (P/x).exists() or hashlib.sha256((P/x).read_bytes()).hexdigest()!=old[x]];sets[label]={'checked':len(files),'changed':bad}
(D/'source_protection.json').write_text(json.dumps(sets,indent=2));print({k:{'checked':v['checked'],'changed':len(v['changed'])} for k,v in sets.items()})
