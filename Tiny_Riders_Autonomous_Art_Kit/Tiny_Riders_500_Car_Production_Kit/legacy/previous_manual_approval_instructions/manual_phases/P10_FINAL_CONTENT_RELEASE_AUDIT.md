# P10 — Final Content Release Audit

> **Mandatory:** Read `AGENTS.md`, `docs/05_PHASE_PLAN.md`, `docs/06_QA_ACCEPTANCE.md` and `progress/phase_status.json` first. Do not start unless the prior phase is explicitly approved, except P00.

## Task

**Precondition:** P09 approved. Run production validation across all SKU IDs, family source refs, export/Unity metadata, UV/material links, legal-originality checks, final 500 card labels, thumbnails, 100 build configs and 50 base silhouettes. Compare ALL package placements automatically against P03 fixed layout data, visually sample at least one package for every body and custom/livery combination, and check no text meshes via Blender inspection script.

Confirm platform requirements from current official CrazyGames docs rather than assuming old limits; test actual WebGL build in supported browsers and on low-end machine, save/reload, accidental crashes, mobile presentation if intended. Produce user-readable pass/fail report with known defects, package/logo signoff, no third-party marks, source/mesh/texture completeness and exact artifact build path. Do NOT publish or claim platform approval without explicit owner action. STOP and ask user to authorize release.

## Mandatory response format
1. **Completed actual assets** (exact verified filesystem paths).
2. **Proof** (actual screenshots/renders, flat texture sources, logs and script outputs).
3. **Pass/fail QA** (including no 3D printed typography).
4. **Packaging-lock compliance** (or `N/A` before P03).
5. **Known issues/blockers** — no fabricated success.
6. **STOP FOR HUMAN APPROVAL** — do not continue to next phase.
