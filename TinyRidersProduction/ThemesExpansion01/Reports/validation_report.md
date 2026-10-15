# Verified expansion delivery

Twenty existing themes / 150 existing products are preserved. Twenty additional themes now cover all thirteen enabled themed base cars: **260 of 260 planned products**, twenty shared primary kits, no supporting kit clutter, and **410 total catalog entries**. No substitutions were needed. No planned product remains blocked. C006/C013 remain reserved exclusions from the earlier verified release; they were not silently omitted from an enabled selection.

## Validation

- Unity: 4740 original body mesh references checked across 260 saved prefabs; configuration, IDs, material assignment, one accessory root and uniform fit scale passed.
- Geometry: all 260 products have sampled contact with actual painted roof/deck triangles, and zero remaining glazing edge intersections. Thirty-eight separately removable roof fog-light modules were hidden only in the affected new prefab overrides. Original car bodies, UVs and supplier prefabs were not modified.
- Models: twenty real Blender sources and shared FBX files, 416–1400 triangles per kit, no boundary/nonmanifold edges or loose vertices in the recorded topology audit. These counts are not a performance guarantee.
- Regeneration: 540 artwork files were byte-identical; all twenty model geometry/UV fingerprints matched; 2760 Unity material/prefab/meta files and all 260 product records matched after unchanged assembly. No extra assets or IDs were introduced.
- Nonregression: 7314 protected files matched their original hashes. All 150 original reference renders were pixel-identical. Old runtime catalog serialization, portable records, recipes and release membership were preserved.
- Visual evidence: all 260 products in family sheets; twenty texture-only, combined, clay and wireframe theme checks; nine requested confusion comparisons; native 64/128-pixel car-width thumbnails; three partially occluded static piles; emission-off examples; exact existing project-camera projection tests. Original/new representatives use the same lighting, framing and body where compared.

## Practical limits

The supplier decal islands limit coverage, especially split hood regions and compact bodies. Larger door/rear panels carry the principal prints; source UVs and body geometry are unchanged. At 64 pixels, small print detail is reduced and palette/silhouette contribute most. The existing Level display pose produces a small sideways view, so neutral views and native-size thumbnails are the clearer readability evidence. Existing camera and gameplay scenes remain unchanged.

The separate small shelf envelope (0.464 × 0.54 × 0.648) already excludes the unscaled source cars. Shelf integration is unsupported; cars and shelves were not resized. Player recognition and target-device performance have not been measured.

See `INTEGRATION_README.md`, `products.json`, `fit_overrides.json`, the reports above and `ContactSheets/ExpansionOverview.png`.
