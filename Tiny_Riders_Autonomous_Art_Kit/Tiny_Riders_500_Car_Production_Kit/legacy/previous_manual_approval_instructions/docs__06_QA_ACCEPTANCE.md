# 06 — QA / Acceptance Tests

## Global non-negotiable checks (all phases)
- 10 exact source boards are present and were actually inspected.
- IDs are stable, unique, and valid (`python tools/validate_catalog.py`).
- No visible 3D lettering/number/logo geometry; printed content is a 2D texture mapped to real UVs.
- Gray clay material override removes ALL print, decorative graphics, eyes/teeth/headlights if texture-based, while physical structure remains.
- No reference screenshot pasted onto a car/packaging.
- No copied licensed brands, automobiles, cartoons, toy logos or catalog art.
- Source file, export, flat texture, actual render and test log all exist before declaring completion.

## Pilot car gate P02
- Chunky toy proportions; all wheels grounded and correctly aligned, no floating/penetrating accessories, no visible primitive-stack failure.
- Body recognizably matches `02_track_club.png` Blue Racer; differentiate nose, roof, hood, wheels and wing from the older failed smooth realistic mesh.
- 3/4, front, side, top, rear and black silhouette actual renders available and manually reviewed.
- Original number `23` rendered as flat UV artwork on readable LEFT and RIGHT; striping also printed. No letter-shaped mesh or spline.
- Custom build physically differs from standard, beyond new color/decal.
- Signed-off master model is not overwritten by subsequent random generation.

## Packaging master gate P03
- One card: 120×180mm, 1200×1800 flat artwork source, identical hanging hole, logo, category chip, upper printed car art, lower physical blister, bottom label and number.
- Front/back mapped correctly and independently; both readable, no mirrored text.
- Real blister has correct physical clearance, thin wall and visible separate flange.
- No fake transparent bubble printed onto cardboard. No duplicate toy image behind real blister.
- Printed car image matches physical car EXACTLY: shape, build, rim, wing, color, stripe, printed race number.
- Universal typography and 2D logo same for all family previews; only controlled colors/scenery/category chip change.
- Flat texture sources and finalized product PNGs both delivered and inspected at 100% zoom.

## Families and bulk gate P05-P08
- 50 base bodies: each specified in `catalog/bodies_50.json`; no substitution/repeated generic 4-door body.
- 2 physical editions per body: visually obvious change and physically valid construction, meshes/parts reusable.
- 500 IDs (10 families × 5 bodies × 2 builds × 5 liveries). No duplicates, gaps, family mismatches, missing package art or invalid model names.
- Contact sheet per family with all 5 base silhouettes, and a full 50-body sheet.
- Every batch reviewed with comparisons and pass/fail table; proposed failures fixed before propagation.
- Rarity/edition graphics encoded as print only; number of base-body shapes does not silently drop under 50.

## Runtime QA P04 / P09
- Real Unity screenshot, not Blender render passed off as Unity.
- Performance measured in actual browser, not assumed from triangle count.
- At least one low-end device and one typical desktop browser pass smoke tests; no crash/repeated shader compile stalls.
- Inspect close-up full car vs many-package shelf LOD; no 500 concurrent high-detail cars or transparent blisters.
- Sorting identifies family and body, snaps correctly to relevant shelves, UI is legible at browser size, no forced level selection.
- Save data persists after reload; rare finds/collection progress deterministic and versionable by SKU.
- Loading/asset budgets checked against current CrazyGames rules on submission day rather than copied from old articles.

## Concrete reject examples
- "A pretty white text logo floating above the hood" → REJECT: modeled print.
- "Car packaging shows a different car with different wing" → REJECT: mismatched illustration.
- "City package changed to tall narrow card because it looked nicer" → REJECT: broken packaging master.
- "10 visually identical hatchbacks with different primary colors" → REJECT: lost shape variety.
- "Rendered all 500 packages with 2M polygon assets and sent editor screenshots only" → REJECT: no web run-time verification.
- "Ran a generator script but no accessible `.blend` or PNG previews" → REJECT: not demonstrated.
