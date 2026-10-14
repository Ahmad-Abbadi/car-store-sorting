# Initial-release source and compatibility audit

- **Project:** Unity 6000.3.22f1, Universal Render Pipeline 17.3.0.
- **Purchased package:** `Assets/ithappy/Modular_Racing_Cars`, retained at its original paths and GUIDs. The saved SHA-256 snapshot covers 1,292 supplier files, including `.meta` files; the final release check matches all 1,292.
- **Complete cars:** `Car_01` through `Car_15`, bound permanently to `C001` through `C015` in `registry_history.json`. All 15 are used.
- **Evidence:** `source_audit_raw.json` records Unity renderer, mesh, material, and bounds data. `source_catalog.json` records source prefab GUIDs and per-car recipes. `Reports/wheel_fit_audit.json` records mounted and candidate wheel bounds, positions, and rotations. `compatibility.csv` lists the actual selections and rejections.

## Physical builds

`STD` keeps the supplied active parts. `CUS` switches the verified same-car front bumper, pipe, and rear bumper on every car, plus headlights on 14 cars. `Car_02` retains its supplied headlight arrangement because both alternatives coexist in its default prefab. `CUS` disables the supplied fixed spoiler on every car. These choices are saved in the build wrappers and validated on final variant prefabs.

The supplied `Wheel_16` is used at the four original wheel mounts on 14 custom builds. Its 0.95 diameter and approximately 0.5 width match those cars' installed wheels. The custom `Car_06` keeps its original wheels because its mounted wheel is wider (approximately 0.571). Front, side, and rear renders of all 15 custom builds were reviewed for placement, scale, clearance, and visible part differences. Cross-car body/part swaps remain unsupported; no geometry was invented.

## Paint and graphics

Each car has ten finished paints. `STD` uses Scarlet, Tangerine, Amber, Lime, and Emerald for `L01`–`L05`; `CUS` uses Teal, Cyan, Cobalt, Violet, and Fuchsia for the same livery codes. `L01` is recolored like every other slot. Colors are authored by build and code, and material/texture copies for custom builds have distinct paths from standard builds. Original supplier color, glass, tire, light, and decal assets remain untouched.

Most paint is driven by a copied version of the supplier `Textures1.png` palette assigned only to the source `Color` material slots outside wheel and racer branches. On `Car_04`, `Car_05`, and `Car_09`, a large neutral body region also uses the supplier `Metal` atlas. Those cars get a separate owned body-metal material and palette copy on the top-level body renderer so all ten paints remain visible. Source UVs, meshes, and graphics are unchanged. The resulting per-car contact sheets show ten different looks at thumbnail size.

Unity renders 128×128 transparent icons through an isolated preview camera. `ContactSheets` contains one sheet per car and an overview. `Reports/QA` contains front, side, and rear custom-build review renders and montages. The static lineup scene contains 150 final prefab instances grouped by car and build.
