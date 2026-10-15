# Tiny Riders gameplay integration

## Organized collection review scene

`Assets/TinyRiders/Scenes/ThemedCollectionReview.unity` displays one instance of every design: 600 cars in 40 labeled theme bays. Each bay has a 5-by-3 grid ordered C001–C015, individual car IDs, uniform original artwork scale and neutral review lighting. The Hierarchy groups each theme separately. The scene is isolated from gameplay and save systems and is not added to the game's build/startup scenes.

In Play mode, PageUp/PageDown or Left/Right switch themes, Home shows the full collection, the wheel zooms, and holding the right mouse button enables WASD movement with Q/E for down/up and Shift for faster movement. The initial view shows Ancient Egypt. Rebuild with **Tools → Tiny Riders → Review → Build Complete Collection Review Scene**. Validation and preview evidence are in `IntegrationEvidence/collection_review_*.json` and `Assets/IntegrationEvidence/collection_review_theme.png`.

## Current collection update — 15 October 2026

The playable Unity scene now uses all **15 base car shapes in all 40 themes: 600 designs**. The 190 added combinations use the existing body meshes, theme artwork recipes and shared accessory kits, including the previously excluded C006 and C013. All existing collectible IDs remain unchanged. Two copies per design provide **1,200 sortable cars**, with two 18-slot shelves per theme: **80 shelves and 1,440 slots**, or 36 slots for each theme's 30 cars.

The 108-car visible pile now uses a shared deterministic sequence for baking and runtime refills. It starts with every theme, all 15 shapes and 108 distinct designs; it schedules the first copy of every design before second copies. Saved world poses and existing instance IDs remain intact. Previously completed themes reopen when new unsorted designs are added.

`IntegrationEvidence/complete_collection_validation.json` validates all 600 designs, 1,800 gameplay forms, correct theme material references, fixed shelf clearance and sufficient capacity. `complete_collection_tests.json` records four passing tests, including the complete pickup, throw/repick, placement, duplicate, shelf-overflow and save/reload regression; an 820-car save expands to 1,200 records while retaining its existing IDs. The bounded-pile stress check now covers 3,000 logical records. `complete_collection_live.json` records the actual running scene at 108 live cars, 40 themes, 15 shapes and 1,200 logical instances; frame advancement was verified before capturing `Assets/IntegrationEvidence/complete_collection_gameplay.png`.

The complete Egyptian and alien lineups are shown in `IntegrationEvidence/TH_ANCIENT_EGYPT_all_15.png` and `TH_ALIEN_all_15.png`. The authoring tools are **Tools → Tiny Riders → Sorting → Complete All 15 Shapes Per Theme** and **Update Completed Collection Gameplay**. Missing UV atlases can be generated with `ArtSource/ThemedCars/Scripts/complete_collection_atlases.py`; existing artwork is preserved. The sections below retain the earlier 410-design integration and performance history and do not represent new performance measurements for 600 designs.

The local WebGL export and `Builds/ThemedSortingWebGL_Playable` package were rebuilt for this collection. The build succeeded with zero errors; the verified gzip package is 58,380,695 bytes (58.4 MB). Browser verification at the existing local server showed `Sorted 0 / 1200 · Designs 0 / 600` and no browser console errors or warnings. `webgl_build_final.json` and `webgl_package.json` describe this updated build; earlier browser performance files remain historical.

Project: `C:\Users\User\Documents\Games_Projects\My_Games_Projects\Car Store Sorting`

Playable scene: `Assets/TinyRiders/Scenes/ThemedSorting.unity`. Open it and press Play, or use the existing main menu's Play button. The original `Assets/00_Project/00_Scenes/Level.unity` remains available. The existing world mapping and Play button now point to the themed continuous showroom. There is no automatic next-level or collection reset flow.

## Content and shelves

