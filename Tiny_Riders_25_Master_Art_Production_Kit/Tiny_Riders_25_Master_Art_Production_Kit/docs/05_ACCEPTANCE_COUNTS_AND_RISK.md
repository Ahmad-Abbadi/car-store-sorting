# Deliverable acceptance, batch estimates and main risks

## Exact count semantics

- **25 approved masters:** distinct high-quality source body/chassis models with actual geometry, license evidence, inspected renders.
- **50 body identities:** 25 masters × 2 named variants; each identity differs by meaningful visible body/roof/nose/rear modules.
- **100 physical builds:** each named identity has a standard/custom version with visibly different geometry.
- **500 collectibles:** five curated painted/printed looks of each of 100 physical builds.
- **500 package products:** one universal packaging geometry; 500 front/back artwork pairs and 500 correctly mapped car/packaging definitions.
- **Unity import:** prefabs may all point to 25 shared master meshes; the prefab file count is not mesh count.

## Risk table

| Risk | Mitigation |
|---|---|
| Premium source meshes absent | Source/audit/commission first; do not hallucinate quality; block affected master IDs only |
| Two identities look identical | Swap major cabin/rear/front body panels; enforce side-silhouette contact sheet |
| Four builds per master are only paint variants | Require physical accessory and roof/hull differences in each standard/custom pair |
| AI lettering becomes geometry | Flat 2D SVG/PNG artwork in UV basecolor, clay render test |
| Packages drift across families | One shared Track Club geometry/layout; 1200×1800 source; fixed pixel regions |
| Package car doesn't match contents | Generate printed car render directly from final SKU 3D assembly |
| Runtime WebGL download/memory too high | Ship low-res atlased shelf cards; high quality asset only when inspecting; share materials and meshes |
| 500 placeholders incorrectly reported complete | Source-file and rendered evidence checks; unbuilt statuses explicit |
| Long run stops midway | Per-master batch ledger, hashes, idempotent regenerate/resume |
| Unlicensed toy brand/model | Original fictional designs, commercial usage rights, no unauthorized extraction |

## Suggested chronological delivery

1. Inspect source models and quality-license audit of 25 masters.
2. Complete one master with 2 identities, 4 physical builds and 20 prints/packages; inspect Unity before scaling.
3. Complete 5 masters / 100 products; inspect mixed-family art style.
4. Complete all 25 masters / 50 identities / 100 builds.
5. Produce 500 livery assets and exact printed package artworks.
6. Unity art import, static gallery scenes, full file QA.

This job can require extended interactive art production even if repetitive assembly is automated. Do not promise a fixed number of unattended hours or success without adequate base models.
