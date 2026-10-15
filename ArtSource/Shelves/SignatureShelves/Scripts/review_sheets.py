"""Assemble Blender and Unity review sheets from locally authored previews."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[4]
SRC=ROOT/'ArtSource/Shelves/SignatureShelves'
OUT=ROOT/'IntegrationEvidence/SignatureShelves'
FONT=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',22)
themes=sorted(p.stem for p in (SRC/'Reports').glob('*.json'))
sheet=Image.new('RGB',(2400,2250),'#17212d');draw=ImageDraw.Draw(sheet)
for i,theme in enumerate(themes):
    im=Image.open(SRC/'Renders'/(theme+'.png')).convert('RGB');im.thumbnail((300,405))
    x,y=(i%8)*300,(i//8)*450;sheet.paste(im,(x+(300-im.width)//2,y))
    draw.text((x+150,y+418),theme.removeprefix('TH_').replace('_',' ').title(),fill='white',font=FONT,anchor='mt')
sheet.save(OUT/'Blender_all_40.jpg',quality=95)
checkpoint=['TH_WILD_WEST','TH_PIRATE','TH_CANDY','TH_SCIENCE_LAB']
detail=Image.new('RGB',(2240,1600))
for i,theme in enumerate(checkpoint):
    im=Image.open(SRC/'Renders'/(theme+'_detail.png')).convert('RGB').resize((1120,800),Image.Resampling.LANCZOS)
    detail.paste(im,((i%2)*1120,(i//2)*800))
detail.save(OUT/'Blender_checkpoint.jpg',quality=95)
for state in ('checkpoint','empty','filled'):
    paths=[OUT/f'Unity_{state}_{t}_aisle.png' for t in checkpoint]
    if all(p.exists() for p in paths):
        view=Image.new('RGB',(1920,1440))
        for i,path in enumerate(paths):
            im=Image.open(path).convert('RGB').resize((960,720),Image.Resampling.LANCZOS);view.paste(im,((i%2)*960,(i//2)*720))
        view.save(OUT/f'Unity_{state}_checkpoint.jpg',quality=95)
print('Review sheets updated for',len(themes),'themes')
