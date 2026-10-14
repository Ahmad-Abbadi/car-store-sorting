THEMED COLLECTIBLE CARS — INITIAL RELEASE

150 permanent collectibles across 20 theme families, using 13 of the 15 supplied cars.
No new meshes, shaders, downloaded assets, packaging, shelves, environments, or gameplay.

OPEN FIRST
Preview/CollectionOverview.png — one representative of every family.
Preview/ContactSheets/ — all 150 cars grouped into 20 family sheets.
Prefabs/Themed/ — individually selectable Unity prefab variants, grouped by theme.
Data/Themes/ThemedCarCatalog.asset — Unity catalog with prefab and preview references.
Data/Themes/variant_catalog.csv and variant_catalog.json — portable catalog exports.

IDENTITY
Example: TR-C001-TH_PIRATE-V01.
C001 always maps to supplier Car_01; the same mapping is reserved through C015.
Every prefab has a ThemedCarIdentity containing collectible, base-car, theme, and colorway IDs.
V01 is the initial family colorway; this release varies the supplied car silhouette within each family.
Never renumber an existing ID. Add V02, V03, etc. for additional colorways.
Data/Themes/id_registry.json records the released identifiers.
The earlier number/livery-based collection remains intact; this is a separate themed catalog.

ART AND SOURCE SAFETY
All car, wheel, and decal geometry still references Assets/ithappy/Modular_Racing_Cars.
Every supplier decal material slot, including inactive parts, uses a derived themed atlas.
Original racing numbers and sponsor words are absent from these replacement atlases.
Original vector icons are in ArtSource/ThemedCars/Icons; no downloaded icons were used.
Icons occupy existing decal UV regions. No new decal planes, text meshes, or props were added.
Shared paint palettes are copied and remapped; dark rubber, glass, and structural details are retained.
C004/C005/C009 need a separate body-metal palette because their body paint also uses Metal.
C012 needs neutral paint recoloring to include its roof.
Supplier Color's original-palette emission is disabled in the derived materials to prevent color leakage.
Wheel and trim accents are derived materials; headlight/fog accents have controlled family colors.
Optional separate engine, spoiler, and turbine objects are disabled in the base wrappers.
Features embedded in the supplied body meshes remain, including C010's fixed rear wing.

LIMITATIONS AND SELECTION
C006 was rejected from this release: its narrow logos make family badges too small.
C013 was rejected: its thin bumper-strip decal cannot carry a large readable family badge.
Their original IDs remain reserved. Neither supplier car was modified.
Those collection slots were assigned to other suitable supplied cars; the result is still 150 variants.
Original UV overlap and mirrored surfaces constrain badge placement and size. No UVs were rewritten.
Theme identity is strongest in front/side views. Use both palette and icon for later sorting UI.
Final gameplay-camera readability needs checking when the gameplay camera is chosen.

VALIDATION
Generated and rendered in Unity 6000.3.22f1 with the project's installed URP packages and settings.
The open Editor's Pipeline discovery was unavailable, so production ran in Temp/ThemedCarBake,
an isolated local bake project. Finished assets and their Unity GUIDs were copied back here.
This release contains 150 front previews (512 px) and 150 reverse previews (384 px).
All 300 renders were visually reviewed through front/reverse proof sheets.
Unity validation passed: 150 unique entries, 20 families, prefab-variant inheritance,
identity consistency, 2,742 supplier mesh references, and 299 themed decal slots.
All 1,292 supplier files, including metadata, match the initial SHA-256 snapshot.
Evidence: ArtSource/ThemedCars/delivery_validation.json, visual_review.json, source_hashes.json,
decal_layouts.json, uv_audit.json, QA/, and Data/Themes/unity_validation.json.
The main project's unrelated scenes and gameplay were not changed or exercised.

REBUILD / INTEGRATE
After Unity imports the assets, select prefabs under Prefabs/Themed or open ThemedCarCatalog.asset.
Keep this collection, its two runtime data scripts, and the supplier package together.
The catalog is data only; nothing is automatically added to existing scenes or gameplay catalogs.
For authored texture changes, use the scripts in ArtSource/ThemedCars/Scripts:
theme_art.py, build_atlases.py, and build_palettes.py.
In Unity: Tiny Riders > Themes > Build Collection regenerates owned materials, prefabs, previews,
and catalog assets from the recipe file. It overwrites generated art; preserve manual edits first.
Tiny Riders > Themes > Validate Collection checks imported Unity references.
package_collection.py is the isolated-bake publishing/review-sheet step for this production workflow.
Its source-integrity and ID-registry checks reject changed supplier files and silent ID retirement.
Texture import uses compressed 1024 px decals and 256 px palette maps with mipmaps.
This collection shares family materials and references original meshes; it does not duplicate models.
