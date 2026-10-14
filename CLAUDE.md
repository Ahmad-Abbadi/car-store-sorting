# Sorting Game Template — Project Rules

First-person sorting game template (built from Sort'em Toys). Copy this folder to start a new game.

## Template layout

- **Scenes:** `Initializer` → `MainMenu Scene` → `Loading Scene` → `Level` (graybox room, realtime light).
- **Sortable items:** `02_prefabs/Items/Sortable/Base` holds `WorldItem_Base`, `OnShelfItem_Base`,
  `HeldItem_Base`; `Variants/` are prefab variants that only override the mesh.
  Data lives in `03_scriptableObjects/Items Data/Sortable` (`Item_*` + `Catagory_Example`).
  New items must also be added to `ItemDatabase.asset` (save restore).
- **Item meshes** use one palette material (`Items_Mat` + `Items_Palette.png`); UVs pick the color
  cell, so `ItemChunkRenderer` can merge them. `ItemLodSet` is empty until real models exist.
- **Shelves:** `Sorting/Section.prefab` (category + shelves) and `Sorting/Sorting_Shelf.prefab`, grayboxed
  with `mats/Graybox` materials.
- **Upgrades:** 7 example upgrades in `03_scriptableObjects/Upgrades`, opened from the `Upgrade Box`.
- **Localization:** `Localization.csv` (project root) is the master; import it via Tools > Localization.

## Editor tooling

- You have the **Unity CLI** and the **Unity Pipeline package** (`com.unity.pipeline`) available.
- Use them to drive the Editor and Inspector: create and modify GameObjects, edit components,
  create prefabs, and run C# in the live Editor.
- Prefer this over hand-editing `.unity`, `.prefab`, or `.asset` files.

## Writing scripts

- **Don't make assumptions.** If anything about the desired behavior is uncertain, ask before
  implementing.
- Always ask about unclear points (requirements, edge cases, naming, data flow, dependencies)
  rather than guessing.

## Sound system

- **Every new sound goes through `GameplaySounds_SO`.** Never add a raw `AudioClip`/`GameSound` field
  to a gameplay script for SFX.
- `GameplaySounds_SO` is `partial` — add the new `GameSound` (or `List<GameSound>` + `GetRandomX()`
  helper) to the matching section file in
  `Assets/00_Project/01_scripts/Game_Systems/SimpleSound System/GameplaySounds/`
  (e.g. `GameplaySounds_SO.ShelfsSFX.cs`), or create a new `GameplaySounds_SO.<Section>.cs` partial
  with its own `[Header]`.
- If the sound should be pickable from the Inspector, also add it to `GameplaySoundKey` and the
  `Get()` switch in `GameplaySounds_SO.Keys.cs`.
- Fetch the sound from the SO through the audio manager, e.g.
  `audio.PlayJuicyGlobalClip(SimpleAudioManager.instance.GameplaySounds.MySFX);`
  (or `audio.PlayJuicyClip(..., position)` for positional).
