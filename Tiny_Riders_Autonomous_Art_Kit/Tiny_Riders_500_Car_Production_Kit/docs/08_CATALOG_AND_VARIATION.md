# 08 — Catalog Data Contract

## Stable formula
10 families × 5 authored shape recipes × 2 physical build configurations (`standard`, `custom`) × 5 livery designs (`CORE`, `SUNSET`, `CLOUD`, `NIGHT`, `CHASE`) = **500 unique products**.

`catalog/family_specs.json` maps all ten families to the exact source-board filename, palette and five base names. `catalog/bodies_50.json` describes each silhouette and how its `custom` build physically differs. `catalog/collectibles_500.json` is the authoritative final product registry; `collectibles_500.csv` is a convenient spreadsheet-friendly view.

### ID convention
- Base model `TC-01` = Track Club / Blue Racer body.
- Product SKU: `TR-TC-01-S-01` = Track Club Blue Racer standard, Core livery.
- Product SKU: `TR-TC-01-C-05` = Track Club Blue Racer custom, Chase livery.
- `collectionNumber` is unique **1–500**, separate from in-world fictional car race number (e.g. `23`).
- Source references are namespaced by family and remain stable across all generation runs.

### Object schema (production can extend safely)
```
SKU, collectionNumber, displayNumber, familyCode, familyName,
bodyId, modelName, buildId, liveryIndex, liveryName, rarity,
cardTemplateId, bodyApproved, cardApproved, referenceSheet
```

### Rarity distribution starter (not immutable economy design)
Standard: `common, common, uncommon, uncommon, rare`.
Custom: `common, uncommon, rare, epic, chase`.
This creates 500 collectible editions; it does not force pay-to-open packs, randomized monetization or arbitrary grind. If changing rarity distribution later, preserve the SKU count and stable product identifiers.

### Package art derivatives
`source_card_front.svg` + `car_hero_{sku}.png` + `model_data.json` → `package_card_front_{sku}.png` → runtime reduced/atlased preview. The 3D inspected package uses flat output mapped via UV; the package image on the card MUST match actual 3D car.

### What cannot change after approval
The product's base silhouette, construction seed, model name, catalog SKU, approved livery colors, package number. Version asset changes rather than silently overwriting things players have collected.
