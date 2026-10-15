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

### Current room: 600-design gameplay replacement (initial staged version)

The original `Assets/00_Project/00_Scenes/Level.unity` now uses all 600 reviewed designs (15 shapes across 40 themes), with two physical copies per design and 1,200 sortable records. Its original room, lighting, player setup, movement, controls and upgrade references are retained. The old 525-car pile and model/color shelves are replaced with a 108-car mixed staging area and 80 theme shelves (1,440 slots). The bounded refill queue makes every design available twice. Each theme has two adjacent shelves, and the entrance remains clear.

The compact catalog, item database and 1,800 world/held/shelf prefabs live under `Assets/TinyRiders/Gameplay/Compact`. They reference the reviewed source artwork without changing it. Shelf colliders support taking cars back individually. Source-art and larger-showroom preservation checks are in `compact_protected_assets_check.json`; all three protected assets remain unchanged. The original Level backup is `IntegrationEvidence/Level_before_compact_collection.unity`.

Main-menu Play and world progression now load Level. The collection save uses `TinyRiders.Compact.Collection.v1`, with a separate player-position key. Existing legacy-store and larger-showroom saves are retained. The HUD and completion/replay flow use the actual 1,200-car/600-design counts. Instantiated cars retain their meshes throughout the room without distance hiding or silhouette replacement. Shelf batching preserves the original shelf geometry.

Validation: `compact_collection_audit.json` passes all 600 designs, 1,800 wrappers, per-slot clearance, 80 shelf IDs, 1,440 slots, and initial diversity across all 40 themes and 15 shapes. `compact_collection_tests.json` passes the isolated gameplay scenario: pickup/drop/repick, take-back, wrong-theme rejection, all 15 Egyptian shapes, a full shelf and overflow, real placements across all 40 themes, scene reload, replay, and main-menu loading. Queue coverage checks all 1,200 records and 600 designs; the final completion threshold/UI/save check injects sorted states after draining the real queue rather than rendering all 1,200 placements.

The WebGL development build succeeded with zero errors and six warnings in 345 seconds, then passed gzip round-trip verification. The normal initializer/menu/loading/Level sequence is included. The 58.44 MB local package is served at `http://127.0.0.1:8190/`. Browser verification confirmed the menu Play route, 0/1,200 and 0/600 HUD counts, movement, and real E pickup of Moon Magic C013 with its themed held artwork and label. No game runtime errors were observed; this in-app browser reports a mouse-capture warning, while drag look and keyboard input worked. The ten-second local idle sample recorded about 80 fps; this is not a target-device performance guarantee. Evidence: `compact_browser_validation.json`, `webgl_build_final.json`, and `Assets/IntegrationEvidence/compact_gameplay.png`.

### Current room: entire collection present at startup (latest)

The staged 108-car behavior above is superseded. Level now creates all **1,200 physical cars at startup: 600 varieties, exactly two copies each**. No records remain pending and no timed refill runs in this mode. Reload restores unsorted world cars, held cars and sorted progress without duplicate IDs; existing saves keep their progress. The larger showroom retains its separate settings and save.

The bounded 31.5-by-27-unit staging grid uses 0.75-unit spacing and two-meter central walking aisles. The room and perimeter racks remain unchanged. Gameplay and Cinemachine cameras persist a 10,000-unit far plane with occlusion culling off and zero layer-distance limits. Distance-hiding components are absent; the live check found all 14,644 car renderer components enabled.

`IntegrationEvidence/full_starting_pile_tests.json` passes the complete gameplay regression, including all 600 varieties twice on startup, no replacement after pickup, opposite-room distance checks, correct/wrong shelf placement, reload, replay and the main-menu Play route. The completion threshold remains a synthetic state test after removing live cars; it does not claim 1,200 manual placements. `IntegrationEvidence/compact_collection_audit.json` passes all 600 designs, 1,800 wrappers and 1,440 shelf slots.

The updated WebGL build succeeded with zero errors and six inherited warnings in 286.58 seconds. The gzip package is 58,508,201 bytes with verified round-trip decoding. Browser launch through the normal menu showed 0/1,200 sorted and 0/600 designs, with instrumentation confirming 1,200 world cars and no game console errors or warnings. The ten-second local browser sample measured 9.21 fps and 34,170 draw calls while Unity Editor also ran the full collection. This exposes a substantial rendering cost in this development environment; no distance hiding was introduced to reduce it. Earlier 108-car performance readings do not describe this configuration.

