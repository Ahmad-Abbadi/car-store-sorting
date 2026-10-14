# P00 — Intake And Reference Lock

> **Mandatory:** Read `AGENTS.md`, `docs/05_PHASE_PLAN.md`, `docs/06_QA_ACCEPTANCE.md` and `progress/phase_status.json` first. Do not start unless the prior phase is explicitly approved, except P00.

## Task

**Execute intake only. NO MODELING.** Read `AGENTS.md`, the art bible, 10 source boards, packaging lock, and catalog. Inspect all 10 exact PNGs with actual image tools. Run `python tools/validate_catalog.py`, verify the 500-row CSV equals JSON, check all 50 bodies, list file sizes and reference count, and summarize the visible difference between the ten family packaging concept art layouts.

Deliver `production/QA/P00/reference_report.md` and `production/QA/P00/file_inventory.json`, with documented Blender/Blender MCP/Unity availability, ten inspected source images, the primary Track Club packaging hero/crop identified, file paths and real test output. Do not generate cars or change locked template size. STOP for human P00 approval.

## Mandatory response format
1. **Completed actual assets** (exact verified filesystem paths).
2. **Proof** (actual screenshots/renders, flat texture sources, logs and script outputs).
3. **Pass/fail QA** (including no 3D printed typography).
4. **Packaging-lock compliance** (or `N/A` before P03).
5. **Known issues/blockers** — no fabricated success.
6. **STOP FOR HUMAN APPROVAL** — do not continue to next phase.
