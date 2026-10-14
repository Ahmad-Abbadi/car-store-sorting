# TINY RIDERS — ONE AUTONOMOUS CODEX GOAL (ALL 3D ART + UNITY IMPORT)

> **PASTE THIS WHOLE FILE INTO CODEX GOAL MODE AFTER EXTRACTING THE ATTACHED AUTONOMOUS PRODUCTION KIT.** The actual referenced art, catalogs, package templates, and tools live in that kit. This is an **execution order to create files**, not a request to write another plan.

## 0. Mission — finish the art without routine human checkpoints

Act as an **autonomous 3D art production team**: art director, Blender procedural modeler, UV/2D artist, packaging artist, asset pipeline engineer, Unity technical artist, and QA tester.

**Produce all final art for TINY RIDERS and import it into Unity**. Continue automatically through every phase, self-inspect, fix errors, checkpoint, and resume until the full requested deliverables exist. **Do not stop after Phase 0 or the first car. Do not ask me to approve each phase. Do not end with only plans, prompts, catalog files, scripts, or primitive mockups.**

I explicitly authorize automatic progression through the art-production phases of this project. You may self-accept a phase after actual automated AND visual QA; record the evidence as `AUTO_QA_PASS` (never pretend I personally approved it). It is acceptable to show brief status reports, but **keep doing the work**. Ask me only if a genuine external permission, irreversible/destructive operation, missing essential reference, license choice, or unavailable tool prevents all reasonable further progress.

**Scope:** 3D collectible cars, 2D printed graphics, physical blister packaging, 3D environment art, thumbnails, reference/QA renders, Unity asset import, art-only prefabs, and static inspection/showroom scenes.

**Out of scope:** game mechanics, sorting logic, pickup controls, level systems, progression, scoring, currencies, UI programming, audio, ads, monetization, SDKs, save systems, networking, storefront release, or full gameplay. Unity Editor tooling for importing/assembling ART is allowed, as are minimal static preview scenes. Do not claim this task created a playable game.

## 1. Locate the real assets and obey the authority rules

Find the extracted `Tiny_Riders_500_Car_Production_Kit/` directory (or the folder containing `catalog/collectibles_500.json`). In that directory, read:

- `AGENTS.md` (in the **AUTONOMOUS ART** kit); this file replaces all previous human approval gates.
- `docs/01_ART_BIBLE.md`, `docs/02_REFERENCE_INDEX.md`, `docs/03_PACKAGING_MASTER_LOCK.md`, `docs/04_GEOMETRY_TEXTURE_CONTRACT.md`, `docs/08_CATALOG_AND_VARIATION.md`, `docs/10_FAMILY_DIRECTIVES.md`, `docs/12_GAME_ASSET_REQUIREMENTS.md`, `docs/13_PRINT_READY_VS_CONCEPT_TEMPLATE.md`.
- `catalog/family_specs.json`, `catalog/bodies_50.json`, and **the exact IDs** in `catalog/collectibles_500.json`.
- `references/original_boards/01_city_crew.png` through `10_creature_cruisers.png`, `references/featured_cars/`, `references/packaging_crops/`, and `design/packaging/MASTER_FRONT_LAYOUT_GUIDE.png`.
- `design/brand/tiny_riders_logo.svg` and existing `design/packaging/svg/` assets.

Actually open/view the image files through available image viewing tools. Do not infer that you inspected an image because its filename exists. The **Blue Racer #23** on the Track Club board is the primary master *car style*. The **Track Club blister card** and `docs/03_PACKAGING_MASTER_LOCK.md` are the permanent *packaging layout*. Individual family boards define their *vehicle silhouettes, printed motifs, and palette* only.

**Priority in conflicts:** this goal prompt > autonomous `AGENTS.md` > `docs/03_PACKAGING_MASTER_LOCK.md` + `docs/01_ART_BIBLE.md` + canonical catalog > source image boards > old legacy phase prompts. Ignore `legacy/` and any older instructions saying `STOP FOR APPROVAL`, `P00 ONLY`, `NEEDS_OWNER_APPROVAL`, `MAKE GAMEPLAY`, or `FULL GAME`. This job is **AUTO-RUN + ART ONLY**.

