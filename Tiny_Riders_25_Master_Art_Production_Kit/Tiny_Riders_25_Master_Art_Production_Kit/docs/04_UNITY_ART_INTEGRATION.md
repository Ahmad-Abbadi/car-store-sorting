# Unity art import architecture (no gameplay)

## Recommended hierarchy

```
Assets/TinyRidersArt/
   Models/Masters25/              25 approved FBX files
   Models/Modules/                body identity kits, attachment meshes
   Materials/Shared/              toy paint, window, tire, shell, cardboard
   Textures/Car/                  print/paint UV atlases
   Textures/Packaging/            derivative runtime package atlases
   Prefabs/Masters/               25 base art prefabs
   Prefabs/Identity50/            identity variants with visual modules
   Prefabs/Build100/              standard/custom geometry
   Prefabs/Products500/           per-SKU prefab variants
   Prefabs/Packaged500/           nested shared packaging prefab + car
   ArtCatalog/                   editor-only product definitions
   Scenes/ArtReview/              preview ONLY
   Editor/                        asset import builders/QA tools
```

Unity nested prefabs and prefab variants are suited to a shared-master-plus-overrides design. Use Editor C# and `PrefabUtility` to assemble/save prefabs; do not duplicate imported mesh data per SKU. Use external material remapping or explicitly create Unity materials rather than assuming Blender shader nodes export unchanged. File-driven authoring is allowed for visual assets only. The project doesn't need to include sorting, economy, ads, or input/gameplay logic.

## Export contract

- Apply desired transforms only on export copies. Keep `.blend` masters editable, `.fbx` sources consistent scale and world orientations.
- Materials split into body paint/decal, dark opaque windows, rubber tires, simple rims, cardboard and transparent blister; UV0 maps printed art and product fronts/backs.
- Each identity and physical custom build attaches to named transforms, not world-space guesses.
- `TR-xx-##-[S|C]-##` SKU is the prefab identifier, not geometry source ID.
- Use selected-product high-detail mesh + actual blister only where selected. Store shelf/card-only low-cost display proxies for mass display.
- Asset bundles/Addressables are optional future loading optimizations; don't claim they exist unless tested.

## What a real Unity verification must show

A static inspection scene with one car from every family, including Blue Racer #23, an oversize monster truck, a crane, and a creature; a package contact sheet with all families; card art and car in agreement; transparent shells; consistent transforms; predictable model/prefab filenames. Include screenshots of the *Unity Editor/Game View*, not Blender renders mislabeled as Unity.

## Startup dependency rules

If Unity is not installed or executable, generate import-ready geometry, material manifest and Unity Editor C# scripts, but log `UNITY_IMPORT_BLOCKED` and do not claim 500 imported prefabs. Use source-controlled generated art output so another execution can resume.
