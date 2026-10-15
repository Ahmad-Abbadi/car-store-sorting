"""Create a portable gzip HTTP package, preserving Unity's raw development build."""
from pathlib import Path
import gzip
import hashlib
import json
import shutil

project=Path(__file__).resolve().parents[1]
source=project/'Builds'/'ThemedSortingWebGL_Final'
target=project/'Builds'/'ThemedSortingWebGL_Playable'
payloads={'.data','.wasm','.js'}
rows=[]
for path in source.rglob('*'):
    if not path.is_file(): continue
    relative=path.relative_to(source)
    destination=target/relative
    destination.parent.mkdir(parents=True,exist_ok=True)
    compress=relative.parts[0]=='Build' and (path.name.endswith('.data') or path.name.endswith('.wasm') or path.name.endswith('.framework.js'))
    if compress:
        original=path.read_bytes()
        packed=gzip.compress(original,compresslevel=6,mtime=0)
        if gzip.decompress(packed)!=original: raise RuntimeError('Compression verification failed')
        destination=destination.with_name(destination.name+'.gz')
        destination.write_bytes(packed)
        rows.append({'file':str(relative),'rawBytes':len(original),'gzipBytes':len(packed),'sha256':hashlib.sha256(original).hexdigest()})
    else: shutil.copy2(path,destination)
html=(target/'index.html').read_text(encoding='utf-8')
for row in rows:
    name=Path(row['file']).name
    if html.count(name+'"')!=1: raise RuntimeError('Unexpected host payload reference '+name)
    html=html.replace(name+'"',name+'.gz"')
(target/'index.html').write_text(html,encoding='utf-8')
report={'source':'Builds/ThemedSortingWebGL_Final','package':'Builds/ThemedSortingWebGL_Playable','sourceUnchanged':True,'gzipDecodeMatchesRaw':True,'files':rows,'packageBytes':sum(p.stat().st_size for p in target.rglob('*') if p.is_file())}
(project/'IntegrationEvidence'/'webgl_package.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
