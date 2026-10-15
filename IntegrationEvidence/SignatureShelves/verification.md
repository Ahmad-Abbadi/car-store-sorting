# Detailed signature shelves — verification

Completed locally in Blender 5.1 and Unity 6000.3.22f1, October 15–16, 2026.

## Delivered

- 40 editable Blender sources with packed textures and 40 UV-mapped FBX exports.
- 120 original 4096-pixel texture masters and 120 separate 2048-pixel runtime maps: base color, normal and metallic/smoothness.
- 40 signature materials, 40 themed prefabs and a registry covering all 80 shelves.
- 80 dimensional theme objects on 11.04 cm posts and 160 model-rendered header symbols.
- 40-theme Blender contact sheet, four detailed checkpoint close-ups, and Unity empty/filled front and aisle captures.
- Reproducible scripts and production instructions in `ArtSource/Shelves/SignatureShelves/README.md`.

The original cabinet textures and three-by-five compartment layout remain intact. Toppers use the existing motifs and car palettes. Both shelves in each theme share all four art meshes and materials. Wide objects retain their proportions within the 60 cm width and 35 cm depth limits. The maximum object height is approximately 40.8 cm.

## Validation results

All eight Unity tests passed:

| Suite | Passed | Coverage |
| --- | ---: | --- |
| ThemedShelfArtTests | 3 / 3 | All 40 designs, materials, maps, triangle/dimension limits, label fit, shared meshes, clearance, baseline scene identity |
| ExactShelfCapacityTests | 4 / 4 | 15 visible compartments per shelf, catalog eligibility/copy counts, per-theme mismatch despite matching total, old sixth-column save migration |
| CompactCollectionTests | 1 / 1 | Placement, occupied/full rejection, wrong placement without credit, takeback, pickup/drop, reload, all 1,200 physically sorted cars, completed reload, replay and main-menu entry |

The completed collection had 1,200 distinct instance IDs, every slot correctly occupied, every shelf full, and no remaining pile cars. Legacy partial and completed saves retained sorted, misplaced and held identities, discovery, coins and progress through a stable second reload. Tests used isolated QA save keys and restored upgrade preferences. The final Editor is out of Play mode with `Level` open, saved, and no QA flags active.

The baseline comparison confirms unchanged shelf IDs and poses, all 1,200 named placement markers, box colliders, catalog, two-copy configuration, seed, room references and pile setup. The scene migration does not place cars or issue rewards.

The independent Blender source audit passed all 40 sources: 15 slots, 12 internal dividers, two header plaques, one topper, three packed signature maps, and 4K master dimensions. All textured meshes have UVs; legacy solid-color tray/structural pieces do not need UV sampling.

The final scene audit found:

- 80 toppers and 80 posts; no added art colliders or realtime lights.
- Highest topper: 2.2463 m; minimum overhead-sign clearance: 0.2137 m; ceiling clearance: 0.9537 m.
- No overlapping category/directory boards and no overflowing sign text.
- Maximum decoration budget: 4,960 triangles, below the 8,000 limit.
- Exactly 30 slots per theme and 1,200 overall.

## Rendering comparison

Same room, enabled shelf renderers including theme-name text; text meshes were initialized for both triangle counts. Unique materials/textures are counted once across matching pairs.

| Metric | Previous decorated cabinets | Raised signature art |
| --- | ---: | ---: |
| Enabled shelf renderers | 320 | 400 |
| Art renderers per shelf, excluding text | 3 | 4 |
| Shelf triangles, including text | 1,388,844 | 1,561,412 |
| Unique materials | 43 | 83 |
| Unique textures | 81 | 201 |
| Editor texture memory, bytes | 172,564,928 | 1,295,526,477 |

Triangles increased by 172,568 (12.4%). The requested three 2K signature maps per theme add substantial texture residency: the Editor profiler reports about 1.21 GiB total versus 165 MiB previously. Signature maps import compressed (DXT1 base color, DXT5 normal and surface maps), with mipmaps, trilinear filtering, anisotropy 4 and Read/Write disabled. These are Editor asset-memory measurements, not standalone-player memory or an FPS benchmark; no performance improvement is claimed.

## Visual review

Reviewed all 40 Blender cabinet renders plus the four checkpoint close-ups. Reviewed Unity empty and fully occupied checkpoint shelves at player height and aisle angles. Header artwork remains distinct beside the names, dimensional silhouettes are recognizable, posts attach to the cabinet and object, and cars fit without clipping into the new art. A small saddle supports objects with a recessed central underside, such as the skull above its crossed bones.

The Unity batching update rebuilds native mesh buffers in place. This fixes stale old relief geometry remaining visible after FBX updates while preserving cached mesh GUIDs.

Evidence files: `Blender_all_40.jpg`, `Blender_checkpoint.jpg`, `Unity_empty_checkpoint.jpg`, `Unity_filled_checkpoint.jpg`, `blender_source_audit.json`, `scene_identity_audit.txt`, `final_scene_audit.json`, `rendering_before.json`, `rendering_after.json`, and `full_collection_test.json`. Gameplay traces are also in `IntegrationEvidence/compact_playmode_passed.txt` and `IntegrationEvidence/ExactShelfCapacity/save_migration_passed.txt`.

## Local environment maintenance

Unity paused behind a low-disk-space dialog during production. Removed only the regenerable project cache `Library/Bee/artifacts/WebGL` (about 1.5 GiB), then enabled NTFS compression for `Library/Artifacts` without changing its logical contents. This recovered enough room to complete import and testing; approximately 5.4 GiB remained afterward. No source artwork, saved game, or published build was removed.
