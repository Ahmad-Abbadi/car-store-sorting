from pathlib import Path
import json
import re
import subprocess

root='Assets/TinyRiders/ThemedCollection/Textures/Themes/'
paths=subprocess.check_output(['git','-c','core.safecrlf=false','diff','--name-only','--',root],text=True).splitlines()
report=[]
def without_webgl(text):
    return re.sub(r'^  - serializedVersion: \d+\n    buildTarget: WebGL\n.*?(?=^  - serializedVersion:|^  spriteSheet:)', '', text, flags=re.M|re.S)
for name in paths:
    if not name.startswith(root) or not name.endswith('.png.meta'): raise RuntimeError('Unexpected source artwork edit: '+name)
    old=subprocess.check_output(['git','show','HEAD:'+name]).decode('utf-8').replace('\r\n','\n')
    new=Path(name).read_text(encoding='utf-8').replace('\r\n','\n')
    if without_webgl(old)!=without_webgl(new): raise RuntimeError('Non-WebGL importer settings changed: '+name)
    old_guid=re.search(r'^guid: (\w+)$',old,re.M).group(1)
    new_guid=re.search(r'^guid: (\w+)$',new,re.M).group(1)
    if old_guid!=new_guid: raise RuntimeError('GUID changed: '+name)
    report.append({'path':name,'guid':new_guid,'onlyWebGLPlatformSettingsChanged':True})
result={'passed':True,'changedImporters':len(report),'originalImagesModified':0,'guidChanges':0,'otherPlatformChanges':0,'files':report}
Path('IntegrationEvidence/webgl_texture_override_audit.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in result.items() if k!='files'}))