Latest evidence: `IntegrationEvidence/full_starting_pile_runtime.json`, `webgl_build_final.json`, `webgl_package.json`, and `Assets/IntegrationEvidence/full_starting_pile.png`. The updated local build is served at `http://localhost:8190/`.

### Collector toy shop room redesign (2026-10-15)

The active `Level` is now a connected 36 × 44 collector shop (1,584 square units, 20.8% smaller than the previous 40 × 50 room). Eight numbered departments occupy two columns of four. A four-unit central aisle, cross aisles, shelf approach lanes and a continuous perimeter route connect the displays. All 80 original shelf identities and 1,440 slot identities remain. The source car/shelf geometry, car scales, movement, reach and sorting rules are retained.

The room uses matte warm-gray flooring, ivory walls, pale oak trim, teal details, soft lights, section accent colors, readable theme headers, car posters, boxed stock and a collector counter. The entrance faces the main aisle. The directory and existing upgrade interaction occupy the front arrival area. The design takes the gradual stock-to-collection transformation from [Sort Them Ducks](https://store.steampowered.com/app/4992070/Sort_Them_Ducks/) and the readable arrangements of [A Little to the Left](https://store.steampowered.com/app/1629520/A_Little_to_the_Left/) as references; their artwork is not used.

`CollectorRoomLayout.asset` stores dimensions, spawn, revision, section/theme assignments, shelf poses and sorting bounds. `Tools > Tiny Riders > Collector Shop > Apply Room Redesign` applies it through Unity and validates the deterministic pile before editing geometry. The existing compact-room builder delegates to this layout when configured. `CollectorPile.json` contains all 1,200 physical IDs, distributed as eight mixed areas of 150 (38–40 themes per area). Two shallow layers keep all stock reachable. There is no refill queue or distance hiding.

Revision 2 extends the existing pile migration. Old unsorted world cars move to the new baked poses once; held IDs, sorted shelf/slot records, discoveries and completed themes survive. Old player positions reset to the new spawn; current-revision positions load normally. The collection key remains `TinyRiders.Compact.Collection.v1`. Currency and upgrade keys are unchanged. Protected larger-showroom/review scenes and the source catalog match their recorded hashes. The pre-redesign Level snapshot is retained at `IntegrationEvidence/CollectorRoom/Level_before_20261015_182309.unity`.

Validation evidence in `IntegrationEvidence/CollectorRoom`:

- `layout_audit.json`: 600 designs, exactly two copies each, 1,200 unique physical IDs, 80 shelves, 1,440 slots, in-bounds car geometry and no initial car AABB overlaps. Every car fits a sorting area and clears the central aisle.
- `gameplay_access_audit.json`: 650 capsule-clearance samples pass at 1.8-unit width. All 1,440 slots are within the existing three-unit reach. All 1,200 settled world cars are reachable in two progressive raycast passes. This simulates removal of pick colliders, restoring them afterward; it does not claim 1,200 manual pickups.
- `walk_validation.json`: the existing 0.3-radius, 1.75-height player motor traversed all 12 main, perimeter, shelf and cross routes without jumping. Test movement was fed through the existing movement-stick input because unfocused Editor keyboard injection did not reach the action map. The test restored the input component afterward.
- `shelf_interactions.json`: actual pickup, placement and take-back passed at every one of the 80 shelves; all 1,200 cars remained afterward.
- `gameplay_tests.json`: the complete regression passed, including fresh counts, opposite-room visibility, wrong-theme/occupied-slot rejection, a full shelf and overflow, pickup/drop/take-back, real sorting across 40 themes, reload, partially sorted/held save migration, second reload without relocation, replay and main-menu loading. Completion counters/UI/save flags use injected sorted states after physical-car removal, not 1,200 manual shelf placements.
- `upgrades.json`: all seven purchase/spend checks pass; original upgrade preferences and the test wallet balance are restored.
- `route_comparison.json`: 320 representative aisle-to-shelf route calculations give a median of 30.83 units before and 20.05 after, a 34.96% reduction. This is an aisle-constrained geometric comparison, not a timed human study.

Entrance, junction, sorting-area, overhead and an actual 18-car completed-shelf capture are under `Assets/IntegrationEvidence/CollectorRoom`. The 720p captures include the existing HUD; 1080p camera renders check native-resolution room/sign readability without overlay UI. The overhead capture hides the ceiling only for the capture. These captures use isolated test progress, so some HUD discovery counters reflect validation activity.

The new WebGL development build at `Builds/CollectorShopWebGL` succeeded in 289.34 seconds with zero errors and six warnings (`webgl_build.json`). The normal initializer/menu/loading/Level route was verified in the browser, showing 0/1,200 sorted and 0/600 designs. The measured game console returned no errors or warnings.

`browser_comparison.json` compares the full collection at the entrance in the same in-app Chromium tab, Medium detail, 1280 × 682 canvas and Unity Editor in Edit mode:

| Measurement | Previous room | Collector shop |
| --- | ---: | ---: |
| Physical world cars / total records | 1,200 / 1,200 | 1,200 / 1,200 |
| Sample duration | 10.07 s | 10.02 s |
| Average FPS | 12.61 | 14.97 |
| Average frame time | 79.28 ms | 67.10 ms |
| 95th percentile frame time | 102 ms | 72 ms |
| Draw calls | 34,170 | 31,336 |
| Peak Unity allocation | 192.59 MB | 192.03 MB |
| WASM heap buffer | 663.68 MB | 661.52 MB |

No frame-time regression was observed in this comparison. An initial redesigned-room probe spanned 40.12 seconds while an Editor state query ran and reported 2.89 FPS; its raw values are retained as an interrupted sample. The repeat ran without Editor calls or other application interaction. These are short local entrance samples, not a whole-route benchmark or target-device guarantee. The earlier 9.21 FPS recording included simultaneous Editor Play mode. Room changes do **not** solve the substantial rendering cost of the full collection. The 174 additional enabled renderer components belong to the room styling; no car distance hiding was introduced.

Play the redesigned build at `http://127.0.0.1:8193/`. The localhost server is running. To restart it from this project: `C:\Python314\python.exe IntegrationEvidence/serve_webgl.py CollectorShopWebGL 8193`. The previous build remains intact. The build script is `IntegrationEvidence/CollectorRoom/BuildCollectorWeb.cs`; the editor room tool can reapply the saved layout. Unity is left in Edit mode on the saved Level with normal background settings restored. Existing user progress was preserved through isolated test saves.

## Compact crowded shop — revision 3 (2026-10-15)

The active `Level` is now 24 × 26 units with a 3.2-unit ceiling: 624 square units, 60.6% less than the preceding 36 × 44 shop. It retains the original player and car scales, movement, reach, controls, 600 designs, two physical copies per design, 80 shelf IDs and 1,440 slot IDs. Forty-eight shelves wrap clockwise around the perimeter; thirty-two form two low, back-to-back islands. Theme pairs share an adjacent display face. The original eight category groups remain in the wall directory and compact category markers.

The four-unit aisle, sorting mats and department pile boundaries are removed. The floor-wide, mixed stock uses seeded full yaw, varied tilt and occasional side-resting poses, then an editor-only physics settlement. The entrance landing and shelf approaches remain clear. Cars occupy the routes between displays; player collision is ignored only for loose themed stock in this configured scene, both in the KCC sweep predicate and in PhysX capsule contacts. Proximity no longer wakes stock in this mode. Pickup/drop still wakes surrounding cars and resting bodies freeze again, including near the player.

The room configuration, baked poses and materials are under `Assets/TinyRiders/Gameplay/Compact/CrowdedRoom`. `CrowdedCollectorRoomBuilder` is reached from the existing compact room tool. Architecture dimensions come from the configuration. The builder validates a candidate before replacing the scene layout and saves a pre-change copy. The first backup is `IntegrationEvidence/CrowdedRoom/Level_before_20261015_193530.unity`. The previous room assets and separate larger showroom remain available.

Save migration advances the existing pile revision to 3, relocates only unsorted stock, preserves held and sorted identities, and resets an old player position once. The collection save key, currency and upgrade saves are unchanged. A fresh or migrated entry looks down 12 degrees toward the nearby stock; normal saved camera poses restore afterward.

### Validation evidence

Evidence is under `IntegrationEvidence/CrowdedRoom`:

- `layout_audit.json`: 1,200 unique physical IDs, 600 designs with exactly two copies, 80 shelves, 1,440 slots, 48 perimeter / 32 island shelves, revision 3.
- `physics_bake.json` and `contact_validation.json`: 1,200 settled bodies after 181 fixed simulation steps; all 1,200 have a direct or connected support contact. Bake colliders are conservative oriented boxes derived from each design's actual rendered mesh bounds, rather than a single nominal car size or triangle-exact meshes. PhysX resting contact tolerance is 12 mm (measured maximum 11.355 mm); support checks use a 20 mm envelope. Persisted poses are checked again before applying or building.
- `access_audit.json`: all 2,045 standable floor samples connect to the entrance using the existing 0.3-radius capsule; all shelf approaches and 1,440 slot reach distances pass. Progressive pickup raycasts reach all 1,200 cars. This is a geometry/raycast access check, not 1,200 simulated hand animations. Highest rendered car point is 0.815 units; 1,199 cars stay below 0.8 units.
- `walk_validation.json`: twelve actual input-driven longitudinal and cross-room traversals pass without jumping, with **0.0 units maximum car displacement**. The initial test exposed proximity waking and PhysX pushing; the saved result is the corrected retest.
- `shelf_interactions.json`: actual collect/place/take-back at all 80 shelves, returning to zero sorted and 1,200 live cars.
- `gameplay_tests.json` and `gameplay_test_details.txt`: the collection regression passed in 54.49 seconds. It checks pickup/drop/repick identity, wrong-theme/occupied/full-shelf rejection, real sorting across all forty themes, persistence, partially sorted/held migration, second reload, completion/replay and menu-to-Level loading. Completion uses injected sorted states after real interaction coverage rather than manually sorting all 1,200 copies.
- `upgrades.json`: all seven upgrade purchase paths pass with test wallet and preference restoration.

`Assets/IntegrationEvidence/CrowdedRoom` contains entrance, overhead, floor-pile and shelf-approach views at 720p and 1080p, plus a filled 18-car shelf. The entrance 720p image includes the HUD; camera captures show room/sign detail without overlay UI. Only the overhead capture hides the ceiling. Test saves were isolated from existing progress and cleaned up afterward.

### WebGL build and comparison

`Builds/CrowdedCollectorWebGL` built successfully in 226.32 seconds: zero errors, six warnings. The normal language/menu/loading/Level path was verified in the browser, showing 0/1,200 sorted and 0/600 designs. The game browser console returned no errors or warnings.

`IntegrationEvidence/CrowdedRoom/browser_comparison.json` records the preceding 36 x 44 room and the new 24 x 26 room in the same Chromium tab, Medium detail, 1280 x 682 canvas, Editor idle in Edit mode. Both views retain all 1,200 cars and use the same 24-body activation probe. Dense-view movement is 900 ms forward and 450 ms left from each entrance. Cameras use each layout's intended entrance pitch, so this compares the playable scenes rather than identical world-space framing.

| Ten-second sample | Previous entrance | Compact entrance | Previous pile view | Compact pile view |
| --- | ---: | ---: | ---: | ---: |
| Average FPS | 14.87 | 15.15 | 17.28 | 20.83 |
| Average frame time | 67.69 ms | 66.42 ms | 58.18 ms | 48.21 ms |
| 95th percentile | 75 ms | 76 ms | 67 ms | 57 ms |
| Draw calls | 31,329 | 29,219 | 25,488 | 20,038 |

No average frame-time regression was observed in either view. Entrance p95 changed by 1 ms; Unity allocation increased about 2.35 MB, while the WASM heap decreased about 2.23 MB. An interrupted background-tab sample and ceiling-facing sample were discarded; repeated screenshot observations kept the accepted dense-view samples rendering continuously. These are short local comparisons, not a target-device or whole-route guarantee. Full-collection rendering remains expensive and is **not solved** by this redesign.

Play at `http://127.0.0.1:8195/`. Restart the local server with `C:\Python314\python.exe IntegrationEvidence/serve_webgl.py CrowdedCollectorWebGL 8195`. The previous build remains intact, and temporary benchmark host modifications were removed. Unity is left in Edit mode on the saved Level. The report's earlier 36 x 44 layout sections describe the superseded revision 2 implementation.
