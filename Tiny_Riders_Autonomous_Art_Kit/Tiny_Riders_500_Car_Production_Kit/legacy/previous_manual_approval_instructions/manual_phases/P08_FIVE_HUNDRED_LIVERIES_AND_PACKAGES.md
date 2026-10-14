# P08 — Five Hundred Liveries And Packages

> **Mandatory:** Read `AGENTS.md`, `docs/05_PHASE_PLAN.md`, `docs/06_QA_ACCEPTANCE.md` and `progress/phase_status.json` first. Do not start unless the prior phase is explicitly approved, except P00.

## Task

**Precondition:** P07 approved AND P03 universal card master approved. Generate exactly 500 SKU-specific 2D UV liveries and product card textures from `catalog/collectibles_500.json` (10 families × 5 bases × 2 builds × 5 liveries), using a deterministic batch script. Process in batches of 25–50 SKUs with owner batch review. **DO NOT** ask a generative image model to design complete packages independently. Do not create letters, logos, stripes, eyes, printed light panels, numbers or images as meshes. Use 2D SVG/PNG source assets aligned to actual UVs.

For each SKU: render actual finalized car with that correct build, wheels, decals and finish, then composite EXACT render onto same universal flat card layout; family-specific color/scene motifs only as family art skin. Preserve hanger, brand, banner, blister position, lower product number strip, font, UVs and geometry. Flatten final front/back art into actual textures. Prevent race number confusion with catalog `001/500` count. Create side-by-side product vs printed car QA and contact sheets. Run `python tools/validate_catalog.py` and generated asset completeness checks. STOP after each batch requiring owner review; report all 500 paths and no invented completion.

## Mandatory response format
1. **Completed actual assets** (exact verified filesystem paths).
2. **Proof** (actual screenshots/renders, flat texture sources, logs and script outputs).
3. **Pass/fail QA** (including no 3D printed typography).
4. **Packaging-lock compliance** (or `N/A` before P03).
5. **Known issues/blockers** — no fabricated success.
6. **STOP FOR HUMAN APPROVAL** — do not continue to next phase.
