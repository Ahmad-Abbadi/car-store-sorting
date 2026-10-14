# CODEX GOAL MODE — TINY RIDERS v2 (25 APPROVED MASTER MESHES → 500 COLLECTIBLES)

> **PASTE THIS WHOLE FILE INTO CODEX GOAL MODE AFTER EXTRACTING THIS PRODUCTION KIT.** This is an **execution request**, not a request for a plan. Deliver actual 3D geometry, textures, renders, Blender source assets and Unity ART imports wherever tools/sources permit. This kit contains specifications and concept references; **it does not contain 25 completed premium master models**.

## THE MISSION

You are the autonomous **technical artist + Blender pipeline engineer + Unity art integrator** for **TINY RIDERS**, a colorful toy-car sorting game. Build the complete high-quality collectible ART set, using **25 premium foundational vehicle meshes**, modular physical parts and consistent printed UV artwork. Follow the provided ten concept-board families, and import the resulting art into Unity. Do **NOT** build gameplay systems, scoring, inventory, monetization, save/load, ads, networking, or CrazyGames SDK.

**Target:** `25 masters × 2 named model identities × 2 physical builds × 5 liveries = 500 collectible products`, each with its correct 3D car and matching locked-layout package.

**Preserve all original catalog IDs, names and ten family collections.** Source-of-truth: `catalog/masters_25.json`, `catalog/body_to_master_50.json`, `catalog/bodies_50_v2.json`, `catalog/builds_100.json`, `catalog/collectibles_500_v2.json`. Do not invent a replacement SKU hierarchy. The original catalog from the previous 50-base proposal is archived read-only for traceability.

## CRITICAL BEHAVIOR: AUTONOMOUS, REAL ART, NEVER FAKE COMPLETION

**Work continuously through your own internal phases. Do not stop after every phase or ask for routine approvals.** Write and execute the asset generators; use Blender MCP for actual visual inspection; render actual assets; correct problems; produce batches; save checkpoints; import into Unity; verify Unity images. This is an execution task, not a documentation-only deliverable.

At the same time, **the 25 premium master meshes are a real quality dependency**. The rejected gray car made from generic primitives is not an acceptable master. The files in this production kit are references and data; they do not magically contain finished premium 3D geometry. Inspect the workspace for licensed or owned high-quality stylized meshes. Create candidate source meshes with supported Blender/model-generation workflows only if you can inspect and correct them to the quality bar. Never count an ugly primitive blockout or unlicensed asset as `APPROVED_MASTER`.

No unauthorized ripping/downloading of other games' cars, no unauthorized paid purchases, no copied real-brand toy logos. If you cannot obtain an acceptable master for a pair, mark it `MISSING_SOURCE` and continue independent pairs. If the remaining work needs external licensed masters or human art, give me an exact list of master IDs, missing assets and what is required. **Never claim 500 finished collectibles when only 500 JSON records or placeholders exist.**

Read `AGENTS.md` and all `docs/` files of **this v2 kit**. Legacy approval-gated instructions from an earlier kit do not govern this job. Preserve production state to `progress/PRODUCTION_STATE.json` and per-batch reports. If a session ends, write precise reproducible resume instructions into `progress/RESUME_HERE.md`. Do not pretend to keep running after a stopped session.

## PHASE 0 — INSPECT REFERENCES, VERIFY SOURCES AND ENVIRONMENT

