# Signature shelf art

Forty locally authored Blender designs extend the existing themed cabinets with a dimensional toy object on an 11.04 cm post and two textured, beveled header plaques. Both shelves in each theme share the same art. Cabinet dimensions, 15 placement markers, gameplay colliders and save identities are preserved.

## Sources and outputs

- `Blends/TH_*.blend`: editable Blender 5.1 scenes, with cabinet and signature runtime textures packed. Studio cameras and lights are for offline previews only.
- `Textures/TH_*_Signature_*_4K.png`: 4096 × 4096 base-color, tangent-normal and surface-property masters.
- `Renders/`: complete cabinets, transparent signature-object renders, and close-ups of Wild West, Pirate, Candy and Science Lab.
- `Reports/`: evaluated triangle counts, dimensions and version metadata for each design.
- `Assets/TinyRiders/Art/Shelves/Themed/Models/`: UV-mapped FBX exports, retaining the existing Unity asset identities.
- `Assets/TinyRiders/Art/Shelves/Themed/Textures/`: 2048 × 2048 signature runtime maps alongside the unchanged 1024-pixel cabinet maps.

Surface maps store metallic in red and smoothness in alpha. Blender converts smoothness to roughness; Unity uses the same map directly. The upper-left quarter of each signature atlas contains an offline render of its own 3D object for the header plaques. The other twelve tiles provide coordinated toy surfaces, fine normal detail and controlled highlights. Materials are opaque and do not add emission or realtime lights.

The original themed cabinet sources remain in `ArtSource/Shelves/ThemedShelves`. Palette and theme identities come from `Assets/TinyRiders/ThemedCollection/Data/Themes/theme_recipes.json`.

## Reproduce

Run from the project root with Blender 5.1 and Python 3.14 with Pillow and NumPy installed. The scripts use the installed Windows paths at the top of the source; update those paths for another machine. OptiX is used when available, with CPU rendering as fallback.

```powershell
& 'C:/Program Files/Blender Foundation/Blender 5.1/blender.exe' --background --factory-startup --python ArtSource/Shelves/SignatureShelves/Scripts/build_signature_shelves.py -- TH_WILD_WEST TH_PIRATE TH_CANDY TH_SCIENCE_LAB
```

Omit the theme IDs to rebuild all forty. Each run starts from the preserved cabinet source, so repeated builds do not accumulate toppers or plaques. The generation checks the 8,000-triangle decoration budget before exporting. Wide objects are uniformly scaled to fit 60 cm width and 35 cm depth; other objects are approximately 40.8 cm high. A small saddle bridges recessed central attachment surfaces where needed.

After Blender finishes, in Unity Edit mode use **Tools → Tiny Riders → Themed Shelves → Import Blender Art**, then **Apply To Current Room** with `Level` open. `ImportThemes(string[])` and `ApplyThemes(string[])` support checkpoint updates. The targeted migration preserves shelf IDs, marker positions and rotations, car catalog and pile configuration. Category signs are moved above the reserved topper band and the entrance directory is separated from the neighboring sign.

Each shelf has four combined art renderers: common frame, trays, cabinet paint, and signature art. Matching shelf pairs share all four cached meshes and materials. Original imported renderers are disabled after batching; they remain available for editing and geometry validation. The cache updates native mesh buffers in place, preserving asset GUIDs without displaying stale geometry.

## Validate and review

```powershell
& 'C:/Program Files/Blender Foundation/Blender 5.1/blender.exe' --background --factory-startup --python ArtSource/Shelves/SignatureShelves/Scripts/audit_sources.py
C:/Python314/python.exe ArtSource/Shelves/SignatureShelves/Scripts/review_sheets.py
```

Run Unity Editor tests `ThemedShelfArtTests`, `ExactShelfCapacityTests`, and `CompactCollectionTests`. They cover art coverage, dimensions, materials, labels, shared meshes, overhead clearance, baseline identity preservation, capacity, placement/rejection/takeback, old-save migration, reload, full completion and replay. Gameplay tests use isolated QA save keys.

Review images and measured before/after rendering statistics are saved under `IntegrationEvidence/SignatureShelves`. `Blender_all_40.jpg` is the complete design contact sheet; `Blender_checkpoint.jpg` contains the four detailed close-ups. Unity empty/filled checkpoint sheets show actual game lighting and aisle views. The metrics count enabled shelf renderers including theme labels; they are asset statistics, not a measured FPS comparison.