At startup, discover actual Blender, Blender MCP, Blender Python, Unity Editor, renderer, 2D artwork, image-viewer, and file access. Do not assume a tool is installed. Use Blender MCP for early scene inspection/art corrections; use **Blender Python background batch scripts** for bulk 3D generation so that 500 objects do not depend on 500 sequential chat-based MCP calls. Use Codex to write and execute scripts, compose artwork, validate files, and automate Unity Editor importing.

If a tool is unavailable: try a reasonable supported alternative, complete independent deliverables, and record precisely which outputs were blocked. **Never fabricate `.blend`, `.fbx`, `.prefab`, screenshots, verified dimensions, or rendered images.**

## 2. Exact output numbers — no catalog inflation

Use **one stable source catalog** and produce:

- **10 families:** City Crew (CC), Track Club (TC), Retro Rollers (RR), Dirt Patrol (DP), Big Wheels (BW), Rescue Crew (RC), Workyard (WY), Snack Street (SS), Future Fleet (FF), Creature Cruisers (CR).
- **50 genuinely different base vehicle silhouettes**, exactly the five named bodies specified per family in `catalog/bodies_50.json`. Respect each `silhouetteDirective`.
- **100 physical configurations** = standard + distinct custom BUILD per base car. Respect each `customBuildChange`; different colors alone DO NOT count as a build.
- **500 final collectible products** = 100 builds × 5 curated UV-printed livery/paint editions, matched to the existing 500 `sku` entries, unique product name/ID, correct family, rarity, number and packaging artwork.
- **500 finished package front textures and 500 finished package back textures**, each generated through the ONE locked master layout. Keep editable 2D source artwork/templates too.
- **500 packaged product visual definitions/prefabs and 500 loose-car product definitions/prefabs in Unity**, preferably referencing shared meshes, materials, and atlases rather than containing 1,000 redundant mesh copies.
- A minimal coherent **3D sorting/collector-room ART SET**, described in section 8.

500 products are **not** 500 unrelated base meshes, and do not inflate progress by counting placeholders or mock metadata as finished products. The original kit has names, IDs, and art references; it does **NOT** already contain the 500 completed assets. You must actually generate them.

Use the source `sku` and `collectionNumber` exactly as provided. Track the livery/race number separately from the collectible SKU (`23` printed on Blue Racer is NOT automatically `023/500`). Deterministic seeds and versioned sources only. Do not rename cars, reassign family, or randomly change approved styles between batch runs.

## 3. The visual contract — simplified premium toys, NOT realistic cars

Match the 10 provided stylized concept sheets. Preserve a shared design language: chunky thick bodies; simple, **intentional** planar or shallow-bevel surfaces; oversized 8–12-sided tires; broad readable cabins; opaque dark stylized windows; bold toy-like color; sturdy large-scale accessories; crisp printed detail. Recognizability at 140-pixel thumbnail width is mandatory.

**DO NOT recreate the earlier failed smooth, realistic-looking wedge with a flimsy unsupported rear wing.** Avoid random stacked primitives that look broken, unrelated generic four-door sedans in every family, subdivision-heavy realistic sculpting, thin mirror stems, complex headlights, deep car interiors, or 500 disconnected one-shot AI mesh results.

Base silhouettes must be meaningfully different: hatchback vs sedan vs van vs bus vs off-road truck vs monster truck vs work machinery vs novelty food car vs futuristic car vs creature car. Use custom cross sections, wedges and broad beveled masses with shape-specific modeling rules. Workyard must include visible real cargo/function volumes; creature vehicles need believable simple fins/shells/horns; Snack Street needs actual large physical toppings, not 2D silhouettes pretending to be objects.

The 50 body shape directives and 10 family references are mandatory. If the supplied reference includes inconsistent angles/text, interpret it into one coherent model, rather than creating distorted geometry to match impossible perspective views. Preserve one exact approved camera/light rig for comparing all batches.

Keep the art attractive in an actual crowded heap, on shelf, in blister packaging, and selected for close-up inspection. Small features that disappear in the sorting view are expendable; distinctive silhouette features are not.

