# TINY RIDERS — Full 500-Collectible Production Kit

**This kit is a production specification, not 500 prebuilt Blender models.** It includes the **10 original AI-generated concept boards** from our discussion; a fixed low-poly style bible; a strict master packaging system; 50 individually defined base-body briefs; a deterministic 500-SKU catalog; phased Codex + Blender MCP prompts; quality gates; sample 2D packaging templates; and Unity/CrazyGames implementation guidance.

## Start in 3 steps
1. Extract the entire ZIP into a persistent folder that Codex can access. **Keep folder names unchanged.**
2. Open `START_HERE_CODEX.md` and paste its initial prompt into Codex while pointing Codex at this folder and the ten PNGs in `references/original_boards/`.
3. Execute **only phase P00** first. Every phase has an approval gate. Review the actual Blender renders and package flat textures before authorizing the next.

## Important distinctions
- **500 collectible products ≠ 500 separate 3D bodies.** This plan uses 10 families × 5 base bodies × 2 physical builds × 5 liveries = 500 unique collectibles.
- **The reference images are concept sheets, not ready-to-UV printable textures or exact orthographic engineering drawings.** Some labels/layouts differ between sheets. `docs/03_PACKAGING_MASTER_LOCK.md` resolves conflicts so EVERY package is consistent.
- **Printed material must stay 2D:** all words, numbers, logos, stripes, character faces, card artwork are texture files. Only actual solid physical parts become meshes.
- **Avoid licensed toy branding and real vehicle copies.** "TINY RIDERS" is a working original identity, not a trademark-checked product name.
- **No claims of completed assets without inspected files.** Generated catalogs and templates in this kit are ready; Blender meshes and Unity prefabs are future phase deliverables.

## Navigation
- `AGENTS.md` — enforced Codex instructions.
- `MASTER_PRODUCTION_PROMPT.md` — complete copy-paste production prompt and phase gates.
- `START_HERE_CODEX.md` — short alternative orchestration prompt.
- `docs/00...` through `docs/12...` — detailed art, packaging, geometry, phases, QA and integration rules.
- `prompts/phases/` — one standalone prompt per production phase.
- `prompts/families/` — one family brief per reference image.
- `catalog/` — 50 base-body descriptions and 500 deterministic collectible IDs.
- `design/packaging/` — editable flat SVG packaging sources and PNG previews, **not final per-car renders**.
- `references/original_boards/` — exact ten reference images from this chat.
- `references/packaging_crops/` — crops for packaging art direction only.
- `progress/` — sign-offs and checklists.
- `tools/` — data validation tools and source regeneration scripts.

## Deliverable count
10 AI reference images, 50 authored base-body directives, 100 builds to construct, 500 collectible metadata records, and 10 fixed family packaging skins sharing one universal layout.
