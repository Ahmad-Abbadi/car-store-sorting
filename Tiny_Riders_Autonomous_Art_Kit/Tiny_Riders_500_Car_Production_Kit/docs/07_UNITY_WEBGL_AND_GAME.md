# 07 — Unity 6 / WebGL / CrazyGames Implementation

## Game fantasy
One expandable 3D toy storage/sorting garage with piles of items, empty shelves, a sorting table and a showroom. Player picks package or loose car, identifies family/model/edition, sorts it into the right slot, unlocks new shelves and discovers special variants. **No traditional level screen** is required; progression occurs inside the room.

## Asset structure and runtime data
- 50 approved master body meshes, with common reusable wheels, panels, accessories, materials, not 500 independent unique full-size prefabs.
- 100 body/build configurations = the base mesh plus custom attachment data; five livery texture identities per build = 500 SKUs.
- 1 master package geometry + fixed 2D template layout, color/category family skins, car-specific rendered hero and catalog metadata. At runtime only selected items load high fidelity artwork.
- `CollectibleDefinition` references one stable SKU, bodyID, physical build, UV art, family, product number and rarity.
- `CollectibleFactory` instantiates shared body mesh with appropriate wheels, accessories, material variants. `PackageFactory` uses shared cardboard/blister geometry + atlas composition.
- Save only product IDs, sorting progress, acquired editions, unlocked garage upgrades—not serialized meshes.

## Three view modes (IMPORTANT)
1. Pile: stylized cars/packages in a busy pile, low-poly, simplified colliders; physics only in active/nearby objects.
2. Shelf: distant lightweight package representation (simple textured card and flat blister highlight, no expensive physically transparent plastic for all objects).
3. Inspect: one actual 3D car inside 3D transparent blister with beautiful card art and correct name/number. Release resources when closed.

## Performance: targets, not guarantees
- Seek first load under ~20 MB if targeting mobile homepage, then use compressed streaming/addressable content. Re-verify *current* CrazyGames limits and SDK docs before final build.
- Pilot car starting target ~1000–3500 triangles; special work equipment up to ~5000 if essential. Selected packages ~500–1500 triangles, simplified distant card significantly cheaper. These are initial art-direction budgets, not magic thresholds.
- Low-poly style, original simple satin materials, limited transparent pixels, URP Simple Lit when appropriate, instanced materials/atlases and renderer pooling.
- Need actual runtime frame-rate, browser memory, shader variant and download size measurements. Start with 100 shelf items; stress test more in P09.
- Don't pack 500 full-resolution 1200×1800 front/back textures into initial Unity addressables. Atlas shelf thumbnails; use 2D templates + exact car renders for near-inspection views or streaming variants.
- Selective colliders, no rigidbody per decorative part, render only what camera shows.

## Scenes and systems
`Boot` → `CollectionRoom` (persistent sorting) with in-world gallery. Data import Editor script reads `catalog/collectibles_500.json`. Primary components `CollectibleDatabase`, `CarFactory`, `PackageFactory`, `InteractionController`, `ShelfSorter`, `CollectionProgress`, `SaveSystem`, `UpgradeManager`, `QualitySwitch`, `ContentValidationWindow`.

## Gameplay progression
Start with one family and 20 visible items. Unlock new family sections on collection milestones, not separate fixed levels. Money or stars fund sorting aids such as scan stickers, temporary magnet pickup, auto stacker and showcase lights. Rarity should be understandable but not predatory; no simulated real licensed pack gambling.

## WebGL acceptance
Run a REAL browser build, review small-window readability, GPU/memory stats, collision and dragging, load/save behavior, page reload and CrazyGames SDK QA. Never report Unity-ready when only `.fbx` exported and Blender rendered.
