#!/usr/bin/env python3
"""Validate Tiny Riders v2 production CATALOG invariants.
This intentionally does not claim any 3D output exists or has been approved.
"""
import collections, json, pathlib, sys
root=pathlib.Path(__file__).resolve().parent.parent
load=lambda name: json.loads((root/'catalog'/name).read_text(encoding='utf-8'))
masters=load('masters_25.json')
map_=load('body_to_master_50.json')
bodies=load('bodies_50_v2.json')
builds=load('builds_100.json')
products=load('collectibles_500_v2.json')
old=load('original_500_catalog_readonly.json')
errors=[]
def verify(x, msg):
    if not x: errors.append(msg)
verify(len(masters)==25,'master count != 25')
verify(len(map_)==50,'body map count != 50')
verify(len(bodies)==50,'named body count != 50')
verify(len(builds)==100,'physical build count != 100')
verify(len(products)==500,'product count != 500')
verify(len(set(m['masterId'] for m in masters))==25,'duplicate masterId')
master_ids={m['masterId'] for m in masters}
all_paired=[]
for m in masters:
    verify(len(m['bodyIds'])==2,f"bad pair in {m['masterId']}")
    all_paired +=m['bodyIds']
verify(len(set(all_paired))==50,'master body pairs not a full partition')
verify(set(all_paired)==set(map_.keys()),'paired body ids differ from map')
for body,vals in map_.items():
    verify(vals['masterId'] in master_ids,f'{body} -> unknown master')
    verify(vals['identityVariant'] in ['A','B'],f'{body} -> invalid identity slot')
    verify(body in all_paired,f'{body} missing from pairs')
by_body=collections.Counter(b['bodyId'] for b in builds)
verify(set(by_body.values())=={2},'not exactly 2 builds per named body')
by_master=collections.Counter(b['masterId'] for b in builds)
verify(set(by_master.values())=={4},'not exactly 4 physical builds per master')
verify(set(p['sku'] for p in products)==set(p['sku'] for p in old),'SKU set changed from original')
verify(len(set(p['sku'] for p in products))==500,'duplicate product SKUs')
verify(len(set(p['collectionNumber'] for p in products))==500,'duplicate product numbers')
verify(set(p['collectionNumber'] for p in products)==set(range(1,501)), 'catalog numbers not 1..500')
by_family=collections.Counter(p['familyCode'] for p in products)
verify(len(by_family)==10 and set(by_family.values())=={50},'10 families must each have 50 SKUs')
by_body_product=collections.Counter(p['bodyId'] for p in products)
verify(len(by_body_product)==50 and set(by_body_product.values())=={10},'each named body must have exactly 10 products')
by_physical=collections.Counter(p['physicalBuildId'] for p in products)
verify(len(by_physical)==100 and set(by_physical.values())=={5},'each physical build must have exactly 5 liveries')
by_master_product=collections.Counter(p['masterId'] for p in products)
verify(len(by_master_product)==25 and set(by_master_product.values())=={20}, 'each master must have 20 products')
all_build_ids={b['buildId'] for b in builds}
for p in products:
    body=p['bodyId']
    verify(p['physicalBuildId'] in all_build_ids,f"unknown build {p['physicalBuildId']}")
    verify(p['masterId']==map_[body]['masterId'],f"bad master for {p['sku']}")
    verify(p['identityVariant']==map_[body]['identityVariant'],f"bad identity for {p['sku']}")
    verify(p['familyCode']==map_[body]['familyCode'],f"bad family for {p['sku']}")
    verify(p['displayNumber']==f"{p['collectionNumber']:03d}/500",f"bad displayNumber for {p['sku']}")
    verify(p['liveryIndex'] in (1,2,3,4,5),f"bad livery idx {p['sku']}")
    verify(p['cardTemplateId']==f"PKG-{p['familyCode']}-FRONT-v1",f"package template ID changed {p['sku']}")
for ix in range(1,11):
    files=list((root/'references/original_boards').glob(f'{ix:02d}_*.png'))
    verify(len(files)==1,f'missing or ambiguous family reference sheet {ix:02d}')
verify((root/'design/packaging/MASTER_FRONT_LAYOUT_GUIDE.png').exists(),'missing locked packaging guide')
verify((root/'design/brand/tiny_riders_logo.svg').exists(),'missing 2D source logo')
print('Catalog size:',len(masters),'masters /',len(bodies),'identities /',len(builds),'builds /',len(products),'products')
print('Family counts:',dict(sorted(by_family.items())))
print('Verified unchanged original SKUs:',len(set(p['sku'] for p in products)))
print('NOTE: These are specification records only, NOT generated or approved 3D meshes.')
if errors:
    print('FAILED:',len(errors),'issues')
    for e in errors[:40]: print(' -',e)
    sys.exit(1)
print('PASS: structural catalog and reference consistency')