## 4. Non-negotiable mesh versus texture separation

**Physical 3D volumes = meshes. Printed artwork = FLAT UV-MAPPED 2D files. Shine/reflection = material and light.**

Allowed mesh parts: vehicle body, cabin, wheels/rims/tires, body cutouts, bumpers, real spoilers/wings, chunky sirens, roof racks, towing hooks, cranes, food sculptures, character fins/horns/shells, cardboard thickness/die-cut, clear plastic blister and flange, actual room furniture and wall/floor shells.

**Texture-only:** EVERY printed logo, brand name, family name, race number, collection number, text, stripe, checker flag, decals, road markings, headlamp coloration, taillights when merely printed, windows, vehicle faces/teeth/eyes, hazard stripes, food graphics, fake gloss accents, package illustrations and all labels. Create/edit artwork as actual SVG/PNG (or equivalent flat files), UV-map to the final meshes, with correct color spaces and texel padding.

BANNED: Blender FONT objects for visible printed words; convert-text-to-mesh; 3D letters; extruded SVG logos; decals made as floating mesh strips; numbers carved into body meshes; fake mesh sparkle/reflection streaks; whole reference-board screenshot pasted onto geometry; screenshot of complete blister used as printed card. If an image or font is missing, **generate the 2D artwork or honestly record the blocker**; NEVER fake it with geometry.

**QA clay test:** render a neutral material override with bump/normal artwork disabled. Every printed word, eye, number, stripe, logo and card illustration must disappear. Only meaningful physical volume should remain. Supply wireframe and UV artwork/contact sheet evidence. Windows are 2D ink regions unless a family absolutely needs true geometric apertures for its silhouette.

## 5. Reusable components and geometry generation

First make a consistent reusable Blender vehicle art kit: wheelsets, tires, rims, window/lights material zones, grille/bumper modules, chassis blocks, hood/cabin/body profile generator, fender/arch generator, and category-specific add-ons. Define attachment sockets and local orientation for wheel axles, rear wing, roof items, front/rear bumpers, work gear. Do **not** forcibly attach the same spoiler to an ice-cream truck or the same fenders to a forklift.

Use an editable source `.blend` master for the component library and `body_{ID}.blend` source per base silhouette or a clearly versioned multi-asset library with unambiguous object names. For the 100 builds store a physical build recipe and the actual exported built geometry, preserving shared parts when possible. Bake modifiers/apply transforms appropriately in **export copies**, not destructive master changes.

Geometry guidance (project starting points, not hidden restrictions): ~600–2,000 triangles for simple cars; ~1,000–3,500 for bulky specialty vehicles; prioritize good readable forms above target polygon count. Full package card/blister is one reusable geometry assembly, not 500 independently modeled blisters. Record all actual counts. No clipping wheels, floating structural accessories, inverted normals, zero-thickness visibly physical parts, or wrong ground contact. Consistent +Z up, consistent forward axis, consistent Unity import scale.

Do not build 50 bodies by blindly randomizing one small car mesh. Create **5 genuinely different shape recipes per family** and compare silhouettes within/across families. Give custom edition a real visible geometry change (e.g., new roof cargo, bumper set, offroad equipment, larger wing, altered work gear), ideally distinguishable in clay render, not merely a recolor.

## 6. Flat artwork and 500 intentional variants

Produce actual livery graphics, not just numeric RGB swatches. Use original stylized stripes, checker motifs, race numbers, cross-family symbols, functional markings, friendly creature faces, food-brand graphics, etc., matched to the vehicle's intended category. Generate flat SVG artwork using an allowed commercial font and deterministic 2D drawing, rasterize to PNG for UV use. Use appropriately curated colors—not five random palettes. Make text correctly readable on both sides; never mirror UV islands containing text. Save editable 2D source and final atlas/texture, with materials connected and verified.

For every `sku`, the actual final car build must load with its intended: body shape, physical custom equipment if applicable, wheel set, car material/livery, name, number, rarity and family. A livery variant can share 3D mesh, but the **final rendered collectible must be distinguishable** from the other four. If a significant printed element such as `23` is present, ensure it lives solely in 2D artwork.

