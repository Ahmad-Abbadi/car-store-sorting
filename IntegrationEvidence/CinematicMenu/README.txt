Cinematic Store Main Menu

Open Assets/00_Project/00_Scenes/MainMenu Scene.unity and enter Play mode.
The six shots loop in order over 48 seconds, with two-second eased blends.
Play uses the existing Loading Scene and loads Level.

Authoring
- Select Cinematic Cameras to edit the MenuCinematicTour shot list and blend time.
- Each shot has Start Pose and End Pose objects in world space. Move and rotate
  those objects to change the framing and camera movement; adjust the virtual
  camera Lens for field of view.
- Cinematic Store contains a visual-only snapshot. It does not read player saves
  or run sorting gameplay. Changes to the gameplay store are not copied live.
- Rebuild from the current Level using Tools > Tiny Riders > Cinematic Menu >
  Rebuild From Store. Rebuilding resets authored shot poses and menu styling.
- MainMenu_before_cinematics.unity is the original menu backup.

Verification
Run the EditMode test CinematicMenuTests.Tour_MenuControls_LoadingAndReturn.
It enters Play mode, checks first-run language selection, observes two complete
loops and all six moving shots, checks menu controls, captures 16:9, 16:10 and
ultrawide layouts, loads Level, checks cinematic cleanup, and returns to the menu.
Gameplay persistence uses the project's existing isolated QA save keys.
Screenshots and verification.txt are generated here by that test.