1. Open the actual PNG boards in `references/original_boards/01_city_crew.png` through `10_creature_cruisers.png`, plus focused refs in `references/featured_cars/`. Inspect `references/ALL_TEN_SHEETS_GALLERY.png` for family coherence. Blue Racer #23 is the CAR art-style anchor.
2. Inspect `design/packaging/MASTER_FRONT_LAYOUT_GUIDE.png`, family flat templates under `design/packaging/`, and the exact original flat logo `design/brand/tiny_riders_logo.svg`. Track Club package is the physical/layout anchor.
3. Read and validate the v2 catalog. Run `python scripts/validate_catalog.py`. Confirm **25, 50, 100, 500**, ten families and **50 SKUs per family**. Catalog statuses start `MISSING_NOT_PRODUCED`; do not mistake them for assets.
4. Inspect installed Blender, Blender MCP, Blender Python version, renderer, fonts, Unity version/project, and actual available model source files. Log results and paths. Do not claim a tool exists without verifying.
5. Record each of the 25 master IDs as `APPROVED_SOURCE`, `NEEDS_REWORK`, or `MISSING_SOURCE` with source path, license/ownership evidence and review render paths. Models can be commissioned/artist-created, locally available licensed assets, or genuinely successful reviewed generation candidates. DO NOT buy/download restricted assets without permission.
6. Create an ordered batch plan using available sources, but immediately execute independent work rather than stopping at a plan.

## PHASE 1 — ESTABLISH PREMIUM CHUNKY ART STYLE ON A REAL MASTER

Take `M03 TRACK_COUPE` (`TC-01 Blue Racer` and `TC-05 Apex`) or, if its source is unavailable, the earliest master with a lawful acceptable source. Create the approved shape from that source with deliberate chunky toy proportions.

Style: broad surfaces; distinctive toy silhouette; clean intentional bevels; sturdy and correctly placed wheels; attractive readable headlights; opaque flat dark window regions; thick uncomplicated aerodynamic or specialty parts. No photorealistic real car reproduction. DO NOT try to fix bad geometry by adding metallic paint and glowing lights. Keep style consistent with the Blue Racer #23 reference.

Create Blender source, export FBX, unwrapped UVs, material test rig and actual renders: front three-quarter, side, top, clay, wireframe. Inspect the real images. Fix bad hood/cabin proportions, floating wings, tire arch intersections, pinched normals, bad front lights, and poor toy quality. Only count an actual high-quality reviewed source as `MASTER_APPROVED`.

Physical rule: car/chassis/tires/wings/spoilers/roof equipment are meshes. **Numbers, words, stripes, lights graphics, window graphics, logos and printed panel markings MUST be 2D images on UVs.** No Blender Font meshes, extruded logos, raised number plates or floating decal meshes. The gray-clay test must eliminate printed art.

## PHASE 2 — MAKE THE MODULAR BODY/BUILD SYSTEM

Each `masterId` supports exactly 2 different original named vehicles. Read `catalog/masters_25.json` for the exact pair, source geometry profile and required structural distinction. Each identity gets `standard` and `custom` physical build, making **4 legitimate physical builds per master**.

Examples:
- `M03`: Blue Racer race coupe vs Apex endurance body; distinct front/roof/rear/wing modules.
- `M13`: Tipper dump-bed truck vs Mixer barrel truck; same approved utility chassis, actual different load-body geometry.
- `M19`: Shark dorsal-fin/snout car vs Dragon horns/wing car; creature facial graphics stay FLAT TEXTURE.
- `M11`: Medic ambulance vs Ladder fire truck; cargo/emergency modules physically different.

Use modular source child objects and safe attachment points for wheels, wings, cabin/roof, front/rear specialty geometry. Physical modules must fit without intersections. An A/B identity cannot be only a paint change. Custom build must also be physically different from standard. Render and inspect A/B × standard/custom comparisons for each master.

Reuse wheels, bumpers, tire profiles, simple specialty accessories wherever they maintain the chosen visual style. Make changes using real approved art components, NOT unconstrained random primitive assemblies.

## PHASE 3 — DETERMINISTIC UV TEXTURE & LIVERY FACTORY

For each of the 100 physical builds, generate **five deliberately curated** paint/livery versions from the catalog: 500 distinct final products. Fixed ID = fixed material/decals. Use one persistent SKU manifest, reproducible seeds, and clean UV layout.

