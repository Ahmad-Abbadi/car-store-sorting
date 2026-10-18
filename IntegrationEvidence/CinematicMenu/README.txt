Cinematic Store Main Menu

The menu uses six authored store views in shuffled rounds, with random starts
and no consecutive repeats across round boundaries. Shots last eight seconds
with two-second eased Cinemachine blends and subtle movement.
Play uses the existing Loading Scene and loads Level.

Authoring
Select Cinematic Cameras to edit the MenuCinematicTour shot list and blend time.
Edit Start Pose and End Pose objects to adjust framing and movement.
Tools > Tiny Riders > Cinematic Menu > Rebuild From Store rebuilds the staging
and menu, and resets authored poses and styling.

Verification
CinematicShuffleTests checks empty, one-, two- and six-view shuffle invariants.
CinematicMenuTests checks controls, scene routing and cinematic cleanup.
Navigation tests isolate physics and restore the previous setting afterward.
