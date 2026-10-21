"""Summarize completed native-browser runs, excluding harness smoke checks."""
from pathlib import Path
from statistics import median
import json

folder = Path(__file__).resolve().parent
runs = []
for path in folder.glob('*.json'):
    if not path.name.startswith(('Chrome-', 'Edge-')) or '-smoke' in path.name:
        continue
    data = json.loads(path.read_text())
    if not data.get('runner') or data.get('webgl', {}).get('capturePending', True):
        continue
    data['evidence'] = path.name
    runs.append(data)

rows = []
for browser in ('Chrome', 'Edge'):
    for scenario in ('pile', 'shelves'):
        before = [r for r in runs if r['runner']['browser'] == browser and r['runner']['build'] == 'Baseline' and r['scenario'] == scenario]
        after = [r for r in runs if r['runner']['browser'] == browser and r['runner']['build'] == 'Optimized' and r['scenario'] == scenario]
        if not before or not after:
            continue
        reference = before[0]
        for group in (before, after):
            if any(run['runner'].get('payloadSha256') != group[0]['runner'].get('payloadSha256') for run in group):
                raise ValueError('Runs mixed different build payloads')
        for run in before + after:
            for field in ('quality', 'width', 'height', 'totalCars', 'worldCars', 'sortedCars', 'eye', 'aim'):
                if run[field] != reference[field]:
                    raise ValueError(f"Unmatched {field}: {run['evidence']}")
            if run['webgl'].get('renderer') != reference['webgl'].get('renderer'):
                raise ValueError(f"Unmatched GPU: {run['evidence']}")
        def summarize(group):
            return {
                'runs': len(group), 'averageFrameMs': median(r['averageFrameMs'] for r in group),
                'p95FrameMs': median(r['p95FrameMs'] for r in group),
                'drawCalls': median(r['webgl']['drawCalls'] for r in group),
                'submittedTriangles': median(r['webgl']['triangles'] for r in group),
                'allocatedBytes': median(r['allocatedBytes'] for r in group),
                'wasmHeapBytes': median(r['wasmHeapBytes'] for r in group),
                'evidence': [r['evidence'] for r in group],
            }
        a, b = summarize(before), summarize(after)
        rows.append({
            'browser': browser, 'scenario': scenario, 'width': reference['width'], 'height': reference['height'],
            'quality': reference['quality'], 'renderer': reference['webgl'].get('renderer'),
            'baseline': a, 'optimized': b,
            'averageFrameImprovementPercent': 100 * (1 - b['averageFrameMs'] / a['averageFrameMs']),
            'drawCallReductionPercent': 100 * (1 - b['drawCalls'] / a['drawCalls']),
            'average60FpsTargetMet': b['averageFrameMs'] <= 1000 / 60,
            'p95Within60FpsBudget': b['p95FrameMs'] <= 1000 / 60,
        })

result = {'measurement': 'Medians of the independent 15-second Unity frame-time samples; WebGL calls counted for one subsequent frame outside timing.',
          'rows': rows}
(folder / 'benchmark-summary.json').write_text(json.dumps(result, indent=2))
print(json.dumps(result, indent=2))
