using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TinyRiders.Quests;
using UnityEditor;
using UnityEngine;

namespace TinyRiders.Editor
{
    public sealed class CollectorQuestExpansionTests
    {
        CollectorQuestDefinition[] definitions;
        CollectorSnapshot snapshot;
        CollectorQuestProgress engine;

        [SetUp] public void Setup()
        {
            definitions=AssetDatabase.FindAssets("t:CollectorQuestDefinition",new[]{CollectorQuestBuilder.Folder})
                .Select(g=>AssetDatabase.LoadAssetAtPath<CollectorQuestDefinition>(AssetDatabase.GUIDToAssetPath(g))).OrderBy(d=>d.authoredOrder).ToArray();
            snapshot=EmptySnapshot(definitions);
            engine=new CollectorQuestProgress(new CollectorCareer(),definitions);
        }

        public static CollectorSnapshot EmptySnapshot(CollectorQuestDefinition[] defs)
        {
            var s=new CollectorSnapshot{total=1200,designs=600,shelfCapacity=80,upgradeCapacity=27};
            foreach(var id in defs.Where(d=>d.goal==CollectorQuestGoal.ThemeSortedCars).Select(d=>d.subjectId).Distinct()) s.themes[id]=(0,30);
            foreach(var id in defs.Where(d=>d.goal==CollectorQuestGoal.FamilySortedCars).Select(d=>d.subjectId).Distinct()) s.families[id]=(0,80);
            foreach(var id in defs.Where(d=>d.goal==CollectorQuestGoal.UpgradeLevel).Select(d=>d.subjectId).Distinct())
                s.upgradeLevels[id]=(0,defs.Where(d=>d.goal==CollectorQuestGoal.UpgradeLevel && d.subjectId==id).Max(d=>d.target));
            return s;
        }
        CollectorQuestRecord Record(string id)=>engine.Career.quests.Single(q=>q.id==id);
        CollectorQuestView[] Views()=>engine.Career.quests.Select(q=>new CollectorQuestView(q)).ToArray();