Create studio camera renders from the actual final assembled vehicle; do not ask image AI to hallucinate a separate car illustration for its package. Keep source master artwork higher resolution if useful, with appropriately compressed or atlased runtime copies later. Do not assume thousands of individual full-res atlases must be loaded simultaneously by Unity.

## 7. Exact LOCKED packaging system — DO NOT improvise

**Use `docs/03_PACKAGING_MASTER_LOCK.md` as literal geometric/layout coordinates.** All 500 packages must use one card-and-blister structure and one consistent brand design, changing only approved family colors, actual car artwork, title, product number, and intended rarity/series treatments.

**Locked physical size:** card **120 mm wide × 180 mm high**, cardboard thickness approximately **0.7 mm**. Master front/back art canvas **1200 × 1800 px**, x/y origin at top-left. Shared card silhouette, rounded corners, hanging cutout centered x=600 (y about 45–105 px), original supplied `TINY RIDERS` 2D logo. Keep the structural Track Club design as source; other 9 boards are COLOR/ILLUSTRATION variants, not alternative package systems.

**Mandatory front coordinates** (pixel regions, max ±12 px variance of fixed zones):

| Element | Rect (x, y, w, h) |
|---|---|
| Brand/logo | x=100..1080, y=135..340 |
| Family banner | x=100..620, y=350..430 |
| Actual car image printed hero zone | x=90..1110, y=460..965 |
| Real blister in front of card | x=86..1114, y=975..1515 |
| Model-name footer | x=90..950, y=1542..1700 |
| Catalog number badge | x=955..1138, y=1560..1700 |
| Safe area | 55px from exterior trim |

One actual shallow low-poly blister shell + flange, adaptable **depth** where needed but not wildly relocating its print footprint. Scale unusually long/tall vehicles uniformly to fit the same pack when possible; for impossible forms report which SKUs require a truly separate packaging size rather than silently breaking the master. Blister must be translucent/clear enough to show car in a dedicated selected-item preview and use cheap shelf representation where hundreds of products are visible.

One shared brand source `design/brand/tiny_riders_logo.svg`. Print lettering as SVG/PNG only, never make 3D logo geometry. Preserve logo position and typographic hierarchy. Each family's background art is a different skin using **the same** front/back master zones. The package's printed hero car image is a render of the EXACT physical car inside, same wheels, wing, body, number, paint, livery, custom parts. No duplicate 2D car in the blister zone. No reference-board screenshot in package artwork. Do not use generic AI-created car art that differs from physical toy.

Back design: consistent logo, family header, real product info and small lineup thumbnails **rendered from actual family cars**, using same artboard dimensions and fixed back layout. Save 500 editable front sources + 500 editable back sources and their correctly rasterized PNGs, plus one reusable packaging 3D prefab. Use 2D rasterized font rather than modeling text.

Important: supplied package SVG/PNG templates are **starting layouts** with placeholder areas. Fill them with generated accurate final artwork. Never count blank placeholder cards as finished.

## 8. Make ALL necessary 3D environment ART — not gameplay

Build a consistent, modular stylized collector/sorting room matching the references and duck-pile inspiration. Provide at least these **physical** meshes/assets:

1. Room shell: walls, floor, ceiling/beam variants, wall trim and corner pieces.
2. At least 3 repeatable shelves: tall wall shelf, short display shelf, open industrial rack; modular extensions/end caps.
3. At least 10 family shelf signage/background panels using the SAME family palettes; printed text/logos as textures on flat art surfaces.
4. Large disorganized collection pile container / tray / pit with a readable boundary (scene can place sample cars, but do not invent live game physics).
5. Sorting workbench or sorting table; a compact inspection podium with turntable-like static base.
6. Rare-car display case, glass/acrylic collector cabinet, small pedestal stands.
7. Cardboard shipping cartons in 3 sizes, open carton variant, plastic storage bins, simple drawer units.
8. Neutral wall decor, soft lamp fixtures, simple ceiling lights, minimal hanging family banners, wall collection posters (all text as 2D textures).
9. Shelf dividers, family/rarity plaques, slot backboards, stackable storage trays.
10. A handful of scene dressing props (tiny plant, stool, utility cart, desk accessories) if they improve the reference look without crowding the scene.