All print belongs in independently editable 2D SVG/PNG files before being mapped: race numbers (Blue Racer's printed `23`), stripes, fictional sponsors, small badges, animal eyes/teeth, packaging typography, family motifs, windows, light graphics. Do not synthesize typography by asking an image model to guess words. Use a permitted font and rasterize correctly. Do not mirror text. Keep alpha and UV padding correct. Never substitute a cube/letter/mesh for missing artwork.

Make car previews from assembled FINAL SKU geometry with correct build, wheels and livery. Car artwork must be interesting at shelf size (~100–150 screen pixels), not just under perfect zoom. Render per-SKU studio images and store filename + checksum in manifests.

## PHASE 4 — LOCK AND PRODUCE ONE PACKAGE DESIGN 500 TIMES

Read `docs/02_PACKAGING_MASTER_LOCK_V2.md`; do not improvise. The reference packaging layout is not a suggestion: **same physical card, hanging cutout, logo, banner, hero area, blister area, product name and SKU footer positions for every SKU**.

- Card: **120×180 mm**, ~0.7 mm thick; 1200×1800 pixel front and back artboard.
- Fixed logo region `x=100..1080, y=135..340` (same `tiny_riders_logo.svg`).
- Fixed family banner `x=100..620, y=350..430`.
- Printed hero rendered car image `x=90..1110, y=460..965`.
- Actual clear 3D blister footprint `x=86..1114, y=975..1515`.
- Product footer `x=90..950, y=1542..1700`; catalog number `x=955..1138, y=1560..1700`.

Color/family theme and product images/wording change; geometry/layout does NOT. Use actual car renders from Phase 3 to depict the exact model inside, including body kit, tire type and livery. A separate AI-rendered lookalike does not count. Do not duplicate the physical toy as a printed picture behind the blister. Model simple physical blister/card geometry; print everything else. Back covers use the same master zones/type hierarchy and real family thumbnails.

Produce **500 real front flat artwork files, 500 real back flat artwork files**, a common package geometry and 500 corresponding prefab/assembly bindings. Do not count blank templates as final print art. Use source SVGs and final high-quality PNG masters, and create smaller game-ready derivatives/atlases rather than packing all full-res art into the startup build.

## PHASE 5 — BATCH ALL 25 MASTERS, 50 IDENTITIES, 100 BUILDS, 500 SKUS

Batch masters in groups of 3–5. Batch livery SKUs in groups of 20–50. Use background Blender Python scripts for generation; Blender MCP is for inspecting representative scenes and correcting master generators/art. Do not schedule 500 independent tool-chat requests when one deterministic script can perform the variation.

After every batch:
- Validate geometry/UV/textures/IDs/files.
- Render actual multi-view proof and compare to the proper family reference.
- Verify two named identities are structurally distinguishable; standard/custom must differ physically.
- Verify five printed variants look curated, not random.
- Verify printed text/stripes disappear in gray-clay mode.
- Verify actual package art and car visual pair.
- Hash outputs and update state.
- Repair clear defects and continue automatically; quarantine genuinely blocked IDs.

Contact sheets: 25 masters, 50 named identity builds, 100 physical standard/custom comparisons, 10 × 50-product family sheets, complete 500-thumbnail gallery, package front/back sheets, and 10 family group package renders. All images must show ACTUAL generated art, not concept-board crops mislabeled as output.

## PHASE 6 — IMPORT ALL VERIFIED ART TO UNITY

If a Unity 6 project is accessible, use it; use URP if present and don't change an established project render pipeline unnecessarily. If only Blender is available, create honest import-ready FBX/material/atlas manifests and Unity Editor C# scripts, but report Unity verification as BLOCKED rather than claiming it happened.

Create the shared asset structure in `Assets/TinyRidersArt/`:
- `Models/Masters25`, `Models/BodyModules`, `Materials`, `Textures`, `Prefabs/Identity50`, `Prefabs/Build100`, `Prefabs/Products500`, `Prefabs/Packaged500`, `PreviewScenes`, `Editor`.
- Source mesh imports should remain shared, not duplicated 500 times.
- Create 500 car SKU art prefabs (or importable equivalent composed variant assets) and 500 packaged SKU art prefabs referencing one common package structure plus appropriate source car and artwork. Stable names reflect catalog SKUs.
- Use nested prefabs/prefab variants, editor batch generation and explicit Unity material setup; correct source texture alpha and smoothness vs roughness mapping.
- Make static art-review scenes showing 25 master bodies, mixed 10-family display, stacks of shelf proxy cards and one fully inspected packaged car.
- Actually open/verify selected assets in the Unity Editor. Show screenshot proof of printed race number, packaging SKU, transparent blister, scale and true car-to-card match. Do not present Blender screenshots as Unity proof.

Unity C# for **editor-only prefab generation** and preview scenes is allowed. Sorting logic, vehicle driving, ads, store, save data, gameplay controllers and economy are OUT OF SCOPE.

## PHASE 6B — MODULAR SORTING-ROOM ENVIRONMENT ART (AFTER CARS/PACKAGES)

Create *art assets only* for the same chunky toy-collector world: one room shell (floor/walls/ceiling), two wall-shelf modules, one freestanding retail shelf, a premium rare-car display case, sorting workbench, pile container/tray, cardboard shipping boxes, storage bins, shelf dividers, flat 2D family signs for ten collections, light fixtures, and a small amount of uncluttered decoration. Maintain the exact color/material design language of the ten family boards and the simplified duck sorting reference.

Use reusable meshes and print all words/logos on 2D textures. Import room art into a static Unity `SortingRoom_ART_PREVIEW` scene with some approved loose cars and cheap proxy packaged cars on shelves. **Do not write gameplay, physics sorting rules or character controls.** Environment generation is lower priority than obtaining 25 good master car sources and 500 matching packages. If those remain blocked, continue with environment art but report car completion honestly.

## PHASE 7 — AUTOMATED QA, RECOVERY AND FINAL DELIVERY

Run `python scripts/validate_catalog.py` at start and finish (catalog validity alone DOES NOT prove art completion). Also implement a *real file* validation script verifying source `.blend`, geometry exports, livery files, card PNG/SVG, preview screenshots, material links, prefab references, geometry triangle totals and Unity scene presence. Record actual model/texture file hashes and Blender/Unity versions.

Keep `progress/PRODUCTION_STATE.json`, `progress/RESUME_HERE.md`, `progress/FAILED_ITEMS.csv` and `progress/VALIDATION_REPORT.md`. A blocked master should identify every dependent SKU. If one master is unusable, process other approved masters instead of failing the entire run. The final report must state actual count of:
1. approved master meshes (out of 25),
2. rendered identity designs (out of 50),
3. validated physical builds (out of 100),
4. rendered final car SKUs (out of 500),
5. unique completed flat front/back package art pairs (out of 500),
6. verified Unity car/packaged art prefab variants,
7. remaining failures, inaccessible tools, source licensing blockers.

**Important honesty:** It is better to deliver 14 excellent confirmed masters and 280 finished SKU variants with 11 clearly blocked sources than to claim all 500 are premium while silently producing filler. Do not interpret this as permission to stop early: keep working until all obtainable items are processed and proven, using resumable checkpoints.

## FINAL EXECUTION COMMAND

Start by inspecting the real files, validating the 25→50→100→500 catalog, and verifying access to high-quality base meshes. Then create and execute the art-production pipeline, produce real artwork and 3D assets in batches, create perfectly consistent packages, import to Unity and inspect the result. **DO NOT respond with only explanations, code snippets, future plans or a low-quality blue cube car.** Deliver actual verified files and honest completion counts. Continue through independent phases without routine user approval, while respecting actual runtime and licensing barriers.
