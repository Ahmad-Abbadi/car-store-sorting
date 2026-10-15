"""Assemble the reproducible Blender previews into review sheets."""
from pathlib import Path
import json
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[4]
R = ROOT / 'ArtSource/Shelves/ThemedShelves/Renders'
OUT = ROOT / 'IntegrationEvidence/ThemedShelves'
OUT.mkdir(parents=True, exist_ok=True)
font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 22)
files = sorted(R.glob('*.json'))
sheet = Image.new('RGB', (2400, 2550), '#17212d')
draw = ImageDraw.Draw(sheet)
for i, report in enumerate(files):
    data = json.loads(report.read_text())
    im = Image.open(report.with_suffix('.png')).convert('RGB')
    im.thumbnail((300, 460))
    x, y = (i % 8) * 300, (i // 8) * 510
    sheet.paste(im, (x + (300-im.width)//2, y))
    name = data['theme'].removeprefix('TH_').replace('_', ' ').title()
    draw.text((x+150, y+465), name, font=font, anchor='mt', fill='white')
    draw.text((x+150, y+491), f"15 slots / {data['decorationTriangles']} badge tris", anchor='mt', fill='#abc2d1')
sheet.save(OUT / 'Blender_all_40.jpg', quality=94)
checkpoint = Image.new('RGB', (2200, 1400), '#17212d')
for i, theme in enumerate(['ANCIENT_EGYPT','PIRATE','CANDY','SCIENCE_LAB']):
    im = Image.open(R / f'TH_{theme}_detail.png').convert('RGB')
    checkpoint.paste(im, ((i%2)*1100, (i//2)*700))
checkpoint.save(OUT / 'Blender_checkpoint.jpg', quality=95)
for state in ('empty','filled'):
    paths=[OUT/f'Unity_{state}_TH_{theme}_aisle.png' for theme in ['ANCIENT_EGYPT','PIRATE','CANDY','SCIENCE_LAB']]
    if all(path.exists() for path in paths):
        unity=Image.new('RGB',(1920,1440))
        for i,path in enumerate(paths):
            im=Image.open(path).convert('RGB').resize((960,720),Image.Resampling.LANCZOS)
            unity.paste(im,((i%2)*960,(i//2)*720))
        unity.save(OUT/f'Unity_{state}_checkpoint.jpg',quality=95)
print('Created 40-theme contact sheet and four-theme close-ups')
