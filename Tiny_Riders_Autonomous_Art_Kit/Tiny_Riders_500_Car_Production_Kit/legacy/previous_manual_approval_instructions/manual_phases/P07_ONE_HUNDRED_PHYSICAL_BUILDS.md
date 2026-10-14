# P07 — One Hundred Physical Builds

> **Mandatory:** Read `AGENTS.md`, `docs/05_PHASE_PLAN.md`, `docs/06_QA_ACCEPTANCE.md` and `progress/phase_status.json` first. Do not start unless the prior phase is explicitly approved, except P00.

## Task

**Precondition:** P06 approved. For each of the 50 base bodies create precisely two physically different builds: `standard` and `custom`. Read `customBuildChange` from `catalog/bodies_50.json`. At least one meaningful mesh accessory/body-kit or chassis change must make custom identifiable WITHOUT color. Modular parts required wherever possible; no floating attachments, broken axles or redundant geometry.

Create 100 build definitions, source assets/attachment metadata and gray/clay comparison sheets in batches no larger than ten bodies (20 builds). Text or words may never become geometry. Deliver `production/QA/P07/build_manifest.json`, complete comparisons and owner approvals. STOP for P07 approval. New customs must keep model ID, template package footprint and shared scale conventions.

## Mandatory response format
1. **Completed actual assets** (exact verified filesystem paths).
2. **Proof** (actual screenshots/renders, flat texture sources, logs and script outputs).
3. **Pass/fail QA** (including no 3D printed typography).
4. **Packaging-lock compliance** (or `N/A` before P03).
5. **Known issues/blockers** — no fabricated success.
6. **STOP FOR HUMAN APPROVAL** — do not continue to next phase.
