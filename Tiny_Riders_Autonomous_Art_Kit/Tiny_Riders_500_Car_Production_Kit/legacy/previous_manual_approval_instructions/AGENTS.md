# Mandatory Codex Production Rules — TINY RIDERS

## Order of authority
1. Explicit user approvals and requirements.
2. These project rules, including phase gates and packaging locks.
3. `docs/` specifications and `catalog/` records.
4. The ten original concept reference boards. They are visual inspiration and may contradict each other. When they conflict with locked specs, follow the locked spec and record the deviation.

## Nonnegotiable rules
1. **One phase at a time.** Don't silently continue through phases. After each phase create an evidence report and STOP. A phase is approved ONLY if the user changes its `progress/phase_status.json` state to `APPROVED` or directly gives explicit approval in the active chat.
2. **Reference-first execution.** Load and inspect the exact source board named by the family spec before making or changing a body. Save a comparison render/contact sheet. Never replace family references with unrelated images.
3. **Do not build 500 individually prompted unrelated meshes.** 50 distinct high-quality base silhouettes; two deliberate physical builds; five 2D livery editions per build; all driven by stable data IDs.
4. **Model shapes, texture print, light with materials.** Disallowed on models: Blender text objects, generated letter geometry, extruded logos, mesh decals/striped overlays, modeled paint highlights, physical fake sparkles. Allowed: actual fenders, tires, wings, beacons, fins, crane booms, roof loads, package card, die-cut, and blister.
5. **Clear packaging lock.** Build ONE master 2D art layout and ONE reusable 3D package assembly. Do not reimagine logo, die-cut, location of product name, blister, numbering, typography or overall card dimensions per model. Family skin uses only controlled palette/background/category label variation.
6. **Match physical car exactly.** Card hero illustration must be derived from the finalized 3D car with identical wheels, spoilers, decals and color. No independent generative car art on the card.
7. **Original intellectual property only.** No real automaker marks, copied Hot Wheels marks/packaging, familiar licensed characters, or third-party product logos. Reference boards are original conceptual direction; retain only original family/product names.
8. **A beauty render is not a production check.** Provide gray clay render, wireframe, UV artwork preview, Blender close-up, side view, comparison view, and genuine Unity capture where possible.
9. **Do not fabricate success.** Report actual generated assets, reproducible scripts, validation evidence, geometry counts, textures, observed browser numbers and explicit blocked work.
10. **Deterministic regeneration.** Fixed IDs and seeds; never randomly regenerate an approved shape/livery; no overwrite of approved masters without specific consent.
11. **No massive render queues without gating.** Start with one `TC-01` car, then a limited family sample, then groups of 10, until all 50 body bases are approved.
12. **Keep polygon complexity appropriate.** Chunky toy style; effective silhouette, simple sturdy accessories, flat faceted windows; avoid trying to simulate detailed realistic sports cars.
13. **Fonts only in 2D artwork.** Blender text curves or mesh fonts are forbidden for visible product print, but traditional SVG/PNG typography is required and correct.
14. **No invisible dependencies.** Always save source files, textures, catalog IDs, exports, QA sheets and build logs in documented paths.

## Must halt when
- Previous phase not approved.
- Reference missing or could not be inspected.
- Current asset cannot pass silhouette QA (no packaging can hide it).
- Any printable lettering is modeled as geometry.
- The actual car differs from the package illustration.
- Package template location deviates beyond specified tolerances.
- Real tool access prevents verifying a claimed result.

## Tool strategy
Blender MCP = visual iteration, in-scene corrective work, screenshots. Blender batch Python = repeated deterministic geometry, UV setup, baking, export and QA. Codex = orchestrates all scripts, makes stable JSON/CSV, Unity Editor automation and validation. Unity = verified implementation only, not a substitute for Blender proof renders.

## Completion requirement for every task
Return an evidence list, artifact path list, pass/fail QA list, and a stop-for-approval status. Never mark `APPROVED` for the owner.
