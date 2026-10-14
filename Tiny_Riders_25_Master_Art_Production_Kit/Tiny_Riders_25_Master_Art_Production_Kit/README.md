# TINY RIDERS v2 — 25 Premium Masters to 500 Collectibles

**Production architecture and Codex art execution kit.** Built for the user’s stylized toy-car sorting game. Includes *existing* ten family concept/reference boards, earlier logo/packaging design templates, a corrected 25-master shared geometry map, preserved 50 named body IDs and 500 SKU numbers, an executable Codex Goal Mode prompt, strict packaging and QA rules, and catalog validation. It **does not** include completed premium 3D master meshes. Those are the required art sources Codex must locate, acquire with appropriate user permission, produce, refine and verify.

## Start

1. Extract this folder where Codex can see it and keep the references alongside the catalog.
2. Connect Blender MCP, confirm Blender executable, and point to an existing Unity project if available.
3. Open `CODEX_GOAL_PROMPT.md`; paste its entire contents into Codex Goal Mode. Do not use the older prompt from the 50-base kit.
4. Codex should run `python scripts/validate_catalog.py` before doing art work, inventory legal/quality sources for 25 master meshes, then execute art batches without routinely stopping.

## Primary files

- `CODEX_GOAL_PROMPT.md`: **single autonomous execution prompt**.
- `docs/01_FULL_PRODUCTION_ARCHITECTURE.md`: full geometry/texture/Unity architecture.
- `docs/02_PACKAGING_MASTER_LOCK_V2.md`: identical packaging placement and dimensions, one logo.
- `docs/03_MASTER_SOURCE_QUALITY_GATES.md`: master mesh quality, rights, and review conditions.
- `docs/04_UNITY_ART_INTEGRATION.md`: Unity art prefab structure.
- `catalog/masters_25.csv`: readable list showing exactly which two named designs share a master.
- `catalog/collectibles_500_v2.json`: existing immutable SKU IDs augmented with master/build identity references.
- `scripts/validate_catalog.py`: strict structural validator, not an art completion validator.
- `references/original_boards`: ten full AI concept images generated for the original art direction.
- `design/packaging`: editable family templates and universal locked guide; they are layouts, not complete per-SKU printed artwork.

## Product equation

**25 premium foundation meshes × 2 distinct named identities × 2 physical builds × 5 liveries = 500 products.** Ten families stay at 50 SKUs each. The named body pairings intentionally cross families in five cases. Preserve original IDs and product numbering.

## Quality-first reality

This plan reduces premium base mesh acquisition/sculpting from 50 to 25, but still requires **25 genuinely appealing source models**. No prompt guarantees an AI model will invent 25 premium cars without human work. Codex should automate physically meaningful modular versions, printed liveries, exact packaging artwork and Unity imports only after base visual quality has been verified. Where source/rights are missing, status remains blocked, not complete.
