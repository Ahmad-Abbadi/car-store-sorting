# 12 — Complete Asset Checklist Beyond Cars

The 500-product collection needs **game assets**, not just 500 isolated toy cars. Every phase prompt should include the following eventual production items while only implementing those in scope.

## Per SKU / product
- Stable ID, name, collection/family, body ID, build ID, livery, rarity, product number.
- Approved 3D car as body + custom parts + livery textures.
- Correct loose-car thumbnail, high/low LOD if needed, collider spec.
- Flat front/back card art, accurate car hero render, universal package prefab assembly, tiny shelf thumbnail.
- QA: clay, 3/4, side, UV, package art match.

## Shared vehicle library
- Tires/rims, bumpers, material palette, 3/4 studio rig, preview camera, simplified light rigs.
- Standard 3D assembly points for four wheels, optional extra wheels, spoilers, roof gear, body accessories.
- Shared shader presets: body satin, rubber rough, opaque windows, flat-print card, optional clear blister for inspection.

## Shared environment
- 3D pile container, collection sorting worktable, shelf modules, wall gallery, collection label slots, floor/walls, lighting, rare showcase cabinet, simple packing/storage boxes.
- Visual hierarchy: messy colorful pile foreground, recognizable family themed shelf groups, clear placement and count feedback.
- Avoid demanding custom 3D room decoration for every family in first pass; family identity through small shelf signs, colored boards and background prints.

## Gameplay UI
- Catalog book/grid, 500 progress, family tabs, selected-car detail card, rarity treatment, input prompts, collection-complete feedback, sorting score/currency, tool upgrade interface, save/resume.
- All UI text belongs in Unity UI TextMeshPro/uGUI or toolkit, NOT physically modeled into the 3D scene as dozens of decorative meshes. UI text differs from physical printed product lettering: game interface naturally uses UI components.

## Audio/VFX/feedback
- Pick up, place, correct snap, wrong placement, rare-find reveal, shelf-completion, pack open if designed.
- Small 2D or particle confetti on collection complete; no heavy permanent model effect.
- Rarity reveal shouldn't depend solely on color for accessibility.

## WebGL QA
- Distance-based LOD, compressed/atlased images, UI scaling, simple physics, object pooling, save system and actual browser performance profile.
