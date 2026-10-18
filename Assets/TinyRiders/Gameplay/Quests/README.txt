COLLECTOR QUESTS

240 permanent quests: the original 8, plus 120 theme stages, 45 car-family
stages, 20 sorting milestones, 12 discovery milestones, 12 shelf milestones,
8 theme-count milestones and 15 upgrade goals. Three optional challenge types
continue to rotate alongside the permanent collection.

Journey recommends one unfinished goal per category and includes all claimable
permanent quests. All Quests browses the full catalog. Category filters apply
to each tab, and scrapbook spreads reuse at most 6 card objects. Simultaneous completions
are counted in a single toast; multiple queued rank-ups show the highest rank.

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

Tiny Riders > Quests > Expand to 240 Collector Quests updates definitions and
the existing UI prefab while preserving physical board placement. Theme and
family IDs come from the eligible catalog; percentage targets round upward.
Generation reads authored layout capacity even when an EditMode test runner
temporarily opens an empty scene. Stable IDs and asset GUIDs survive rebuilding.

New goals use 5/10/20 coin and 15/30/60 XP tiers. The complete permanent catalog
awards 2678 coins and 8185 XP; original rewards and upgrade prices are unchanged.
Ranks now continue through Curator (2500), Collector Legend (5000) and Hall of
Fame (10000). Replaying optional challenges can continue advancing the career.

Runtime
CollectorQuestProgress evaluates snapshots without Unity scene dependencies.
CollectorQuestManager waits for collection restoration, consumes session,
upgrade and shelf events, and exposes immutable views. Dirty events are batched
once per frame. One record traversal supplies global, theme and car-family
counts; collection scans do not run every idle frame. Unfinished goals with
missing content are hidden without reducing separate milestones to one target.

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
CollectorQuestTests and CollectorQuestExpansionTests contain 36 focused cases.
Expansion verification and screenshots are in IntegrationEvidence/Quests240/.
CollectorQuestExpansionQA is an explicit opt-in editor probe: Prepare seeds an
isolated eight-quest career with a pending claim; scene callbacks redirect the
level, position, wallet and career keys before startup; Finish removes only its
own QA keys and verifies production progress is unchanged. Do not leave a QA
session active when handing the Editor back to a player.

Scrapbook presentation
CollectorScrapbookPresenter owns page turns, category artwork, membership stamps,
wallet feedback and passive corner notifications. CollectorQuestUI commits claims
through the unchanged manager before displaying a short collected stamp.
Tiny Riders/Quests/Build Collector Scrapbook updates existing prefabs in place.
Build and Expand also apply this presentation. All Quests has 40 six-card spreads.
New artwork, UI labels and checks are in IntegrationEvidence/Scrapbook/.

