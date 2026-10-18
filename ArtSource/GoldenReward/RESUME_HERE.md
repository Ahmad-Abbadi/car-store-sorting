# Completed golden reward art

Production is complete on 2026-10-17. No remaining authorized art work or running background render is required.

Start with HANDOFF.md and QA_REPORT.md. Primary prefabs, optional accessories and filled preset are in Assets/TinyRiders/Art/GoldenReward/Prefabs. Empty and filled Unity previews are in PreviewScenes. Editable Blender sources, artwork, scripts and images are under ArtSource/GoldenReward.

The actual Unity audit passed 34 checks, zero failures. It verifies references, UVs, identity roots, four wheel pivots with correct left/right labels, +Z forward, ground contact, full-turn cover/platter clearance, independent cover/accents/anchor and empty/filled hierarchy. See unity_verification.txt and unity_audit.txt. QA_ContactSheet.jpg includes 24 inspected delivery views; the supplier audit is an additional PNG.

The importer is repeatable and uses only the GoldenReward subtree. It creates/closes an additive preview scene and restores the previous active scene. MainMenu Scene was left clean. Do not run the importer during another chat's Unity editing/testing window. Source supplier cars, shelves, catalog and gameplay systems are outside this task's scope.

Reproduction: make_artwork.py, Blender build_reward.py, render_reward.py (-- initial and -- final), save_sources.py, Unity ImportGoldenReward.Main, VerifyGoldenReward.Main, make_contact_sheet.py. Blender rendering is capped at eight CPU threads. No external model services or downloads were used.

The current gameplay installation is the GRAND CEREMONY in Level: a 5× station/car on a 3m-wide low base at (500,0,-7.75), near the counter, with no placard or reward text. GoldenRewardCeremony supplies a 90-second, six-phase energy reveal, car rise/full turn, camera shots, synchronized existing audio, and nine owned Polygon Arsenal URP effect variants. Glass remains visible. StoreCompleted and correct-sorting rules are unchanged; completed saves show the settled car immediately, and resets/replay empty the station. The updated pile protects the exhibit walkway and older obstructing loose-car saves. See IntegrationEvidence/GoldenReward/README.md, grand-inspection.json, grand-tests-passed.xml, and GrandCeremony/ captures. Earlier four-second/small-installation evidence is historical. Browser/device performance has not been measured.
