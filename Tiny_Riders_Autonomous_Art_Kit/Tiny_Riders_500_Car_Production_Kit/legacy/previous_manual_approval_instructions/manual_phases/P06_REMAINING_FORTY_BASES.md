# P06 — Remaining Forty Bases

> **Mandatory:** Read `AGENTS.md`, `docs/05_PHASE_PLAN.md`, `docs/06_QA_ACCEPTANCE.md` and `progress/phase_status.json` first. Do not start unless the prior phase is explicitly approved, except P00.

## Task

**Precondition:** P05 approved. Build the remaining four base silhouettes for every family, following exact `catalog/bodies_50.json`. Process at most one family or 10 bodies per batch. Shape variety is mandatory; do not recolor the hero and call it a new body. For each family produce 5 approved base shapes and standardized contact sheets including black silhouette, profile, front and 3/4. Use the defined family source PNG and detailed family prompt in `prompts/families/`.

At each batch: automated geometry check, owner visual comparison, revise weak silhouettes, then STOP for batch approval. After all batches, deliver 50 editable base-body meshes with real proof, reproducible blender scripts and full `comparison_50_bodies.png`. Zero missing base IDs. Never force completion without review. STOP for P06 approval.

## Mandatory response format
1. **Completed actual assets** (exact verified filesystem paths).
2. **Proof** (actual screenshots/renders, flat texture sources, logs and script outputs).
3. **Pass/fail QA** (including no 3D printed typography).
4. **Packaging-lock compliance** (or `N/A` before P03).
5. **Known issues/blockers** — no fabricated success.
6. **STOP FOR HUMAN APPROVAL** — do not continue to next phase.
