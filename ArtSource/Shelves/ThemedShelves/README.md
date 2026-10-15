# Themed shelf production

Forty Blender-authored relief designs share the approved 3-row, 5-column cabinet. Each design is used by both shelves in its theme. Palettes come from `Assets/TinyRiders/ThemedCollection/Data/Themes/theme_recipes.json`.

## Contents

- `Blends/`: 40 editable Blender 5.1 sources, each with 15 placement markers, editable badge parts, and packed base-color and metallic/smoothness atlases.
- `Textures/`: original 1024 × 1024 painted atlases and surface-property maps. Surface R is metallic; A is smoothness. Relief comes from geometry, so no normal map is required.
- `Renders/`: 40 hero renders, four checkpoint close-ups, and triangle-budget reports.
- `Scripts/`: deterministic texture, Blender geometry/export, contact-sheet, and source-audit scripts.
- Unity FBX exports, materials, prefabs and theme registry: `Assets/TinyRiders/Art/Shelves/Themed/`.
- Review sheets, Unity empty/filled captures and validation evidence: `IntegrationEvidence/ThemedShelves/`.

## Reproduce

From the project root, using Python with Pillow and installed Blender 5.1:

```powershell
python ArtSource/Shelves/ThemedShelves/Scripts/make_textures.py
& 'C:/Program Files/Blender Foundation/Blender 5.1/blender.exe' --background --factory-startup --python ArtSource/Shelves/ThemedShelves/Scripts/build_themed_shelves.py
& 'C:/Program Files/Blender Foundation/Blender 5.1/blender.exe' --background --factory-startup --python ArtSource/Shelves/ThemedShelves/Scripts/audit_sources.py
python ArtSource/Shelves/ThemedShelves/Scripts/make_contact_sheets.py
```

For only the initial checkpoint, append `-- TH_ANCIENT_EGYPT TH_PIRATE TH_CANDY TH_SCIENCE_LAB` to the build command. Source scripts open the existing base cabinet in a dedicated background process; they do not use or clear a live Blender session.

In Unity Edit mode, use **Tools → Tiny Riders → Themed Shelves → Import Blender Art**, then **Apply To Current Room** with `Level` open. Import validates all required inputs before creating the registry. Apply replaces presentation only, compares every marker's world position and rotation, retains existing shelf objects and colliders, batches by theme and material, validates exact capacity, and saves the scene. It does not load, rewrite or award rewards to gameplay saves.

The registry uses stable `themeId` values. Adding a theme requires its Blender outputs and a registry entry; layout generation still requires exact catalog capacity. Room section colors cannot override themed materials. Labels remain live TextMeshPro objects for localization.

## Budgets and review

Every shelf has 15 usable compartments; two shelves provide exactly 30 slots per theme, 1,200 overall. Added relief uses at most 1,344 triangles per design, below the 3,000 limit. The art adds no gameplay colliders, realtime lights, transparency or emission.

Matching pairs share three batched meshes and materials. Shelf renderers including labels decreased from 720 to 320; submitted shelf mesh triangles decreased from 1,390,284 to 1,387,040. Unique visible shelf materials increased from 16 to 43 (40 theme atlases, two common materials, one label material). These are scene geometry statistics, not an FPS benchmark; 40 atlases intentionally trade additional texture memory for theme recognition.

Unity tests: `ThemedShelfArtTests`, `ExactShelfCapacityTests`, and `CompactCollectionTests.CompactRoom_CollectionSortingSaveCompletionAndReplay`. Tests use isolated QA save keys and restore upgrade preferences. The collection test captures fully occupied checkpoint shelves before checking reload and replay.
