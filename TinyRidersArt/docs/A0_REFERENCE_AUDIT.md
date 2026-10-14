# Tiny Riders — Phase A0 reference audit

**Status:** Ready for owner review. This is a reference and production-planning pass only. The workspace already contains an A1 Blue Racer pilot from earlier work; this A0 pass did not modify or approve it, create package assets, or import art into Unity.

## Sources actually inspected

- [All ten sheets gallery](../../Tiny_Riders_500_Car_Production_Kit/references/ALL_TEN_SHEETS_GALLERY.png), followed by all ten full-size PNGs in [original_boards](../../Tiny_Riders_500_Car_Production_Kit/references/original_boards/).
- All ten close-up PNGs in [featured_cars](../../Tiny_Riders_500_Car_Production_Kit/references/featured_cars/), including the [Blue Racer #23 crop](../../Tiny_Riders_500_Car_Production_Kit/references/featured_cars/02_track_club.png).
- [Locked front layout guide](../../Tiny_Riders_500_Car_Production_Kit/design/packaging/MASTER_FRONT_LAYOUT_GUIDE.png), [all-family front/back preview](../../Tiny_Riders_500_Car_Production_Kit/design/packaging/packaging_all_10_families_preview.png), the standalone [logo](../../Tiny_Riders_500_Car_Production_Kit/design/brand/tiny_riders_logo.png), and full-size TC front/back, CC front, and CR front template PNGs.
- Kit specifications: [art bible](../../Tiny_Riders_500_Car_Production_Kit/docs/01_ART_BIBLE.md), [packaging lock](../../Tiny_Riders_500_Car_Production_Kit/docs/03_PACKAGING_MASTER_LOCK.md), [geometry/texture contract](../../Tiny_Riders_500_Car_Production_Kit/docs/04_GEOMETRY_TEXTURE_CONTRACT.md), [family directives](../../Tiny_Riders_500_Car_Production_Kit/docs/10_FAMILY_DIRECTIVES.md), [50-body catalog](../../Tiny_Riders_500_Car_Production_Kit/catalog/bodies_50.json), and [family specifications](../../Tiny_Riders_500_Car_Production_Kit/catalog/family_specs.json).

Each original sheet is about 2.1–2.3 MB. The kit also contains ten packaging concept crops and ten editable front/back SVG pairs. The rendered template PNGs are previews, not finished packaging artwork.

## Master vehicle style

The Track Club Blue Racer is the shape anchor: a short, broad blue coupe; a simple rectangular nose; slanted dark opaque cabin; oversized thick tires; substantial rear wing; clean broad bevels; and a white stripe plus race number **23**. Number and stripe are printed texture artwork. The race number is independent of the product's collection number (the catalog starts TC-01 at **051/500**).

Across the ten sheets, the shared language is squat toy proportions, large low-sided wheels, few sturdy masses, strong readable silhouettes, saturated flat color regions, and large family accessories. Windows are opaque dark patches on real cabin surfaces. Body, cabin, tires, wings, fins, roof props, work equipment, card, and blister use geometry. Numbers, words, logos, stripes, faces, lamps, and package print use UV-mapped 2D artwork. Satin plastic and actual lighting create shine; no modeled or painted false reflections.

The boards are polished perspective concepts, not orthographic geometry or ready-to-print maps. A1 must test the Blue Racer in gray clay, silhouette, side, top, rear, wireframe, UV, and beauty views before any expansion.

## One locked packaging system

The lower-left Track Club package concept is the composition reference. The measured [packaging lock](../../Tiny_Riders_500_Car_Production_Kit/docs/03_PACKAGING_MASTER_LOCK.md) and [layout guide](../../Tiny_Riders_500_Car_Production_Kit/design/packaging/MASTER_FRONT_LAYOUT_GUIDE.png) are the production contract wherever concept cards differ.

| Fixed element | Master specification |
| --- | --- |
| Card and source art | 120 × 180 mm, about 0.7 mm thick; 1200 × 1800 px front and independent readable back; rounded corners |
| Hanger | Physical centered cutout at x=600, y=45–105 px, about 135 px wide |
| Brand | One `TINY RIDERS` logo asset in x=100–1080, y=135–340 px; same scale and anchor across families |
| Family bar | x=100–620, y=350–430 px; label and color vary within a fixed position |
| Printed hero | x=90–1110, y=460–965 px; image rendered from the exact approved 3D car/build/livery |
| Clear blister | Physical shared cavity and flange projected at x=86–1114, y=975–1515 px; only depth may adapt |
| Footer and number | Name at x=90–950, y=1542–1700 px; unique catalog number at x=955–1138, y=1560–1700 px |

Keep the background motif, family colors, name, and number as controlled variables. Do not move the card regions, change the logo, place a second printed car behind the blister, or use the guide PNG as a card texture. The existing SVG/PNG templates deliberately leave the hero area empty until the actual approved car exists. Their blister image is a preview of placement; A2 must build the real clear blister.

## Conflicts and items to resolve at the relevant art gate

1. **Board packaging varies.** The ten generated concept sheets use different logo treatments, footer hierarchy, and numbers. Use the measured universal Track Club-derived grid and the supplied original logo; family sheets provide palette and scenery only. The front template PNGs show a blank top edge, so the hanger must be cut in the A2 card mesh at the locked location rather than inferred from the preview artwork.
2. **TC-04 Rocket differs in type.** The Track Club sheet depicts a boxy olive vehicle with roof lamps, while `bodies_50.json` specifies a long low dragster. The catalog currently has higher authority in the kit, so the build plan treats Rocket as a dragster. This deviation needs explicit visual sign-off before TC-04 modeling.
3. **CC-05 Loop differs in type.** The City Crew sheet shows a small purple car with a large rear loop, while the catalog specifies a tall microbus. Use the catalog silhouette pending owner review of this discrepancy.
4. **Hero accessories can blur standard/custom.** Diner's roof sign, Pebble's roof lamps, Crusher's roof lamps, Flash's beacon, and Scoop's large food topping appear on the featured sheet cars, while the catalog assigns related equipment to custom builds. Define the exact standard and custom geometry for each before family production; keep the featured visual identity without making both builds indistinguishable.
5. **CR banner preview overflows.** In the full-size Creature Cruisers front template, `CREATURE CRUISERS` extends beyond the fixed family ribbon. Correct the typography *inside the locked banner* in A2; do not widen or move the banner.
6. **Concept lettering is unreliable.** Use the catalog names, stable SKU numbers, and real 2D typography. Do not trace AI-rendered microcopy, barcodes, or vehicle numbers as authoritative print art.

## Catalog and file checks

`python tools/validate_catalog.py` from the kit returned `passed: true` again during this review, with 10 families, 50 bases, 100 planned physical builds, 500 planned collectibles, ten original boards, and no errors. A separate earlier field-by-field check found **0 mismatches** across all 500 CSV rows and their matching JSON objects. There are 500 unique SKUs and exactly one collection number each from 1 through 500. These checks validate the *plan and source records*, not finished assets.

The [A0 file inventory](../qa/A0_FILE_INVENTORY.json) records paths and byte sizes for 54 relevant PNG files: ten boards, ten featured crops, ten packaging crops, twenty template previews, the gallery, layout guide, all-family template preview, and logo. The validator's exact output is saved in the kit's [kit_validation_report.json](../../Tiny_Riders_500_Car_Production_Kit/progress/kit_validation_report.json).

Earlier tool intake recorded a Blender MCP connection failure and no `blender` executable on `PATH`; it also found a Unity launcher but no live Editor. Those environment observations were **not retested** in this A0 review and should be checked at the start of any future modeling/import phase. They do not affect the reference audit.

## Phase A0 review conclusion

The source set is present and sufficient for the Blue Racer pilot after approval. The main production risks are concept-versus-catalog body conflicts, featured-car accessory assignments, and template lettering fit. A Blue Racer pilot already exists in the workspace but is outside this A0 approval; assess it separately in A1. Stop here for A0 review. Packaging belongs to A2, and Unity art import belongs to A7.
