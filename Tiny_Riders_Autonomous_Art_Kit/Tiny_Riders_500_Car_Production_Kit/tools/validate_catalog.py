#!/usr/bin/env python3
"""Read-only validation for the planned 500-product Tiny Riders catalog.
Run: python tools/validate_catalog.py
Writes progress/kit_validation_report.json. Does not claim 3D assets exist.
"""
from pathlib import Path
import json, csv, sys
BASE=Path(__file__).resolve().parents[1]
errors=[]
def check(condition,message):
    if not condition: errors.append(message)
families=json.loads((BASE/'catalog/family_specs.json').read_text(encoding='utf-8'))
bodies=json.loads((BASE/'catalog/bodies_50.json').read_text(encoding='utf-8'))
cars=json.loads((BASE/'catalog/collectibles_500.json').read_text(encoding='utf-8'))
with (BASE/'catalog/collectibles_500.csv').open(newline='',encoding='utf-8') as f:
    csv_rows=list(csv.DictReader(f))
check(len(families)==10, f'Expected 10 families, got {len(families)}')
check(len(bodies)==50, f'Expected 50 bodies, got {len(bodies)}')
check(len(cars)==500, f'Expected 500 SKUs, got {len(cars)}')
check(len(csv_rows)==500, f'Expected 500 CSV rows, got {len(csv_rows)}')
check(len({x['code'] for x in families})==10,'Duplicated family codes')
check(len({x['bodyId'] for x in bodies})==50,'Duplicated body IDs')
check(len({x['sku'] for x in cars})==500,'Duplicated product SKUs')
check({int(x['collectionNumber']) for x in cars}==set(range(1,501)),'Non-sequential or duplicated collection numbers')
fmap={x['code']:x for x in families};bmap={x['bodyId']:x for x in bodies}
for f in families:
    code=f['code']
    check(len(f['models'])==5,f'{code}: must have exactly five original base shapes')
    for ext in ['.png']:
        check((BASE/'references/original_boards'/f['sheet']).is_file(),f'Missing original reference {f["sheet"]}')
        check((BASE/'references/packaging_crops'/f['sheet']).is_file(),f'Missing packaging crop {f["sheet"]}')
    for side in ('front','back'):
        check((BASE/f'design/packaging/svg/{code}_{side}_template.svg').is_file(),f'Missing {code} {side} SVG template')
        check((BASE/f'design/packaging/png_templates/{code}_{side}_template.png').is_file(),f'Missing {code} {side} PNG preview')
    fam_bodies=[x for x in bodies if x['familyCode']==code]
    check(len(fam_bodies)==5,f'{code}: should have 5 bodies, found {len(fam_bodies)}')
    fam_skus=[x for x in cars if x['familyCode']==code]
    check(len(fam_skus)==50,f'{code}: should have 50 SKUs, found {len(fam_skus)}')
for b in bodies:
    check(b['familyCode'] in fmap,f'{b["bodyId"]}: invalid family')
    check(len(b['silhouetteDirective'])>25,f'{b["bodyId"]}: missing meaningful silhouette brief')
    check(len(b['customBuildChange'])>15,f'{b["bodyId"]}: missing physical custom change')
    products=[x for x in cars if x['bodyId']==b['bodyId']]
    check(len(products)==10,f'{b["bodyId"]}: expected 10 products, found {len(products)}')
    for build in ('standard','custom'):
        subset=[x for x in products if x['buildId']==build]
        check(sorted(x['liveryIndex'] for x in subset)==list(range(1,6)),f'{b["bodyId"]}:{build} missing a livery index')
for i,car in enumerate(cars):
    check(car['sku'] == csv_rows[i]['sku'],f'CSV SKU mismatch row {i+1}')
    check(car['displayNumber']==f'{car["collectionNumber"]:03d}/500',f'{car["sku"]}: invalid printed number')
    check(car['bodyId'] in bmap,f'{car["sku"]}: missing body ID')
    check(car['familyCode'] in fmap,f'{car["sku"]}: missing family')
    check(car['modelName']==bmap[car['bodyId']]['name'],f'{car["sku"]}: model name mismatch')
    check(car['cardTemplateId']==f'PKG-{car["familyCode"]}-FRONT-v1',f'{car["sku"]}: invalid package template ID')
report={'passed':not errors,'families':len(families),'baseBodies':len(bodies),'physicalBuildsPlanned':len(bodies)*2,'plannedCollectibles':len(cars),'originalReferenceBoards':len(list((BASE/'references/original_boards').glob('*.png'))),'errors':errors,'note':'Counts and file references validated; does NOT prove models, livery maps, Unity builds or finished package textures were produced.'}
(BASE/'progress/kit_validation_report.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf8')
print(json.dumps(report,indent=2))
if errors: sys.exit(1)
