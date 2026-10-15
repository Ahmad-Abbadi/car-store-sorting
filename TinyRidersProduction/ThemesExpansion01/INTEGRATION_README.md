# Themes Expansion 01

This release adds twenty themes to the existing Tiny Riders themed collection. It preserves the original twenty themes and their 150 products. The expansion plan contains one V01 for each new theme on each of the thirteen currently enabled themed base cars: 260 products. C006 and C013 remain reserved and excluded, as documented in the prior R02 validation. The separate legacy collection's fifteen cars do not override that enabled themed selection.

## Assets and data

- Individual products: `Assets/TinyRiders/ThemedCollection/Prefabs/Themed/TH_*/TR-Cxxx-TH_*-V01.prefab`.
- Car textures and materials: new theme folders in the existing `ThemedCollection/Textures/Themes` and `Materials/Themes` trees.
- Shared accessory assets: new theme folders in `Assets/TinyRiders/Art/ThemeAccessories/{Meshes,Textures,Materials,Prefabs}`.
- Editable sources: `ArtSource/ThemesExpansion01/Blender/<theme>` and `Artwork/<theme>`.
- Production plan and fit records: `variant_plan.json`, `products.json`, `fitting_profiles.json`, `accessory_library.json` in this directory.
- Runtime catalog: existing `ThemedCarCatalog.asset` receives appended entries. Portable JSON/CSV retain existing rows. Existing recipe records are retained, then new recipes are appended.
- Original `id_registry.json` release and immutable IDs retain their membership. New IDs are under `additionalReleases.themes_expansion_01`; consumers seeking all IDs should include both sections.

Every product has saved materials and a single owned `ThemeAccessories` root containing one shared kit. No runtime attachment script is needed. Supplier meshes, source UVs, physical configuration remains unchanged except for 38 explicitly logged new-prefab overrides that disable a separately removable roof fog-light module when its lens intersects the accessory. Supplier parts and previous products remain intact. The car geometry remains present once. The expansion uses uniform accessory scale and measured per-base roof/deck mounting profiles. C010 uses the solid rear deck behind its open cockpit.

## Regeneration

Run local Python `ArtSource/ThemesExpansion01/Scripts/artwork.py` with optional theme IDs. It writes only expansion artwork and derived texture folders. Execute `models.py` in Blender MCP, then call `build('TH_THEME_ID')`; each build saves a source scene, shared FBX and color/clay/wireframe views. Each source blend contains the real mesh, packed texture, camera and lights.

In the connected Unity Editor, invoke `TinyRiders.Editor.ThemesExpansionBuilder.Build(theme, bases, render)`. `bases` is `ALL` or a comma-separated base list; `render` defaults to true. The builder explicitly scopes work to this release, reuses existing assets and GUIDs, and preserves the old physical configuration except for logged removable-module overrides. Shared material templates are selected deterministically from the preserved baseline by role, with explicit new finish values, so targeted subsets and full builds produce the same materials. Keep the baseline folder for regeneration. `Validate(suffix)` checks saved products. `Publish()` appends catalog entries without replacing old entries.

Use Unity CLI `run_script --caller plugin --skill unity-cli` with the saved runner files. Do not invoke the older global themed-car builder to regenerate this release. The expansion artwork and model scripts are local original production sources; no external generation or asset downloads are involved.

## Review evidence and limitations

`ContactSheets` contains family views, hidden-name mixes, actual 64/128-pixel car-width thumbnails, layer comparisons and accessory proof. Individual side, elevated, texture-only, clay and perspective renders are in `Views`. Project-camera tests copy the established Level camera pose/projection into an isolated preview with the existing observed display placement. Cars retain their source scale. Static piles are review arrangements only; no gameplay scene is changed.

Supplier decal islands constrain print coverage. Hood artwork is split across the available original islands on several bodies; door and rear panels carry larger motifs. No source UV, decal geometry or body geometry is changed to bypass those constraints. Neutral preview lighting matches the previous verified collection. Clay replaces materials, so printed detail, normal and bump contributions disappear. Source wireframe renders show physical geometry separately.

The existing small shelf envelope is 0.464 wide × 0.54 high × 0.648 deep. These unscaled purchased cars already exceed it before accessories. This release does not resize cars or change shelves; shelf integration remains unsupported for the raw themed library. This is distinct from an accessory mounting failure.

Small concealed mounting embed is allowed. The fit check samples vertices, edge midpoints and triangle centers against actual painted roof/deck triangles; it also checks accessory-edge intersections against glass triangles. The initial 38 module conflicts and the corrected results are retained in Reports and fit_overrides.json. Geometric contact checks and visual multi-angle inspection supplement each other; neither constitutes a target-device performance or player-recognition study. One-second recognition remains an untested design goal.

Completion and exact validation results are recorded in `production_state.json`, `Reports/validation_report.md`, `source_protection.json` and the Unity validation files. Do not infer readiness from file existence alone.
