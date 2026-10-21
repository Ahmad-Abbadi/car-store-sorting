# CrazyGames desktop release

Use Unity 6000.3.22f1 with the WebGL module. Exit Play mode and preserve any unsaved scene work before building.

Run **Tools > Toy Car Sort > CrazyGames > Build Desktop Release**, or use the connected Editor:

```powershell
$releaseProject = (Get-Location).Path
unity command eval --project-path $releaseProject --caller plugin --skill unity-cli --timeout 3600 --format json -- --timeout 3600000 --code 'CrazyGamesReleaseBuild.Build(); return "Release build finished";'
python IntegrationEvidence/CrazyGamesRelease/package_release.py
```

The build includes enabled scenes, requires Initializer first and Tutorial present, and writes to `Builds/CrazyGamesRelease`. It applies WebGL-only compressed texture overrides without modifying original PNGs or other platform imports. Existing smaller texture caps are retained. The profile uses Brotli, DXT desktop textures, IL2CPP size optimization, high managed stripping, explicit exception support, and no development/debug/profiler flags. Per-machine LTO settings are reapplied on every build.

The two selected background music tracks receive WebGL-only quality 0.60 overrides. Unity's translated WebGL AAC format, stereo channels, sample rates, and load type are preserved; default and other platform settings and source WAVs remain unchanged.

Mesh compression is applied temporarily for the build. Source mesh assets and model metadata are backed up first and restored with checksum verification afterward, including when a build fails. Backups remain locally for recovery. If Unity exits mid-build, run **Tools > Toy Car Sort > CrazyGames > Restore Temporary Mesh Imports** before continuing. The recovery menu restores the recorded original files; inspect any intervening edits before using it.

`build-report.json` records the result and asset sizes. Packaging validates a conservative 50,000,000-byte cap on the entire upload and fewer than 1,500 files before creating `Builds/Toy-Car-Sort-CrazyGames-Desktop.zip`; it records size and SHA-256 in `package-validation.json`.

All build errors remain in the report. Only FMOD import failures from the stock audio libraries are classified as non-blocking when the failing clip is absent from the current scene/Resources dependency snapshot. Unknown errors and failures for referenced audio block packaging. The game's selected audio is stored separately under `Assets/00_Project/Audio/PackageSelection`.

Packaging also creates a checksum-verified `Builds/CrazyGamesPortal-<hash>` directory with only Build and StreamingAssets for the Unity 6 portal picker; its exact path is recorded in `package-validation.json`. Keep the root ZIP for HTML5 hosts. Verify gameplay and persistence in the actual portal loader because it supplies its own HTML page.

Marketing files are in `Marketing/CrazyGames/Deliverables`. The similarly named ZIP there is marketing media, not the playable game.

Submission is only complete when portal review-submission status is visibly confirmed. Build/package success alone does not confirm browser QA or publication.
