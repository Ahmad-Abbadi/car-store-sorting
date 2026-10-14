THEMED COLLECTIBLE CARS — CONTENT REVISION 2

150 existing collectible IDs across 20 theme families, using the same 13 supplier cars.
R02 improves every variant in place. Prefab GUIDs, hierarchy, transforms and source mesh
references are preserved. R01 is archived at ArtSource/ThemedCars/Revisions/R01/Collection.

OPEN FIRST
Preview/CollectionOverview.png — all 20 refined families.
Preview/BeforeAfter_R01_R02.png — same-car comparisons for all 20 families.
Preview/ContactSheets/ — every variant grouped by theme.
Preview/Multiscale/ — every variant at native 256 px perspective and 96 px thumbnail sizes.
Preview/MixedGroups/ — 15 live Unity renders, ten mixed-theme cars per image; all 150 included.
Prefabs/Themed/ — individually selectable prefab variants, retaining the original paths.
Data/Themes/ThemedCarCatalog.asset — Unity catalog, with contentRevision=2 on every entry.
Data/Themes/variant_catalog.json and .csv — updated portable catalog and validation notes.

WHAT CHANGED
Larger cropped-to-silhouette emblems and broad family motif panels replace the small badges.
Candy has mint frosting and readable spirals; Robot has large bot faces and circuit panels;
Construction has hazard bands; Ice has crystal framing; Ocean has waves and bubbles;
Pirate has dark flags; Fire has magma cracks; Farm has gingham borders and apple badges.
The remaining families share their own deliberate palettes and motif treatments.
Secondary body panels and bumpers reinforce each family; wheel and trim accents are coordinated.
Glossy Candy, metallic Robot/Space, restrained Fire and frosted Ice use existing supplier shaders.
The trim material now uses a derived palette, avoiding original atlas color contamination.
Overlapping decal UV regions are clipped to exclusive areas to avoid stray partial emblems.
No racing numbers or sponsor text are authored in the replacement atlases.

IDENTITY AND SAFETY
TR-C001-TH_PIRATE-V01 remains the same collectible identity; contentRevision increases to 2.
All 150 IDs, prefab GUIDs, hierarchy nodes, transforms, active states and mesh references match R01.
All 1,292 supplier files, including metadata, match the initial SHA-256 snapshot.
No new meshes, decal planes, shaders, downloaded assets, gameplay, environment, shelves or packaging.
The runtime scripts hold data only. Existing scenes and gameplay catalogs are not wired to this library.
Keep the collection, its two runtime data scripts, and Assets/ithappy/Modular_Racing_Cars together.

VISUAL VALIDATION AND LIMITS
Built in the main connected Unity Editor, 6000.3.22f1, with its installed URP packages.
Each variant has front 512 px, reverse 384 px, gameplay-like perspective 256 px, and 96 px renders.
The perspective art test uses FOV 60 and camera distance 1.5 times maximum model dimension.
All 150 were reviewed through front, reverse, gameplay and native-size thumbnail proof sheets,
and all 15 mixed-group renders were inspected. These are art review tests, not human user testing.
At thumbnail size, family palette and the large emblem carry recognition; micro-patterns disappear.
Existing body UVs collapse to palette samples: hood/roof-wide painted patterns cannot be mapped
without changing UVs or shaders. Broader color blocking uses the supported material slots;
icons and patterns use only the supplier's existing decal geometry and UVs.
C007/C008/C009/C011/C012 rely more on side badges and palette in near-frontal/high views.
C006 and C013 remain excluded, exactly as in R01, because their decal surfaces are too narrow.
Their base IDs remain reserved. No released collectible IDs were removed or renumbered.
The eventual gameplay camera, lighting and dense occlusion still need integration-specific checks.

EVIDENCE
Data/Themes/revision2_validation.json — source, ID, structure, image and reference checks.
Data/Themes/revision2_unity_validation.json — live Unity metadata/reference/structure checks.
Data/Themes/revision2_visual_notes.json — per-variant notes and mixed-group render membership.
ArtSource/ThemedCars/Revisions/R02/QA/ — full front/reverse/gameplay/thumbnail proof sheets.
ArtSource/ThemedCars/Revisions/R02/before_hierarchy.json and after_hierarchy.json.
ArtSource/ThemedCars/Revisions/R02/decal_coverage.json — atlas coverage comparison to R01.
Earlier root-level ArtSource reports describe R01; use Revisions/R02 for the current release.

REBUILD
Run Scripts/theme_art.py, Scripts/refine_atlases.py and Scripts/build_palettes.py in that order.
Use Tiny Riders > Themes > Build Collection in Unity, or RunRefinement.cs via Unity CLI.
This updates owned materials/prefabs/previews/catalog metadata; manual edits should be preserved first.
Run Revisions/R02/RenderMixed.cs, then review_sheets.py and ValidateRevision.cs.
After visual inspection, record visual_review.json and run finalize.py to export reviewed metadata.
The old build_atlases.py and package_collection.py workflow belongs to R01. Do not republish
Temp/ThemedCarBake over the live collection; the old publisher now rejects an R02 destination.
The .meta files preserve GUIDs. Texture import uses compressed 1024 px decals, 256 px palettes and mipmaps.