        [Test] public void Catalog_HasExactly240StableUniqueQuestsAndCorrectBreakdown()
        {
            Assert.AreEqual(240,definitions.Length); Assert.AreEqual(240,definitions.Select(d=>d.id).Distinct().Count());
            Assert.AreEqual(120,definitions.Count(d=>d.goal==CollectorQuestGoal.ThemeSortedCars));
            Assert.AreEqual(45,definitions.Count(d=>d.goal==CollectorQuestGoal.FamilySortedCars));
            Assert.AreEqual(20,definitions.Count(d=>d.seriesId=="sort")); Assert.AreEqual(12,definitions.Count(d=>d.seriesId=="discover"));
            Assert.AreEqual(12,definitions.Count(d=>d.seriesId=="shelf")); Assert.AreEqual(8,definitions.Count(d=>d.seriesId=="themes"));
            Assert.AreEqual(15,definitions.Count(d=>d.seriesId.StartsWith("upgrade_")));
            CollectionAssert.AreEquivalent(CollectorQuestBuilder.LegacyIds,definitions.Where(d=>d.seriesId=="legacy").Select(d=>d.id));
            Assert.AreEqual(2678,definitions.Sum(d=>d.coins)); Assert.AreEqual(8185,definitions.Sum(d=>d.xp));
            Assert.AreEqual(6,(int)CollectorQuestGoal.SpecificTheme); Assert.AreEqual(7,(int)CollectorQuestGoal.ThemeSortedCars);
        }
        [Test] public void RebuildingContent_IsIdempotentAndPreservesAssetGUIDs()
        {
            var before=definitions.ToDictionary(d=>d.id,d=>AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(d)));
            var beforeRewards=definitions.ToDictionary(d=>d.id,d=>(d.coins,d.xp));
            var generated=CollectorQuestBuilder.Definitions();
            Assert.AreEqual(240,generated.Length);
            foreach(var d in generated)
            {
                Assert.AreEqual(before[d.id],AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(d)));
                Assert.AreEqual(beforeRewards[d.id],(d.coins,d.xp),"Reward generation must not depend on the runner's active scene.");
            }
            engine.Evaluate(snapshot); engine.Evaluate(snapshot);
            Assert.AreEqual(240,engine.Career.quests.Count(q=>!q.challenge));
        }
        [Test] public void EveryAuthoredGoal_IsReachableAndPercentageTargetsRoundUp()
        {
            engine.Evaluate(snapshot);
            Assert.True(engine.Career.quests.Where(q=>!q.challenge).All(q=>q.available));
            foreach(var q in engine.Career.quests.Where(q=>q.goal==CollectorQuestGoal.ThemeSortedCars))
                Assert.Contains(q.target,new[]{8,15,30});
            foreach(var q in engine.Career.quests.Where(q=>q.goal==CollectorQuestGoal.FamilySortedCars))
                Assert.Contains(q.target,new[]{20,40,80});
        }
        [Test] public void ThemeAndFamilyGoals_TrackNetPlacementAndLatchCompletion()
        {
            engine.Evaluate(snapshot);
            var theme=definitions.First(d=>d.goal==CollectorQuestGoal.ThemeSortedCars && d.stageOrder==1);
            var family=definitions.First(d=>d.goal==CollectorQuestGoal.FamilySortedCars && d.stageOrder==1);
            snapshot.themes[theme.subjectId]=(7,30); snapshot.families[family.subjectId]=(19,80); engine.Evaluate(snapshot);
            Assert.AreEqual(7,Record(theme.id).progress); Assert.False(Record(theme.id).complete);
            Assert.AreEqual(19,Record(family.id).progress); Assert.False(Record(family.id).complete);
            snapshot.themes[theme.subjectId]=(4,30); snapshot.families[family.subjectId]=(10,80); engine.Evaluate(snapshot);
            Assert.AreEqual(4,Record(theme.id).progress); Assert.AreEqual(10,Record(family.id).progress);
            snapshot.themes[theme.subjectId]=(8,30); snapshot.families[family.subjectId]=(20,80); engine.Evaluate(snapshot);
            snapshot.themes[theme.subjectId]=(0,30); snapshot.families[family.subjectId]=(0,80); engine.Evaluate(snapshot);
            Assert.True(Record(theme.id).complete); Assert.True(Record(family.id).complete);
        }
        [Test] public void DiscoveryShelvesAndUpgrades_EvaluateTheirOwnCounters()
        {
            engine.Evaluate(snapshot); snapshot.discovered=75; snapshot.fullShelves=20;
            var max=definitions.First(d=>d.seriesId=="upgrade_max"); snapshot.upgradeLevels[max.subjectId]=(max.target,max.target);
            snapshot.totalUpgradeLevels=10; snapshot.upgrades=1; engine.Evaluate(snapshot);
            Assert.True(Record("discover_75").complete); Assert.False(Record("discover_100").complete);
            Assert.True(Record("shelf_20").complete); Assert.False(Record("shelf_30").complete);
            Assert.True(Record(max.id).complete); Assert.True(Record("upgrade_own_"+max.subjectId).complete);
            Assert.True(Record("upgrade_levels_10").complete); Assert.False(Record("upgrade_levels_20").complete);
        }
        [Test] public void Migration_PreservesOldAchievementsRewardsXPAndPendingClaim()
        {
            var oldDefs=definitions.Where(d=>CollectorQuestBuilder.LegacyIds.Contains(d.id)).ToArray();
            var old=new CollectorQuestProgress(new CollectorCareer(),oldDefs); old.Evaluate(snapshot);
            var first=old.Career.quests.Single(q=>q.id=="first_display"); first.complete=true; first.progress=5;
            old.FinishClaim(first.id); first.coins=9; // a previously snapshotted reward must not be rewritten
            var pending=old.Career.quests.Single(q=>q.id=="getting_organized"); pending.complete=true; pending.progress=25;
            old.Career.pendingClaim=pending.id; old.Pin(pending.id);
            string run=old.Career.runId;
            var saved=JsonUtility.FromJson<CollectorCareer>(JsonUtility.ToJson(old.Career));
            engine=new CollectorQuestProgress(saved,definitions); engine.Evaluate(snapshot);
            Assert.AreEqual(240,saved.quests.Count(q=>!q.challenge)); Assert.AreEqual(20,saved.xp); Assert.AreEqual(run,saved.runId);
            Assert.True(Record(first.id).claimed); Assert.AreEqual(9,Record(first.id).coins);
            Assert.AreEqual(pending.id,saved.pendingClaim); Assert.AreEqual(pending.id,saved.pinnedId);
            Assert.True(engine.FinishClaim(pending.id)); Assert.False(engine.FinishClaim(pending.id)); Assert.AreEqual(50,saved.xp);
        }
        [Test] public void MissingContent_HidesUnfinishedGoalsWithoutCollapsingMilestones()
        {
            engine.Evaluate(snapshot); var theme=definitions.First(d=>d.goal==CollectorQuestGoal.ThemeSortedCars);
            snapshot.themes.Remove(theme.subjectId); snapshot.total=100; snapshot.sorted=100; engine.Evaluate(snapshot);
            Assert.False(Record(theme.id).available); Assert.False(Record("sort_125").available);
            Assert.AreEqual(125,Record("sort_125").target); Assert.False(Record("sort_125").complete);
            Assert.False(CollectorQuestPresentation.Select(Views(),CollectorQuestTab.AllQuests).Any(q=>q.Id==theme.id || q.Id=="sort_125"));
        }
        [Test] public void FullCollection_CompletesEveryQuest_ClaimsOnce_AndReplayKeepsCareer()
        {
            snapshot.sorted=1200; snapshot.discovered=600; snapshot.fullShelves=80; snapshot.upgrades=7; snapshot.totalUpgradeLevels=27;
            foreach(var id in snapshot.themes.Keys.ToArray()){snapshot.themes[id]=(30,30); snapshot.completedThemes.Add(id);}
            foreach(var id in snapshot.families.Keys.ToArray()) snapshot.families[id]=(80,80);
            foreach(var id in snapshot.upgradeLevels.Keys.ToArray()) {var u=snapshot.upgradeLevels[id]; snapshot.upgradeLevels[id]=(u.max,u.max);}
            engine.Evaluate(snapshot); var journey=engine.Career.quests.Where(q=>!q.challenge).ToArray();
            Assert.True(journey.All(q=>q.complete));
            foreach(var q in journey){Assert.True(engine.FinishClaim(q.id)); Assert.False(engine.FinishClaim(q.id));}
            Assert.AreEqual(8185,engine.Career.xp); engine.Replay(); engine.Evaluate(EmptySnapshot(definitions));
            Assert.AreEqual(240,engine.Career.quests.Count(q=>!q.challenge && q.claimed)); Assert.AreEqual(8185,engine.Career.xp);
            Assert.AreEqual(3,engine.Career.quests.Count(q=>q.challenge));
        }
        [Test] public void Journey_RecommendsOnePerCategory_AndAlwaysIncludesClaimableQuests()
        {
            engine.Evaluate(snapshot); var initial=CollectorQuestPresentation.Select(Views(),CollectorQuestTab.Journey);
            Assert.AreEqual(6,initial.Length); Assert.AreEqual(6,initial.Select(q=>q.Category).Distinct().Count());
            snapshot.sorted=50; snapshot.discovered=25; engine.Evaluate(snapshot);
            var journey=CollectorQuestPresentation.Select(Views(),CollectorQuestTab.Journey);
            CollectionAssert.IsSubsetOf(Views().Where(q=>!q.Challenge && q.Complete && !q.Claimed).Select(q=>q.Id),journey.Select(q=>q.Id));
            Assert.True(journey.Where(q=>!q.Complete).GroupBy(q=>q.Category).All(g=>g.Count()==1));
            var discovery=journey.Single(q=>q.Category==CollectorQuestCategory.Discoveries && !q.Complete);
            Assert.AreEqual("discover_50",discovery.Id,"closest incomplete discovery milestone");
        }
        [Test] public void FiltersAndPagination_CoverAll240QuestsWithoutDuplicates()
        {
            engine.Evaluate(snapshot); var all=CollectorQuestPresentation.Select(Views(),CollectorQuestTab.AllQuests);
            Assert.AreEqual(40,CollectorQuestPresentation.PageCount(all.Length));
            var pages=Enumerable.Range(0,40).SelectMany(i=>CollectorQuestPresentation.Page(all,i)).ToArray();
            Assert.AreEqual(240,pages.Length); Assert.AreEqual(240,pages.Select(q=>q.Id).Distinct().Count());
            foreach(var category in Enum.GetValues(typeof(CollectorQuestCategory)).Cast<CollectorQuestCategory>())
                Assert.True(CollectorQuestPresentation.Select(Views(),CollectorQuestTab.AllQuests,category).All(q=>q.Category==category));
            Assert.AreEqual(6,CollectorQuestPresentation.Page(all,999).Length);
        }
        [Test] public void FreshChallenges_AreCategorizedBeforeTheFirstGameplayEvent()
        {
            engine.Evaluate(snapshot); var views=Views();
            Assert.AreEqual(1,CollectorQuestPresentation.Select(views,CollectorQuestTab.Challenges,CollectorQuestCategory.Sorting).Length);
            Assert.AreEqual(1,CollectorQuestPresentation.Select(views,CollectorQuestTab.Challenges,CollectorQuestCategory.Discoveries).Length);
            Assert.AreEqual(1,CollectorQuestPresentation.Select(views,CollectorQuestTab.Challenges,CollectorQuestCategory.Themes).Length);
        }
        [Test] public void Notifications_CombineCompletionBatchesAndKeepOnlyTheHighestPendingRank()
        {
            engine.Evaluate(snapshot); var uiRoot=PrefabUtility.LoadPrefabContents(CollectorQuestBuilder.UIPath);
            try
            {
                var ui=uiRoot.GetComponent<CollectorQuestUI>();
                const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                var batch=typeof(CollectorQuestUI).GetMethod("OnCompleted",flags);
                batch.Invoke(ui,new object[]{Views().Take(3).ToArray()}); batch.Invoke(ui,new object[]{Views().Take(2).ToArray()});
                Assert.AreEqual(5,typeof(CollectorQuestUI).GetField("pendingCompletions",flags).GetValue(ui));
                var rank=typeof(CollectorQuestUI).GetMethod("OnRankedUp",flags);
                rank.Invoke(ui,new object[]{1}); rank.Invoke(ui,new object[]{5}); rank.Invoke(ui,new object[]{3});
                Assert.AreEqual(5,typeof(CollectorQuestUI).GetField("pendingRank",flags).GetValue(ui));
            }
            finally{PrefabUtility.UnloadPrefabContents(uiRoot);}
        }
        [TestCase(2499,4)] [TestCase(2500,5)] [TestCase(4999,5)] [TestCase(5000,6)] [TestCase(9999,6)] [TestCase(10000,7)]
        public void ExtendedRankThresholds(int xp,int rank)=>Assert.AreEqual(rank,CollectorQuestProgress.RankIndex(xp));
    }
}
