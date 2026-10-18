# Grand golden reward ceremony

Current Level installation: five times the original art scale, a 3m-wide/0.2m-high base at (500,0,-7.75), no reward text, and a 90-second golden energy ceremony. The car is 1.757m long; the station is 2.705m wide. Both remain connected nested prefab instances. The glass stays active with a scene-only Fresnel edge material. A separate pivot carries the car's 0.32m rise and full rotation.

Six phases cover awakening (0–12s), gathering (12–28s), manifestation (28–45s), the floating turn (45–62s), settling (62–78s), and finale/return (78–90s). There is no skip control. Camera approach and return sweep around the cover. Nine project-owned Polygon Arsenal variants supply motes, streams, spirals, beam, rings, manifestation/landing bursts, and two fireworks cues. Only quiet motes and restrained lights persist afterward. Existing Sweep/Complete audio respects SFX mute.

The cached Polygon Arsenal 2.07 package and its bundled Unity 6 URP upgrade were imported. A legacy PolySolidIntense material left by the publisher's upgrade is replaced only in project variants. The player's/reveal cameras request depth textures for soft particles; no global pipeline settings changed. Publisher demo scripts, audio, and lights are removed from owned variants.

Existing Complete(bool), ResetDisplay(), correct-sorting rules, and StoreCompleted remain authoritative. Fresh/partial saves stay empty, completed reloads show the settled car immediately, and replay/reset clear the display. There is no save-format or catalog change. A pile exclusion protects the exhibit/walkway; obstructing older saved loose cars recover to safe authored poses.

Current evidence:

- GrandCeremony/: actual gameplay-camera captures of empty, gathering, rising, rotating, settling, finale, settled, and completed reload states.
- GrandCeremony/full-run.txt: real 90-second reveal, advancing frames, phase times, and camera collision checks.
- grand-inspection.json: dimensions, contact, fixture clearance, sampled camera path, prefab references, and URP materials.
- grand-tests-passed.xml: latest completion/reward suite result. Lifecycle tests use four seconds; the full-length visual test uses 90 seconds.

Tests isolate save keys and restore audio/upgrade preferences. Editor frame timing includes synchronous screenshot stalls and is not a device benchmark. No standalone/browser build was produced or profiled.

## Historical small installation (superseded)

Completed 2026-10-17 in Unity 6000.3.22f1 with the existing URP setup.

The new display and golden car are installed in Level at the existing reward location (501.10, 0, -10.80). The raised pedestal, collider, placard and spotlight are retained. The old wedge, cloth, rim and pad are replaced by the authored prefab instances. The station sits at the pedestal top (approximately Y=0.96); the car is under Anchor_Car at unit scale. Carpet and ropes are not installed on this raised base.

GoldenCarReveal keeps its existing completion/reset interface, duration and camera/control ownership. The new keepCoverVisible flag is true for this installation; revealFieldOfView is 35 degrees. The glass remains fixed and visible before, during and after the reveal. Legacy cloth behavior remains the default for other configurations. CarStoreUI and LevelSaveManager still own completion and persistence; no new counter, catalog item or save schema was introduced.

## Verification

All 8 tests in tests-passed.xml passed:

- Fresh 1200-car completion, repeated completion, four-second reveal, saved reload and replay.
- Incomplete save containing misplaced and held cars; an empty visible pile remains incomplete.
- Final normal shelf placement triggers the reward; disabling the controller restores camera and movement.
- Pause during reveal is preserved; scene reload restores the earned reward without replaying the cinematic.
- Mixed sorting states, insufficient capacity, completed-collection reset and mixed-state reset retain their existing guarantees.

Tests use TinyRiders.DebugSortAll.QA save keys. Replay tests back up and restore upgrade preferences. Temporary save keys were removed, Cinemachine SaveDuringPlay was restored to true, and Unity was returned to Edit mode on the original clean MainMenu Scene.

installation.txt confirms the installer is repeatable: the root, pedestal, collider, placard, position and child count survive a second run, with exactly one reward controller. gameplay-inspection.txt records live frame advancement, empty-state visibility, pedestal contact and three player-capsule clearance queries (front, left and right), all clear.

The scene-preservation audit found no removed objects outside the old reward hierarchy. Unity also normalized four unrelated TextMeshPro serialized color-cache blocks (m_fontColor32); their authored floating-point font colors and component wiring are unchanged. The pre-integration scene text and pre-existing Git diff are retained for comparison.

The first test runs exposed a stale zero-offset shelf assertion and a restore timing assumption. Assertions now use the existing fitted shelf pose with a 0.5 mm world-space tolerance at world X=500, and saved-reward checks wait for UI restoration. Shelf placement production code was not changed. The final complete suite passes with no skipped tests.

## Images

- reveal-in-game.png: actual game-view capture from the real reveal camera after earning the reward.
- empty-in-game.png: close view from a temporary inspection camera in the running Level before completion.
- shop-context.png: running-game view showing the existing raised pedestal and collector-counter location.

No standalone game build was created. Runtime performance across target devices was not measured.

Final validation: all 10 tests passed (zero failures/skips). The final reveal ran for 90.0018 seconds with 12,027 advancing frames and zero camera collision frames. QA keys were removed; background running and Cinemachine SaveDuringPlay were restored. Level is saved and left open in Edit mode with the exhibit selected. The native Unity console reported zero current errors.

