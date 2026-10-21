"""Collate distinct NUnit cases and the lossless geometry/image evidence."""
from datetime import datetime, timezone
from pathlib import Path
import hashlib
import json

folder = Path(__file__).resolve().parent
reports = ['geometry-tests.json', 'held-tests.json', 'collection-tests.json',
           'detail-mesh-tests.json', 'detail-play-tests.json', 'powers-tests.json',
           'tutorial-tests.json', 'tutorial-guidance-tests.json', 'rendering-artifact-tests.json']
cases = {}
summaries = {}
for name in reports:
    result = json.loads((folder / name).read_text(encoding='utf-8-sig'))['data']['result']
    if result['status'] != 'completed' or result['summary']['failed']:
        raise RuntimeError(f'Validation incomplete or failed: {name}')
    summaries[name] = result['summary']
    for case in result['results']:
        cases[case['FullName']] = {'status': case['Status'], 'evidence': name}
if any(case['status'] != 'Passed' for case in cases.values()):
    raise RuntimeError('Every selected case must pass')
comparisons = json.loads((folder / 'VisualReview/comparisons.json').read_text())
maximum_changed = max(row['changedPixelFraction'] for row in comparisons)
if len(comparisons) != 192 or maximum_changed > .001:
    raise RuntimeError('Rendered comparison failed')
bake = json.loads((folder / 'geometry-bake.json').read_text())
bounds = (folder / 'bounds.txt').read_text()
root = folder.parents[1]
code = ['Assets/TinyRiders/Gameplay/Editor/CarVisualBatchBuilder.cs',
        'Assets/TinyRiders/Gameplay/CarBatchGeometry.cs',
        'Assets/00_Project/01_scripts/Game_Systems/Rendering/SharedOutline.cs',
        'Assets/TinyRiders/Gameplay/Editor/ItemDetailBuilder.cs',
        'Assets/00_Project/01_scripts/Game_Systems/Rendering/ItemLodSet.cs']
result = {
    'completedUtc': datetime.now(timezone.utc).isoformat(), 'uniqueCases': len(cases),
    'reportedCases': sum(summary['total'] for summary in summaries.values()), 'suiteSummaries': summaries,
    'allPassed': True, 'cases': cases, 'bakerVersion': bake['version'],
    'designs': len(bake['cars']), 'materialDrawsAcrossAllDesignsBefore': bake['drawsBefore'],
    'materialDrawsAcrossAllDesignsAfter': bake['drawsAfter'],
    'materialDrawReductionPercent': 100 * (1 - bake['drawsAfter'] / bake['drawsBefore']),
    'pairedImages': len(comparisons), 'maximumChangedPixelsPer512SquareImage': maximum_changed * 512 * 512,
    'maximumMeanChannelError': max(row['meanChannelError'] for row in comparisons),
    'bounds': bounds,
    'codeSha256': {path: hashlib.sha256((root / path).read_bytes()).hexdigest() for path in code},
    'releasePerformanceAcceptance': 'Requires the separate native-browser benchmark summary.'
}
(folder / 'validation-summary.json').write_text(json.dumps(result, indent=2))
print(json.dumps({key: value for key, value in result.items() if key not in ('cases', 'codeSha256')}, indent=2))