- 410 validated collectible designs, 40 themes: the existing initial 150 designs / 20 themes plus the verified expansion's 260 designs / 20 themes.
- Two physical copies per design: 820 deterministic world-instance records. Unique discoveries count designs separately from physical cleanup.
- 60 instances of the approved base shelf, all with the 18 original named markers; 1,080 slots. Initial themes use one unit each; expansion themes use two each. Exact IDs are in `IntegrationEvidence/shelf_assignments.txt`.
- All 410 actual prefab references, materials and attached accessory kits are used. Generated world, held and shelf wrappers retain the source prefab, mesh and material references. Their artwork uses the same original scale in every form. No supplier meshes, artwork, original prefab GUIDs or collectible IDs were changed.
- With the user's explicit approval, shelf instances have one fixed uniform scale of 9; the copied room and player distances are calibrated accordingly. Maximum source bounds measured across the collection: approximately 2.628 wide × 3.369 high × 4.863 deep. Fixed slot clearance is 4.176 × 4.860 × 5.832. All measured designs fit. The calibrated camera height is 26.1 and the player capsule is 27 high, allowing the upper cars to be seen above the tray lip. Shelf geometry, dividers, base and blank generic header remain the approved design.
- Shelf headers are separate text components bound to catalog theme metadata. Names can change without remodeling. No storage boxes or legendary displays were added.

## Existing systems reused

The existing KCC player, mouse/touch input authority, inventory and stash, pickup flight, held-item forms, aiming ray, shelf placement animation and sounds, occupancy, take-back, progress event/UI infrastructure, upgrade wallet, and PlayerPrefs save coordinator are reused. `IntegrationEvidence/INTEGRATION_MAP.md` records the initial audit.

`ThemedCarCatalog.asset` remains the authoring source of truth. Its existing entries gained availability, release membership, measured bounds and references to generated sorting adapters. Theme names, colors and motifs come from the existing `theme_recipes.json`; permanent theme and collectible IDs are preserved. Missing references, identity mismatches, absent accessory roots, or failed clearance checks are excluded by the builder, with reasons recorded.

## Gameplay implementation

`Assets/TinyRiders/Gameplay` contains the continuous collection coordinator, physical instance identity, theme shelf binding, replaceable header binding, real-mesh visibility culling, development performance recorder, generated sorting data and three wrapper forms per design. Editor tools and tests are under its `Editor` folder.

Existing scripts were extended for inventory instance IDs, identity transfer through pickup/placement/take-back/drop, theme/fit/occupancy rejection before spending a held car, mixed-design shelf completion, continuous progress, the existing HUD, persistence extension and scaled interaction reach. Input actions now disable both maps before disposal, fixing the teardown leak revealed by reload testing.

Held forms reuse the template's camera-fixed socket, calibrated to the view with one consistent pose `(1.6, -2.1, 7.8)` and the original car scale. This keeps the artwork visible while looking up/down after the player height adjustment. The carried theme is shown in the main HUD even with the optional collection directory closed. The normal 16:9 carry-frustum check covers all 410 measured accessory bounds, in addition to actual carried-car visual verification.

The mixed pile uses 108 actual cars, with an initial physically settled layout baked using the template's isolated preview-scene physics approach (1,100 steps / 22 simulated seconds; zero moving bodies at the end). Pending records refill available visible capacity. Picked world forms can be pooled, with a maximum 24 cached forms. The existing physics culler has a 24-body budget in this scene. Distant cars retain real themed geometry when shown; the legacy generic silhouette proxy is not used. Cars falling below the room are recovered.

World wrappers use body colliders for physical contact and a separate simple trigger over the actual accessory bounds for aiming. Visible hats, fins and other approved accessories retain their original meshes. The shelf instances use eight shared geometry batches per shelf instead of 54 individual renderers. Triangle count remains 18,316 per shelf and vertex-derived world bounds match within 0.002 units; all original materials are shared. These derived batches live under `Gameplay/Rendering`; the source shelf prefab and its meshes are untouched. See `IntegrationEvidence/shelf_batching.json`.

