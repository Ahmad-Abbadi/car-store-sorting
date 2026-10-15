from pathlib import Path
s=Path('Builds/ThemedSortingWebGL/Build/ThemedSortingWebGL.framework.js').read_text(encoding='utf-8')
for token in ['getLayoutMap','keyboard.lock','requestPointerLock']:
 i=s.find(token)
 print(token,':',s[max(0,i-130):i+240] if i>=0 else 'not present')
