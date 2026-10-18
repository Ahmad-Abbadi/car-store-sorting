COLLECTOR QUESTS

Open the Level scene and enter Play mode. The physical board stands to the right
of the entrance at (501.7, 0, -10.25). Aim at it and use the normal interaction
action. Select Journey, Challenges or Completed; pin a goal and claim completed
rewards at the board. Escape closes the panel.

Authoring
Quest assets, UI/card prefabs, board prefab and materials live in Content/.
Each journey definition has a stable ID, localized title/description key, goal,
target and coin/XP rewards. Keep IDs stable for existing careers. The manager's
definitions array controls journey display order. Quest text is in the root
Localization.csv and imported through the existing localization tools.

Tiny Riders > Quests > Build Collector Quest System recreates the supplied
default content and integrates it into the open Level scene. It resets authored
default assets; edit definitions directly for custom balancing instead of
rebuilding afterwards. The builder uses the upgrade station interaction layer.

Runtime
CollectorQuestProgress evaluates snapshots without Unity scene dependencies.
CollectorQuestManager waits for collection restoration, consumes session,
upgrade and shelf events, and exposes immutable views. Dirty events are batched
once per frame; collection scans do not run every idle frame.

Challenges count progress above assignment baselines. Sorting assignments use
the run's highest sorted count so removing and reshelving cannot farm rewards.
Theme replacement prefers another unfinished theme. Targets shrink near the
end of the collection and no new challenge is offered without achievable work.

Persistence
CollectorQuests stores career, quests, assignments, pin choice and pending claim.
Coins_RewardWallet stores balance and claim receipts together; Coins remains a
legacy mirror. Claim intent is persisted first, then the wallet receipt, then
claimed state and XP. Startup recovers interrupted claims without paying twice.
Isolated level-save keys use a corresponding isolated quest career key.

Replay keeps career XP and all journey completion/claims, starts a new challenge
run and uses the existing coin/upgrade/store reset. Reset Progress clears career
and wallet receipts while retaining settings. Unreadable/unsupported quest or
wallet saves are preserved and reward claims remain unavailable.

Verification
CollectorQuestTests contains 18 focused tests. Live gameplay verification and
screenshots are recorded in IntegrationEvidence/Quests/verification.json.
