"""Fill missing theme/base UV atlases using the existing editable artwork recipes."""
import json
import pathlib
import sys

import numpy as np
from PIL import Image

from refine_atlases import layout_groups, patch as initial_patch

ROOT = pathlib.Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'ArtSource/ThemesExpansion01/Scripts'))
from artwork import patch as expansion_patch, THEMES as EXPANSION


def build():
    folder = ROOT / 'Assets/TinyRiders/ThemedCollection'
    recipes = json.loads((folder / 'Data/Themes/theme_recipes.json').read_text())['themes']
    expansion = {t['id']: t for t in EXPANSION}
    layouts = json.loads((ROOT / 'ArtSource/ThemedCars/decal_layouts.json').read_text())
    created = []
    size = 2048
    for theme in recipes:
        for car in range(1, 16):
            target = folder / f"Textures/Themes/{theme['id']}/C{car:03d}_ThemeDecals.png"
            if target.exists():
                continue
            atlas = Image.new('RGBA', (size, size))
            groups = layout_groups(layouts[str(car)])
            used = []
            for index, island in enumerate(groups):
                x, y, right, top = island['uv']
                ox, oy, original_right, original_top = x, y, right, top
                for j, other in enumerate(groups):
                    if j == index:
                        continue
                    u, v, ur, vt = other['uv']
                    if max(x, u) >= min(right, ur) or max(y, v) >= min(top, vt):
                        continue
                    gap = 3 / size
                    options = [(x, y, min(right, u-gap), top), (max(x, ur+gap), y, right, top),
                               (x, y, right, min(top, v-gap)), (x, max(y, vt+gap), right, top)]
                    options = [r for r in options if r[2] > r[0] and r[3] > r[1]]
                    if options:
                        x, y, right, top = max(options, key=lambda r: (r[2]-r[0]) * (r[3]-r[1]))
                box = (round(x*size), round((1-top)*size), round(right*size), round((1-y)*size))
                width, height = box[2]-box[0], box[3]-box[1]
                if min(width, height) < 8 or any(max(box[0], b[0]) < min(box[2], b[2]) and
                                                  max(box[1], b[1]) < min(box[3], b[3]) for b in used):
                    continue
                used.append(box)
                ratio = island['ratio'] * ((right-x)/(original_right-ox)) / ((top-y)/(original_top-oy))
                recipe = expansion.get(theme['id'], theme)
                art = (expansion_patch if theme['id'] in expansion else initial_patch)(recipe, max(.5, min(4, ratio)))
                art = art.resize((width, height), Image.Resampling.LANCZOS)
                direction = np.array(island['dv'])
                up = direction[1] if abs(direction[1]) > abs(direction[2])*.5 else -direction[2]
                if up < 0:
                    art = art.transpose(Image.Transpose.ROTATE_180)
                for dx in [-size, 0, size]:
                    for dy in [-size, 0, size]:
                        px, py = box[0]+dx, box[1]+dy
                        if px < size and py < size and px+width > 0 and py+height > 0:
                            atlas.alpha_composite(art, (px, py))
            target.parent.mkdir(parents=True, exist_ok=True)
            atlas.save(target)
            created.append(str(target.relative_to(ROOT)))
        print(theme['id'], flush=True)
    (ROOT / 'IntegrationEvidence/collection_completion_atlases.json').write_text(json.dumps(created, indent=2))
    print(f'Created {len(created)} missing atlases; existing artwork preserved.', flush=True)


if __name__ == '__main__':
    build()