Create **art-only Unity scenes**:
- `TinyRiders_CarGallery` — 50 base bodies lined up plus variant examples in consistent light.
- `TinyRiders_PackagedGallery` — packages from all families under the same presentation rules.
- `TinyRiders_SortingRoom_ART_PREVIEW` — big pile of sample loose cars, thematic shelves and environment, no sorting logic.
- `TinyRiders_StressPreview_ART` — hundreds of simplified visible items for looking at readability, scene cleanliness and potential overdraw; no invented performance numbers unless actually measured.

Cars in a heap should be readable, bright and appealing like the supplied duck game screenshot, but these are placed art instances, NOT runtime simulated gameplay. Keep environment secondary to the car collection.

## 9. Unity import — ART ONLY

Use an existing Unity project if it is clearly available; otherwise create a clean **Unity 6 URP** project if Unity Editor is present and you can legally access it. If Unity is unavailable, export an **import-ready Unity art directory**, scripts, manifests and previews; do not falsely claim a tested `.unitypackage` or `.prefab`.

Write editor-only asset-import helpers as needed. Import actual geometry and material textures with correct scale, transforms, pivots, axes and alpha settings. Store under `Assets/TinyRidersArt/` organized by `Bodies`, `Builds`, `SharedComponents`, `Liveries`, `Packaging`, `Environment`, `Materials`, `Prefabs`, `PreviewScenes`, `Editor`.

Create **one loose-car art prefab and one packaged-art prefab per SKU** (500 each, preferably referring to shared meshes/materials rather than duplicating geometry), along with preview thumbnails. Make one canonical `PackagingMaster` prefab with the proper clear blister and mapped front/back artwork. Use correct Unity materials; Blender shader node setups need explicit mapping to URP rather than assumed automatic preservation. Package preview needs legible front/back, controlled alpha and coherent consistent dimensions. Test actual Unity screenshots for at least Blue Racer, a specialty work vehicle, a creature, a monster truck, and one package from every family.

ScriptableObject/JSON metadata definitions and editor-only scripts are allowed **only to make the art assets importable and inspectable**; do not create runtime sorting/progression logic, timers, ads, menus, or gameplay controllers.

Optimize obvious duplication: shared wheel mesh, common family card backgrounds, texture atlases, material palette reuse, instanced samples; choose cheap card-only shelf representations for distant packages. Record asset sizes/tris and leave performance optimization to later unless there is a severe obvious art import issue. No fake claim of WebGL build/test.

## 10. Fully AUTOMATIC phases — do not stop for approvals

**A0 — Inventory and environment:** inspect all 10 references and 500 records; verify folder/tool access. Record source asset manifest and baseline catalog validation. Initialize `progress/auto_status.json` and output structure. CONTINUE.

**A1 — Reusable art factory:** create deterministic Blender scripts, component library, consistent view rigs, 2D vector/texture generator and Unity import editor tooling. Actually test each component on a scene. CONTINUE.

**A2 — Blue Racer / Track Club anchor:** make a high-quality Blue Racer #23 standard+custom, livery textures, side/front/clay/wireframe renders. If bad, fix geometry now, not with paint. Generate real FBX and `.blend`. CONTINUE after `AUTO_QA_PASS`.

**A3 — Universal packaging anchor:** construct the master card and blister; generate actual flat front/back for Blue Racer using a render of THE SAME car; match the fixed template; verify no print geometry and no duplicate printed car underneath blister. Freeze template and reuse for all 500. CONTINUE after `AUTO_QA_PASS`.

**A4 — Ten distinct family anchor cars:** one representative base body per family, with real source and exports. Inspect a 10-family clay/silhouette comparison sheet; fix sameness or bad construction; render sample family packages to confirm master lock. CONTINUE.

**A5 — All 50 base bodies:** finish remaining 40 distinct silhouettes. Work family by family in batches up to 5–10; run geometry/visual/texture review for each batch. Generate per-family and global contact sheets. CONTINUE.

**A6 — All 100 physical builds:** make standard/custom version of each base according to actual per-body `customBuildChange`. Export/source/store 100 real configurations; clay/silhouette compare all custom vs standard. CONTINUE.

