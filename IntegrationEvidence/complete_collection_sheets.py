import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parents[1]
catalog = json.loads((root / 'Assets/TinyRiders/ThemedCollection/Data/Themes/variant_catalog.json').read_text())['entries']
font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 18)
title_font = ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf', 26)
for theme in ['TH_ANCIENT_EGYPT', 'TH_ALIEN']:
    entries = sorted((e for e in catalog if e['themeId'] == theme), key=lambda e: e['baseCarId'])
    assert len(entries) == 15, (theme, len(entries))
    sheet = Image.new('RGB', (1280, 870), '#e8edf3')
    draw = ImageDraw.Draw(sheet)
    draw.text((24, 14), f'{theme[3:].replace("_", " ")} — ALL 15 SHAPES', font=title_font, fill='#203047')
    for index, entry in enumerate(entries):
        source = root / entry.get('thumbnailPath', entry['previewPath'])
        image = Image.open(source).convert('RGBA')
        image.thumbnail((250, 230), Image.Resampling.LANCZOS)
        x, y = (index % 5) * 256, 56 + (index // 5) * 270
        sheet.paste(image, (x+(256-image.width)//2, y), image)
        draw.text((x+90, y+235), entry['baseCarId'], font=font, fill='#203047')
    target = root / f'IntegrationEvidence/{theme}_all_15.png'
    sheet.save(target)
    print(target)
