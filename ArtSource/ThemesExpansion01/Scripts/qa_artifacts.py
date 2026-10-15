"""Downsample real renders, assemble review sheets, and compare protected files."""
import pathlib,json,hashlib,csv
import numpy as np
from PIL import Image,ImageDraw,ImageChops
P=pathlib.Path(__file__).resolve().parents[3];D=P/'TinyRidersProduction/ThemesExpansion01';R=P/'Assets/TinyRiders/ThemedCollection';S=P/'ArtSource/ThemesExpansion01'

def sheet(items,path,cols=5,size=256,labels=True):
    rows=(len(items)+cols-1)//cols;out=Image.new('RGB',(cols*size,rows*(size+24 if labels else size)),(232,237,242));draw=ImageDraw.Draw(out)
    for j,(label,file) in enumerate(items):
        im=Image.open(file).convert('RGB');im.thumbnail((size,size),Image.Resampling.LANCZOS);x=j%cols*size;y=j//cols*(size+24 if labels else size);out.paste(im,(x+(size-im.width)//2,y))
        if labels:draw.text((x+6,y+size+3),label,fill=(30,40,54))
    path.parent.mkdir(parents=True,exist_ok=True);out.save(path)

def crop_car(file,width):
    im=Image.open(file).convert('RGB');a=np.asarray(im).astype(int);background=np.median(np.concatenate([a[0],a[-1],a[:,0],a[:,-1]]),axis=0);mask=np.max(abs(a-background),axis=2)>24;y,x=np.where(mask)
    if len(x):im=im.crop((max(0,x.min()-2),max(0,y.min()-2),min(im.width,x.max()+3),min(im.height,y.max()+3)))
    return im.resize((width,round(im.height*width/im.width)),Image.Resampling.LANCZOS)

