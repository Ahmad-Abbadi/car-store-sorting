# COPY THIS INTO CODEX (after extracting the whole kit)

You are working in the extracted **Tiny_Riders_500_Car_Production_Kit** folder. This is an ASSET PRODUCTION EXECUTION task, not a request for another concept or a new plan.

MANDATORY: First read `AGENTS.md`, `README.md`, `docs/00_PROJECT_SCOPE.md`, `docs/01_ART_BIBLE.md`, `docs/02_REFERENCE_INDEX.md`, `docs/03_PACKAGING_MASTER_LOCK.md`, `docs/04_GEOMETRY_TEXTURE_CONTRACT.md`, `docs/05_PHASE_PLAN.md`, `docs/06_QA_ACCEPTANCE.md`, and `progress/phase_status.json`. Look at each original PNG in `references/original_boards/`. Never guess that you have viewed an image; open it with actual available image inspection.

### Mandatory output contract
- Use Codex for writing scripts, deterministic catalogs, validation, and Unity integration.
- Use Blender MCP for **interactive Blender scene creation/inspection** and Blender Python/batch processing for repeatable production.
- Follow the strict **TINY RIDERS** deliberately chunky toy style, NOT realistic automotive modeling.
- `Track Club` is the structural packaging-layout master. The ten source boards are family-specific artistic references. All final packages use the SAME card size, die-cut, logo hierarchy, clear blister placement, typography system, card-layout zones, and 2D compositing method, varying only the approved family skin, actual car render, name, ID and rarity.
- **Never use 3D words, 3D logos, extruded letters, decorative decal planes or fake 3D reflections.** The words and designs are UV-mapped 2D textures, produced independently in editable vector/raster artwork files. Geometry is only for genuine physical shapes.
- **Never copy a concept-board screenshot directly onto the card.** Render the actual approved 3D car for the card illustration, and construct the layout in editable flat 2D art layers.
- **Create no more than the currently authorized phase.** `NOT_STARTED` or `NEEDS_REVIEW` is not an approval. Only the owner may approve a visual quality gate.
- If Blender MCP, Blender or Unity access is absent, state exactly what is unavailable and complete only the independent tasks that tools permit. Never claim unverified screenshots or finished assets.

### Start now with Phase P00 only
Execute `prompts/phases/P00_INTAKE_AND_REFERENCE_LOCK.md`. Verify files and tools, run `python tools/validate_catalog.py`, inspect the ten boards, establish reference-to-family mappings, identify conflicts, and produce a short report with a recommended canonical packaging breakdown. Do **not** generate 500 cars, or any models, in this first phase. Stop and ask for P00 approval after providing real evidence. Subsequent phase prompts are already in `prompts/phases/`.

Always report: created files, inspected views, validation results, missing dependencies, known defects, and the exact next approval gate.
