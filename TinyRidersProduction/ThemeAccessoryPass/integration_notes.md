# Integrating R03 accessories

Use the existing Assets/TinyRiders/ThemedCollection/Prefabs/Themed prefabs and ThemedCarCatalog.asset. Existing product IDs, prefab references, body materials and textures are retained; contentRevision is 3. No runtime attachment script is needed. Each prefab contains ThemeAccessories/KIT_TH_* with a nested shared kit. The neutral kit root preserves Blender's FBX import transform on AccessoryGeometry; do not overwrite that child transform.

Reusable kits, meshes, 1024 atlases and URP materials are under Assets/TinyRiders/Art/ThemeAccessories. Editable scene-based Blender files with packed textures are under ArtSource/ThemeAccessories/Blender. ThemeAccessories_Studio.blend presents the library in a grid; exported meshes and individual source files use a local bottom-center origin.

The authoritative fit records are fitting_profiles.json and variant_assignments.json. They record measured surface, local position/rotation/uniform scale and expanded bounds. The existing shelf system uses a separate smaller collection: do not substitute these raw full-size prefabs without a later explicit integration/clearance pass. No physics or storage rules were changed here.

## Regeneration

Use Unity CLI with the connected project open. Every `unity command` invocation needs `--caller plugin --skill unity-cli`.

1. To reapply existing kits: `unity command run_script --caller plugin --skill unity-cli --file ArtSource/ThemeAccessories/Scripts/RunAccessory.cs --entry RunAccessory.Run --args '["all"]' --timeout_ms 600000 --detach --format json`. Poll the returned job ID to terminal success.
2. For one kit/base: use args `["apply","TH_SPACE","C001,C007,C010"]`. This changes the existing prefab assets in place.
3. Validate with ValidateAccessories.cs / ValidateAccessories.Run and a distinct pass name. Rebuild sheets using `python ArtSource/ThemeAccessories/Scripts/review_sheets.py`. Publish previews with PublishAccessoryReview.cs / PublishAccessoryReview.Run.
4. Run check_protection.py after Unity saves. If Unity's material validator only reintroduces the documented emission/lightmap flags, RestoreFoundationFlags.cs restores them from the baseline through the Material API; inspect any other diff before changing it. Never overwrite manual edits blindly.
5. For geometry edits, use live Blender MCP and model_accessories.py (execute its definitions then `build("TH_SPACE")`, for example), or edit the corresponding source .blend. Export only that accessory with -Z forward/Y up. Do not export the studio's presentation offsets. Existing .meta files must stay in place. Regeneration scripts are deterministic art recipes, not permission to overwrite future manual revisions.

The old R02 ThemedCarBuilder now refuses to recreate a collection containing R03 entries, because doing so would discard fitted overrides. R01/R02 artwork scripts and historical comparisons remain archived.

## Recovery

baseline/Collection is the complete R02 collection snapshot, including original .meta files; baseline/assignments.json and baseline/source_hashes.json record original references and hashes. For an explicit rollback, restore only the affected project-owned files from that snapshot while preserving GUIDs; use Unity asset import/save operations, then validate. Do not restore supplier files or unrelated workspace changes. No automatic rollback was performed.
