"""Populate the user-supplied locked package templates with the actual car render.

This is a layout/fit candidate. The five-car back lineup remains pending until
the other family masters exist. It cannot be counted as a finished SKU package.
"""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont,ImageOps
import base64,json,hashlib,re,io
ROOT=Path(__file__).resolve().parents[1]
KIT=ROOT.parent/'Tiny_Riders_25_Master_Art_Production_Kit/Tiny_Riders_25_Master_Art_Production_Kit'
OUT=ROOT/'Textures/Packaging/Candidates';OUT.mkdir(parents=True,exist_ok=True)
hero=Image.open(ROOT/'Previews/Packaging/M03_exact_hero.png').convert('RGBA')
hero=hero.crop(hero.getchannel('A').getbbox());hero=ImageOps.contain(hero,(1020,505),Image.Resampling.LANCZOS)
front=Image.open(KIT/'design/packaging/png_templates/TC_front_template.png').convert('RGBA')
position=(90+(1020-hero.width)//2,460+(505-hero.height)//2)
front.alpha_composite(hero,position)
front.save(OUT/'TC_master_front.png')
back=Image.open(KIT/'design/packaging/png_templates/TC_back_template.png').convert('RGBA')
draw=ImageDraw.Draw(back);draw.rectangle((70,1697,1130,1760),fill='#FBFCFE')
draw.text((85,1725),'TR-TC-01-S-01  /  CORE  /  STANDARD',font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',24),fill='#123D8A',anchor='ls')
back.save(OUT/'TC_master_back.png')
# Preserve the full editable supplied template and insert the render as SVG image.
svg=(KIT/'design/packaging/svg/TC_front_template.svg').read_text()
buffer=io.BytesIO();hero.save(buffer,format='PNG');payload=base64.b64encode(buffer.getvalue()).decode()
slot=f'<image x="{position[0]}" y="{position[1]}" width="{hero.width}" height="{hero.height}" href="data:image/png;base64,{payload}"/>'
svg=re.sub(r'<!-- HERO_RENDER_SLOT:.*?-->',slot,svg)
(OUT/'TC_master_front.svg').write_text(svg,encoding='utf8')
svg=(KIT/'design/packaging/svg/TC_back_template.svg').read_text().replace('Working concept template — replace with approved production copy.','TR-TC-01-S-01 / CORE / STANDARD')
(OUT/'TC_master_back.svg').write_text(svg,encoding='utf8')
report={'status':'PACKAGING_LAYOUT_CANDIDATE','dimensionsPixels':[1200,1800],
    'heroSource':'Previews/Packaging/M03_exact_hero.png','heroPlacementPixels':[position[0],position[1],hero.width,hero.height],
    'logoSource':'design/brand/tiny_riders_logo.svg','logoSha256':hashlib.sha256((KIT/'design/brand/tiny_riders_logo.svg').read_bytes()).hexdigest(),
    'templateSource':'design/packaging/png_templates/TC_front_template.png',
    'fixedLayoutPreserved':True,'finishedPackageCount':0,
    'pending':['Actual five-car Track Club lineup on the back after all five identities are approved','Blister and card geometry validation','Unity appearance and transparency validation','Final SKU build/livery assembly']}
(OUT/'artwork_audit.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print('Locked package artwork candidate saved')