New runtime scripts: `ThemedWorldInstance`, `ThemeShelfBinding`, `ThemedCollectionSession`, `ThemeShelfHeader`, `ThemedVisibility`, and `ThemedPerformanceProbe`, plus the WebGL metrics bridge. Editor tools: `ThemedSortingBuilder`, `BakeThemedPile`, `FinishThemedScene`, `ThemedPickingBounds`, `ThemedShelfBatcher`, `ThemedPlayerCalibration`, `ThemedSortingAuthoringSettings`, `ThemedWebGLHost`, `ThemedWebGLImportProfile`, and the end-to-end test fixture. Generated assets include 410 item adapters, 40 category adapters, 410 preview sprite references and 1,230 wrapper prefabs.

Modified template scripts: `InventoryHandler`, `StashHandler`, `ItemCollectingBehaviour`, `WorldItemBase`, `ItemsHandler`, `PlayerInput`, `PlayerSortingItemsBehaviour`, `SortingShelf`, `SortingProgressTracker`, `LevelSaveData`, `LevelSaveManager`, `CarStoreUI`, `UpgradeManager`, `AutoCollectPower`, `ItemPhysicsCulling`, and the legacy editor builder's first-run guard. Main-menu and world data point to the new scene. Existing dynamic font atlases and Unity-generated URP shader-prefilter serialization also update during verification/build; no package or render-pipeline replacement is involved.

## Persistence

The existing `LevelSaveManager` handles the themed extension of `LevelSaveData`. The new key is `TinyRiders.Themed.Sorting.v1`; player position uses `TinyRiders.Themed.PlayerPosition.v1`. Existing legacy save keys and artwork IDs remain available.

The extension stores seed, permanent instance and definition IDs, pending/world/held/sorted state, poses, discoveries, completion milestones, inventory order, shelf IDs and named slot IDs. Slot reservation precedes checkpointing and animation, so reload during movement restores the committed car in its slot. Invalid/missing shelf mappings recover to the unsorted reserve; retired content records and discoveries are retained. Unsupported or unreadable saves are retained instead of automatically deleted or overwritten. Old unsorted staging positions can migrate to the settled pile without changing held/sorted progress or instance IDs.

## Controls

Use the existing mouse or E/F controls for pickup and placement/drop, mouse wheel for inventory, WASD to walk, Space to jump, and Tab to pause. Existing touch controls remain in the scene. Press C or use the Collection button to open theme progress. The full directory is closed by default so it does not cover the pile.

## Validation and build evidence

Automated results, screenshots and build measurements are recorded below. Tests use temporary QA save keys and exercise the actual scene and gameplay APIs; API-driven interaction is distinguished from human mouse/touch playtesting.

## Adding content later

1. Complete and validate the real art prefab and accessory kit using the established art pipeline.
2. Append its permanent IDs and release membership to the existing registry/catalog and theme recipe data. Never reuse or rename an existing collectible ID.
3. Configure seed, physical copies per design and visible pile limit in `Assets/TinyRiders/Gameplay/Editor/ThemedSortingSettings.asset`. Run **Tools → Tiny Riders → Sorting → Build Themed Showroom** in Edit mode. It generates the sorting adapters, checks metadata/fit, assigns shelves and rebuilds the themed scene, including the settled pile. This is an explicit authoring operation: preserve a customized scene before regeneration.
4. Existing content uses the same runtime logic. A new theme needs metadata and shelf capacity, not another sorting script. Extra shelf units can also be duplicated/configured with a distinct shelf save ID and the appropriate theme binding; keep all 18 markers and ensure saved IDs remain stable.
5. Enable/disable verified content through the catalog's availability/theme fields. Pending/retired records remain in saves; unique discovery and physical progress are separate. The builder computes additional 18-slot shelf units from active designs × copies and expands the perimeter layout beyond the current 60 units when necessary. It retains existing theme/entry enable choices.

## Remaining limits

The runtime catalog directly references the full approved art library. Active-object bounds control simulation and rendering; the separate WebGL import profile controls texture startup cost. This is a tested desktop development integration, not CrazyGames certification or a mobile/Safari performance claim. No publishing SDK, monetization, new economy, Unity upgrade or render-pipeline replacement was introduced.

### WebGL texture profile

Actual browser instrumentation exposed heap growth to 2,063,794,176 bytes in the first complete build despite approximately 120 MB of steady Unity allocations. The Build Report traced 1,663,804,000 bytes of unpacked startup asset data to the 410 theme decals. The baseline is saved in `IntegrationEvidence/browser_metrics_before_texture_profile.json`.

