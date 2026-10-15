from pathlib import Path
import hashlib,re
root=Path(__file__).resolve().parents[1]/'Builds'/'ThemedSortingWebGL_Final'
version=hashlib.sha256((root/'TinyRiders.host.js').read_bytes()).hexdigest()[:12]
path=root/'index.html'
html=path.read_text(encoding='utf-8')
html=re.sub(r'TinyRiders\.host\.js(?:\?v=[a-f0-9]+)?','TinyRiders.host.js?v='+version,html)
path.write_text(html,encoding='utf-8')
print('Versioned web host: '+version)
