# Tiny Riders — Rich Illustrated Shop Interior

Apply with **Tools → Tiny Riders → Collector Shop → Apply Shop Interior Polish**.
`ShopInteriorPolishBuilder.ApplyToCurrentRoom()` changes the current room without rebuilding shelves or the car pile. The full crowded-room builder calls the same implementation after sign-clearance adjustment.

## Artwork and scale

Run `python ArtSource/ShopInterior/Scripts/build_rich_interior_art.py` with Pillow to compose and pack the current artwork. `build_reference_art.py` is now a compatibility entry point for the same workflow, so it cannot restore the old logos. `build_interior_art.py` retains the original surface-map authoring recipe; its older skyline output is no longer used.

- Artwork was generated with the built-in image-generation tool. Eight category masters, one connecting-landscape sheet, and the exact prompts are saved under `RichArtwork/Masters` and `RichArtwork/Prompts`. Native generated images are approximately 2K wide; the 8K source atlas is a composed export, not a claim of native 8K generation.
- Source atlas: 8192 × 8192; runtime atlas: 4096 × 4096. Eight category panoramas contain forty unique scene environments. Five contiguous UV tiles per panorama allow efficient packing; they do not introduce visible panel borders or extra material slots. Extruded gutters protect the atlas from mip bleeding.
- `RichArtwork/composition.json` specifies source crops, physical lengths, feather widths, and colors. `RichArtwork/Layouts/*.svg` retains linked raster layers, clipping and masks; `RichArtwork/Masks` stores the editable grayscale masks. Generated scene details are raster artwork, as agreed. Rebuilding uses the JSON recipe; manual SVG edits require exporting their updated panorama before repacking.
- All illustrated layers preserve their proportions. Low-detail authored landscapes connect the five landmarks on longer walls; only connecting terrain repeats. Short entrance scenes use narrow ivory matting to preserve all five scenes without cropping their subjects. Each major theme scene appears once per category.
- Mural height: 2.06–3.105 m. The atlas uses approximately 350 runtime pixels/meter at this band. Five categories follow perimeter displays; Creative Corner, Party & Treats, and Grand Adventures remain at the entrance around the directory and doorway. Coast continues around its wall corner using a shared panorama offset. Cosmic Workshop fills the former rear branding reservation.
- The ceiling print now contains palette colors only. Its car, wordmark, and badge border are removed, while the teal inset and warm edge remain. The rear `Shop Brand` object and racing flags are removed on every application, including full-room rebuilds. The existing cove gradient continues to suggest warm lighting without adding lights or bloom.
- Wall plaster and ceiling acoustic surfaces have separate 2048 × 2048 base-color, tangent normal, and smoothness maps. Each tile covers 2 × 2 meters, giving 1024 texture pixels/meter.
- Smoothness is stored in alpha, with zero metallic in RGB. Base color is sRGB; normal and smoothness data are linear. Imports use mipmaps, trilinear filtering, 8× anisotropy, BC7 color/masks and BC5 normals on desktop.
- The project's active target remains WebGL. It uses Unity's high-quality compressed fallback (DXT1 color, DXT5 normal/masks in the current Editor); the desktop override does not change the build target or require a WebGL BPTC extension.
- Surface grain is intentionally subtle; it adds close-range variation without making distant surfaces noisy. Opposing texture edges are identical.

## Scene construction

The wall root has three combined meshes: plaster, dusty teal vertical panels, and mural/trim/sign-frame artwork. The ceiling root has five: acoustic fields, racing ribbon and teal inset, cove edges, slim housings, and opal diffusers. Each mesh uses one material. All eight renderers ignore raycasts, receive shadows, cast none, and have no colliders. Meshes and materials update in place and retain their asset GUIDs. The atlas-layout JSON is consumed by the Editor builder only; there is no runtime API or save-format change.

The original wall, floor, and ceiling colliders and transforms are retained. The original four wall rails and six diffuser renderers are disabled in favor of the combined finishes. Original diffuser objects and all six point lights remain in place. New ceiling geometry starts at 3.121 m; original fixtures previously reached 3.095 m. The directory reaches 3.09 m.

The original ceiling used an Unlit material without a shadow pass. Its backing renderer explicitly casts no shadow to preserve daylight reaching the floor. This reference pass preserves the preceding finish pass's lighting: directional intensity 0.72 and shadow strength 0.68. All six point-light locations, intensity, color, range, and shadows remain unchanged.

Ceiling panels use URP Lit with restrained direct reflectance and a textured emission fill to maintain warm white surfaces without nearby point-light hotspots. URP requires the material's BakedEmissive flag to retain its emission keyword; the decoration is **not Contribute GI**, so it creates no baked/realtime lighting. Diffusers use the existing URP Unlit shader for an even opal face. No realtime lights, post-processing, runtime scripts, or save-format changes are added.

## Review

`ArtSource/ShopInterior/Scripts/RichInteriorReview.cs` runs through Unity's `run-script` command. `Baseline()` records protected state and empty-room views; `Capture(string state)` renders eighteen views including entrance, center, outer aisle, corner, directory, ceiling, detail, overhead, and all eight themed walls. `Audit()` checks protected state, roots, material-slot budget, flags, colliders, and clearance. It also verifies that the rear brand is absent and ceiling-road triangles sample only solid palette UVs, with no image-mapped logo face. Category signs and directory remain checked against baseline.

Current captures and audit reports are in `IntegrationEvidence/ShopInterior/RichScenes`; previous artwork captures remain in `ReferenceMatch` for comparison. Overhead views temporarily hide the original ceiling and new ceiling root and restore their prior visibility afterward. The earlier racing-floor review helper uses the same hiding rule. Full-pile captures use the existing isolated QA save key, preserving player progress. The existing collection regression exercises pickup, drop/throw, shelf sorting, saving, completion, and replay.
