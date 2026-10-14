import pathlib, json, shutil, hashlib
P=pathlib.Path(__file__).resolve().parents[3]
B=P/'Temp/ThemedCarBake'
S=P/'Assets/ithappy/Modular_Racing_Cars'
out=P/'ArtSource/ThemedCars'
snapshot={str(p.relative_to(P)).replace('\\','/'):hashlib.sha256(p.read_bytes()).hexdigest() for p in S.rglob('*') if p.is_file()}
snapshot_path=out/'source_hashes.json'
if snapshot_path.exists():
    assert json.loads(snapshot_path.read_text())==snapshot,'Supplier files changed since the original audit; review before rebuilding.'
else:snapshot_path.write_text(json.dumps(snapshot,indent=2))
for folder in ['Meshes','Materials','Textures','Prefabs']:
    shutil.copytree(S/folder,B/'Assets/ithappy/Modular_Racing_Cars'/folder,dirs_exist_ok=True)
    src=S/(folder+'.meta')
    if src.exists(): shutil.copy2(src,B/'Assets/ithappy/Modular_Racing_Cars'/(folder+'.meta'))
for name in ['ProjectVersion.txt','ProjectSettings.asset']:
    (B/'ProjectSettings').mkdir(parents=True,exist_ok=True)
    shutil.copy2(P/'ProjectSettings'/name,B/'ProjectSettings'/name)
(B/'Assets/Editor').mkdir(parents=True,exist_ok=True)
shutil.copy2(P/'Assets/TinyRiders/Editor/ThemedCarBuilder.cs',B/'Assets/Editor/ThemedCarBuilder.cs')
cache={json.loads((p/'package.json').read_text())['name']:p for p in (P/'Library/PackageCache').iterdir() if (p/'package.json').exists()}
deps={}
def add(n):
    if n in deps:return
    if n.startswith('com.unity.modules.') or n=='com.unity.ext.nunit':deps[n]='1.0.0' if n!='com.unity.ext.nunit' else '2.0.5';return
    p=cache[n]; deps[n]='file:'+str(p).replace('\\','/')
    for d in json.loads((p/'package.json').read_text()).get('dependencies',{}):add(d)
for n in ['com.unity.render-pipelines.universal','com.unity.nuget.newtonsoft-json','com.unity.modules.imageconversion','com.unity.modules.physics','com.unity.modules.imgui']:add(n)
(B/'Packages').mkdir(parents=True,exist_ok=True)
(B/'Packages/manifest.json').write_text(json.dumps({'dependencies':deps},indent=2))
print('Bake project ready:',B,'; supplier files protected:',len(snapshot))
