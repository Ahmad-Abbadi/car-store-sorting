# P03 — One Canonical Package Master

> **Mandatory:** Read `AGENTS.md`, `docs/05_PHASE_PLAN.md`, `docs/06_QA_ACCEPTANCE.md` and `progress/phase_status.json` first. Do not start unless the prior phase is explicitly approved, except P00.

## Task

**Precondition:** P02 approved. Make ONE real 3D `TINY RIDERS` blister package for approved `TC-01-S` edition and lock all visual hierarchy across 500 SKUs. Structural visual ref: **Track Club lower-left front/back blister card**, `references/original_boards/02_track_club.png` and cropped version. Exact production layout: `docs/03_PACKAGING_MASTER_LOCK.md`. 120×180mm card, printed master 1200×1800, fixed centered hanger, fixed brand/logo y-position, chip, actual Blender car hero, physical clear blister lower, product/footer/name and SKU. Back card on separate UV material. Never paste the reference screenshot as texture. Never model words as geometry. Never generate independent AI image of car.

Make editable flat SVG and PNG front/back for the pilot, both mapped onto physical cardboard, plus separate shared `package_master.blend`, exact car-in-blister render, side comparison of hero art vs actual car, close-up blister, front/back textures, package gray clay and wireframe. The existing flat templates in `design/packaging/` are *guides*, not finished product artworks: replace their empty hero slot with render of THIS exact approved 3D car, and correctly typeset all details. Sign off ONE stable package first. **Do not generate 500 variants and do not alter the master package per family.** STOP for P03 packaging approval.

## Mandatory response format
1. **Completed actual assets** (exact verified filesystem paths).
2. **Proof** (actual screenshots/renders, flat texture sources, logs and script outputs).
3. **Pass/fail QA** (including no 3D printed typography).
4. **Packaging-lock compliance** (or `N/A` before P03).
5. **Known issues/blockers** — no fabricated success.
6. **STOP FOR HUMAN APPROVAL** — do not continue to next phase.
