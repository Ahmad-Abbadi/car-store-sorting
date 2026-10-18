# Golden reward QA

Completed: Blender modeling, UVs, materials, exports and Unity import executed successfully. The complete 24-view deliverable contact sheet was inspected, including actual Unity empty/filled, car and existing-collectible scale images. The saved-asset audit passed 34 checks with zero failures (unity_verification.txt). All requested views and detail renders are saved; the supplier audit is an additional image.

## Inspected evidence
Opened both original local reference PNGs. Imported and rendered all 15 actual ithappy core car meshes for source selection. Inspected car clay, hero, rear and top views; display empty and filled hero images; lighting-ring and wireframe inspection renders.

Three focused correction passes: (1) integrated hood graphics into body faces and seated the scoop on the sloped hood; (2) reduced excessive cover reflection ghosts, added wing inset checker regions and corrected grille UV direction; (3) enlarged display crown relief, corrected circular band checker aspect and changed hero views to perspective to expose the canopy underside.

Uniform gray material override makes printed checkers and grille detail disappear; structural forms and intentional shallow crowns remain. No final FONT/text objects, checker-tile geometry, floating printed decal planes, reflection streaks, particle meshes or light beams are used.

## Measured Blender geometry

| Asset | Triangles | Editable mesh objects | Unique materials |
|---|---:|---:|---:|
| Golden car | 39,376 | 143 | 9 |
| Display core | 38,072 | 65 | 6 |
| Optional accessories | 6,396 | 19 | 3 |

Every mesh has a UV layer. Mesh-object counts are editable Blender components, not final Unity renderer counts. Unity importer combines logical groups while retaining independent wheel pivots, wing, body, trim, glass, platter and accents.

