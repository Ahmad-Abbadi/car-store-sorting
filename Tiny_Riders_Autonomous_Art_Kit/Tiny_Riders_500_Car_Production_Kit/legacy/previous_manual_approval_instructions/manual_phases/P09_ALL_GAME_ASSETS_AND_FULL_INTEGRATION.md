# P09 — All Game Assets And Full Integration

> **Mandatory:** Read `AGENTS.md`, `docs/05_PHASE_PLAN.md`, `docs/06_QA_ACCEPTANCE.md` and `progress/phase_status.json` first. Do not start unless the prior phase is explicitly approved, except P00.

## Task

**Precondition:** P08 approved. Integrate approved 500-SKU catalog into Unity sorting game, including ALL extra assets from `docs/12_GAME_ASSET_REQUIREMENTS.md`: sorting table, pile area, garage walls/floor, shelf modules, rarity/showcase cabinets, shelf category signs, collection catalog UI, currency/upgrades UI, correct/wrong snap feedback, inspection mode, save/resume, lightweight VFX/audio placeholders only if rights-cleared. No fixed numbered levels; expand families in the same sorting garage.

Build optimized resources using shared meshes, low-detail shelf package thumbnails/atlases, pooled objects and actual package close-up only when inspecting. Never keep 500 high-detail/transparent blister objects active. Test browser loading, memory, text legibility and gameplay on practical desktop/low hardware. Store genuine build output, screenshots, profiler captures and asset manifests. Stop for P09 review with real browser evidence.

## Mandatory response format
1. **Completed actual assets** (exact verified filesystem paths).
2. **Proof** (actual screenshots/renders, flat texture sources, logs and script outputs).
3. **Pass/fail QA** (including no 3D printed typography).
4. **Packaging-lock compliance** (or `N/A` before P03).
5. **Known issues/blockers** — no fabricated success.
6. **STOP FOR HUMAN APPROVAL** — do not continue to next phase.
