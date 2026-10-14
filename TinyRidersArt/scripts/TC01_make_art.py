"""Rebuild the flat, UV mapped Blue Racer pilot artwork. Requires Pillow."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "textures" / "cars" / "TC-01"
OUT.mkdir(parents=True, exist_ok=True)
SIZE = 2048

BLUE = (25, 93, 216)
BLUE_LIGHT = (45, 119, 240)
BLUE_DARK = (9, 50, 134)
WHITE = (245, 247, 245)
INK = (24, 29, 46)
GLASS = (31, 39, 61)
GLASS_LIGHT = (45, 54, 77)
ORANGE = (255, 130, 31)
CREAM = (255, 245, 205)

REGIONS = {
    "BODY_TOP": (32, 32, 1000, 448),
    "BODY_LEFT": (32, 512, 1000, 840),
    "BODY_RIGHT": (32, 900, 1000, 1228),
    "BODY_FRONT": (1050, 32, 1520, 448),
    "BODY_REAR": (1560, 32, 2030, 448),
    "CABIN_FRONT": (1050, 512, 1520, 840),
    "CABIN_REAR": (1560, 512, 2030, 840),
    "CABIN_LEFT": (1050, 900, 1520, 1228),
    "CABIN_RIGHT": (1560, 900, 2030, 1228),
    "CABIN_ROOF": (32, 1300, 1000, 1720),
}

img = Image.new("RGB", (SIZE, SIZE), BLUE)
d = ImageDraw.Draw(img)
font = ImageFont.truetype(r"C:\Windows\Fonts\arialbd.ttf", 164)


def centered(text, box, fill, use_font=font):
    x0, y0, x1, y1 = box
    bb = d.textbbox((0, 0), text, font=use_font, stroke_width=0)
    tx = x0 + ((x1 - x0) - (bb[2] - bb[0])) / 2 - bb[0]
    ty = y0 + ((y1 - y0) - (bb[3] - bb[1])) / 2 - bb[1]
    d.text((tx, ty), text, font=use_font, fill=fill)


# A broad white painted ribbon across the hood, with a blue race number.
d.rectangle((50, 179, 980, 295), fill=WHITE)
d.polygon([(125, 120), (405, 120), (455, 354), (125, 354)], fill=WHITE)
centered("23", (130, 155, 425, 330), BLUE_DARK)
d.rectangle((52, 169, 980, 178), fill=BLUE_LIGHT)

# Both side paintings are independently laid out so UVs need not mirror letters.
for key in ("BODY_LEFT", "BODY_RIGHT"):
    x0, y0, x1, y1 = REGIONS[key]
    d.rectangle((x0 + 28, y0 + 280, x1 - 22, y0 + 296), fill=WHITE)
    d.rectangle((x0 + 455, y0 + 62, x0 + 748, y0 + 251), fill=WHITE)
    centered("23", (x0 + 455, y0 + 66, x0 + 748, y0 + 245), BLUE_DARK)
    d.rectangle((x0 + 744, y0 + 255, x0 + 827, y0 + 294), fill=ORANGE)

# Headlight and grille marks are ink on the front body surface.
d.rounded_rectangle((1074, 139, 1162, 286), radius=14, fill=INK)
d.rounded_rectangle((1083, 148, 1152, 277), radius=10, fill=CREAM)
d.rounded_rectangle((1408, 139, 1496, 286), radius=14, fill=INK)
d.rounded_rectangle((1418, 148, 1487, 277), radius=10, fill=CREAM)
d.rounded_rectangle((1190, 265, 1380, 332), radius=10, fill=INK)
d.rectangle((1205, 287, 1365, 300), fill=GLASS_LIGHT)

# Rear lamps and a recessed dark panel.
d.rounded_rectangle((1585, 154, 1662, 296), radius=12, fill=INK)
d.rounded_rectangle((1594, 164, 1653, 286), radius=8, fill=(229, 55, 46))
d.rounded_rectangle((1928, 154, 2005, 296), radius=12, fill=INK)
d.rounded_rectangle((1937, 164, 1996, 286), radius=8, fill=(229, 55, 46))
d.rounded_rectangle((1714, 280, 1876, 331), radius=8, fill=INK)

# Opaque cabin windows are painted directly onto the actual wedge faces.
for key in ("CABIN_FRONT", "CABIN_REAR", "CABIN_LEFT", "CABIN_RIGHT"):
    x0, y0, x1, y1 = REGIONS[key]
    pad_x = 28 if "FRONT" in key or "REAR" in key else 33
    d.polygon(
        [(x0 + pad_x + 14, y0 + 29), (x1 - pad_x - 14, y0 + 29),
         (x1 - pad_x, y1 - 36), (x0 + pad_x, y1 - 36)],
        fill=GLASS,
    )
    d.line((x0 + pad_x + 34, y0 + 55, x0 + pad_x + 44, y1 - 72), fill=GLASS_LIGHT, width=10)

# The roof ribbon continues the printed hood stripe.
d.rectangle((55, 1455, 978, 1568), fill=WHITE)
d.rectangle((55, 1445, 978, 1454), fill=BLUE_LIGHT)

texture = OUT / "TC-01_BlueRacer_23_Albedo.png"
img.save(texture, optimize=True)

guide = img.copy()
g = ImageDraw.Draw(guide)
small = ImageFont.truetype(r"C:\Windows\Fonts\arialbd.ttf", 26)
for name, rect in REGIONS.items():
    g.rectangle(rect, outline=(245, 216, 57), width=5)
    g.rectangle((rect[0] + 4, rect[1] + 4, rect[0] + 320, rect[1] + 40), fill=INK)
    g.text((rect[0] + 10, rect[1] + 6), name, font=small, fill=WHITE)
guide.save(OUT / "TC-01_UV_Artwork_Guide.png", optimize=True)

print(texture)
print(OUT / "TC-01_UV_Artwork_Guide.png")
