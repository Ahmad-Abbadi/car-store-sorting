# Tiny Riders — reusable base display shelf

Delivered 14 October 2026. One red shelf, three display rows, six spaces per row, a closed solid lower base, a blank category header, and a blank circular number badge. Cars appear only in the review scene.

## Main files

- Editable source: `TR_Shelf_Base_01.blend`
- Evaluated source copy: `TR_Shelf_Base_01_Export.blend`
- Imported art: `Assets/TinyRiders/Art/Shelves/BaseShelf_01/`
- Final prefab: `Prefabs/TR_Shelf_Base_01.prefab` within that art folder
- Geometry: `Models/TR_Shelf_Base_01.fbx`
- Preview scenes: `PreviewScenes/TR_Shelf_Empty_ArtPreview.unity` and `TR_Shelf_Filled_ArtPreview.unity`
- Review images: this source folder's `Renders/`; start with `Reference_Comparison.png` and `D_Unity_Filled.png`
- Editor authoring tool: `Assets/TinyRiders/Editor/TRBaseShelfArtBuilder.cs`; a source copy is in `Scripts/`

## Dimensions and scale

The approximate 1.20 × 1.20 × 0.30 m concept envelope was uniformly enlarged **3×** to preserve the game's established car scale. Nominal dimensions are 3.60 × 3.60 × 0.90 m. Actual rendered mesh bounds, including the inset signs, bevels and projecting trim, are **3.633 m wide × 3.600 m high × 0.927 m deep**.

All 150 `Shelf_TR-C…` prefabs in the existing GeneratedCarStore were measured using active, enabled mesh/skinned-mesh renderers, including visible roof accessories and spoilers. Maximum bounds: 0.38730 m wide, 0.38193 m tall, 0.58000 m long. No car scale, supplier mesh, material, prefab or ID was changed.

Conservative usable space per slot: 0.464 m width, 0.648 m depth, 0.540 m height. The worst-case total margins are 0.07670 m width, 0.06800 m depth and 0.15807 m height. The 3° tray rise and lowered tapered divider backs preserve visibility. These margins are total clearances, not margins on each side.

## Axes and markers

- Unity root `TR_Shelf_Base_01`: origin at the floor center, rotation (0,0,0), scale (1,1,1), +Y up, +Z toward the open front.
- Blender source: +Z up, -Y open front. The FBX exporter uses -Z forward / Y up, `FBX_SCALE_ALL`, and does not bake the experimental space transform. The Unity builder verifies the front/back relationship.
- Rows run top to bottom; columns run left to right **when facing the shelf**. Thus column 1 is at Unity +X, column 6 at Unity -X.
- `PlacementMarkers/Row_01/Slot_R01_C01` through `Slot_R03_C06` are empty transforms. Origins are car ground-contact centers on the tray, local up follows the surface, and forward points out with the 3° tray slope.
- Column centers in Unity X are +1.25, +0.75, +0.25, -0.25, -0.75, -1.25 m. Row nominal heights are 2.460, 1.695 and 0.930 m; markers include the small surface-rise offset.
- Imported tray surfaces and marker origins were checked numerically; maximum surface error was under 0.000001 m.

## Header replacement and recoloring

`Geometry/Header/HeaderFace` has its own `MAT_Header` material and predictable full-face 0–1 UVs. Replace the Base Map texture on **MAT_Header** with artwork of the same 8:1 aspect ratio. The default `TEX_Header_Blank.png` has checker graphics at the ends and an empty center. There is no runtime text/category system.

All red paint, including the header border and trim housings, uses **MAT_SidePanels_Red**. Change its **Panel paint / _BaseColor** color to recolor the shelf. `TinyRiders/PrintedPaint` is a small URP lit shader that blends flat printed ink using the texture alpha; the checker graphics and badge remain unchanged when the paint color changes. The shader is included in `Materials/TRPrintedPaint.shader`.

In Blender, change the paint-color input on the Mix node in the same named material. The texture Alpha masks ink over that color. No printed detail is modeled as geometry or placed on a floating decal plane.

## Textures and editable artwork

