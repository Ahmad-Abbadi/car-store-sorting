"""Derived contact sheets for the authoring catalog and Unity PNG previews."""

import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

PROJECT = Path(__file__).resolve().parent.parent
PRODUCTION = PROJECT / "TinyRidersProduction"
SHEETS = PRODUCTION / "ContactSheets"
SHEETS.mkdir(exist_ok=True)
catalog = json.loads((PRODUCTION / "source_catalog.json").read_text(encoding="utf-8"))
state = json.loads((PRODUCTION / "production_state.json").read_text(encoding="utf-8"))
records = {p["variantId"]: p for p in state.get("products", [])}
products = {p["variantId"]: p for p in catalog["products"]}
liveries = {(l["buildCode"], l["code"]): l["name"]
            for l in catalog["liveries"] + catalog["buildLiveries"]}
overrides = {(l["baseCarId"], l["buildCode"], l["code"]): l["name"]
             for l in catalog.get("liveryOverrides", [])}


def font(size):
    return ImageFont.truetype("C:/Windows/Fonts/arial.ttf", size)


def cell(identifier, width, height, small=False):
    result = Image.new("RGB", (width, height), (242, 245, 248))
    draw = ImageDraw.Draw(result)
    record = records.get(identifier, {})
    icon_path = PROJECT / record.get("thumbnailPath", "")
    if record.get("status") == "technical_validated" and icon_path.is_file():
        icon = Image.open(icon_path).convert("RGBA")
        size = min(width - 12, height - (42 if small else 64))
        icon = icon.resize((size, size), Image.Resampling.LANCZOS)
        layer = Image.new("RGBA", result.size, (0, 0, 0, 0))
        layer.alpha_composite(icon, ((width - icon.width) // 2, 2))
        result = Image.alpha_composite(result.convert("RGBA"), layer).convert("RGB")
        draw = ImageDraw.Draw(result)
    else:
        draw.text((12, height // 3), record.get("status", "planned").upper(), fill=(138, 55, 55), font=font(14 if small else 21))
    product = products[identifier]
    name = overrides.get((product["baseCarId"], product["buildCode"], product["liveryCode"]),
                         liveries[product["buildCode"], product["liveryCode"]])
    draw.text((8, height - (38 if small else 57)), identifier, fill=(25, 36, 50), font=font(12 if small else 18))
    draw.text((8, height - (20 if small else 29)), name, fill=(60, 77, 96), font=font(11 if small else 17))
    draw.rectangle((0, 0, width - 1, height - 1), outline=(205, 214, 223), width=1)
    return result


for base in catalog["baseCars"]:
    width, height = 310, 360
    sheet = Image.new("RGB", (width * 5, height * 2 + 64), (229, 234, 240))
    draw = ImageDraw.Draw(sheet)
    draw.text((14, 15), f'{base["id"]} · {base["sourceName"]}', fill=(20, 32, 48), font=font(30))
    for row, build in enumerate(("STD", "CUS")):
        for col in range(1, 6):
            identifier = f'TR-{base["id"]}-{build}-L{col:02d}'
            sheet.paste(cell(identifier, width, height), ((col - 1) * width, 64 + row * height))
    sheet.save(SHEETS / f'{base["id"]}.png')

width, height = 178, 207
overview = Image.new("RGB", (width * 10, height * 15 + 58), (229, 234, 240))
draw = ImageDraw.Draw(overview)
draw.text((15, 10), "Tiny Riders · initial_v1 · STD / CUS × L01–L05", fill=(20, 32, 48), font=font(30))
for row, base in enumerate(catalog["baseCars"]):
    for build_index, build in enumerate(("STD", "CUS")):
        for livery in range(1, 6):
            identifier = f'TR-{base["id"]}-{build}-L{livery:02d}'
            overview.paste(cell(identifier, width, height, small=True), ((build_index * 5 + livery - 1) * width, 58 + row * height))
overview.save(SHEETS / "initial_v1_overview.png")
print(f"Saved {len(catalog['baseCars'])} base sheets and one overview in {SHEETS}")
