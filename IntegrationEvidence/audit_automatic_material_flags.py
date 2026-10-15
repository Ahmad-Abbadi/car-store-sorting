from pathlib import Path
import subprocess,json,re
root='Assets/TinyRiders/ThemedCollection/Materials/Themes/'
paths=subprocess.check_output(['git','-c','core.autocrlf=false','diff','--name-only','--',root],text=True).splitlines()
verified=[]
for name in paths:
    if not name.startswith(root) or not name.endswith('.mat'): raise RuntimeError(name)
    original=subprocess.check_output(['git','show','HEAD:'+name]).decode('utf-8').replace('\r\n','\n')
    current=Path(name).read_text(encoding='utf-8').replace('\r\n','\n')
    old_flag=int(re.search(r'^  m_LightmapFlags: (\d+)$',original,re.M).group(1))
    new_flag=int(re.search(r'^  m_LightmapFlags: (\d+)$',current,re.M).group(1))
    candidate=current.replace('  m_ValidKeywords:\n  - _EMISSION\n','  m_ValidKeywords: []\n',1)
    candidate=candidate.replace('  m_LightmapFlags: '+str(new_flag)+'\n','  m_LightmapFlags: '+str(old_flag)+'\n',1)
    if candidate!=original or new_flag!=old_flag+4: raise RuntimeError('Contains unrelated changes: '+name)
    verified.append({'path':name,'oldFlag':old_flag,'newFlag':new_flag})
Path('IntegrationEvidence/automatic_material_flag_audit.json').write_text(json.dumps(verified,indent=2),encoding='utf-8')
print(json.dumps({'verifiedFiles':len(verified),'onlyChanges':'_EMISSION keyword added and GI flag bit 4 set','unrelatedChanges':0,'action':'read-only audit; no artwork changed'}))
