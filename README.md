# Toy Car Sort

A Unity car-store sorting game project, including game assets, source art, production kits, and localization data.

## Open the project

1. Clone this repository.
2. Install Unity **6000.3.22f1** through Unity Hub.
3. In Unity Hub, choose **Add project from disk** and select the cloned repository folder.
4. Open the project and allow Unity to restore packages and import assets.

## Project contents

- `Assets/`: game scripts, scenes, prefabs, textures, and other Unity assets with their `.meta` files.
- `Packages/` and `ProjectSettings/`: package dependencies and Unity configuration.
- `ArtSource/`, `TinyRiders*`, and `Tiny_Riders_*`: source art and production resources.
- `Localization.csv`: master localization data.

Unity caches, build output, logs, local settings, and repair backups are excluded from version control and regenerated locally as needed.

## Car rendering

Web builds incrementally bake compatible opaque car parts with **Tools > Toy Car Sort > Rendering > Bake Exact Car Batches**.
Generated visuals and shared meshes live under `Assets/TinyRiders/Gameplay/Rendering/CarBatches`;
edit the original car art and rebuild instead of editing this cache. Glass, decals, incompatible parts,
all three existing mesh detail levels, and gameplay identities are retained. Original art is unchanged.
After geometry edits, run **Tools > Items > Bake Item LODs** before rebaking the car batches.
**Restore Authored Car Visuals** switches the gameplay wrappers back for comparisons.

`CarVisualBatchTests` checks the collection's geometry and transparent sorting. Bake and validation
reports are written to `IntegrationEvidence/CarBatching`. Local diagnostics are opt-in with
`http://localhost:PORT/?carBenchmark=1&scenario=pile` (or `scenario=shelves`); they isolate save keys
and control the camera only for that run. Normal URLs and remote hosts do not activate diagnostics.
The shelves scenario stress-tests populated shelves, including wrong-placement feedback.

## ByteBrew analytics (WebGL)

Toy Car Sort's Web app has been created in ByteBrew and its Game ID and SDK Key
are configured in the Unity settings asset. The connected WebGL output is
`Builds/ByteBrewConnected`. Rebuild after changing credentials.
The connection was verified on 2026-10-18 using the bundled Web SDK: ByteBrew
accepted `bytebrew_connection_check`, and its dashboard showed the first test user/session.

The official ByteBrew Unity SDK **0.2.4** is vendored in `Assets/ByteBrewSDK`, from
commit `187e4ebeae557196639f027cf38d354ae5f75e44` of
[ByteBrewUnitySDK](https://github.com/ByteBrewIO/ByteBrewUnitySDK).
The upstream MIT license is included there. Mobile settings are disabled.
Local Editor fix: the SDK settings handler resolves the existing asset if its delayed
initialization has not run, keeping the settings menu usable after automated imports.

1. Create an account at the [ByteBrew dashboard](https://dashboard.bytebrew.io/) and add a **web game** for Toy Car Sort.
2. Open the app's Game Settings and copy its Web Game ID and SDK Key.
3. In Unity, choose **Window > ByteBrew > Select ByteBrew settings**. Web is already enabled; fill in **Web Game ID** and **Web Game SDK Key** and save.
4. Build and host WebGL starting from `Initializer`. Verify sessions and custom events in the ByteBrew dashboard. See the [official setup guide](https://docs.bytebrew.io/sdk/unity).

The startup scene contains one persistent ByteBrew object with `ByteBrewInitializer`
and `GameAnalytics`. Missing keys skip initialization. Startup never waits for SDK
network readiness; events before readiness or while unavailable are dropped without
a custom offline queue. The Editor and non-WebGL builds never initialize analytics.
For local inspection, enable **Log Events In Editor** on the startup ByteBrew object's
`GameAnalytics` component and start from `Initializer`.

Tracked events: `level_started` / `level_completed` (`level`, `scene`),
`tutorial_started` / `tutorial_completed` / `tutorial_skipped` (`scene`),
`tutorial_step` (`step`, `scene`), `quest_completed` (`quest_id`, `scene`), and
`collector_rank_up` (`rank`, `scene`). Rank values use the game's zero-based rank index.
Tutorial steps are reported once per attempt. Restored quest progress is not replayed.
Direct store entry from the menu/tutorial and new store completions are tracked too;
loading an already completed store does not emit a completion event.
Parameter values replace punctuation with underscores to match ByteBrew's format.
Automatic sessions come from the SDK; ads, purchases, push, and remote configs are not initialized.

**Tools > Toy Car Sort > Analytics > Configure ByteBrew** repairs startup wiring
without replacing credentials. After an SDK update, verify the settings and keys again.
Use the ByteBrew dashboard to verify live sessions and events after launching a hosted build.
