# 05 — Production Phases & Stop/Approval Gates

**A phase is a bounded task, not permission to automate until 500 assets appear.** Only the human owner authorizes the next phase after inspecting evidence. The machine may mark NEEDS_REVIEW but not self-approve.

| Phase | Goal | Minimum actual evidence / deliverables | Stop gate |
|---|---|---|---|
| **P00 Intake** | Verify 10 images, tools, catalogs, style conflicts | Reference mapping, catalog validation output, dependency list | Owner confirms sources & scope |
| **P01 Visual grammar** | Lock proportions, 2D logo, art and family palette system | Written art lock, palette sheet, component/angle specification | Owner approves art rules |
| **P02 Pilot car** | Create ONE Track Club Blue Racer base and custom version | Blender scene, clay/side/3/4/front views, editable UV/flat decal, dimensions | **Car silhouette + texture approved** |
| **P03 Package master** | Create ONE finished pilot blister, exact printed artwork + back | UV files, real front/back card textures, physical card + blister, final car package render + clay audit | **Packaging master approved** |
| **P04 Unity slice** | One small non-level sorting game with approved car/packs | Unity project scene, pick/move/snap, 10-30 display instances, on-screen selected-car inspection, browser capture/profiler | User approves gameplay & visual match |
| **P05 Family masters** | One approved base per each of 10 families | 10 real base bodies, 10 family previews, gray contact sheets, packaging color skins | Family shape/palette gate |
| **P06 Complete 50 bases** | Four remaining silhouettes per family | 50 genuinely distinct base-body meshes, family contact sheets, silhouette QA | Owner approves 50 silhouettes |
| **P07 Produce 100 physical builds** | Standard/custom geometry for all bodies | 100 build definitions and approved physical add-ons, no color-only customs | Owner approves 2-build system |
| **P08 Produce 500 liveries/packages** | Five texture editions for each build; finished SKU art | 500 metadata-linked SKUs, flat 2D artwork, package renders, corrected text, ID/rarity checks | Owner approves batch QA |
| **P09 Full game integration** | Real collection rooms and progress with 500-SKU content | Unity browser build with streaming, saves, shelves, upgrades, no traditional level map | Gameplay/performance gate |
| **P10 Release review** | Freeze content, verify platform compliance | QA ledger, real browser test results, legal audit, build artifacts | User authorizes release |

## The sequence that saves cost
- **P02 first!** If one pilot car is unacceptable, don't attempt packaging or 50 models.
- **P03 second!** If the first packaged product does not match the reference, revise the universal template ONCE; no 500 card generation.
- **P04 early!** Test the final art direction in the intended Unity browser before investing in thousands of details.
- P05 selects a hard problem from each family: food topping, creature face, low racer, heavy machine, emergency vehicle, and so on. Fix generator shortcomings before bulk assets.
- P06: work in **batches of at most 10 bases** (ideally one family of 5 at a time). QA each batch; do not build all 50 blindly.
- P08: build artwork in **batches of 25–50 products**, create review sheets, inspect label/UV errors and demand approval for the production batch before continuing. Phase may have many internal batch signoffs.

## Approval criteria
Every phase must deliver: (a) actual file paths; (b) screenshots/contact sheets; (c) test commands and results; (d) failures/deviations; (e) the next gate. An agent cannot use an internally assigned success score to bypass the owner's visual approval.

## Rework policy
If a base-body design fails, return to P02/P05/P06 master body, not a painted livery. If card layout fails, return to P03 master—not a per-product image edit. If performance fails, revise runtime assembly/LOD and texture delivery, not reference style or original master artwork unnecessarily.
