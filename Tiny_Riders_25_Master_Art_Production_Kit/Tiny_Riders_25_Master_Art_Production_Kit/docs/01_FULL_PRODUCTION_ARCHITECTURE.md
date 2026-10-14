# Tiny Riders v2 — 25 high-quality master meshes to 500 collectibles

This document defines **art production and Unity ART import only**. It does not implement sorting gameplay, currencies, controls, progression, SDKs or monetization. Its contents are an executable specification for Codex, not pre-generated geometry.

## 1. Production unit of work

Keep the **existing 500 SKUs, ten families, five original named models per family, original 500 card numbers and family reference boards**. Reuse *25* approved high-quality physical master car/chassis models. Each master has **two identity variants** from the existing named catalog, each identity has a standard and custom configuration, and each physical configuration receives five independently designed liveries.

`25 approved master geometries × 2 named identities × 2 physical builds × 5 printed liveries = 500 collectibles.`

**Do not describe this as 500 unique body meshes.** It is 500 distinct product configurations with 25 foundational body meshes, modular extensions, and print artwork. Some pairs span families; that is intentional and preserves 50 SKUs per family. See `catalog/masters_25.csv`, `body_to_master_50.json`, `builds_100.json` and `collectibles_500_v2.json`.

## 2. Essential dependencies and acquisition gate

**The earlier kit contained NO finished 3D car meshes.** These must be supplied, commissioned, purchased under commercial license, or sculpted/produced by a qualified artist. Codex can assist strongly with texturing, rigging modular mounts, conversion, and automation, but asking it to invent 25 premium vehicle shells entirely from primitives risks the same poor output as the rejected gray sports-car test.

Codex must first audit local geometry. For every master, it must record one of: `APPROVED_SOURCE`, `NEEDS_REWORK`, or `MISSING_SOURCE`, license provenance, source filepath, and target identity pair. Source meshes must be within project-use rights (including modifying, displaying inside game, exporting Unity assets). Never assume an online screenshot, AI-generated concept image or asset preview grants mesh rights. Do not buy assets or violate paid download/access controls without user authorization.

When meshes are missing, generate reasonable *candidates* only if a supported source method exists and actual renders prove fidelity. Candidates are NOT approved masters and cannot be counted as final product art. Log unresolved missing masters; continue independent work without pretending 500 premium cars are complete.

## 3. Geometry architecture

```
Master25 (approved source model, shared broad shell/vehicle chassis)
 ├── BodyIdentityA (one named vehicle; own roof/nose/rear body modules)
 │    ├── Standard physical build
 │    └── Custom physical build (visible geometry: spoiler, rack, alternate rear module etc.)
 └── BodyIdentityB (second named vehicle; intentionally distinct structural configuration)
      ├── Standard physical build
      └── Custom physical build

Each physical build -> 5 livery SKUs (paint, decals, family markings)
```

Example `M03`: one approved chunky race coupe master supports **Blue Racer** and **Apex**. Their fronts, roof/rear deck and race-wing configuration differ by real swappable geometry; each then has standard/custom build hardware and 5 liveries. Never treat a decal-only change as a distinct *physical build* or *identity*.

Preserve child roots and pivots across models: `CAR_ROOT`, `BODY`, `CABIN`, `FRONT_MODULE`, `REAR_MODULE`, `CHASSIS`, `WHEEL_FL/FR/RL/RR`, `ATTACH_ROOF`, `ATTACH_REAR`, `DECOR_MESH` (physical accessories only). Six-wheel Rover can add `WHEEL_ML/MR` attach points. Wheels' positions and ground contact must be validated physically. Use Blender scene units as meters, +Z up, choose a **fixed consistent Unity-forward axis**, and include export-axis mapping in the manifest.

Shared component library: 10–16 wheel/hub designs, 4 tire proportions, 8 simple spoiler types, roof boxes, lights bars, off-road rack, fire ladder, crane, mixer, dump bed, ice-cream toppers, creature ears/fins, etc. These are separate legitimate physical meshes. All printing belongs in UV-mapped art.

## 4. Art-quality bar

Style: intentional chunky collectible/toy cars from the supplied Blue Racer #23 reference; coherent 10-family palettes, readable faces/lights, simplified but polished parts, stout wings, oversized wheels, broad readable surfaces. Do not convert highly photorealistic real-brand meshes into untouched game assets. If licensed meshes differ in style, adjust proportions/materials and compare the 25 together in identical lighting and scale.

A master qualifies only after **five real images** are generated and inspected: front three-quarter, side, top, clay, wireframe. Also require painted/UV previews and one Blender/Unity material parity screenshot. Record screenshots as evidence. Do not fabricate ratings. Use high-quality visual inspection for subjective attributes and scripts for measurable technical criteria (polycount, export, UVs, missing resources). A failed source is repaired or replaced, not mass-produced with color changes.

Starting art targets: car 1.5–6k triangles depending on complexity, with clearly distinguishable silhouette at ~120-pixel width. This is a *guideline*, not reason to wreck a beautiful mesh. Use texture atlases, shared PBR materials, and correctly UV-mapped names/numbers. Model genuine body changes; paint/livery in texture.

