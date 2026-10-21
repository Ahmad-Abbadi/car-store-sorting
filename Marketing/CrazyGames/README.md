# Toy Car Sort — CrazyGames preview package

Upload the two silent MP4 files and three PNG covers from `Deliverables/`.
The footage shows real pickup, held-car, shelf-placement, and completion animations.
Pirate, space, and wizard collections are featured, followed by a completed-display reveal.
The scenes are staged for marketing: existing cars are selected and nearby shelves are
filled before recording. The gameplay itself is captured at normal simulation speed.

## Files

- `Deliverables/`: landscape and portrait previews; landscape, portrait, square covers.
- `SourceClips/`: clean 20-second gameplay masters without cover transitions.
- `Frames/`: original 30 fps PNG sequences and action/placement logs.
- `QA/`: sampled final-video frames and contact sheets.
- `validation.json`: dimensions, duration, frame count, size, audio, decode checks.
- `Backup/`: pre-capture preferences and restoration verification; do not upload.

## Reproduce

Use Unity 6000.3.22f1 with this project's existing dependencies. The editor-only
runner is `Assets/TinyRiders/Gameplay/Editor/CrazyGamesCapture.cs`.

1. Save the current scene and exit Play mode. Enable Unity Pipeline auto-tick.
2. Evaluate `CrazyGamesCapture.Begin()` once. This backs up preferences, remembers
   the active scene, opens Level, and enters an isolated Play session.
3. Wait for `ThemedCollectionSession.Instance.IsReady`, then evaluate
   `CrazyGamesCapture.StartTake("landscape")`. Wait for `status.txt` to say Complete.
4. Exit and re-enter Play mode (do not call Begin again). Run
   `CrazyGamesCapture.StartTake("portrait")` and wait for completion.
5. With the final display still visible, set each cover size using
   `CrazyGamesCapture.SetSize(width,height)`, allow the Game view to update,
   and evaluate the body in `tools/cover.cs`. Capture the Game view to the
   corresponding `Deliverables/cover-*.png` file. Sizes: 1920×1080, 800×1200, 800×800.
   The cover uses the existing logo and rendered game models, with no AI redraw.
6. Exit Play mode and evaluate `CrazyGamesCapture.Restore()`. Require
   `Backup/restoration.json` to report `playerPrefsRestored: true`.
7. Run `tools/encode.py` using Python with Pillow. FFmpeg is provided locally
   by `tools/python/imageio_ffmpeg/binaries/`. Reinstall if needed with
   `python -m pip install --target tools/python imageio-ffmpeg` from this directory.

The encoder produces H.264/yuv420p MP4 with fast-start metadata, no audio stream,
600 frames at 30 fps, and brief cover dissolves at the loop boundary. Review both
full videos in a video player and the generated contact sheets before upload.

## Recovery

If recording is interrupted, exit Play mode and call `CrazyGamesCapture.Restore()`
before starting another session. Never overwrite the backup by calling Begin while
an earlier session is active. Capture staging is runtime-only; authored scenes are
not saved by this tool. Existing unrelated project edits are left intact.

Requirements checked against https://docs.crazygames.com/requirements/game-covers/ .
