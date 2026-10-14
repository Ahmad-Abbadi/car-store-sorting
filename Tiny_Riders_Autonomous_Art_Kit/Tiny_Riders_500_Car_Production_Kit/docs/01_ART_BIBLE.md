# 01 — Locked Art Bible: The Chunky Toy Look

**Primary shape reference:** `references/original_boards/02_track_club.png`, hero `Blue Racer` (#23) for sporty proportions; `01_city_crew.png` for friendliness; `10_creature_cruisers.png` for simple character detail; remaining sheets define family variations. Treat source sheets as **concepts** rather than literal all-angle blueprints.

## Geometry style
- Clear, exaggerated 3D toy silhouettes, sturdy connected objects, clean flat and broad beveled surfaces, intentional low-poly faceting.
- Tiny cars **must not** appear as poorly made realistic cars. Simplify intentionally: broad slab hood, one clean cabin mass, prominent chunky wheels, distinct recognizable accessories.
- Avoid round-body subdivision and thin windshield pillars. Broad sloped windows may be flat, opaque dark ink applied onto the cabin geometry.
- Car classes deliberately differ: small cars, wagons, vans, monster trucks, work trucks, creatures, buses, and stylized futuristic cars.
- Treat printed headlights/eyes/window seams as **textures**, unless a deliberate large physical shape fundamentally changes silhouette.
- One-segment or shallow bevels to create lightcatching edges; no noise, subdivided curves or complex realistic features.

## Design consistency across families
- Camera for contact sheets: orthographic 3/4 front, side, top, back and black silhouette at identical reference size; 30–40 degree azimuth; camera elevation about 22–30 degrees; choose and save exact settings in P01.
- Short and broad body proportions when visually sensible; larger thick tires than real cars. No excessively long narrow car noses unless family demands (dragster).
- Opaque dark windows; little/no internal cabin modeling. Not glass transmission.
- Hard-plastic main body with satin sheen, simple black/charcoal tires, slightly smoother window regions, muted dark metallic hubs where desired.
- Family-specific readable palettes derived from each individual original board. Avoid huge procedural painterly textures on tiny toys.
- A vehicle must be recognizable as the named toy in a pile of 50 without reading its model number.

## Fixed shared modeling vocabulary
- Construct basic geometry from custom low-poly body cross sections, beveled cuboids, wedges, boxes and low-sided cylinder tires. Respect continuity and deliberate flow between shapes.
- Block out geometry with untextured materials FIRST, then get owner approval before detailed paint or packaging.
- Prefer 8–12-sided tire cylinders as initial target, keep axle positions exact and four tires grounded. Adjust for monster tires / six-wheeled Rover / heavy machinery where required.
- Standard mirrors can be omitted if they hurt scale readability. Chassis can be simplified. Roof accessories must be thick enough to read from the in-game camera.
- Meaningfully distinct **custom build geometry** per body. Color or decal changes alone do NOT count as the second physical build.

## Textures and materials
- Print: racing stripes, car number, headlights, taillights, grilles, mouth/teeth/eyes, food logo, brand text, rescue markings and patterns are UV-mapped 2D image textures.
- Geometry: actual physical fins, big horns, eyes as raised domes if necessary, roof food prop, siren block, spoiler, crane, wheels, cargo box, blister, printed card as a physical flat slab.
- Car source artwork: vector/SVG/PNG authored independently and UVs from the REAL modeled surfaces, text never mirrored. Use 1024² albedo as a practical pilot target; master artwork can be higher resolution. Material colors as solid palettes can replace texture where there are no printed details.
- **No** fake shadows, lighting streaks, glints baked on car UV paint texture. Let lighting/materials handle them.

## Visual pass/fail
- Black silhouette test: all 5 within a family can be differentiated, with two edits max for clearly similar shapes before escalating to review.
- Unlit albedo test: hue and print remain crisp without expensive lighting.
- Gray clay test: printed numbers/words/eyes/stripes/headlight graphics disappear entirely; physical silhouette remains valid.
- Small-size test: viewed at 140px width, each family and primary silhouette still reads clearly.
- Family comparative sheet approved for shape variety and coherent palette BEFORE producing 100s of versions.

## Do not recreate earlier failed realistic car
Never use smooth wedge-box bodies with high unsupported wings, unrealistic real-car attempts, paper thin tires, or randomly scattered automotive details to mimic realism. Match the supplied cartoon toy style instead.
