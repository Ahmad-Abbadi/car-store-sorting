# P02 — Blue Racer 3D Pilot

> **Mandatory:** Read `AGENTS.md`, `docs/05_PHASE_PLAN.md`, `docs/06_QA_ACCEPTANCE.md` and `progress/phase_status.json` first. Do not start unless the prior phase is explicitly approved, except P00.

## Task

**Precondition:** P01 approved. Create ONLY `TC-01` — Track Club Blue Racer (#23 livery) — as a real Blender model using Blender MCP and deterministic bpy. Match the chunky reference: short broad blue coupe, simple trapezoidal cabin, opaque dark glass, thick black low-sided tires, simple rectangular headlights, sturdy wing, white dual race stripes and printed `23` on readable left AND right door. No smooth realistic car from previous failed test. Build editable body mesh and meaningful custom physical configuration. **All words/numbers/stripes/painted headlight details are 2D UV textures, never modeled**.

Create actual `production/Blender/Masters/TC-01/base.blend`, `production/Blender/Builds/TC-01/custom.blend`, UV/PNG/flat livery art, 3/4/side/top/rear/clay/silhouette renders, and comparison contact sheet with the attached track club reference. Confirm no Blender text/FONT or text-shaped meshes. Report triangle counts, material hookups, texture file paths and failures. STOP immediately if clay silhouette unconvincing; don't hide bad modeling with paint. STOP for human P02 car approval before packaging.

## Mandatory response format
1. **Completed actual assets** (exact verified filesystem paths).
2. **Proof** (actual screenshots/renders, flat texture sources, logs and script outputs).
3. **Pass/fail QA** (including no 3D printed typography).
4. **Packaging-lock compliance** (or `N/A` before P03).
5. **Known issues/blockers** — no fabricated success.
6. **STOP FOR HUMAN APPROVAL** — do not continue to next phase.
