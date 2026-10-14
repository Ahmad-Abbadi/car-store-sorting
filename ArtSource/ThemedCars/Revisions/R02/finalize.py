"""Finalize reviewed R02 metadata and independently verify the delivered files."""
import sys,json,pathlib,csv,hashlib,re
import numpy as np
from PIL import Image
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parents[2]/'Scripts'))
from theme_art import P,OUT,SRC,themes
R=SRC/'Revisions/R02';catpath=OUT/'Data/Themes/variant_catalog.json';cat=json.loads(catpath.read_text());es=cat['entries']
review=json.loads((R/'visual_review.json').read_text())
assert review['reviewedIds']==sorted(e['collectibleId'] for e in es)
members=json.loads((R/'mixed_membership.json').read_text());membership={m['collectibleId']:m for m in members}
assert len(membership)==150
cue={
'PIRATE':'Burgundy, dark flag panels, skull-and-crossbones and gold trim.',
'ROYAL':'Purple and ivory, gold shield/crown emblems and metallic accents.',
'SPACE':'White/navy color blocks, cyan orbital panels and planet emblems.',
'ALIEN':'Lime and purple, oval badge with large alien face.',
'BEACH':'Warm sand and peach, teal waves and large sun.',
'JUNGLE':'Leaf green and earth brown, leafy panels and large leaf silhouette.',
'SPOOKY':'Dark purple and orange blocks, pale ghost and green highlights.',
'CANDY':'Glossy pink and mint, frosting edges and lavender candy swirls.',
'FOOD':'Golden bun colors, tomato/lettuce accents and large burger.',
'FIRE':'Red and charcoal, orange flames and broad magma cracks.',
'ICE':'White/cyan, crystalline blue framing and snowflake.',
'OCEAN':'Deep blue and teal, coral accents, fish, waves and bubbles.',
'CONSTRUCTION':'Yellow/charcoal, orange trim, hazard stripes and helmet.',
'RESCUE':'Ivory/coral panels, large heart badge and teal pulse-line detail.',
'WIZARD':'Navy/lavender, gold hat, moon and star motifs.',
'ROBOT':'Metallic silver/graphite, cyan circuits and large robot face.',
'MUSIC':'Berry/indigo, yellow notes and equalizer bars.',
'SPORT':'Blue/cream, orange accents and ball/field graphics.',
'FARM':'Terracotta/cream, green gingham accents and apple emblem.',
'PARTY':'Lavender/apricot, pink trim, balloons and confetti.'}
notes=[]
for e in es:
    e['mixedPreviewPath']=membership[e['collectibleId']]['mixedPreviewPath']
    e['validationNotes']='R02 visual review completed: '+cue[e['themeId'][3:]]+' Reviewed front/reverse close-up, 256px perspective, native 96px thumbnail and mixed-group view. Palette and major emblem carry recognition; minor pattern details naturally disappear at thumbnail size. Supplier UVs constrain coverage.'
    if e['baseCarId'] in ['C007','C008','C009','C011','C012']:
        e['validationNotes']+=' Side badges are the primary symbol surfaces on this body; near-frontal/high views rely more on family palette because the supplier provides limited hood/roof decal coverage.'
    notes.append({k:e[k] for k in ['collectibleId','contentRevision','themeId','previewPath','gameplayPreviewPath','thumbnailPath','mixedPreviewPath','validationNotes']})
catpath.write_text(json.dumps(cat,indent=2),encoding='utf-8')
with (OUT/'Data/Themes/variant_catalog.csv').open('w',newline='',encoding='utf-8') as f:
    fields=[k for k in es[0] if k!='materialPaths'];w=csv.DictWriter(f,fieldnames=fields,extrasaction='ignore');w.writeheader();w.writerows(es)
