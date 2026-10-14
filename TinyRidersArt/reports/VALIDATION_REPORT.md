# Validation report

Generated 2026-10-13T20:14:05.894091+00:00

## File and image validation

- base_blends: 50
- physical_builds: 100
- car_products: 500
- package_products: 500
- car_prints: 500
- front_cards: 500
- back_cards: 500
- car_renders: 500
- package_renders: 500
- unity_car_prefabs: 500
- unity_package_prefabs: 500
- contact_sheets: 33
- clay_renders: 11

- Duplicate car texture groups: 0
- Suspicious silhouette pairs (IoU > 0.92): 1

## Visual inspection

Reviewed the 50-base contact sheet, complete car/package overviews, selected family sheets, the blue #23 pilot, package front/back artwork, clay renders, and Unity captures. The one remaining numerical silhouette flag is SS-05 vs CR-02; the doughnut roof and turtle shell/face are clearly different in their actual renders.

## Blender geometry audit

Pass: True; failed builds: 0.

## Unity validation

Connected Editor audit 2026-10-13T20:11:41.5929598Z: 500 car prefabs, 500 package prefabs, 7 scenes, 0 failures.

The connected Unity Editor audit checked every SKU's loose prefab print, packaged prefab front/back prints, card UV0, and enclosed toy placement. It also checked ten environment sign materials and seven preview scenes. Visual captures show the loose car, package front/back, and sorting room.

## Most similar base silhouettes

- SS-05 vs CR-02: IoU 0.936
