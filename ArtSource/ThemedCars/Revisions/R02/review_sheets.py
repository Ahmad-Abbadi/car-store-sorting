"""Create R02 reviews from the LIVE project's outputs; never republish the old bake."""
import sys,json,pathlib,csv
from PIL import Image,ImageDraw
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parents[2]/'Scripts'))
from theme_art import P,OUT,SRC,themes
from package_collection import font,sheet
REV=SRC/'Revisions/R02';QA=REV/'QA';QA.mkdir(exist_ok=True)
cat=json.loads((OUT/'Data/Themes/variant_catalog.json').read_text());entries=cat['entries'];ts=themes()
assert len(entries)==150 and all(e['contentRevision']==2 for e in entries)
def source(e,view):
    return P/(e['previewPath'] if view=='Front' else e['previewPath'].replace('/Themes/','/Reverse/',1) if view=='Reverse' else e['gameplayPreviewPath'] if view=='Gameplay' else e['thumbnailPath'])
for t in ts:
    es=[e for e in entries if e['themeId']==t['id']]
    sheet(es,t['name']+' / R02',OUT/'Preview/ContactSheets'/(t['id']+'.png'))
    sheet(es,t['name']+' / R02',QA/'ReverseSheets'/(t['id']+'.png'),True)
    im=Image.new('RGB',(len(es)*270+40,540),'#E8EDF2');d=ImageDraw.Draw(im)
    d.text((20,12),t['name']+' / 256 px perspective and 96 px thumbnail (native size)',font=font(22,True),fill='#213447')
    for i,e in enumerate(es):
        x=20+i*270;im.paste(Image.open(source(e,'Gameplay')).convert('RGB'),(x,65));im.paste(Image.open(source(e,'Thumbnail')).convert('RGB'),(x+80,360))
        d.text((x+25,330),e['baseCarId'],font=font(18),fill='#213447')
        d.text((x+6,485),e['collectibleId'],font=font(12),fill='#213447')
    path=OUT/'Preview/Multiscale'/(t['id']+'.png');path.parent.mkdir(parents=True,exist_ok=True);im.save(path)
for view in ['Front','Reverse','Gameplay','Thumbnail']:
    cell=250 if view in ['Front','Reverse'] else 270 if view=='Gameplay' else 180
    rowh=224 if view in ['Front','Reverse'] else 310 if view=='Gameplay' else 154
    for page in range(4):
        proof=Image.new('RGB',(cell*8+40,rowh*5+70),'#E8EDF2');d=ImageDraw.Draw(proof)
        d.text((20,12),f'R02 {view.upper()} / {page+1} / '+('NATIVE PIXEL SIZE' if view in ['Gameplay','Thumbnail'] else 'ALL VARIANTS'),font=font(23,True),fill='#20364B')
        for row,t in enumerate(ts[page*5:(page+1)*5]):
            y=52+row*rowh;d.text((20,y),t['id'],font=font(17,True),fill='#20364B')
            for col,e in enumerate(e for e in entries if e['themeId']==t['id']):
                thumb=Image.open(source(e,view)).convert('RGB')
                if view in ['Front','Reverse']:thumb=thumb.resize((240,240),Image.Resampling.LANCZOS).crop((0,28,240,208))
                x=20+col*cell;proof.paste(thumb,(x,y+23));d.text((x+6,y+23+thumb.height),e['baseCarId'],font=font(13),fill='#20364B')
        proof.save(QA/f'{view}_{page+1}.png')
overview=Image.new('RGB',(1560,1310),'#E8EDF2');d=ImageDraw.Draw(overview)
d.text((30,24),'THE COLLECTIBLE GARAGE / R02',font=font(38,True),fill='#20364B')
d.text((32,80),'20 THEME FAMILIES / 150 REFINED COLLECTIBLES / SAME PERMANENT IDS',font=font(18),fill='#5A6D7E')
comparison=Image.new('RGB',(2100,2180),'#E8EDF2');cd=ImageDraw.Draw(comparison)
cd.text((25,20),'R01 TO R02 / SAME CAR, STRONGER FAMILY IDENTITY',font=font(30,True),fill='#20364B')
for i,t in enumerate(ts):
    es=[e for e in entries if e['themeId']==t['id']];e=next((e for e in es if e['baseCarId']=='C001'),es[0]);x=30+(i%5)*300;y=130+(i//5)*290
    overview.paste(Image.open(P/e['previewPath']).convert('RGB').resize((290,290),Image.Resampling.LANCZOS).crop((0,25,290,260)),(x,y))
    d.text((x+12,y+232),t['name'],font=font(21,True),fill='#20364B');d.text((x+12,y+261),t['id']+' / '+str(len(es))+' cars',font=font(13),fill='#5A6D7E')
    x=25+(i%4)*520;y=85+(i//4)*415;cd.text((x,y),t['name']+' / '+e['baseCarId'],font=font(23,True),fill='#20364B')
    for j,folder in enumerate([SRC/'Revisions/R01/Collection',OUT]):
        p=folder/pathlib.Path(e['previewPath']).relative_to(pathlib.Path('Assets/TinyRiders/ThemedCollection'))
        comparison.paste(Image.open(p).convert('RGB').resize((250,250),Image.Resampling.LANCZOS),(x+j*255,y+50))
        cd.text((x+90+j*255,y+315),'R01' if j==0 else 'R02',font=font(20,True),fill='#20364B')
overview.save(OUT/'Preview/CollectionOverview.png');comparison.save(OUT/'Preview/BeforeAfter_R01_R02.png')
print('20 family sheets, 20 multiscale sheets, 16 proof sheets, overview and before/after created.')
