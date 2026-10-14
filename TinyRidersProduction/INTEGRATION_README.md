# Tiny Riders initial car collection

The **single editable content source** is `TinyRidersProduction/source_catalog.json`. It defines the 15 source cars, 30 build recipes, build-specific liveries, and 150 permanent product IDs. `registry_history.json` locks the original source and product bindings and reserves retired IDs. Edit authored intent there, increment a changed product's `contentRevision`, then regenerate. `production_state.json`, `variant_catalog.csv`, and Unity assets are generated results. Direct edits to generated prefabs, materials, or textures may be replaced by regeneration; a changed owned file is detected and reported as a conflict instead of being silently overwritten.

## Saved outputs

| Asset | Location |
| --- | --- |
| Release manifest (`CarCatalog`) | `Assets/TinyRiders/Data/Releases/initial_v1.asset` |
| One `CarVariantDefinition` per product | `Assets/TinyRiders/Data/Variants/` |
| Drag-ready prefab with `CarVariantIdentity` | `Assets/TinyRiders/Prefabs/Variants/Cxxx/STD/` or `CUS/` |
| Transparent Sprite icon | `Assets/TinyRiders/Previews/Icons/Cxxx/` |
| Shared base and physical build wrappers | `Assets/TinyRiders/Prefabs/BaseCars/` and `Prefabs/Builds/` |
| Static art review scene | `Assets/TinyRiders/Scenes/ArtPreview/initial_v1_lineup.unity` |
| Per-car and overview contact sheets | `TinyRidersProduction/ContactSheets/` |

All 150 release entries have a direct prefab and Sprite reference. The catalog lookup uses the serialized definition assets and rebuilds its in-memory dictionary after deserialization. Metadata fields supply base-car, build, livery, release, display order, and color tags. Filter those fields; do not parse filenames or ID text for gameplay grouping. The edition label is authored in the editor catalog as descriptive art metadata.

Each base car has ten finished colors: `STD` L01–L05 are Scarlet, Tangerine, Amber, Lime, and Emerald; `CUS` L01–L05 are Teal, Cyan, Cobalt, Violet, and Fuchsia. Standard and custom paints have separate derived material and texture paths. `CUS` also uses the verified same-car part swaps and removes the fixed spoiler; 14 custom builds use the supplied `Wheel_16` at the original mounts. `C006` keeps its wider original wheels. See `compatibility.csv` and `Reports/QA` for the part choices and visual inspection.

For example, a caller with an assigned `CarCatalog` reference can use:

```csharp
using TinyRiders;
using UnityEngine;

public static class CarCatalogExample
{
    public static bool TryResolve(CarCatalog catalog, string permanentId,
                                  out GameObject prefab, out Sprite thumbnail)
    {
        prefab = null;
        thumbnail = null;
        if (catalog == null || !catalog.TryGetById(permanentId, out var definition))
            return false;
        prefab = definition.prefab;
        thumbnail = definition.thumbnail;
        return prefab != null && thumbnail != null;
    }
}
```

`variantId` identifies a collectible design. Multiple future scene/gameplay instances may share it. A display-name change leaves `variantId` unchanged. A corrected version of the same design keeps its ID and increases `contentRevision`; a new design gets a new ID. Append later car IDs and livery/build codes, and add explicit later-release manifests rather than renumbering initial products. Run `seal_registry_history.py` after adding IDs; it refuses to change an existing binding or reuse a retired ID. A later manifest may reference an existing definition without duplicating the product.

## Editor commands

In Unity, use the **Tiny Riders** menu:

1. **Audit Source Package** — refresh `source_audit_raw.json` from the installed package.
2. **Validate Catalog and Compatibility** — check IDs, recipes, source GUIDs, and referenced parts.
3. **Generate Missing or Changed Variants** — build affected variants in place, preserve GUIDs, and report conflicts.
4. **Regenerate Selected Variant IDs** — select variant assets whose names are IDs, then use the menu. For explicit IDs from an editor eval, call `TinyRiders.Editor.TinyRidersProductionTool.RegenerateIds("TR-C001-STD-L01,TR-C001-CUS-L01")`.
5. **Generate Previews** — refresh Unity PNG icons with the isolated preview renderer.
6. **Validate Initial Release** — live validation and publish only technically validated, visually reviewed definitions.
7. **Generate Static Lineup Scene** — rebuild the art review scene from those published products. If the existing lineup is open, use `TinyRiders.Editor.TinyRidersProductionTool.RebuildOpenLineupScene()` from editor eval; it preserves unsaved lineup edits in `lineup_before_variant_refresh.unity` and reopens the updated scene.

From the project root, run `py -3.14 TinyRidersProduction/generate_contact_sheets.py` after previews to rebuild the per-car and overview sheets. Pillow is required for that report script. Run `py -3.14 TinyRidersProduction/generate_compatibility.py` for the compatibility CSV and `py -3.14 TinyRidersProduction/verify_release.py` for saved-file integrity. To check an unchanged regeneration pass, run `check_idempotency.py before`, generate in Unity, then run `check_idempotency.py after`. The Python launcher/version may be replaced with another Python 3 interpreter with Pillow installed. The execution log and per-ID errors/next actions are in `TinyRidersProduction/execution.log` and `production_state.json`.

Keep `Assets/ithappy/Modular_Racing_Cars` installed at its current GUID-bearing location. Generated wrappers still reference its prefabs, FBX meshes, materials, decals, and textures. The collection adds no sorting logic, inventory/save state, stats, prices, drop rates, controllers, runtime paint customization, packaging, or shop environment.
