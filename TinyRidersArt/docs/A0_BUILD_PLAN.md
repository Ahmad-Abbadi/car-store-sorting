# Tiny Riders — Phase A0 art build plan

**Scope:** 3D vehicle and package art, printed 2D artwork, previews, and clean Unity art import only. The user-directed A0–A8 phases below govern this project. Stop at each review gate; never treat a successful script run as visual approval.

| Phase | Art work and review evidence | Gate |
| --- | --- | --- |
| **A0 — now** | Audit all ten sheets, featured crops, gallery, packaging guide/templates, 50-body catalog; write this plan and family summary. | Owner reviews references and flagged discrepancies. |
| **A1** | Make only `TC-01` Blue Racer #23. First establish unprinted shape; then real UVs and 2D number/stripe print. Save editable Blender source, Unity-friendly export, textures, front 3/4, side, rear 3/4, top, gray clay and wireframe renders. | Owner approves car silhouette and artwork. |
| **A2** | Build one 120 × 180 mm card and real die-cut/blister, with separate front/back UV art based on the locked guide. Print the exact A1 car render on the upper card and place that car inside. Fix long-family-label fit inside the fixed banner. | Owner approves the one permanent package structure. |
| **A3** | Make one featured hero and one package per family; compare at one camera scale using 3/4, side, clay and family sheets. Resolve featured-car accessories against the standard/custom definitions. | Owner approves ten family identities. |
| **A4** | Resolve the `CC-05` and `TC-04` concept-versus-catalog shape conflicts before modeling those two; then model five distinct catalog base silhouettes per family in reviewable batches. Save editable scenes, exports, textures and per-car previews; make ten family sheets and one 50-car gallery. | Owner approves the 50 silhouette set. |
| **A5** | Create one standard and one visibly changed custom physical configuration per base, with side-by-side clay and beauty comparisons. | Owner approves 100 physical builds. |
| **A6** | Create five controlled, UV-printed livery editions per build; compose exact-car package artwork and unique catalog numbers for all 500 SKUs. Review in manageable batches with contact sheets and label checks. | Owner approves the 500-product catalog. |
| **A7** | Import finished cars, packages, textures and materials into Unity; create loose and packaged art prefabs. Capture actual Unity views for loose cars, packaged cars, family lineups, pile and shelf. | Owner approves Unity appearance and scale. |
| **A8** | Inspect UVs, text orientation, missing maps, duplicates, numbers, family colors, blister alignment and Unity imports. Fix art issues and issue a before/after report. | Final art approval. |

## Working method

1. Save source under `TinyRidersArt/blender/`, meshes under `exports/cars/` and `exports/packages/`, 2D artwork under `textures/cars/` and `textures/packages/`, renders by phase, and reports under `qa/`. Create folders when their phase begins; do not populate later-phase assets during A0.
2. Use catalog IDs (`TC-01`) and SKUs (`TR-TC-01-S-01`) as stable links among body, build, livery, package art and preview. Keep race number `23` separate from collection number `051/500`.
3. Model meaningful volume; UV-print all words, numbers, stripes, logos, windows and face/lamp graphics; use materials and lights for sheen. Gray-clay checks must erase every printed feature.
4. Use one fixed camera and scale for comparison sheets. Inspect the source reference beside each render, plus a 140 px readability view and readable left/right typography before sign-off.
5. Treat the existing template art as editable **bases**, not finished print masters. A2 locks the universal logo, card coordinates and real blister; later family versions only change controlled palette, motif, name, number and exact-car illustration.
6. Keep the work art-only. No sorting mechanics, UI programming, saves, SDKs, monetization, or gameplay changes.

**Current stop:** Phase A0 is ready for review. Do not continue or approve the pre-existing A1 pilot until the owner approves A0 and the logged reference interpretations.
