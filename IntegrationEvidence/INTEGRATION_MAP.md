# Technical integration map

The existing `Assets/00_Project/00_Scenes/Level.unity` is a complete continuous car store template: 75 Standard variants, 525 authored copies, model/color shelves, a KCC first person player, mouse/touch input, stash inventory, DOTween pickup/placement, audio, upgrade/economy systems, PlayerPrefs persistence and physics/visual culling.

Reuse the player, input, inventory, pickup flight, placement animation/sounds, shelf occupancy, HUD and LevelSaveManager. Extend inventory to carry physical-copy IDs and extend the existing save payload with themed records. Keep old scenes, IDs, saves, upgrades and source artwork intact.

The existing ThemedCarCatalog and portable registry contain 410 products across 40 themes; production validation reports verify the initial 150 and expansion 260 with their accessory kits. The catalog remains authoritative; sorting data and wrapper prefabs are generated adapters referencing these exact prefabs. Theme display names/palettes/motifs come from theme_recipes.json.

Modify classification from model/color to theme metadata; reject invalid placements before spending inventory; support 18 named markers, mixed variants, multiple shelves per theme, physical progress and unique discovery. Replace the legacy proxy-car culling path in the themed scene because it hides distinguishing accessories.

Add a bounded visible pile over deterministic records, theme shelf bindings, fit checks, content availability and theme collection progress. Use a separate themed scene and save key to preserve legacy player progress. No level completion/reset sequence is attached to the themed collection.

Fit dependency: source themed cars are approximately 2–4 metres versus 0.464 × 0.54 × 0.648 metre original shelf clearance. On 2026-10-15 the user explicitly approved a single fixed uniform shelf/world scale and corresponding player/room calibration. Cars retain source scale, materials, accessory transforms, and supplier references. Final fixed scale is determined from measured bounds of all verified products and recorded in the audit evidence.
