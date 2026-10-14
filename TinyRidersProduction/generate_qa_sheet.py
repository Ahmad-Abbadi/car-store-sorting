from pathlib import Path

from PIL import Image, ImageDraw


root = Path(__file__).resolve().parent / "Reports" / "QA"
for view in ("front", "side", "rear"):
    sheet = Image.new("RGB", (5 * 320, 3 * 340), "#e8edf3")
    draw = ImageDraw.Draw(sheet)
    for index in range(15):
        car_id = f"C{index + 1:03d}"
        path = root / f"TR-{car_id}-CUS-L01_{view}.png"
        with Image.open(path) as source:
            tile = source.convert("RGB")
            tile.thumbnail((310, 310))
            x, y = (index % 5) * 320, (index // 5) * 340
            sheet.paste(tile, (x + (320 - tile.width) // 2, y))
            draw.text((x + 10, y + 312), f"{car_id} CUS {view}", fill="#182235")
    sheet.save(root / f"all_custom_{view}.png")