def build():
    products=json.loads((D/'products.json').read_text(encoding='utf-8-sig'));themes=json.loads((D/'texture_recipes.json').read_text(encoding='utf-8-sig'))['themes'];thumbs=[]
    for p in products:
        file=P/p['previewPath']
        for width in [64,128]:
            im=crop_car(file,width);path=D/'Thumbnails'/str(width)/(p['variantId']+'.png');path.parent.mkdir(parents=True,exist_ok=True);im.save(path)
            if width==128:target=P/p['thumbnailPath'];target.parent.mkdir(parents=True,exist_ok=True);im.save(target)
        thumbs.append(p['variantId'])
    reps=[]
    for t in themes:
        rows=[p for p in products if p['themeId']==t['id']]
        if not rows:continue
        rep=next((p for p in rows if p['baseCarId']=='C001'),rows[0]);reps.append(rep)
        sheet([(p['baseCarId'],P/p['previewPath']) for p in rows],D/'ContactSheets'/(t['id']+'_Family.png'),cols=5,size=256)
        sheet([(p['baseCarId']+' '+view,D/'Views'/view/(p['variantId']+'.png')) for p in rows for view in ['Side','Elevated']],D/'ContactSheets'/(t['id']+'_Fits.png'),cols=6,size=240)
        sheet([(v,D/'Views'/v/(rep['variantId']+'.png')) for v in ['TextureOnly','Clay']]+[('Combined',P/rep['previewPath'])],D/'ContactSheets'/(t['id']+'_Layers.png'),cols=3,size=384)
    if reps:
        sheet([(p['themeId'][3:],P/p['previewPath']) for p in reps],D/'ContactSheets/ExpansionOverview.png',cols=5,size=320)
        sheet([('',P/p['previewPath']) for p in reps],D/'ContactSheets/MixedHiddenNames.png',cols=5,size=256,labels=False)
        for width in [64,128]:
            imgs=[Image.open(D/'Thumbnails'/str(width)/(p['variantId']+'.png')).convert('RGB') for p in reps];height=max(i.height for i in imgs);out=Image.new('RGB',(5*(width+12),((len(imgs)+4)//5)*(height+12)),(232,237,242))
            for j,im in enumerate(imgs):out.paste(im,(j%5*(width+12),j//5*(height+12)))
            out.save(D/'ContactSheets'/f'NativeCarWidth{width}_HiddenNames.png')
        sheets=[(t['id'][3:]+' '+v,S/'Renders'/(t['id']+'_'+v+'.png')) for t in themes for v in ['Color','Clay','Wire'] if (S/'Renders'/(t['id']+'_'+v+'.png')).exists()]
        sheet(sheets,D/'ContactSheets/AccessoryProof.png',cols=6,size=256)
    print('Reviewed artifacts generated for',len(products),'products;',len(reps),'themes')

def nonregression():
    baseline=json.loads((D/'baseline/hashes.json').read_text(encoding='utf-8-sig'));allowed={str((R/'Data/Themes'/x).relative_to(P)).replace('\\','/') for x in ['variant_catalog.json','ThemedCarCatalog.asset','theme_recipes.json','id_registry.json','variant_catalog.csv']};bad=[];checked=0
    for path,sha in baseline.items():
        if path in allowed:continue
        f=P/path;actual=hashlib.sha256(f.read_bytes()).hexdigest() if f.exists() else None;checked+=1
        if actual!=sha:bad.append(path)
    pixels=[]
    for f in (D/'baseline/Renders').glob('*.png'):
        other=D/'Nonregression'/f.name
        if not other.exists():pixels.append({'file':f.name,'missing':True});continue
        a=np.array(Image.open(f)).astype(int);b=np.array(Image.open(other)).astype(int)
        if not np.array_equal(a,b):pixels.append({'file':f.name,'changedPixels':int(np.any(a!=b,axis=2).sum()),'maxDelta':int(abs(a-b).max())})
    old=json.loads((D/'baseline/Files/Assets/TinyRiders/ThemedCollection/Data/Themes/variant_catalog.json').read_text(encoding='utf-8-sig'))['entries'];new=json.loads((R/'Data/Themes/variant_catalog.json').read_text(encoding='utf-8-sig'))['entries'];mapping={p['collectibleId']:p for p in new};oldchanges=[p['collectibleId'] for p in old if mapping.get(p['collectibleId'])!=p]
    report={'protectedFilesChecked':checked,'changedProtectedFiles':bad,'oldRendersCompared':len(list((D/'baseline/Renders').glob('*.png'))),'renderDifferences':pixels,'oldCatalogRecordsChanged':oldchanges,'passed':not bad and not pixels and not oldchanges};(D/'source_protection.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))

def comparisons():
    new=json.loads((D/'products.json').read_text(encoding='utf-8-sig'));old=json.loads((D/'baseline/Files/Assets/TinyRiders/ThemedCollection/Data/Themes/variant_catalog.json').read_text(encoding='utf-8-sig'))['entries'];allrows=old+new
    groups=json.loads((D/'Reports/confusion_groups.json').read_text(encoding='utf-8-sig'))
    for j,group in enumerate(groups):
        sets=[{p['baseCarId'] for p in allrows if p['themeId']=='TH_'+t} for t in group];common=set.intersection(*sets);base=sorted(common)[0] if common else None;items=[]
        for t in group:
            candidates=[p for p in allrows if p['themeId']=='TH_'+t];p=next((p for p in candidates if p['baseCarId']==base),candidates[0]);id=p['collectibleId'];file=(D/'baseline/Renders'/(id+'.png')) if p in old else P/p['previewPath'];items.append((t+' '+p['baseCarId'],file))
        sheet(items,D/'ContactSheets'/f'Confusion_{j+1:02d}.png',cols=len(items),size=384)
    oldthemes=sorted({p['themeId'] for p in old});newthemes=sorted({p['themeId'] for p in new});items=[]
    for ot,nt in zip(oldthemes,newthemes):
        op=next(p for p in old if p['themeId']==ot);new_product=next(p for p in new if p['themeId']==nt and p['baseCarId']==op['baseCarId']);items.extend([(ot+' '+op['baseCarId'],D/'baseline/Renders'/(op['collectibleId']+'.png')),(nt+' '+new_product['baseCarId'],P/new_product['previewPath'])])
    sheet(items,D/'ContactSheets/OldNewSameLightingAndBody.png',cols=8,size=240)
    for batch in range(5):
        themes=newthemes[batch*4:(batch+1)*4];items=[(p['themeId'][3:]+' '+p['baseCarId'],P/p['previewPath']) for t in themes for p in new if p['themeId']==t]
        sheet(items,D/'ContactSheets'/f'AllProductsReview_{batch+1}.png',cols=13,size=160)
    reps=[next(p for p in new if p['themeId']==t and p['baseCarId']=='C001') for t in newthemes]
    cameras=[(p['themeId'][3:],D/'Views/ActualCamera'/(p['collectibleId']+'.png')) for p in reps]
    if all(file.exists() for _,file in cameras):sheet(cameras,D/'ContactSheets/ActualCameraOverview.png',cols=4,size=384)
    noGlow=[]
    for p in reps:
        file=D/'Views/NoEmission'/(p['collectibleId']+'.png')
        if not file.exists():continue
        a=np.array(Image.open(file)).astype(int);b=np.array(Image.open(P/p['previewPath'])).astype(int);noGlow.append({'variantId':p['collectibleId'],'changedPixels':int(np.any(a!=b,axis=2).sum()),'maxChannelDelta':int(abs(a-b).max())})
    (D/'Reports/emission_comparison.json').write_text(json.dumps(noGlow,indent=2))
    print('Confusion, old/new, all-product and actual camera sheets saved')

if __name__=='__main__':
    import sys
    if 'nonregression' in sys.argv:nonregression()
    elif 'comparisons' in sys.argv:comparisons()
    else:build()
