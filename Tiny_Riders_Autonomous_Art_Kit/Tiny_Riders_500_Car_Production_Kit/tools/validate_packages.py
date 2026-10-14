#!/usr/bin/env python3
"""Verify approved base-template consistency. This does NOT replace visual QA."""
from pathlib import Path
import json,re,sys
ROOT=Path(__file__).resolve().parents[1]
F=json.loads((ROOT/'catalog/family_specs.json').read_text())
errors=[]
for f in F:
    for side in ('front','back'):
        p=ROOT/f'design/packaging/svg/{f["code"]}_{side}_template.svg'
        s=p.read_text()
        if 'viewBox="0 0 1200 1800"' not in s: errors.append(f'{p.name}: wrong locked artboard')
        if 'data:image/png;base64' not in s: errors.append(f'{p.name}: shared logo image missing')
        if f['collection_mark'] not in s: errors.append(f'{p.name}: family banner missing')
        if side=='front':
            for token in ['HERO_RENDER_SLOT', 'x="86" y="985" width="1028" height="530"', 'x="955" y="1555"']:
                if token not in s: errors.append(f'{p.name}: missing locked region token {token}')
        if 'font-size="130"' in s: errors.append(f'{p.name}: duplicate unapproved logo text detected')
print('Packaging flat-template layout checks:', 'PASS' if not errors else 'FAIL')
for x in errors:print('ERROR',x)
sys.exit(1 if errors else 0)
