# Initial release completion — 2026-10-14

The purchased `ithappy/Modular_Racing_Cars` package provides 15 complete cars (`Car_01`–`Car_15`). Each now has ten visibly different finished paints: five warm-to-green `STD` looks and five teal-to-pink `CUS` looks. All **150 permanent IDs** are preserved; changed products received higher `contentRevision` values. There are **0 missing or blocked products**.

| Result | Count |
| --- | ---: |
| Supported base cars | 15 |
| Supported physical builds | 30 (2 per car) |
| Finished variant prefabs | 150 |
| Variant definition assets | 150 |
| Transparent Sprite thumbnails | 150 |
| Initial-release catalog entries | 150 |
| Static lineup prefab instances | 150 |
| Base-car contact sheets, plus overview | 15 + 1 |
| Custom builds with supplied alternate wheels | 14 |

Every custom build uses verified same-car bumper and pipe alternatives and disables the supplied spoiler. Fourteen use `Wheel_16` at their existing mounts. `C006` retains its wider supplier wheels after rejecting the narrower alternate. `Car_02` retains its original headlight combination; the other 14 custom builds swap headlights. `compatibility.csv` records each choice. No new geometry or runtime customization was added.

Unity 6000.3.22f1 compiled the final scripts with **0 errors and 0 warnings**. The saved final prefabs passed live technical validation, and `Validate Initial Release` published 150 direct references. The 15 contact sheets and overview were reviewed for ten color differences at thumbnail size. Front, side, and rear QA renders of one custom product per car were reviewed for wheel seating and accessory clearance. This visual review represents the supplied lighting and camera setup; final gameplay lighting and animation remain outside this static art pass.

`verify_release.py` passed the 150 ID, prefab, definition, icon, release, lineup, and ten-preview uniqueness checks. All 1,292 supplier files match the saved SHA-256 snapshot. One supplier `Jacket.mat` was automatically rewritten by Unity while the project was open; it was restored byte-for-byte from the purchased `.unitypackage` to its recorded snapshot hash. The rewritten copy is preserved in `Reports/QA/Jacket_after_unity_import.mat.txt`. A no-change regeneration pass altered none of **1,554** tracked generated files or metadata files and added no IDs or assets. The future `C016` fixture preserved all 150 current identity, name, prefab, and thumbnail bindings. Machine-readable results are in `release_integrity.json` and `idempotency_result.json`.

The previously open lineup scene had unsaved edits and was copied to `Assets/TinyRiders/Scenes/ArtPreview/lineup_before_variant_refresh.unity` before the final 150-car lineup was rebuilt and reopened. Keep the purchased source package installed when integrating these products. Gameplay, shop, inventory, and save systems remain outside this collection.
