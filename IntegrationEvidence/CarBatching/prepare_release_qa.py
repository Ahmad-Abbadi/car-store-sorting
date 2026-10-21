"""Copy a completed release for local measurements without changing the distributable."""
from pathlib import Path
import hashlib
import json
import shutil
import sys

root = Path.cwd()
evidence = root / 'IntegrationEvidence/CarBatching'
report = json.loads((root / 'IntegrationEvidence/CrazyGamesRelease/build-report.json').read_text(encoding='utf-8-sig'))
if report.get('result') != 'Succeeded' or report.get('blockingErrors', 0):
    raise RuntimeError('A successful, completed release is required')
source = root / 'Builds/CrazyGamesRelease'
label = sys.argv[1] if len(sys.argv) > 1 else 'Optimized'
if label not in ('Optimized', 'Instanced'):
    raise ValueError('Expected Optimized or Instanced')
target = root / ('Builds/CarBatching' + label)
shutil.copytree(source, target, dirs_exist_ok=True)
shutil.copyfile(evidence / 'counters.js', target / 'car-counters.js')
index = target / 'index.html'
html = index.read_text(encoding='utf-8')
if 'car-counters.js' not in html:
    html = html.replace('</head>', '<script src="car-counters.js"></script></head>')
index.write_text(html, encoding='utf-8')
files = []
for path in sorted((source / 'Build').glob('*')):
    if path.is_file():
        files.append({'path': str(path.relative_to(source)), 'bytes': path.stat().st_size,
                      'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})
result = {'releaseCompletedUtc': report.get('completedUtc'), 'source': str(source),
          'qaCopy': str(target), 'developmentBuild': report.get('developmentBuild'),
          'packageGatePassed': report.get('sizeGatePassed'), 'files': files}
(evidence / ('build-' + label + '-qa.json')).write_text(json.dumps(result, indent=2))
print(json.dumps(result, indent=2))