**Tools → Tiny Riders → Sorting → Apply WebGL Texture Profile** applies WebGL-only import overrides: up to 512 pixels for decals, 256 for theme palettes, desktop DXT texture compression, and a 256 MB initial heap with geometric growth. The build script reapplies this profile so the per-machine WebGL subtarget is reproducible. Default/Standalone settings and the original PNG pixels are preserved. `IntegrationEvidence/webgl_texture_override_audit.json` verifies all 650 changed importer files: zero image changes, zero GUID changes, and zero changes outside each WebGL platform block. This desktop profile needs a separately tested texture target/device pass before mobile distribution.

The development host supports viewport sizing, a 1× render pixel ratio, automatic browser save synchronization, and a handled pointer-capture failure path. Its script URL is versioned by content hash to prevent stale host settings. `?qa=1` exposes measured load/performance information; its physics test reloads before sampling, wakes up to 24 real pile bodies, and holds mouse-look stable during the sample. Dedicated QA origins were used; the final physics origin had fresh save storage. QA tools are absent from the normal host view and disabled in non-development players.

### Automated results

`IntegrationEvidence/PlayModeResults.xml` records a passing Unity test fixture (one end-to-end UnityTest with twelve A–L scenarios and a drop/repick check). It enters Play Mode, uses the real scene/player/gameplay methods, reloads the actual scene, and exits cleanly. `IntegrationEvidence/playmode_passed.txt` records each exercised scenario. The final regression also verifies a moved unsorted car's pose, checks every sorted instance ID against the restored pile, and checks carried-object visibility from the calibrated camera.

`IntegrationEvidence/integration_validation.json` passes: 40 themes, 410 variants, 1,230 wrapper forms, 23,676 source mesh references, 60 distinct shelf IDs, and all 1,080 named slots. Source mesh/material references, accessory roots, permanent metadata and unchanged art scale are checked. Original art files were checked against the initially clean repository state. Automatic Unity emission keyword/GI edits introduced during WebGL import were separately audited and corrected without replacing artwork.

Final live Game-window captures are `Assets/IntegrationEvidence/final_mixed_pile.png` and `Assets/IntegrationEvidence/final_held_pirate.png`, using the WebGL texture profile. Frame advancement was verified before capture. The shelf capture stages six real, distinct Pirate placements plus a held car using the gameplay methods in an isolated QA session. Player saves and the existing wallet are not used for these staged captures. These are agent-driven checks, not a human usability or touch-device study.

The movement calibration was verified after reopening the scene: 49.5 stable speed, derived from the original 5.5 × 9, with the corresponding air movement, gravity and jump scaling. Prefab override recording prevents these values from reverting. The final A–L regression passes after this correction.

### Final WebGL result

The final Unity 6000.3.22f1 development build succeeded with zero errors. `IntegrationEvidence/webgl_build_final.json` records 156,835,914 build bytes and the successful result. The raw exported directory is `Builds/ThemedSortingWebGL_Final`. The portable gzip HTTP package is `Builds/ThemedSortingWebGL_Playable`: **53,498,719 bytes (53.5 MB)**. `IntegrationEvidence/webgl_package.json` verifies each compressed payload decodes byte-for-byte to the raw build, including SHA-256 digests.

| Measurement | Idle starting view | After 24-body activation |
|---|---:|---:|
| Actual canvas | 1280 × 682 | 1280 × 682 |
| Sample duration | 10.005 s | 10.003 s |
| Frames per second | 106.35 | 106.27 |
| Average / 95th percentile frame time | 9.40 / 11 ms | 9.41 / 11 ms |
| Maximum visible world cars | 108 | 108 |
| Maximum dynamic pile bodies observed | 0 | 24 |
| Logical instance records | 820 | 820 |
| Peak Unity allocated memory | 119.48 MB | 120.34 MB |
| WASM linear heap buffer | 463.99 MB | 463.99 MB |
| Draw calls reported | 3,645 | 3,645 |
| Loaded / enabled renderer components | 5,139 / 1,899 | 5,139 / 1,899 |