**A7 — All 500 art variants:** generate five curated UV-mapped liveries per physical build and verify 500 final assembled vehicle definitions. Generate actual per-SKU car previews and texture assets. Batch around 20–50 products to avoid memory crashes. Resume using progress ledger when interrupted. CONTINUE.

**A8 — All 500 exact packages:** generate 500 real front SVG/PNG and back SVG/PNG artworks with unique names/numbers, render of corresponding actual car, and correct blister/prefab assembly. Verify package identity against SKU and image matching. CONTINUE.

**A9 — Environment art:** produce all reusable 3D sorting-room elements and decorate representative gallery/sorting art scene. CONTINUE.

**A10 — Unity import:** import art assets, correct material/texture assignments, create shared prefabs and all 500 loose + 500 packaged SKU prefab instances, build 4 art-preview Unity scenes, capture actual proof views. CONTINUE.

**A11 — Final QA/fix:** validate all counts, actual Blender/FBX/prefab asset existence, text-as-texture clay audit, correct card coordinates, materials, broken references, 500 ID uniqueness, catalog matching, package/vehicle render matching, file sizes and final quality contact sheets. Repair failures and re-run checks. Produce final manifest and accurate delivery report. FINISH ONLY WHEN VERIFIED, OR REPORT CLEAR INCOMPLETE ITEMS IF BLOCKED BY REAL TOOL/LIMITS.

Within each phase **automatically self-check**; if failed, attempt up to 3 targeted repairs for the specific item, mark unresolved failures with exact file IDs and reasons, continue safe independent work, then revisit flagged items. Never hide or replace failed products with generic placeholders in the final count. If a failure makes every subsequent item wrong (e.g. master package template or wheel kit), repair that global foundation before bulk generation. Only block the entire job when no useful independent work can proceed.

Do not use prior human approval state fields to block progression; the new `progress/auto_status.json` is authoritative for this new **art-only autonomous** job. Use values such as `NOT_STARTED`, `RUNNING`, `AUTO_QA_PASS`, `REWORK`, `BLOCKED` and supply image/file evidence for each pass.

## 11. Resumability, audits and honesty

Every batch writes a durable output and progress record to disk. Do not rely on chat context to remember processed items. Implement:

- `build_all_art.py` or equivalent orchestrator with `--resume`, `--phase`, `--family`, `--batch-size`, `--dry-run` or a documented comparable CLI.
- `progress/auto_status.json` plus per-SKU stage statuses and QA log files.
- Deterministic filenames derived from stable SKU and `bodyId`, no random names.
- `generated/` trees: `.blend` sources, `.fbx` exports, UV textures, source SVG, finalized card PNG, renders, contact sheets, Unity assets, QA reports, and manifests.
- An incremental-safe mechanism to skip verified outputs but rebuild changed dependencies when a master is updated.
- Previews at consistent camera angle, scale and exposure, plus clay/wireframe checks to make wrong mesh/text separations obvious.

At end run existing `tools/validate_catalog.py`, a real asset-existence/count script, package-coordinates validation and any available Blender/Unity import validators. Distinguish **planned IDs** from **finished meshes**, **finished textures**, **finished packages**, **finished prefabs**, and **verified Unity screenshots**.

**Final deliverables:** a usable Unity art project (when Unity accessible), editable Blender source library, all 50 base bodies, 100 physical builds, 500 visually finished SKU variants and matching packages, modular environment art set, consistent preview scenes, matching atlas/source artwork, production reports and contact sheets. Generate a `FINAL_DELIVERY_REPORT.md` with actual numbers and any incomplete or blocked items. Do not claim 500 finished assets merely because a JSON file has 500 entries.

## 12. Begin execution immediately

**START NOW.** Inspect the kit and run A0, then automatically A1 through A11. Make files and render actual assets. Keep going without asking for routine approvals. **Do not reply only with a plan or a reformulated prompt.** If this Goal Mode session cannot finish the full collection in one invocation, persist a reproducible checkpoint and clear continuation instructions, then use any available session continuation mechanism. Never invent that work will continue in the background if the current environment does not support it.
