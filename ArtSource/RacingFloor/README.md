# Tiny Riders racing floor

The collector shop uses an illustrated toy-town playmat around a rounded teal racing track: sage parks, curved walking paths, tree and bench illustrations, paved promenades, parking bays, corner rooftops, a back courtyard and an entrance compass plaza. The track retains its central straight, ivory lane paint, coral corner accents and entrance checker. The satin palette is calibrated for the existing bright shop lighting.

## Rebuild

With `Level` open in Edit mode, choose **Tools > Tiny Riders > Collector Shop > Apply Racing Floor**, or call `RacingFloorBuilder.ApplyToCurrentRoom()` through Unity. This updates the existing graphics and native mesh assets in place and saves the scene. The full crowded-room builder also calls `RacingFloorBuilder.Apply` after creating its original floor. The design requires the existing 24 x 26 meter room.

The builder lives in `Assets/TinyRiders/Gameplay/Editor/RacingFloorBuilder.cs`. Runtime art is in `Assets/TinyRiders/Art/RacingFloor`: five shared meshes, six opaque URP Lit materials, a small periodic grain texture and the 2048px town texture. The town texture has mipmaps, trilinear filtering, anisotropy 8, clamped edges and no alpha. Palette, track dimensions and painted details are editable in the builder.

Run `python ArtSource/RacingFloor/Scripts/build_toy_town.py` (Python with Pillow) to regenerate `ToyTown/Toy Town Playmat.svg`, its 4096px source PNG and the runtime PNG. Both exports come from the same vector primitives, with named SVG groups and room-local coordinates. The editable SVG can also be revised in a vector editor and exported at 2048 x 2048 to the runtime PNG; that export supersedes the procedural source until the script is run again. The full-sheet UVs map the 24.6 x 26.6 meter floor exactly, with north at the top of the image. The town material applies a neutral tint to compensate for the bright room lighting.

The original floor keeps its transform, collider, collision layer and physics material. The background sits 1 mm above it; track graphics sit 3–6 mm above it. All graphics have no colliders, ignore raycasts, receive shadows and cast none. There are six floor renderers/material slots including the original base, with 1,418 decorative triangles. Additional depth/shadow passes depend on the render pipeline; this is not a measured GPU draw-call or FPS benchmark. No new runtime components, lights or save fields are added.

## Verification — 16 October 2026

- Unity 6000.3.22f1 compilation succeeded with no errors.
- Repeated application retained the same decoration root and shared mesh identities.
- Before/after structural checks verified the original floor collider/transform, shelf identities/transforms, session/pile layout and lights were unchanged.
- Reopening the saved scene retained the town texture, five graphic objects, and original collision properties. Repeat application preserved mesh identities.
- The town update passed texture-size, mipmap, anisotropy and UV-range checks. The original track's physics and sorting regression suites passed before this visual-only addition; those earlier results remain in `IntegrationEvidence/RacingFloor`.
- Empty and populated entrance, central, outer-aisle and overhead captures for the town revision are in `IntegrationEvidence/RacingFloor/ToyTown`. The populated review had all 1,200 loose cars, verified advancing Play-mode frames, and used isolated QA save keys. The overhead view temporarily hides the ceiling and ceiling diffusers; normal room lighting is retained.

`Scripts/RacingFloorReview.cs` can be run through Unity's `run_script` command for review captures. Its `Baseline` and `Audit` pair compares transient Editor object references, so run them within the same Editor scene lifetime before a reload. Persistent reopen verification is recorded separately in `reopen_audit.txt`. Original scene and collider snapshots are retained alongside the screenshots for recovery and inspection.

Use the targeted floor menu for future art adjustments. The full room builder also regenerates the wider room and car pile and is unnecessary for a floor-only update.