Car bounds: width 0.200089, length 0.351440, height 0.158989. Tire contact is at Z = 0 within floating-point tolerance. Four aligned tires use radius 0.042955. Arch opening radius is 0.045739 (at least 0.002784 radial tire clearance before the tire's rounded shoulder). The full car radial envelope, including wing, mirrors and all badge tips, is 0.195329. Cover inner radius is 0.215409: full-turn margin 0.020080. Platter radius is 0.205227: full-turn support margin 0.009898. No rotation behavior was implemented.

Display bounds: 0.540909 diameter, 0.428076 height. Car contact anchor: +0.054091 Unity Y after axis conversion. Coherent circular footprint replaces the contradictory concept-sheet 30 cm depth label. The wider reward car is deliberately proportional to the existing compact 0.35-long collectible.

## Maps, transparency and lights
Two 512-square RGB sRGB maps: repeating checkers and grille microdetail. Gold/trim/window/rubber/fabric roughness and metallic are material constants; no unused normal, metallic or roughness maps are generated. Final Blender cover is a thin 96-segment cylinder, separate from the frame, with one low-opacity glass material plus opaque rim trim. Three broad studio area lights; no decorative point-light forest, bloom, volumetrics or particles.

## Actual Unity results

Unity 6000.3.22f1, existing High_URP pipeline. No pipeline/quality assets were changed. Raw imported FBX fronts faced -Z; the importer measured this from the grille/front plaque and baked a 180-degree Y correction into all three normalized assets. Wheel suffixes are normalized from their actual Unity X positions after conversion (left negative, right positive). Stored prefab tests confirm +Z front grille, -Z rear lamps, +Y up, ground contact, unit root scales, independent wheel pivots and correct accessory placement. Actual Unity radial-clearance measurements match Blender.

| Saved Unity asset | Triangles | Renderers | Material slots | Unique materials |
|---|---:|---:|---:|---:|
| Golden car | 40,860 | 8 | 32 | 9 |
| Display core | 38,408 | 8 | 20 | 6 |
| Optional accessories | 6,396 | 1 | 3 | 3 |

Unity's imported triangulation counts are reported separately from Blender's polygon triangulation. No missing mesh, material, shader or UV references were found. The filled preset nests the independent golden-car prefab beneath Anchor_Car. The empty primary display has no car inside it. All four saved prefabs have no gameplay MonoBehaviours.

Two 512 × 512 sRGB repeat maps and a 64 × 64-per-face HDR preview cubemap are persistent assets. Cover: one separate transparent material/submesh in GlassCover, opacity .025, ZWrite off, backface culling, shadows off; gold rim remains opaque. URP's preserved specular response originally created distracting white spots, so specular/environment reflection on this simplified transparent material is explicitly disabled. The source glass geometry is retained.

Three scene-local directional preview lights, one with soft shadows, and one custom reflection probe. Shadow bias is .001 and normal bias .005 to suit the toy scale. No bloom/volume/particle system was added. The screenshots are actual Unity camera renders, supersampled through render textures without changing global render scale. Emission requires the BakedEmissive material flag in this installed URP version; it is enabled for shader validation, with no lighting bake performed. Saved emission keywords were verified.

The importer was successfully re-executed after correcting an update-only material-reference bug; persistent .meta GUIDs are preserved. MainMenu Scene was restored after every additive preview run and confirmed clean. Supplier mesh/material and existing compact-car representative hashes are unchanged. Existing unrelated/concurrent changes were not reset, staged or edited.

## Limits
These counts and editor images are not browser/device performance measurements. The asset is a simplified toy-art interpretation, not an exact CAD reconstruction of the concept board. Low-opacity cover rendering and coherent circular dimensions are deliberate documented adaptations. No outstanding technical failure was found in the saved Unity assets.

## Grand ceremony follow-up

The current scene installation supersedes the original small trophy described below. It uses 5× art, a 3m-wide/0.2m-high base, no reward text, visible Fresnel glass, a 90-second ceremony, and nine owned Polygon Arsenal URP effect variants. Car length is 1.757m; station width is 2.705m. Authored car transforms and nested prefab references are preserved. The presentation pivot rises 0.32m and makes one full turn within the glass.

Inspection measured flush contact within 0.000001m, more than 2m fixture clearance, and zero blocked samples along the authored camera path. Only 94 of 1200 authored loose-car poses moved; all identities and catalog/shelf definitions remain intact. Existing correct-sorting rules, StoreCompleted, completed-save reload, reset/replay, pause, and interruption restoration remain authoritative. Actual full-length run/captures and current suite results are under IntegrationEvidence/GoldenReward/GrandCeremony and grand-tests-passed.xml. Editor capture stalls are not target-device performance measurements.

## Historical small installation (superseded)
The authored station and car now replace the old wedge/cloth in Level at the existing reward location. The raised pedestal, collider and placard are retained. The station width is 0.540894 versus pedestal width 0.830000; contact height differs by less than 0.000001. Re-running the installer preserves the same root, pedestal, collider, placard and location, with one controller and unchanged child count.

All 8 DebugSortAllTests pass, covering actual 1200-car completion and persistence, normal final placement, fixed glass throughout the four-second reveal, repeated completion, saved reload without animation, replay, resets, partial/misplaced/held saves, an empty visible pile, pause, scene unload and interrupted camera recovery. The prior zero-offset shelf assertion was updated to the existing fitted placement behavior with a 0.5 mm world-space tolerance for floating-point precision at X=500; shelf placement production code was not changed.

Actual running-game screenshots are in IntegrationEvidence/GoldenReward: reveal-in-game.png uses the real reveal camera; empty-in-game.png and shop-context.png use a temporary inspection camera in the running Level. Frames were confirmed advancing. Player-capsule clearance queries at front, left and right positions found no blockers. Unity reported no current console errors. Test-save keys and temporary Editor settings were cleaned up; the original MainMenu Scene is restored in Edit mode.

Runtime claim/pickup and driving remain outside this decorative-reward integration. The grand ceremony animates the reward's presentation pivot only. No ordinary item catalog, new progress counter or save schema was introduced.
