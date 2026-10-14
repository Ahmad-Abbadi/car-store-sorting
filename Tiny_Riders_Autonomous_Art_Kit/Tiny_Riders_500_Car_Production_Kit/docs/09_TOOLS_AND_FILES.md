# 09 — Tool Integration and File Strategy

## Codex + Blender MCP workflow
1. Codex reads `AGENTS.md`, user-approved phase prompt, source board and relevant family brief.
2. Codex writes/updates deterministic Blender Python generation functions; Blender MCP provides live Blender inspection, actual camera previews and hands-on geometry corrections.
3. Codex uses background/headless Blender for repetitive batches **after** a human-approved sample succeeds. MCP is not an efficient substitute for thousands of batch operations.
4. Flat printed artwork is generated outside Blender with SVG/raster scripting and projected via actual UVs; no 3D typography.
5. Blender exports approved meshes and material manifests; Unity Editor scripts import, assign runtime shaders, configure prefabs and build QA scenes.
6. Codex reports actual artifacts and screenshot paths; tool unavailability must be stated without fabrication.

## Folder contract for PRODUCTION OUTPUTS (these are created later)
```
production/
  Blender/Masters/{bodyId}/base.blend
  Blender/Builds/{bodyId}/{build}/build.blend
  Textures/Liveries/{sku}/basecolor.png
  Textures/Liveries/{sku}/source.svg
  Textures/Package/{sku}/front_source.svg
  Textures/Package/{sku}/back_source.svg
  Textures/Package/{sku}/front_master.png
  Textures/Package/{sku}/back_master.png
  Exports/Car/{bodyId}/{build}.fbx
  Exports/Package/package_master.fbx
  Previews/{sku}/car_3quarter.png
  Previews/{sku}/package_3quarter.png
  QA/{batchId}/contact_sheet.png
  QA/{batchId}/audit.json
  UnityProject/... (or link to existing Unity project)
```

Keep sources, exports and previews separate. Do not fill this kit with empty fake `.blend` files that look complete. Do not create placeholder success reports.

## Fonts and rights
Use fonts installed on build machine for 2D layout and record the font name/license. No need to distribute font files. `TINY RIDERS` is a working brand and needs proper commercial name clearance before publication.

## Tool fallback
If Blender MCP disconnected, stop Blender creation and report steps to connect it; Codex can still inspect and validate JSON and pre-rendered images. If Unity unavailable, export work can be completed but the Unity QA gate must remain unapproved. If AI image generation unavailable, create original flat background vector art; do not model decorative text to fill the gap.
