"""Publish only generated assets, preserve Unity GUIDs, and produce review sheets."""
import pathlib,json,shutil,hashlib,csv,sys
from PIL import Image,ImageDraw,ImageFont
from theme_art import P,OUT,SRC,themes
B=P/'Temp/ThemedCarBake'; BA=B/'Assets/TinyRiders/ThemedCollection'
def font(size,bold=False):
    return ImageFont.truetype('C:/Windows/Fonts/'+('segoeuib.ttf' if bold else 'segoeui.ttf'),size)
def sheet(entries,title,path,reverse=False):
    cols=4;cw=340;ch=320;rows=(len(entries)+cols-1)//cols
    im=Image.new('RGB',(cols*cw+64,rows*ch+150),'#E8EDF2');d=ImageDraw.Draw(im)
    d.text((32,22),title,font=font(34,True),fill='#213447')
    d.text((34,72),'THEMED CAR COLLECTION  /  '+str(len(entries))+' COLLECTIBLES  /  '+('REVERSE VIEW' if reverse else 'FRONT VIEW'),font=font(15),fill='#5B6C7C')
    for i,e in enumerate(entries):
        x=32+(i%cols)*cw;y=122+(i//cols)*ch
        source=pathlib.Path(str(e['previewPath']).replace('/Themes/','/Reverse/',1)) if reverse else pathlib.Path(e['previewPath'])
        thumb=Image.open(P/source).convert('RGB').resize((320,320),Image.Resampling.LANCZOS).crop((0,25,320,295))
        im.paste(thumb,(x,y));d.text((x+10,y+272),e['baseCarId'],font=font(18,True),fill='#213447')
        d.text((x+10,y+298),e['collectibleId'],font=font(12),fill='#546879')
    path.parent.mkdir(parents=True,exist_ok=True);im.save(path)

def publish():
    live=OUT/'Data/Themes/variant_catalog.json'
    if live.exists() and any(e.get('contentRevision',1)>1 for e in json.loads(live.read_text())['entries']):
        raise RuntimeError('R02 exists in the live project. Do not overwrite it with the R01 bake. Use Revisions/R02/review_sheets.py for current review exports.')
    cat=json.loads((BA/'Data/Themes/variant_catalog.json').read_text())
    validation=json.loads((BA/'Data/Themes/unity_validation.json').read_text())
    assert validation['passed'] and validation['variantCount']==150
    # All candidates without an approved recipe are disposable outputs of THIS bake only.
    valid={e['collectibleId'] for e in cat['entries']}
    recipe={(t['id'],f'C{c:03d}') for t in themes() for c in t['cars']}
    for root in [OUT,BA]:
        for f in list(root.rglob('*')):
            if not f.is_file():continue
            name=f.name.removesuffix('.meta');stem=pathlib.Path(name).stem
            invalid=stem.startswith('TR-') and stem not in valid
            invalid|=('_ThemeDecals' in name or '_Decals.mat' in name) and (f.parent.name,name[:4]) not in recipe
            invalid|=f.parent.name=='Base' and stem in ['C006','C013']
            if invalid:
                assert f.resolve().is_relative_to(root.resolve());f.unlink()
    # Match runtime-script GUIDs before copying serialized prefab/catalog references.
    for script in ['ThemedCarIdentity','ThemedCarCatalog']:
        local=P/f'Assets/TinyRiders/Runtime/Data/{script}.cs.meta';stage=B/f'Assets/TinyRiders/Runtime/Data/{script}.cs.meta'
        if local.exists():assert local.read_text()==stage.read_text(),'Runtime GUID differs: '+script
        else:shutil.copy2(stage,local)
    shutil.copytree(BA,OUT,dirs_exist_ok=True)
    for e in cat['entries']:
        assert (P/e['prefabPath']).exists() and (P/e['previewPath']).exists()
    with (OUT/'Data/Themes/variant_catalog.csv').open('w',newline='',encoding='utf-8') as f:
        fields=['collectibleId','baseCarId','themeId','colorwayId','displayName','prefabPath','previewPath','sourcePrefabPath','sourceGuid']
        w=csv.DictWriter(f,fieldnames=fields,extrasaction='ignore');w.writeheader();w.writerows(cat['entries'])
    history={'schemaVersion':1,'release':'themed_initial_v1','immutableIds':sorted(valid),'baseIds':{f'C{c:03d}':f'Car_{c:02d}' for c in range(1,16)},'retiredIds':[]}
    historyPath=OUT/'Data/Themes/id_registry.json'
    if historyPath.exists():assert set(json.loads(historyPath.read_text())['immutableIds']).issubset(valid),'Never silently retire released IDs'
    historyPath.write_text(json.dumps(history,indent=2))
    for t in themes():
        es=[e for e in cat['entries'] if e['themeId']==t['id']]
        sheet(es,t['name'],OUT/'Preview/ContactSheets'/(t['id']+'.png'))
        sheet(es,t['name'],SRC/'QA/ReverseSheets'/(t['id']+'.png'),True)
    cols=5;cw=300;ch=290
    im=Image.new('RGB',(1560,1310),'#E8EDF2');d=ImageDraw.Draw(im)
    d.text((30,24),'THE COLLECTIBLE GARAGE',font=font(40,True),fill='#20364B')
    d.text((32,80),'20 THEME FAMILIES   /   150 COLLECTIBLES   /   ORIGINAL SUPPLIER MESHES',font=font(18),fill='#5A6D7E')
    for i,t in enumerate(themes()):
        candidates=[e for e in cat['entries'] if e['themeId']==t['id']]
        e=next((e for e in candidates if e['baseCarId']=='C001'),candidates[0]);x=30+(i%cols)*cw;y=130+(i//cols)*ch
        im.paste(Image.open(P/e['previewPath']).convert('RGB').resize((290,290),Image.Resampling.LANCZOS).crop((0,25,290,260)),(x,y))
        d.text((x+12,y+232),t['name'],font=font(21,True),fill='#20364B')
        d.text((x+12,y+261),t['id']+'  /  '+str(len(candidates))+' cars',font=font(13),fill='#5A6D7E')
    im.save(OUT/'Preview/CollectionOverview.png')
    for view in ['Front','Reverse']:
        for page in range(4):
            proof=Image.new('RGB',(2000,1180),'#E8EDF2');pd=ImageDraw.Draw(proof)
            pd.text((20,12),f'{view.upper()} VISUAL PROOF / {page+1}',font=font(23,True),fill='#20364B')
            for row,t in enumerate(themes()[page*5:(page+1)*5]):
                pd.text((20,54+row*224),t['id'],font=font(17,True),fill='#20364B')
                for col,e in enumerate(e for e in cat['entries'] if e['themeId']==t['id']):
                    source=e['previewPath'] if view=='Front' else e['previewPath'].replace('/Themes/','/Reverse/',1)
                    thumb=Image.open(P/source).convert('RGB').resize((240,240),Image.Resampling.LANCZOS).crop((0,28,240,208))
                    x=col*250;y=76+row*224;proof.paste(thumb,(x,y));pd.text((x+15,y+180),e['baseCarId'],font=font(13),fill='#20364B')
            (SRC/'QA').mkdir(parents=True,exist_ok=True);proof.save(SRC/'QA'/f'{view}_Proof_{page+1}.png')
    original=json.loads((SRC/'source_hashes.json').read_text())
    changed=[p for p,h in original.items() if not (P/p).exists() or hashlib.sha256((P/p).read_bytes()).hexdigest()!=h]
    hashes={str(p.relative_to(P)).replace('\\','/'):hashlib.sha256(p.read_bytes()).hexdigest() for p in OUT.rglob('*') if p.is_file() and p.suffix!='.meta'}
    report={'supplierFilesChecked':len(original),'supplierChanges':changed,'publishedVariants':len(cat['entries']),'themeCount':len(themes()),'usedBaseCars':sorted({e['baseCarId'] for e in cat['entries']}),'excludedBaseCars':{'C006':'Narrow decal surfaces: family badge too small at sorting-camera size.','C013':'Only a thin bumper-strip decal; no suitable large badge surface without changing mesh/UVs.'},'newMeshes':0,'newShaders':0,'frontPreviews':150,'reversePreviews':150,'themeContactSheets':20,'sourceIntegrityPassed':not changed,'unityValidation':validation}
    (SRC/'delivery_validation.json').write_text(json.dumps(report,indent=2));(SRC/'output_hashes.json').write_text(json.dumps(hashes,indent=2))
    assert not changed
    print(json.dumps(report,indent=2))
if __name__=='__main__':publish()
