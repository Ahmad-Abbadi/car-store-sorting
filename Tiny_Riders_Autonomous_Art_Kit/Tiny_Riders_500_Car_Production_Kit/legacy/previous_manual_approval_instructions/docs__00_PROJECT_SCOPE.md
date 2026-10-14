# 00 — Project Scope and Deliverables

## What the player will see
A browser-based, 3D endless toy-collection sorting room with a large pile of chunky original collectible toy vehicles, shelves, packaging, attractive families, accurate model/category matching, discoveries, progress, and unlocks. **No forced individual numbered levels.** The same core sorting room can expand with more shelves and families.

## Catalog arithmetic (locked)
- 10 vehicle families shown in `references/original_boards/`.
- 5 distinct **physical base body silhouettes** per family = 50 bodies.
- 2 meaningful physical builds per body = 100 geometry configurations.
- 5 distinctly composed printed liveries per build = 500 collectible SKUs.
- Reuse one canonical blister card geometry and shared master layout. Ten family skins, one printable master composition, 500 product-specific artwork outputs (or composited runtime variants).

## What is prebuilt in this ZIP
- All 10 original generated concept boards, original files intact.
- `catalog/bodies_50.json` with each base-body shape and custom variant directive.
- `catalog/collectibles_500.csv` and `.json`, 500 stable IDs and planned attributes.
- Flat packaging **TEMPLATE SOURCES**, preview PNGs and packaging source crops.
- Strict master and phase prompts, QA plans, approval logs and a data validator.

## What must be built later in Codex/Blender/Unity
- All body geometry and component library meshes.
- Production car UV layouts, real individual livery atlases, car renders and product card artworks.
- Blender source projects, exports, actual Unity prefabs, gameplay, testing and browser build.
- Human visual approval at every defined gate. These are *not* included as finished items in this planning ZIP.

## Primary success condition
A 3D toy vehicle that looks pleasing in neutral clay and remains readable as a tiny thumbnail. Once approved, its printed livery must look correct on both sides. Its blister pack must preserve the locked layout and show the **exact same physical car** as the product hero render. The actual Unity implementation must load and perform acceptably on low-end browser hardware.

## Explicit out-of-scope for the first production pass
Photorealistic hot-wheel-like sports cars, official vehicle replicas, licensed franchises, animations for thousands of independent parts, functional car driving physics, detailed interiors, skeletal rigs, custom font models, 500 separately handcrafted full textures loaded simultaneously.
