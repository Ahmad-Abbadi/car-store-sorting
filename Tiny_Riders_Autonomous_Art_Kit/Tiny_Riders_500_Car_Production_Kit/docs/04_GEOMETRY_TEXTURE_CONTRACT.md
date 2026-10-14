# 04 — Geometry / Texture / Material Contract

## The decision rule
**If it would be PRINTED on a real toy/card, it goes into a FLAT image texture. If it occupies meaningful PHYSICAL VOLUME, it becomes geometry. If it is only LIGHT/SHINE, use a MATERIAL or the lighting setup.**

| Feature | Correct production representation | Forbidden representation |
|---|---|---|
| Vehicle body / roof / real fin | Low-poly physical mesh | Flat billboard for a close-up interactive car |
| Wheel, axle volume, roof rack | Simple instanced mesh | A pasted wheel side image without any tire volume |
| Door numbers / text / model name | 2D UV-mapped printed PNG/SVG | Blender FONT mesh, SVG extrusion, individual floating character polygons |
| Racing stripe / checker pattern / flame | 2D UV-mapped printed texture | Thin white stripe meshes or spline tubing |
| Window / windshield on this simplified style | Dark UV-mapped patch on cabin geometry | Complex translucent glass and interior, floating window polygon |
| Eyes, teeth, mouth, painted headlights | 2D texture on approved body surfaces | Separate tooth meshes, face text meshes, glowing modeled icons |
| Large dorsal fin/horn/food topping | Mesh attached to body | Baking an important silhouette change into a flat image |
| Package design / artist art / barcode-like marks | 2D UV-mapped front/back texture | Carved text, physically raised decals |
| Card, die-cut, molded blister | Physical simple meshes | Screenshot of the entire product printed on the card |
| Gloss, specular glints, shadow | Materials and real lighting | Artificial geometry or painted white highlights on car color map |

## Texturing sequence — mandatory
1. Finish unprinted *clay* model and approve silhouette.
2. UV unwrap the **actual approved car**; each text-bearing side gets correct independent UV orientation. No mirrored letters.
3. Export UV guide and create external vector/raster artwork aligned to exact UVs.
4. Compose opaque basecolor with readable icons, windows, lights, printed side numbers. Keep gloss out of albedo.
5. Use one shared flat art source per livery/build as appropriate, not text or SVG curves imported as geometry.
6. Add limited roughness/materials (plastic satin; tire matte; cardboard coated; blister clear for selected view). Avoid unused 4K maps and excessive material copies.
7. Gray clay QA: all print and windows disappear; physical objects remain.
8. Export a final loose-car render, UV preview, and actual flat texture files in PNG/SVG.

## UV / typography tests
- Image text must be spelled correctly (`TINY RIDERS`, family and model names, numbers), appropriate commercial font to be identified but not included as a font file.
- Front/back package UV seams cannot cross logo or product text; they must not be reversed.
- For car sides, UV islands are not blindly mirrored when printed text exists; right and left are both readable.
- Atlas padding and texture filtering tested at both 128px and 512px presentation widths; lettering remains crisp in selected inspection mode, while small unreadable legal microtext is omitted.
- If an AI image generator is used for family scenic backgrounds, it must produce backgrounds only. Never let image AI invent final word spelling or fake car hero illustrations.

## Asset-output minimum
`{sku}/source.blend` (or shared original body), mesh `.fbx`, albedo PNG, flat SVG/PNG livery source, UV overlay, clay PNG, beauty 3/4 PNG, top/side/rear renders, package front/back flat art, packaged render, text/UV check, metadata link and QA report.
