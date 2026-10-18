PREMIUM COLLECTOR NOTICEBOARD

Rebuild: Tiny Riders > Quests > Rebuild Premium Noticeboard Only.
The base quest, polish and scrapbook builders delegate their board construction
to PremiumCollectorBoardBuilder. Full quest-system builds preserve the position
of an existing board; initial placement is applied only when creating one.

The existing CollectorQuestBoard prefab GUID, root interaction collider/layer,
scene pose and interfaceUI binding are retained. Three original text objects
are reused. There are no quest-definition, reward or save-format changes.

The stand is authored by the editor builder with rounded extrusions and front/
rear bevels. The car emblem is authored in Blender with an integrated coupe
silhouette, windows, wheel arches, tires, hubs, lights and a fine rolled rim.
Its editable source is Blender/CarBadge.blend; regenerate with Blender in
background mode and build_car_badge.py. CarBadge.mesh.json preserves evaluated
split normals and maps Blender coordinates explicitly into Unity coordinates.
The board builder combines those parts into the existing material batches.
Meshes combine by material; only the reward medallion is animated.
Static geometry: 37,902 triangles including the inactive reward badge. The full
hierarchy has 29 renderers including twelve text labels and three photographs.
Wood, cork and paper maps are deterministic 512px textures with mipmaps and
matching normal maps. Existing car preview textures are referenced unchanged.
Tiny printed details do not cast shadows; the wooden stand still casts its
ground shadow. This avoids jagged shadows over labels at showroom resolution.

Card captions reuse the three translated parts of quest.board.subtitle.
Collection names use existing theme localization keys. English and Arabic are
verified; the title reserves a separate area clear of the car emblem.

Review helpers (run-script, no permanent runtime QA components):
- BoardReview.Capture(stage): scene views under both lighting presets, restoring
  exact lighting values afterward; outputs to IntegrationEvidence/CollectorBoard.
- BoardReview.Front(name): single front capture in Edit or Play mode.
- BoardReview.Back(): finished rear using a temporary prefab copy.
- BoardPlayReview.LiveAudit(): requires the existing isolated Quest240QA session
  and verifies its unique storage prefix before exercising reward claims.
  Prepare with CollectorQuestExpansionQA.Prepare outside Play mode, run the
  audit once the manager is Ready, then stop and call Finish to remove QA data
  and compare production preferences with their original snapshot.

Evidence: badge_before.png and badge_after.png show the latest close-up revision.
final_board.png, before_Bright_front.png, after_* views,
finished_back.png, play_Arabic_refined.png and play_reward_preview.png.
The reward-preview screenshot is a temporary visual-only badge preview;
actual reward state transitions were separately checked in the isolated audit.
