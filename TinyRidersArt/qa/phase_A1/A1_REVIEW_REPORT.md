# Tiny Riders A1 — Blue Racer #23 pilot review

**Status: NEEDS REVIEW.** This is the A1 master car style test. Work stops here before the package phase.

## Visual review

- Inspected the front three-quarter, both sides, rear three-quarter, top, clay, silhouette, and mesh-edge views. The short body, large wheels, opaque windows, and wing read as one chunky toy car. The side `23` is legible on both sides.
- Compared the pilot directly with the Track Club Blue Racer #23 concept crop in `TC-01_reference_comparison.png`. The cobalt body, white stripe and numbers, dark windows, cream lamp graphics, wheel scale, and rear wing follow the reference. The pilot has a squarer shell and a taller, simpler wing than the painted concept; this is the main art-direction choice for owner review.
- Clay shows the physical body, wheel, fender, bumper, and wing shapes after removing print. The wireframe view uses projected evaluated mesh edges over the clay view. The exact UV artwork overlay is saved alongside the atlas.

## Deliverables

| Asset | Path |
|---|---|
| Editable master | `../../blender/TC-01_BlueRacer_A1.blend` |
| Export-ready FBX | `../../exports/cars/TC-01_BlueRacer_A1.fbx` |
| 2048 px albedo atlas | `../../textures/cars/TC-01/TC-01_BlueRacer_23_Albedo.png` |
| UV artwork guide | `../../textures/cars/TC-01/TC-01_UV_Artwork_Guide.png` |
| Actual UV overlay | `../../textures/cars/TC-01/TC-01_UV_Actual_Overlay.png` |
| Six-view review sheet | `../../renders/phase_A1/TC-01_review_sheet.png` |
| Reference comparison | `../../renders/phase_A1/TC-01_reference_comparison.png` |
| Individual views | `../../renders/phase_A1/TC-01_front_three_quarter.png`, `TC-01_side.png`, `TC-01_rear_three_quarter.png`, `TC-01_top.png`, `TC-01_clay.png`, `TC-01_wireframe.png` |
| Additional views | `../../renders/phase_A1/TC-01_right_side.png`, `TC-01_front.png`, `TC-01_silhouette.png` |
| Machine validation | `TC-01_asset_validation.json` |

The source and export contain **29 car mesh objects**. The source has 378 base polygons / 884 triangles before modifiers; the FBX round trip has 887 polygons / 1,768 triangles with modifiers applied. Source and FBX share the same approximately **3.20 × 2.13 × 1.64 Blender-unit** bounding box. The Blender master also contains a separate review studio collection, which is excluded from the FBX export.

## QA checklist

| Check | Result | Evidence |
|---|---|---|
| Blender MCP connection and model build | PASS | Model built and saved in live Blender 5.1 through MCP. |
| Printed `23`, stripe, windows, and lamp designs are image artwork | PASS | Atlas and UV overlay; no `FONT` objects or mesh letters in the Blender source. |
| Printed meshes have UV maps and atlas coordinates stay in range | PASS | Two printed meshes; UV range 0.0–1.0; `uv_issues` empty in machine validation. |
| FBX reimports as car geometry | PASS | Blender imported the exported FBX with all 29 meshes; bounds match the master. |
| FBX carries the printed texture | PASS | Reimport found a packed 2048 × 2048 albedo image. |
| Required A1 image evidence exists and is readable | PASS | Six-view sheet plus both side views, silhouette, and reference comparison, visually inspected. |
| Exact concept silhouette match | REVIEW | Pilot has a squarer body and taller wing; owner should decide whether to keep this stylization. |

The asset has **not** been imported into Unity; that verification belongs to phase A7. No A2 package has been created.

## Reproduction

- Texture: `../../scripts/TC01_make_art.py`
- Blender model and FBX: `../../scripts/TC01_build_blender.py`
- Renders and evidence: `../../scripts/TC01_render_review.py`, `../../scripts/TC01_export_wire_data.py`, `../../scripts/TC01_export_uv_data.py`, `../../scripts/TC01_make_review_sheets.py`
- Blender/FBX validation: `../../scripts/TC01_validate_asset.py`

**Decision requested:** Approve the A1 shape and livery as the Track Club style master, or request specific changes to its shell, wheel proportions, or wing before A2.
