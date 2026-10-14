# Remove Missing Scripts

A tiny, self-contained Unity **editor** tool that removes the "missing script" component slots
left on prefabs and scene objects after their MonoBehaviour scripts get deleted.

It uses Unity's own safe API (`GameObjectUtility.RemoveMonoBehavioursWithMissingScript`), so
prefab-instance and stripped-component structures are handled correctly — no hand-editing of
prefab/scene YAML.

## Install

Copy the whole `RemoveMissingScripts` folder into your project's `Assets/`. That's it — the
included Editor-only assembly definition keeps the code out of your runtime build, so nothing
ships with your game.

## Use

Menu bar:

- **IrahimzTools > Cleanup > Remove Missing Scripts In All Prefabs** — scans every prefab in the
  project, strips missing-script slots, saves the ones it changed, and logs a per-prefab count.
- **IrahimzTools > Cleanup > Remove Missing Scripts In Open Scene(s)** — same, for whatever scene(s)
  are currently open.

Check the Console for a summary of how many components were removed.

## What it does *not* do

It removes the empty **component** slots only. It will not:

- delete the now-purposeless **GameObjects** that used to hold those scripts, or
- clear leftover **null / "None" entries** in serialized lists (e.g. a manager's behaviour list)
  that referenced the removed components.

Do those by hand afterwards if you want a spotless hierarchy.

## Requirements

Unity 2019.3+ (uses `PrefabUtility.LoadPrefabContents` / `SaveAsPrefabAsset`).
