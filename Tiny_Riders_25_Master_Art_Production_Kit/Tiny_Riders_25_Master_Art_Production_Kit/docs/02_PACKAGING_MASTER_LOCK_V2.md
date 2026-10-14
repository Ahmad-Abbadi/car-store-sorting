# Lock packaging across all 500 collectibles

**Visual source:** `references/original_boards/02_track_club.png` lower-left package references, reinforced by `design/packaging/MASTER_FRONT_LAYOUT_GUIDE.png`. The concept sheet's text and tiny details are illustrative; use the editable source template for actual printed artwork. All ten families share *one structural layout*.

## Dimensional contract

- Cardboard: **120 mm width × 180 mm height × approximately 0.7 mm thickness**.
- Round outer corners; **same hanger die-cut geometry for every SKU**, centered at `x=600` on 1200 px art scale, `y≈45..105`.
- Flat artwork source: **1200 × 1800 px** or vector equivalents, independent front and back, top-left image origin.
- One shared cardboard mesh + UVs; one common blister/flange footprint, allowing only approved depth fitting per car category. Card and blister are real geometry; printed graphics are 2D artwork only.

| Element | Canonical coordinates (px) |
|---|---|
| Outer card and artboard | x=0..1200, y=0..1800 |
| Tiny Riders source logo | x=100..1080, y=135..340 |
| Family stripe | x=100..620, y=350..430 |
| Correct car render hero | x=90..1110, y=460..965 |
| Physical blister front projection | x=86..1114, y=975..1515 |
| Product name strip | x=90..950, y=1542..1700 |
| SKU/rarity footer | x=955..1138, y=1560..1700 |
| Important artwork safe area | ≥55 px from trim |

Layout position tolerance on fixed zones: at most ±12 px. Do not interpret the 10 concept boards as permission to alter placement.

## Permanent assets

- Use exactly `design/brand/tiny_riders_logo.svg` as the **2D logo**. Never rebuild letters as mesh, curves, or a different logo.
- Use a consistent font family across all packaging; only permitted commercial fonts, with notes on licenses.
- Keep Track Club example as the layout authority, including where the logo, collection strip, visual hero, blister and model footer appear.
- Preserve stable SKU (e.g. `051/500`) distinct from printed livery number (e.g. race number 23).
- Do not reproduce third-party toy-company names, car logos, or copyrighted characters.

## Per-product variation

Only change family palette, original flat background motifs, *correct vehicle hero image*, name, rarity, SKU, family title, and intended rarity decoration. Render the assembled 3D **exact same** car for each printed hero view. Its wheels, attachment, color, livery and printed race number must match the physical car in package. Keep the printed space behind the blister free of an identical toy image. No AI-generative fake typography.

## Check procedure

1. Render flat fronts/backs side by side for one SKU from each family. Check relative coordinates and shared logo hash.
2. Render 10 packaged products in a single matching-camera contact sheet; measure card bounds and blister location.
3. Clay render removes ALL words, card ink, car lettering and stripes; only cardboard/blister/car shapes remain.
4. Compare the printed hero image to a separately rendered assembled car thumbnail; visually inspect critical parts, not just filenames.
5. Produce a failed-SKU report rather than silently shipping a mismatched card.
6. A selected-item Unity preview uses a high-quality card and actual 3D car/blister; crowded shelves may use simple textured-proxy images.

Do not turn 500 print-ready masters into 500 permanently loaded 1200×1800 Unity textures; store source masters outside runtime, generate appropriate compressed game derivatives.