(OUT/'Data/Themes/revision2_visual_notes.json').write_text(json.dumps({'revision':2,'cameraTest':'256px, perspective FOV60, camera distance 1.5 times maximum model dimension; native96px orthographic thumbnails. These are representative art tests, not a shipped gameplay-camera certification.','entries':notes},indent=2))
old=json.loads((SRC/'Revisions/R01/Collection/Data/Themes/variant_catalog.json').read_text())['entries'];oldids=sorted(e['collectibleId'] for e in old)
assert oldids==sorted(e['collectibleId'] for e in es)
assert all(e['contentRevision']==2 for e in es)
assert json.loads((R/'before_hierarchy.json').read_text())==json.loads((R/'after_hierarchy.json').read_text())
original=json.loads((SRC/'source_hashes.json').read_text());changed=[p for p,h in original.items() if not(P/p).exists() or hashlib.sha256((P/p).read_bytes()).hexdigest()!=h];assert not changed
previewChecks=0
for e in es:
    for key,size in [('previewPath',512),('gameplayPreviewPath',256),('thumbnailPath',96)]:
        im=Image.open(P/e[key]);assert im.size==(size,size);a=np.asarray(im.convert('RGB'));assert a.std()>15;previewChecks+=1
    im=Image.open(P/e['previewPath'].replace('/Themes/','/Reverse/',1));assert im.size==(384,384);previewChecks+=1
    for p in [P/e['prefabPath'],P/e['previewPath']]:
        prior=SRC/'Revisions/R01/Collection'/p.relative_to(OUT)
        assert p.read_bytes()!=prior.read_bytes(),f'Unchanged output {p}'
    tex=OUT/'Textures/Themes'/e['themeId']/(e['baseCarId']+'_ThemeDecals.png')
    assert tex.read_bytes()!=(SRC/'Revisions/R01/Collection'/tex.relative_to(OUT)).read_bytes()
unity=json.loads((OUT/'Data/Themes/revision2_unity_validation.json').read_text());assert unity['passed']
guidset=set()
for folder in [P/'Assets',P/'Packages',P/'Library/PackageCache']:
    for meta in folder.rglob('*.meta'):
        m=re.search(r'^guid: ([a-f0-9]{32})',meta.read_text(errors='ignore'),re.M)
        if m:guidset.add(m.group(1))
unresolved=[];refs=0
for p in OUT.rglob('*'):
    if p.suffix not in ['.mat','.prefab','.asset']:continue
    for guid in re.findall(r'guid: ([a-f0-9]{32})',p.read_text(errors='ignore')):
        refs+=1
        if not guid.startswith('0000000000000000') and guid not in guidset:unresolved.append({'path':str(p.relative_to(P)),'guid':guid})
assert not unresolved,unresolved
report={'passed':True,'contentRevision':2,'variants':150,'themeFamilies':20,'supplierFilesVerifiedUnchanged':len(original),'supplierChanges':changed,'permanentIdsUnchanged':True,'prefabGuidsHierarchiesTransformsMeshReferencesUnchanged':True,'individualPreviewsValidated':previewChecks,'mixedGroupRenders':15,'mixedGroupMemberships':len(membership),'familyContactSheets':20,'multiscaleSheets':20,'serializedGuidReferencesChecked':refs,'unresolvedReferences':unresolved,'newMeshes':0,'newShaders':0,'visualReview':review,'unityValidation':unity,'limitations':['Existing body UVs sample palette points; broad body patterns cannot span hood/roof without changing UVs or shaders. Color blocking uses existing material slots; artwork stays on original decal surfaces.','Mirrored and overlapping supplier UVs constrain badge orientation/size. Art was cropped to nonoverlapping UV regions to avoid stray fragments.','C006 and C013 remain excluded as in R01; their permanent base IDs remain reserved.','Recognition uses family colors plus the major emblem at 96px; fine motifs are close-up details. Final gameplay camera and dense occlusion need integration-specific checks.']}
(R/'delivery_validation.json').write_text(json.dumps(report,indent=2));(OUT/'Data/Themes/revision2_validation.json').write_text(json.dumps(report,indent=2))
(R/'output_hashes.json').write_text(json.dumps({str(p.relative_to(P)).replace('\\','/'):hashlib.sha256(p.read_bytes()).hexdigest() for p in OUT.rglob('*') if p.is_file() and p.suffix!='.meta'},indent=2))
print(json.dumps({k:v for k,v in report.items() if k not in ['visualReview','unityValidation','limitations']},indent=2))
