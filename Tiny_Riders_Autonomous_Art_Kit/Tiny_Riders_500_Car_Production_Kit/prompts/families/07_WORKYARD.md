# 07 — Workyard Family Production Prompt

**Original source board:** `references/original_boards/07_workyard.png`  
**Package study crop:** `references/packaging_crops/07_workyard.png`  
**Catalog family code:** `WY`  
**Mandatory shared packaging master:** `docs/03_PACKAGING_MASTER_LOCK.md`

## Mission
Make FIVE genuinely distinct chunky toy car base silhouettes from the exact family reference (not recolors) plus a physical custom build for each, followed by five 2D livery variations per build. **Only execute these assets when phase P05–P08 authorizes the relevant family and batch.** Do not proactively make all 50 SKUs while phase gates are pending.

## Family art lock
Mood: **bright construction machinery, industrial block geometry**.
Colors for original 2D graphics/art direction: primary `#EBAE21`, secondary `#283B49`, accent `#F47E28`. Match actual family board visually, not merely numeric swatches. Keep thick tires, simple windows, blocky geometry, pleasing toy proportions, minimal facets/controlled bevels.

## Five required unique base shapes
- **01. Tipper** — Geometry: square dump truck cab with open blocky rear dump bed. Physical custom build: higher raised bucket mechanism and striped bed sides.
- **02. Mixer** — Geometry: small cab with simple medium cylindrical mixing drum. Physical custom build: larger tilted drum with bold belt and rear chute.
- **03. Lift** — Geometry: forklift with compact exposed driver cage and front tines. Physical custom build: taller chunky mast and safety roof.
- **04. Crane** — Geometry: small truck-mounted mobile crane with compact cabin and simple boom. Physical custom build: extended chunky articulated crane and hanging hook.
- **05. Loader** — Geometry: short front-loader tractor with a big flat bucket. Physical custom build: raised front bucket and reinforced cab cage.

## Geometry and UV print distinction
All car shapes, wheels, physical custom equipment and physically protruding large decorative pieces are geometry. All model names, body numbers, stripes, faces, food label writing, side logos, siren emblems, safety stripes and package copy are **2D UV image textures**. Do not create a single 3D word. Dark windows can be part of the UV printed area. No imported screenshot as texture.

## Family package requirements
Use exact shared 120×180mm front/back cardboard and low-poly blister geometry approved in P03. Same logo, same hanger, same hero-image region, same bottom product bar, same rarity/number badge. Preserve only this family's **palette and background motif**, use the exact actual rendered toy car as hero art and packaged physical object. No fabricated licensed product art. Use approved name and SKU from `catalog/collectibles_500.json`. Reference style identity should be clear but must never break the global layout.

## Review gates
1. Show black-silhouette lineup of all five bases.
2. Show realistic comparison to provided source board in neutral three-quarter, side, top and gray clay views.
3. Show standard/custom physical difference without printed color.
4. Show five livery previews on the approved geometry, with separate flat UV art.
5. Show all cards aligned in a same-scale package contact sheet; check brand, cutout, banner, hero, blister and footer align EXACTLY.
6. Stop for human sign-off; no bulk generation without approval.
