# Base shelf QA — 14 October 2026

**Result: requested shelf structure, artwork, car-fit and Unity-import checks passed.**

## Acceptance evidence

| Check | Result | Evidence |
| --- | --- | --- |
| One reusable red shelf, 3 rows × 6 spaces | PASS | Prefab hierarchy, front render, 5 internal dividers per row |
| 18 empty placement markers | PASS | `acceptance_checks.json` |
| Solid base; no storage, doors, drawers or bins | PASS | Front, side, hero and clay renders |
| Blank header center and number badge | PASS | Artwork masters, header close-up and Unity captures |
| Independent red paint and replaceable header | PASS | MAT_SidePanels_Red and MAT_Header; UV-mapped artwork |
| Printed graphics are textures | PASS | Clay override removes all prints; 0 FONT objects; all printed surfaces have UVs |
| Repeated elements share source geometry | PASS | All internal dividers share 1 mesh datablock |
| Existing cars fit | PASS | Bounds of all 150 established shelf-car prefabs; four representative prefabs shown in 18 review positions |
| Tall/wide/roof/spoiler clearance and visible fronts | PASS | `C_Unity_Fit_Front.png`, `C_Unity_Fit_Closeup.png` |
| Root scale/orientation/origin | PASS | Unit scale, zero root rotation and position, front +Z, ground origin |
| Markers lie on actual tray planes | PASS | Maximum numerical error 7.45e-8 m |
| Material/texture/shader references | PASS | 0 missing textures, 0 missing materials, no shelf shader compilation errors |
| Shelf contains no cars or runtime systems | PASS | 0 MonoBehaviours, lights and colliders in prefab; car instances are outside it |
| Source car assets preserved | PASS | 2,192 source/package/prefab files unchanged across the final verification fingerprint comparison |
| Emission on/off appearance | PASS | Both Blender and actual Unity camera captures inspected |

Measured final bounds: **3.633 × 3.600 × 0.927 m (W × H × D)**. Final evaluated/imported triangle count: **18,316**. The source remains editable with bevels and weighted normals; a separate evaluated export copy is also saved.

## Fit checks

The intended set is the current 150 `Assets/00_Project/GeneratedCarStore/Prefabs/Shelf_TR-C…` assets. Only active enabled mesh/skinned-mesh renderers contributed to bounds. Effects, colliders and unrelated non-rendering helpers did not contribute. Source meshes and established prefab scale were retained.

Maximum car bounds: **0.387296 × 0.381931 × 0.580000 m (W × H × L)**. Conservative slot envelope: **0.464 × 0.540 × 0.648 m**. Minimum total remaining clearance: **0.076704 m width, 0.158069 m height, 0.068000 m depth**. These conservative bounding-box checks cover every intended car; visual checks use representative existing cars, including the wide/tall C014, the roof-accessory/spoiler C001 variants and C015.

Review car instances were rotated and translated under wrappers. Their supplier assets and individual scales were not altered. Full per-prefab measurements and selected paths are in `car_measurements.json` and `preview_car_slots.txt`.

## Visual comparison and focused corrections

`Renders/Reference_Comparison.png` places the supplied large three-quarter concept beside the Blender result and actual Unity filled capture. The three-row layout, six spaces, stepped red sides, charcoal back/frame, light trays, blank white framed sign and closed base follow the authoritative large image.

Necessary scale deviation: the original 0.30 m depth cannot hold existing cars up to 0.58 m long. The complete shelf uses a consistent 3× enlargement; no individual car rescaling was introduced. The final bounds exceed the nominal enlarged envelope slightly because physical trim and inset panels project from it.

Focused corrections made during inspection: exposed the initially buried red skins; corrected lower-panel UVs; made the blank badge circular in side view; lowered divider backs for car visibility; smoothed bevel shading; corrected FBX empty-parent unit/axis conversion; corrected preview-car facing; isolated preview captures from lights/objects in the already-open game scene.

Visible differences retained: the concept's dramatic warm light spill and glow are reduced. The reusable Unity shelf uses emission without baked GI, per-slot lights or bloom. Blender and Unity use their own restrained studio illumination, so highlight strength and red appearance differ. The actual 2D glyphs/checker arrangement are clean reconstructions, and both category text and badge digits are intentionally absent as requested. Tiny fasteners and hidden mechanisms were omitted per scope.

## Saved checkpoint images

- Shape: `A_Front.png`, `A_Side.png`, `A_Clay.png`, `B_Hero_EmissionOn.png`.
- Materials: `B_Header.png`, `B_Dividers.png`, `B_SideDetail.png`, `B_Hero_EmissionOff.png`.
- Car fit: `C_Unity_Fit_Front.png`, `C_Unity_Fit_Closeup.png`.
- Actual Unity output: `D_Unity_Empty_EmissionOn.png`, `D_Unity_Empty_EmissionOff.png`, `D_Unity_Filled.png`.

Clay uses a uniform material override without texture-driven normal/bump inputs. No word-, number- or checker-shaped geometry remains in that view. The flat artwork files contain the printed graphics.

## Boundaries of verification

Verified in the existing Unity Editor and current URP pipeline. Future cars/configurations outside the measured 150-prefab set are unverified. A standalone player build and platform-specific GPU performance were not part of this art task and were not tested. No gameplay, sorting, inventory, progression, category-management or runtime interaction systems were added.

The original open scene was kept loaded; its unsaved work was not saved over or discarded. The Unity Editor was taken out of Play mode to author preview scenes. Only the generated preview scene is isolated in Scene view. Source-car fingerprint details are in `source_car_integrity.json`; numerical and source audits are in `acceptance_checks.json`, `unity_validation.json`, `geometry_report.json` and `blender_audit.json`.
