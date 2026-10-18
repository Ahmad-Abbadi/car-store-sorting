# CrazyGames SDK configuration

Configured on 2026-10-17 for Basic Launch. No build or upload was performed.

## SDK

- Official Unity SDK **5.35.0**, downloaded from https://sdk.crazygames.com/UnityCrazySDK.unitypackage.
- Version confirmed in `Assets/CrazySDK/Scripts/CrazySDK.cs` and against https://sdk.crazygames.com/UnityChangelog.md.
- Imported package assets are in `Assets/CrazySDK`; the browser bridge is `Assets/Plugins/crazySDK.jslib`. This bridge loads the official JavaScript SDK itself; the existing web host and template need no changes.
- Default `CrazyGamesSettings` retained: no developer whitelisted domains; Editor/localhost support and SDK default sitelock unchanged.
- No ad requests, accounts, or cloud saves have been added. Existing PlayerPrefs persistence is unchanged.

## Integration

- The existing Initializer scene registers `CrazyGamesInitializer` as its first boot system. Its callback completes before the existing main-menu transition.
- Initialization runs through `BaseSystemInitializer`, not a runtime initialization attribute. SDK-unavailable platforms skip initialization successfully. A 15-second timeout returns failure through the existing boot system, whose current `continueOnFailure` setting lets the menu open without platform events.
- A separate persistent integration object watches game states and scene transitions. Calls require SDK availability, completed initialization, and a running SDK.
- Only `Level` and `ThemedSorting` count as gameplay, with an active player, Playing state, no scene transition, and no loading overlay. New scenes wait past their first frame before reporting.
- Pauses, freezes, wins, losses, and transitions stop tracking. Repeated states do not duplicate events. Starts are deferred until LateUpdate to avoid reporting the temporary Playing state inside Restart.
- `LoadingScreen` now clears `SceneLoader.IsLoading` after settling and fade-out, so gameplay tracking does not start behind the loading overlay.
- No focus-change gameplay events are sent, as CrazyGames handles focus separately.
- Start the game through the Initializer scene when testing the integration.

## Verification

- Unity 6000.3.22f1 compilation completed with no errors after SDK import and integration. Existing project warnings and one SDK unused-field warning remain.
- Play mode initialized SDK 5.35.0 and opened MainMenu without reporting gameplay. Frame advancement was confirmed.
- ThemedSorting loaded through the existing loading screen and reported gameplay after loading completed.
- Pause/resume, duplicate pause, freeze/unfreeze, LevelFinished, LevelLost, and restart produced the expected alternating gameplay start/stop events; see `editor-events.log`.
- Test-only scene-loaded callbacks suspended level persistence during gameplay checks. No save format or production persistence behavior changed.
- Loading the original Level from active ThemedSorting emitted gameplay stop. The original Level and subsequent return-to-menu checks could not be completed because the Unity automation main-thread commands timed out during that load. Do not interpret those checks as passing.
- Unsupported native platforms were checked by inspecting the official SDK availability guard and the integration's early return; no native player build was created.
- Browser/localhost execution, CrazyGames Preview, release compression, file size, and upload readiness remain unverified until a build is requested.

## Editor test-session cleanup

The original Level test temporarily left the Editor unresponsive to automation. On retry, automation successfully stopped Play mode and restored the previous Play Mode Start Scene (null/default), then erased the temporary SessionState entry. The original open MainMenu scene remains dirty, preserving its pre-existing unsaved edits. Run In Background returned to false. Gameplay checks suspended persistence through runtime-only callbacks; these disappeared on domain reload. The event log later recorded gameplay start in the original Level, but its remaining lifecycle and return-to-menu checks were not rerun.

References: https://docs.crazygames.com/sdk/intro/ and https://docs.crazygames.com/sdk/game/.
