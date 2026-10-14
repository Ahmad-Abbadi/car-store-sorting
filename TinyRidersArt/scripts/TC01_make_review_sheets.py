"""Compose A1 reference/preview sheets and rasterize projected mesh edges."""
from pathlib import Path
import json
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
RENDERS = ROOT / "renders" / "phase_A1"
REF = ROOT.parent / "Tiny_Riders_500_Car_Production_Kit" / "references" / "featured_cars" / "02_track_club.png"
DATA = ROOT / "qa" / "phase_A1" / "TC-01_projected_wire_edges.json"
UV_DATA = ROOT / "qa" / "phase_A1" / "TC-01_uv_edges.json"
FONT = r"C:\Windows\Fonts\arialbd.ttf"


def heading(draw, text, x, y, size=35, fill=(24, 44, 82)):
    draw.text((x, y), text, font=ImageFont.truetype(FONT, size), fill=fill)


wire = json.loads(DATA.read_text(encoding="utf-8"))
clay = Image.open(RENDERS / "TC-01_clay.png").convert("RGB")
overlay = Image.new("RGBA", clay.size, (0, 0, 0, 0))
d = ImageDraw.Draw(overlay)
for p, q in wire["projected_edges"]:
    d.line((p[0], p[1], q[0], q[1]), fill=(20, 39, 74, 180), width=2)
wire_image = Image.alpha_composite(clay.convert("RGBA"), overlay).convert("RGB")
wire_image.save(RENDERS / "TC-01_wireframe.png", optimize=True)

uv_data = json.loads(UV_DATA.read_text(encoding="utf-8"))
atlas = Image.open(ROOT / "textures" / "cars" / "TC-01" / "TC-01_BlueRacer_23_Albedo.png").convert("RGBA")
uv_overlay = Image.new("RGBA", atlas.size, (0, 0, 0, 0))
ud = ImageDraw.Draw(uv_overlay)
for p, q in uv_data["edges"]:
    ud.line((p[0], p[1], q[0], q[1]), fill=(255, 224, 47, 205), width=3)
Image.alpha_composite(atlas, uv_overlay).convert("RGB").save(
    ROOT / "textures" / "cars" / "TC-01" / "TC-01_UV_Actual_Overlay.png", optimize=True)

ref = Image.open(REF).convert("RGB")
beauty = Image.open(RENDERS / "TC-01_front_three_quarter.png").convert("RGB")
comparison = Image.new("RGB", (1900, 920), (245, 247, 250))
dc = ImageDraw.Draw(comparison)
heading(dc, "TRACK CLUB BLUE RACER #23 — REFERENCE / A1 PILOT", 40, 20, 41)
ref.thumbnail((730, 750), Image.Resampling.LANCZOS)
beauty.thumbnail((1090, 750), Image.Resampling.LANCZOS)
comparison.paste(ref, (40 + (730-ref.width)//2, 95 + (750-ref.height)//2))
comparison.paste(beauty, (790 + (1090-beauty.width)//2, 95 + (750-beauty.height)//2))
heading(dc, "Original concept crop", 215, 855, 28)
heading(dc, "Actual Blender pilot render", 1140, 855, 28)
comparison.save(RENDERS / "TC-01_reference_comparison.png", optimize=True)

names = [
    ("Front 3/4", "TC-01_front_three_quarter.png"),
    ("Side", "TC-01_side.png"),
    ("Rear 3/4", "TC-01_rear_three_quarter.png"),
    ("Top", "TC-01_top.png"),
    ("Clay", "TC-01_clay.png"),
    ("Mesh edge overlay", "TC-01_wireframe.png"),
]
sheet = Image.new("RGB", (1860, 1530), (241, 244, 249))
sd = ImageDraw.Draw(sheet)
heading(sd, "TC-01 BLUE RACER — A1 ART REVIEW", 35, 20, 44)
for i, (label, filename) in enumerate(names):
    col, row = i % 3, i // 3
    x, y = 35 + col * 610, 95 + row * 700
    im = Image.open(RENDERS / filename).convert("RGB")
    im.thumbnail((570, 580), Image.Resampling.LANCZOS)
    sheet.paste(im, (x + (570-im.width)//2, y + (580-im.height)//2))
    heading(sd, label, x+15, y+595, 29)
sheet.save(RENDERS / "TC-01_review_sheet.png", optimize=True)

print("REVIEW_SHEETS_OK", wire["evaluated_mesh_objects"], len(wire["projected_edges"]),
      "uv_edges", len(uv_data["edges"]))