## 5. Deterministic SKU assembly

The only source of truth is `catalog/collectibles_500_v2.json`. Each SKU references `masterId`, `identityVariant`, `physicalBuildId`, `liveryIndex`, and original stable fields `sku`, `collectionNumber`, `displayNumber`, `familyCode`, `modelName`, and `rarity`.

A single importer/generator script should reconstruct any SKU using `assemble_sku(sku)`. Use fixed seeds per SKU for any optional random decor. An assembly/export manifest records source mesh hashes, mesh/material references and texture filenames. No invented replacement IDs. Regenerate only changed dependent outputs.

Texture stages:
- **Master** source textures/clean surfaces shared where possible.
- **Identity** surface artwork (body shape modifications and family motifs).
- **Build** actual physical attachments and distinguishing equipment.
- **Livery** curated, separate 2D printed art, e.g. stripes, taxi symbols, race numbers and character faces.
- **Final** rendered SKU thumbnail from the assembled 3D product, then composited into packaging flat art.

Never make Blender text objects, extruded logos, decals as floating physical mesh planes, or 3D model effects to replace graphics. Use 2D SVG/PNG artwork + actual UVs; logo/text and numbers remain readable on both sides.

## 6. One truly locked packaging system

`docs/02_PACKAGING_MASTER_LOCK_V2.md` and `design/packaging/MASTER_FRONT_LAYOUT_GUIDE.png` define packaging. The 10 family boards contain useful palette/art directions but **not 10 different physical package designs**. ONE card geometry (120×180 mm, ~0.7 mm thick), same cutout, brand position, fixed family banner, printed hero region, lower blister region and footer. Family accents vary only within pre-existing designated zones.

Generate front/back flattened artwork at source size 1200×1800. Make a printed car hero by rendering the EXACT assembled car SKU with same wheels and livery. Do NOT paste a package photograph or render a different vehicle onto the card. Physical car inside a clear lightweight blister, plus a cheap shelf representation when many products are visible. Do not embed a second rendered toy in the printed blister region.

Keep source SVG/template and output PNG. For production runtime, create smaller texture atlases/preview sheets and defer full selected-item images. Hundreds of full-resolution card textures loaded together will waste browser memory.

## 7. Unity import without duplicating geometry

ART-only Unity project, ideally using existing Unity 6 URP installation/project if available; do not assert imports without running the Editor.

Hierarchy:
- 25 imported master/source meshes (FBX)
- 50 name/identity build recipes (plus optional small geometric modules)
- 100 standard/custom physical builds or nested prefabs
- 500 SKU definitions/material bindings/preview assets
- 500 car **prefab variants** or equivalent composed assets referring to shared models
- 500 packaged **prefab variants** referring to universal packaging mesh, SKU artwork, and selected car

Use Editor C# to build/import prefabs and configure materials/texture settings. Unity supports nested prefabs/prefab variants; do not bake 500 redundant mesh copies into independent source FBXs. Avoid loading 500 complete blister cars into scene at once. The car *selection view* has 3D car + shell, while full shelves use low-cost proxy/cards.

Unity art preview scenes: `Gallery_Masters25`, `Gallery_Identities50`, `Gallery_Builds100`, `Gallery_SKUs500_Thumbnails`, `Gallery_Packages_Families`, `SortingRoom_ART_PREVIEW`. These are **static inspection scenes**, not playable sorting games.

## 7A. Environment art only

After priority cars and packages, assemble a reusable sorting-room art kit: room shell, toy-shop shelving, rare display case, open storage racks, packaging boxes, bins, pile tray, sorting table, shelf labels for ten families, simple lights and decorative posters. All printed text is texture artwork. Create a static Unity inspection scene, not functional sorting gameplay.

## 8. Automated processing & failure recovery

Batch states: `MISSING_SOURCE` → `SOURCE_AUDITED` → `MASTER_APPROVED` → `IDENTITIES_APPROVED` → `BUILDS_APPROVED` → `LIVERIES_RENDERED` → `PACKAGES_RENDERED` → `UNITY_IMPORTED` → `VERIFIED`. Each may fail with a logged reason. Store `progress/PRODUCTION_STATE.json` + per-SKU status file and hashes. Retry failing steps up to 2–3 times, then quarantine that item and process independent jobs.

Always continue automatically unless real access, licensing, or quality-source blockers prevent work. No routine pause-for-approval; however, **never auto-approve a visibly defective master and multiply it 20 times**. If a licensed/master source cannot be secured, report precisely what's missing, proceed with unaffected SKUs, and end with honest counts.

## 9. Real completion criteria

25 master sources approved with provenance, 50 named identity configurations visually distinguishable, 100 legitimate standard/custom builds, 500 real textured SKU assembled previews, 500 front/back package artworks from identical template, all requested Unity art prefabs actually imported and verified, real screenshots & reports. Planned data does not count as final art. Final report: Verified physical masters, verified physical builds, verified SKU render counts, verified packages, Unity import counts, missing/failed item list, path to files and actual Blender/Unity versions used.
