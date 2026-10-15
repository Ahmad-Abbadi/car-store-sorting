# Themed shelves — verification, 2026-10-15

Implemented in the current `Level` scene: 40 Blender designs across 80 shelves, with 15 compartments each. Every theme has 30 cars and exactly 30 slots; total capacity is 1,200.

- Blender source audit: 40 editable `.blend` files; each has 15 markers, 12 dividers and two packed 1024-pixel atlases. All 40 FBX exports and theme prefabs are present.
- Art tests: **2 passed**. Complete theme coverage, distinct prefabs, triangle budgets, opaque materials, atlas dimensions, unchanged marker positions/rotations, matching pairs sharing all three batched meshes, no added colliders/lights, and all 80 labels fitting their text rectangles.
- Capacity/save-migration tests: **4 passed**. Exact per-theme and total capacity; mismatches fail even with a matching total; session eligibility and copy counts; partial and completed old sixth-column saves preserve identities, sorted/misplaced/held state, discovery, coins, completed progress and stable second reload.
- Full gameplay test: **1 passed**. Pickup/drop, wrong placement, takeback, occupied/full shelf rejection, all 40 themes, all 1,200 cars physically placed into 1,200 correct occupied slots, completion, save/reload, replay and main-menu entry.
- Pre/post scene comparison: all 80 shelf IDs and transforms, all 1,200 named slot positions, and all box-collider dimensions/centers/trigger settings match the scene backup. Room and pile assets were not changed.
- Visual review: all 40 Blender hero renders; detailed Egypt, Pirate, Candy and Science Lab previews; empty and filled Unity views of those four themes at player height and from the aisle. Decorations do not obstruct car compartments. Existing room lighting is retained.

| Shelf rendering metric | Before | After |
| --- | ---: | ---: |
| Enabled mesh renderers, including labels | 720 | 320 |
| Submitted shelf triangles, including labels | 1,390,284 | 1,387,040 |
| Distinct visible materials | 16 | 43 |
| Maximum added decoration triangles per design | — | 1,344 |

Matching shelf pairs share assets. The 43 visible materials comprise 40 theme materials, two common cabinet materials and the label material. No new realtime lights, transparent surfaces or emission were added. These counts are geometry/rendering statistics, not an FPS claim.

During verification, the gameplay test needed to wait for the completion UI's initialized state before invoking Replay. A separate rerun encountered a Windows lock on the optional performance-report file; that report writer is now disabled only inside test isolation, since bulk automated placement is not a representative performance sample. The subsequent full gameplay run passed. Production save code, schema and keys remain unchanged.

Review files: `Blender_all_40.jpg`, `Blender_checkpoint.jpg`, `Unity_empty_checkpoint.jpg`, `Unity_filled_checkpoint.jpg`. Raw images and JSON audits are in this folder; reproducible scripts and authoring instructions are in `ArtSource/Shelves/ThemedShelves/README.md`.
