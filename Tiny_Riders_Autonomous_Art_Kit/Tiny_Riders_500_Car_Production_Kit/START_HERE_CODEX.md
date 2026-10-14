# ONE PROMPT FOR CODEX GOAL MODE

1. Extract this entire `Tiny_Riders_500_Car_Production_Kit` directory into a Codex-accessible workspace.
2. Ensure Blender and Blender MCP and Unity Editor are available. No need to connect image-generation services; the reference PNGs and editable package templates are included.
3. In Codex Goal Mode, paste the full contents of **`GOAL_MODE_AUTONOMOUS_ART_ONLY.md`** (also duplicated as `MASTER_PRODUCTION_PROMPT.md`).
4. Let Codex run autonomously. It should NOT stop after Phase 0 or request approval for ordinary quality gates. It should record real model/render/import work in progress files and finish A0–A11.

**Kickoff shortcut (if Codex sees this directory):**

> Read `AGENTS.md`, then execute the entire `GOAL_MODE_AUTONOMOUS_ART_ONLY.md` now as an autonomous, resumable ART-ONLY task. Create 50 distinct base car meshes, 100 physical builds, 500 collectible livery variants and exact matching packaging, the modular 3D sorting room, all Unity art prefabs and static showcase scenes. Do not stop for human approval. Use automatic QA and checkpoints; do not write gameplay systems.

**Important:** The older manual gated prompts are in `legacy/` and MUST NOT override the autonomous contract.