| Texture | Resolution | Use |
| --- | --- | --- |
| TEX_Header_Blank | 2048 × 256 | White header, checker ends, blank center |
| TEX_SidePanel | 512 × 2048 RGBA | Circular blank badge and side checks; transparent paint region |
| TEX_Printed_Left | 512 × 1024 | DRIVE / SORT / COLLECT |
| TEX_Printed_Right | 512 × 1024 | SMALL / CARS / BRIGHTER / DAYS |

Editable SVG masters and PNGs are in `Artwork/`. `Scripts/make_artwork.py` rebuilds the raster artwork using Pillow. Font: **Arial Narrow Bold Italic**, the installed Windows font `ARIALNBI.TTF`. Font files are not bundled. Blender texture paths are relative to this source folder.

## Geometry and rendering

The complete shelf has **18,316 evaluated triangles**, matching the imported Unity prefab. Repeated trays and dividers share source mesh data; bevel and weighted-normal modifiers remain editable in the main .blend. The export-copy meshes have evaluated modifiers. There are no cars, runtime behaviours, realtime lights, rigidbodies or colliders in the shelf prefab.

The prefab uses the project's existing Unity 6000.3.22f1 / URP 17.3.0 pipeline. Warm accents use `MAT_Accent_Warm` emission. Set its emission color to black to disable emission; the physical diffuser remains warm colored. No bloom or per-slot lights are required. Emission alone does not cast realtime light onto the backing; the Unity preview deliberately shows this without baked illumination or bloom.

The empty and filled preview scenes contain a neutral ground, camera and three studio lights. Preview objects use layer 31 only within these scenes to isolate camera captures from the already-open game scene. The reusable prefab remains on its ordinary layer. Existing cars are nested prefab instances under separate placement wrappers; no supplier assets are overwritten. Captures are rendered by the actual URP camera through `RenderPipeline.SubmitRenderRequest`.

## Rebuilding

1. Run `python ArtSource/Shelves/BaseShelf_01/Scripts/make_artwork.py` from the project root.
2. Run Blender in background with `--factory-startup --python ArtSource/Shelves/BaseShelf_01/Scripts/build_shelf.py`. This builds the source, exports FBX, and renders the checkpoint images. For export only, run `Scripts/export_shelf.py` instead.
3. In Unity Edit mode, run **Tools → Tiny Riders → Base Shelf → Measure Existing Cars**, then **Build Prefab and Previews**. The builder updates existing generated asset paths without replacing their .meta files.
4. Run the standalone `Scripts/ValidateShelf.cs`, entry `TRShelfAcceptance.Run`, through Unity Pipeline `run_script` for numerical acceptance checks.

The builder opens additive preview scenes and leaves the pre-existing scene loaded. The final Scene view is isolated to the generated preview; exit Scene visibility isolation to see other loaded scenes again. No global render-pipeline or game-lighting assets were changed.

## Level scene integration

`Assets/00_Project/00_Scenes/Level.unity` now uses this shelf art for all 150 existing `SortingShelf` objects. The integration keeps each shelf's accepted variant, save ID, status display, and seven gameplay spots. The spots align to seven of the art prefab's 18 markers, so existing saved spot indices still apply. Model sections use two rows of five shelves with an aisle between them. The replacement removes the old rack presentation and hides the sorting prefab's small slab; simple plinth and back colliders provide solid cabinet collision. The new art prefab itself remains free of gameplay scripts and colliders.

To reapply the layout to the current Level scene, use **Tools → Tiny Riders → Base Shelf → Replace Level Shelves**. This saves a one-time copy of the loaded Level at `Assets/TinyRiders/Art/Shelves/BaseShelf_01/Backup/Level_before_shelf_replacement.unity` before editing. `CarStoreBuilder.Generate()` also applies the replacement when it regenerates the store.

## Verification and limitations

Car fit and actual Unity appearance were verified. See `QA_REPORT.md` and the saved JSON evidence. Future car configurations outside the current 150 shelf-car prefabs have not been measured. The Unity preview is intentionally less glowy than the concept board; production GI or bloom is not implemented. The reusable art prefab remains static; the Level scene retains its existing sorting and inventory components.
