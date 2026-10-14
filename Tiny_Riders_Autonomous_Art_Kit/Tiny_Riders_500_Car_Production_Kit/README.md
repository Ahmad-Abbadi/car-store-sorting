# TINY RIDERS — AUTO-RUN ART FACTORY (CODEX GOAL MODE)

This version of the reference and catalog kit is specifically updated for the current goal: **produce ALL 3D art and import it to Unity automatically, with no routine manual approvals and NO gameplay implementation**.

## Start

1. Unzip this archive and keep the complete folder intact, including `references/`, `catalog/`, `design/`, `tools/`.
2. Connect Codex to Blender MCP, make sure local Blender is installed, and provide a Unity project/Editor when available.
3. Paste the entire `GOAL_MODE_AUTONOMOUS_ART_ONLY.md` into Codex Goal Mode.
4. Let it self-validate and work A0→A11 automatically, resuming in batches if interrupted.

## What's already in this kit

- **10 original concept sheets** in `references/original_boards/` and 10 hero crops.
- **Canonical 50-body blueprint** in `catalog/bodies_50.json`.
- **500 stable collectible SKUs** in `catalog/collectibles_500.json`.
- **One 120×180 mm locked packaging system** in `docs/03_PACKAGING_MASTER_LOCK.md`, family SVG templates and original shared vector logo in `design/`.
- Scripts for validating planned catalog IDs and card positioning.
- **One autonomous Goal Mode prompt**, updated `AGENTS.md`, QA and asset checklists.

**Important:** 500 model/packaging assets are NOT prebuilt. They are the intended output of Codex executing the task. JSON records and reference images are not counted as finished assets.

## What Codex should actually create

- 50 original chunky 3D base bodies, 100 standard/custom physical builds, 500 distinctly finished SKU variants, matching 500 packaging fronts/backs, reusable physical blister mesh.
- A modular art set for a 3D collector sorting room: shelves, storage, showcase, pile tray, sorting table, room shell, family signage, and supporting props.
- Real Blender sources, exports, UV artwork, source SVGs, production texture atlases, renders/contact sheets, QA files.
- Unity-ready art, 500 loose-car art prefabs, 500 packaged art prefabs, and art-only preview scenes, if Unity is available.

## Which files govern?

- `GOAL_MODE_AUTONOMOUS_ART_ONLY.md` — **highest active task contract**.
- `AGENTS.md` — new autonomous run policy.
- `docs/01_ART_BIBLE.md`, `docs/03_PACKAGING_MASTER_LOCK.md`, `catalog/*.json` — visual and ID truth.
- `prompts/families/` — family-specific art descriptions.
- `legacy/` — older manual phase approval instructions; **not active**, never follow their STOP rules.

**Not included:** a playable game, sorting mechanics, interactive inventory, UI systems, ads/monetization, gameplay saves, SDK integration, final CrazyGames upload. Those will be separate future work.