Source evidence: `browser_metrics_final_idle.json` and `browser_metrics_final_physics.json`. These are local in-app Chromium development-player samples. Loader-to-ready was 2.21 and 2.20 seconds with cache-busted asset URLs; the browser/compiler/GPU environment was already exercised. This is not a first-visit WAN timing or a target-device guarantee. The physics sample records a wake-and-settle cycle, not ten seconds of sustained motion for every body. Unity allocation excludes browser, JavaScript and GPU memory; the WASM heap figure is its current buffer size, not total process memory.

The measured heap fell **77.5%**, from 2.064 GB to 464 MB. Unpacked decal asset data fell from 1.664 GB to 143.37 MB; palettes fell from 88.77 MB to 10.13 MB. The gzip development package fell from 66.13 MB to 53.50 MB. Frame-rate readings varied with browser/display conditions, so no frame-rate speedup is claimed from that comparison. A diagnostic 1920 × 1023 sample is retained separately in `browser_metrics_optimized_high_dpi.json`.

The final browser also exercised actual WASD movement, mouse-look, E pickup of a real Aviation C001 car, F release, C collection-directory toggle, and Tab pause/resume. The carried model and accessory remained visible and its catalog theme appeared in the HUD. Correct/wrong shelves, every slot, identity preservation and actual scene reload are covered by the automated gameplay tests. The final browser console had no errors or warnings in the measured origin.

### Running the build and remaining issues

Run `C:\Python314\python.exe IntegrationEvidence/serve_webgl.py ThemedSortingWebGL_Playable 8190` from the project, then open `http://127.0.0.1:8190/`. The helper supplies gzip encoding and the correct WASM/JavaScript MIME types. A localhost server is already running for this handoff. Public hosting must retain those headers. Rebuild with the existing Unity workflow or `IntegrationEvidence/ThemedWebGLBuild.cs`, then run `IntegrationEvidence/package_webgl.py` for the verified gzip package.

No verified car or shelf dependency is missing, and no sorting/save blocker remains in the exercised scenarios. Final source-art checks show no original PNG, FBX, prefab, shelf artwork or theme material edits; only the audited WebGL importer overrides remain on the texture metadata.

The inherited pause-menu helper checkbox still displays its untranslated `pause.disable_helper` key. Its pause/resume behavior works; that legacy label is a minor remaining UI issue. Touch-device usability, Safari/mobile GPU texture variants and CrazyGames publishing certification remain untested. The package is a development player; a release-size/streaming/CDN and publishing-SDK pass remains appropriate before public distribution. Final build warnings are the intentionally absent runtime Pipeline automation configuration and an existing empty utility assembly; details are in `IntegrationEvidence/webgl_final_warnings.json`.

### Collection review visibility and navigation

`Assets/TinyRiders/Scenes/ThemedCollectionReview.unity` keeps all 600 unique cars active in 40 organized bays. Its camera uses a 10,000-unit far plane in theme, car-focus, overview and flight views, with occlusion culling disabled and no layer distance limits. The Editor Scene view uses the same fixed range while this scene is active.

The Car Collection editor window (`Tools > Tiny Riders > Review > Collection Navigator`) and the Play-mode panel provide theme search, previous/next theme, 15 individual car buttons, theme framing and a full overview. Existing flight and keyboard controls remain available. Navigation moves the view without hiding other cars. The runtime panel uses its own UI Toolkit assets, isolated from gameplay UI.

`IntegrationEvidence/review_navigation_editor.json` and `review_navigation_runtime.json` pass: 600 active unique cars, all 600 individual focus views, 40 searchable theme buttons, previous/next/car/overview button actions, and 871 distant-car samples within the frustum beyond the former 85-unit cutoff across opposite collection views. Runtime frames advanced from 1 to 766 during the smoke check. Theme-list scrolling reaches its last entries, and the final composited panel was visually inspected in `Assets/IntegrationEvidence/review_navigation_runtime.png`. No new runtime errors remained after the initialization fix.
