# Theme accessory refinement R03 — validation report

Completed: 20 discovered themes, 20 usable shared kits, 20 unique primary meshes, 0 secondary models, 150 existing variants updated from revision 2 to 3 across 13 base builds. No new collectible IDs; 0 skipped variant fits. C006 and C013 remain outside the existing catalog, as before this pass.

| Existing theme | Primary model | Added triangles | Updated variants |
|---|---|---:|---:|
| TH_PIRATE | Tricorn | 1176 | 8 |
| TH_ROYAL | Crown | 1452 | 8 |
| TH_SPACE | RingedPlanet | 1156 | 8 |
| TH_ALIEN | Saucer | 1360 | 8 |
| TH_BEACH | Surfboard | 612 | 8 |
| TH_JUNGLE | LeafCanopy | 512 | 8 |
| TH_SPOOKY | Pumpkin | 888 | 8 |
| TH_CANDY | WrappedSweet | 712 | 8 |
| TH_FOOD | Burger | 1072 | 8 |
| TH_FIRE | MoldedFlame | 404 | 8 |
| TH_ICE | CrystalCluster | 188 | 7 |
| TH_OCEAN | DorsalFin | 156 | 7 |
| TH_CONSTRUCTION | HardHat | 804 | 7 |
| TH_RESCUE | RescueBeacon | 968 | 7 |
| TH_WIZARD | BentWizardHat | 868 | 7 |
| TH_ROBOT | SensorHead | 688 | 7 |
| TH_MUSIC | Headphones | 796 | 7 |
| TH_SPORT | Football | 668 | 7 |
| TH_FARM | FarmHat | 900 | 7 |
| TH_PARTY | BalloonCluster | 1248 | 7 |

## Actual checks

- Blender 5.1 sources and FBX exports: one UV layer and material slot per kit, zero boundary/non-manifold edges, no rigs or negative scale. Per-theme scene-based .blend files include packed original 1024 atlases. Triangle range 156–1452. Ocean and Ice intentionally use fewer than 400 triangles; their simple silhouette/facets do not need filler geometry.
- Unity 6000.3.22f1 / existing URP 17.3.0: imported mesh scale/axes, UV coverage, shaders, shared prefab instances and geometry budgets checked for all 150. No new runtime behavior, colliders, rigidbodies or animation components.
- All original nodes retain meshes, material assignments, active state, transforms and component types. All 150 prefab GUIDs and catalog IDs retained. Supplier protection: 1,292 files unchanged; existing body material/texture protection: 1,384 files unchanged; 2,338 existing .meta files unchanged; 10 runtime files unchanged.
- Unity's material validation had added _EMISSION/EmissiveIsBlack to 109 derived R02 materials. Restored only these flags through the Material API; final file hashes match the baseline exactly.
- Regeneration completed twice. Both audits report 150 owned roots, 150 shared-kit instances, 20 kit assets, 20 accessory materials, unchanged prefab/mesh/kit GUIDs, no duplicate IDs or roots. See regeneration_validation.json.
- Measured actual body triangles selected 13 fitting profiles. Open-cockpit C010 uses the solid rear deck ahead of the fixed wing. Uniform scales and small concealed mounting embed only. Maximum vertical vertex/body penetration 0.0735 source units; no glass-penetration or missing-contact flags. This numerical screen complements visual review; it is not a physics collision proof.
- Reviewed actual Unity front, side and elevated images for all 150, all 20 gray-clay representatives, native 64/128 px car-width sheets, and two fixed static pile arrangements. All 150 also have individual clay and perspective renders. Large body art, wheels and windshields remain visible; no optional vendor parts were disabled.
- Controlled before/after uses the same baseline bounds, camera, light, background, scale and exposure. Hidden-accessory controls: 150/150 exact pixel matches; maximum mean absolute 8-bit channel error 0.000000. See hidden_accessory_controls.json.

## Camera and shelf limits

The existing Level Main Camera is at (500, 2.9, -20.857), rotation (0,0,0), FOV 60, aspect 16:9. Captured its existing view read-only and rendered 20 before/after representatives at an observed displayed product pose, with identical camera projection/transform. Tests use an isolated preview and studio lighting; this is not completed scene integration. The chosen existing product is rotated in the current pile, so these views also expose side/underside occlusion. See actual_camera_test.json.

The existing shelf-ready collection is separate and substantially smaller. Its cell is approximately 0.464 × 0.54 × 0.648 units. All 150 unscaled themed prefabs already exceeded those limits before R03 and remain unsupported at that shelf's raw scale. Exact IDs and expanded bounds are in geometric_fit_checks.json and variant_assignments.json. No car/shelf scale, scene, shelf, or gameplay changes were made to force compatibility.

## Visual findings and limits

Pirate's broad three-corner folded hat differs from Wizard's tall bent cone. Space's physical ring, Alien's lens/dome and Robot's angular visor remain different. Ocean's single swept foil differs from Ice's three facets. Fire's lobes differ from Spooky's ribbed pumpkin; Candy's wrappers differ from Food's layered bun. Gray-clay comparisons retain these differences without artwork.

At 64 px, small printed marks are naturally lost. Beach's low board is weaker from a low side view; Music's arch is strongest from front/elevated and narrow edge-on; Rescue's heart is less clear edge-on. Farm and Construction both use hat language, distinguished by brim/crown/ridge, palette and body art. In the piled samples some accessories are occluded by other cars; no claim is made that every theme is visible from every pose. These are visual review results, not evidence of one-second player recognition. Recognition playtesting remains a later integration task.

## Evidence

Start with contact_sheets/AllThemes_Front.png, AllThemes_Clay.png, Native128.png, Native64.png, MixedNamesHidden.png and before_after/AllThemes_BeforeAfter.png. Per-theme sheets and all 150 multi-view renders are beside them. Two pile pairs and 15 mixed pairs retain matching before/after layouts. Baseline recovery is under baseline/Collection with original .meta files and source hashes.
