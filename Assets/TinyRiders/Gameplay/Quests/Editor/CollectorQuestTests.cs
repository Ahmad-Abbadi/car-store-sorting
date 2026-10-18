using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TinyRiders.Quests;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TinyRiders.Editor
{
    public sealed class CollectorQuestTests
    {
        CollectorQuestDefinition[] definitions;
        CollectorQuestProgress engine;
        CollectorSnapshot s;
        [SetUp] public void SetUp()
        {
            definitions=AssetDatabase.FindAssets("t:CollectorQuestDefinition",new[]{CollectorQuestBuilder.Folder})
                .Select(g=>AssetDatabase.LoadAssetAtPath<CollectorQuestDefinition>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            Assert.AreEqual(8,definitions.Length);
            engine=new CollectorQuestProgress(new CollectorCareer(),definitions);
            s=new CollectorSnapshot{total=100,designs=50}; s.themes.Add("A",(0,50)); s.themes.Add("B",(0,50));
        }
        CollectorQuestRecord Quest(string id)=>engine.Career.quests.Single(q=>q.id==id);
        CollectorQuestRecord Challenge(CollectorQuestGoal goal)=>engine.Career.quests.Single(q=>q.challenge && !q.claimed && q.goal==goal);

        [Test] public void ExistingSave_CreditsAchievementsWithoutGrantingXP()
        {
            s.sorted=100; s.discovered=50; s.fullShelves=3; s.upgrades=2;
            s.themes["A"]=(50,50); s.themes["B"]=(50,50); s.completedThemes.UnionWith(new[]{"A","B"});
            engine.Evaluate(s);
            Assert.True(engine.Career.quests.Where(q=>!q.challenge).All(q=>q.complete));
            Assert.False(engine.Career.quests.Any(q=>q.claimed)); Assert.Zero(engine.Career.xp);
            Assert.False(engine.Career.quests.Any(q=>q.challenge));
        }
        [Test] public void PlacementRemovalWrongPlacement_UsesNetProgressAndLatchesCompletion()
        {
            engine.Evaluate(s); s.sorted=4; engine.Evaluate(s); Assert.AreEqual(4,Quest("first_display").progress);
            engine.Evaluate(s); Assert.AreEqual(4,Quest("first_display").progress,"wrong placement changes no authoritative count");
            s.sorted=2; engine.Evaluate(s); Assert.AreEqual(2,Quest("first_display").progress);
            s.sorted=5; engine.Evaluate(s); s.sorted=0; engine.Evaluate(s);
            Assert.True(Quest("first_display").complete); Assert.AreEqual(5,Quest("first_display").progress);
        }
        [Test] public void RepeatedPlacementsCannotFarmSuccessiveChallenges()
        {
            engine.Evaluate(s); var q=Challenge(CollectorQuestGoal.SortedCars);
            s.sorted=10; engine.Evaluate(s); Assert.True(q.complete); Assert.True(engine.FinishClaim(q.id));
            s.sorted=0; engine.Evaluate(s); q=Challenge(CollectorQuestGoal.SortedCars); Assert.AreEqual(10,q.baseline);
            s.sorted=10; engine.Evaluate(s); Assert.Zero(q.progress); Assert.False(q.complete);
            s.sorted=15; engine.Evaluate(s); Assert.AreEqual(5,q.progress);
            s.sorted=20; engine.Evaluate(s); Assert.True(q.complete);
        }
        [Test] public void RemainingWorkClampsTargets_CompletedStoreOffersNoNewChallenges()
        {
            s.sorted=98; s.discovered=49; engine.Evaluate(s);
            Assert.AreEqual(2,Challenge(CollectorQuestGoal.SortedCars).target);
            Assert.AreEqual(1,Challenge(CollectorQuestGoal.DiscoveredDesigns).target);
            s.sorted=100; s.discovered=50; s.themes["A"]=(50,50); s.themes["B"]=(50,50);
            s.completedThemes.UnionWith(new[]{"A","B"}); engine.Evaluate(s);
            foreach(var q in engine.Career.quests.Where(q=>q.challenge).ToArray()) Assert.True(engine.FinishClaim(q.id));
            engine.Evaluate(s); Assert.False(engine.Career.quests.Any(q=>q.challenge && !q.claimed));
        }
        [Test] public void ClaimsPinningAndReplay_PreserveCareerAndGiveFreshChallengeIDs()
        {
            engine.Evaluate(s); var old=Challenge(CollectorQuestGoal.SortedCars).id;
            Assert.False(engine.FinishClaim("first_display")); Assert.False(engine.Pin("missing"));
            Assert.True(engine.Pin(null)); engine.Evaluate(s); Assert.Null(engine.Career.pinnedId);
            s.sorted=5; engine.Evaluate(s); Assert.True(engine.FinishClaim("first_display")); Assert.False(engine.FinishClaim("first_display"));
            Assert.AreEqual(20,engine.Career.xp);
            engine.Replay(); s.sorted=0; engine.Evaluate(s);
            Assert.True(Quest("first_display").claimed); Assert.AreEqual(20,engine.Career.xp);
            Assert.AreNotEqual(old,Challenge(CollectorQuestGoal.SortedCars).id);
        }
        [Test] public void ReplacingChallengePaysNothing_AndDoesNotReplaceCompletedQuest()
        {
            engine.Evaluate(s); var q=Challenge(CollectorQuestGoal.SortedCars);
            Assert.True(engine.ReplaceChallenge(q.id,s)); Assert.Zero(engine.Career.xp);
            q=Challenge(CollectorQuestGoal.SortedCars); s.sorted=10; engine.Evaluate(s);
            Assert.False(engine.ReplaceChallenge(q.id,s));
        }
        [Test] public void ThemeChallenge_RequiresWholeSpecificTheme_AndDoesNotReuseFinishedThemes()
        {
            engine.Evaluate(s); var q=Challenge(CollectorQuestGoal.SpecificTheme);
            Assert.AreEqual("A",q.themeId); s.themes["A"]=(49,50); engine.Evaluate(s); Assert.False(q.complete);
            s.themes["A"]=(50,50); s.completedThemes.Add("A"); engine.Evaluate(s); Assert.True(q.complete);
            engine.FinishClaim(q.id); engine.Evaluate(s); Assert.AreEqual("B",Challenge(CollectorQuestGoal.SpecificTheme).themeId);
        }
        [Test] public void Serialization_PreservesPendingClaimBaselinesAndCareer()
        {
            engine.Evaluate(s); s.sorted=5; engine.Evaluate(s); engine.Career.pendingClaim="first_display";
            var saved=JsonUtility.FromJson<CollectorCareer>(JsonUtility.ToJson(engine.Career));
            var restored=new CollectorQuestProgress(saved,definitions);
            Assert.AreEqual("first_display",saved.pendingClaim); Assert.True(restored.FinishClaim(saved.pendingClaim));
            Assert.AreEqual(20,saved.xp); Assert.False(restored.FinishClaim("first_display"));
            Assert.AreEqual(engine.Career.runId,saved.runId);
        }
        [Test] public void FullReset_ClearsCareerAndRewardReceiptsButKeepsSettings_UsingIsolatedStorage()
        {
            var keys=new System.Collections.Generic.HashSet<string>{"LevelSave","Coins","Coins_RewardWallet","CollectorQuests","PlayerPositionSave","Language","MusicVolume"};
            typeof(ResetProgressButton).GetMethod("DeleteProgressKeys",BindingFlags.NonPublic|BindingFlags.Static)
                .Invoke(null,new object[]{new Action<string>(key=>keys.Remove(key))});
            CollectionAssert.AreEquivalent(new[]{"Language","MusicVolume"},keys);
        }
        [Test] public void ReplacingThemeChallenge_SelectsAnotherUnfinishedTheme()
        {
            engine.Evaluate(s); var q=Challenge(CollectorQuestGoal.SpecificTheme);
            Assert.True(engine.ReplaceChallenge(q.id,s));
            Assert.AreNotEqual(q.themeId,Challenge(CollectorQuestGoal.SpecificTheme).themeId);
        }
        [TestCase(0,0)] [TestCase(99,0)] [TestCase(100,1)] [TestCase(250,2)] [TestCase(500,3)] [TestCase(1000,4)]
        public void RanksUseClaimedXP(int xp,int rank) => Assert.AreEqual(rank,CollectorQuestProgress.RankIndex(xp));

        [Test] public void Wallet_MigratesLegacyBalance_AndRecoversInterruptedClaimExactlyOnce()
        {
            const string key="CollectorQuestTests.Coins", envelope=key+"_RewardWallet";
            PlayerPrefs.DeleteKey(envelope); PlayerPrefs.SetInt(key,17);
            GameObject go=null;
            try
            {
                go=new GameObject("Quest wallet test"); var wallet=go.AddComponent<CoinsManager>();
                typeof(CoinsManager).GetField("saveKey",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(wallet,key);
                typeof(CoinsManager).GetMethod("Awake",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(wallet,null);
                Assert.AreEqual(17,wallet.Coins); Assert.True(wallet.AddRewardOnce("claim-one",3)); Assert.AreEqual(20,wallet.Coins);
                Object.DestroyImmediate(go); go=new GameObject("Reload quest wallet test"); wallet=go.AddComponent<CoinsManager>();
                typeof(CoinsManager).GetField("saveKey",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(wallet,key);
                typeof(CoinsManager).GetMethod("Awake",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(wallet,null);
                Assert.True(wallet.AddRewardOnce("claim-one",3)); Assert.AreEqual(20,wallet.Coins);
                wallet.TrySpend(8); Assert.AreEqual(12,wallet.Coins); wallet.ResetForReplay(); Assert.Zero(wallet.Coins);
                Assert.True(wallet.AddRewardOnce("claim-one",3)); Assert.Zero(wallet.Coins);
                Assert.True(wallet.AddRewardOnce("new-run/claim-one",3)); Assert.AreEqual(3,wallet.Coins);
            }
            finally {if(go) Object.DestroyImmediate(go); PlayerPrefs.DeleteKey(key); PlayerPrefs.DeleteKey(envelope); PlayerPrefs.Save();}
        }

        [Test] public void AuthoredAssets_HaveValidBindingsAndPhysicalInteraction()
        {
            var board=PrefabUtility.LoadPrefabContents(CollectorQuestBuilder.BoardPath);
            var ui=PrefabUtility.LoadPrefabContents(CollectorQuestBuilder.UIPath);
            try
            {
                Assert.NotNull(board.GetComponent<CollectorQuestBoard>());
                var interaction=Object.FindFirstObjectByType<InteractionHandler>();
                int interactionMask=interaction ? new SerializedObject(interaction).FindProperty("interactableMask").intValue : 1<<8;
                Assert.AreNotEqual(0,interactionMask & (1<<board.layer),
                    "The board must be visible to the player's configured interaction ray.");
                Assert.AreEqual(1,board.GetComponentsInChildren<Collider>(true).Length);
                var view=ui.GetComponent<CollectorQuestUI>(); Assert.NotNull(view.cardPrefab);
                Assert.NotNull(view.scroll.viewport.GetComponent<UnityEngine.UI.Mask>());
                Assert.NotNull(view.scroll.content.GetComponent<UnityEngine.UI.ContentSizeFitter>());
                Assert.True(view.window.GetComponent<CollectorQuestPanel>().RequireMouse);
                Assert.True(view.window.GetComponent<CollectorQuestPanel>().RequirePause);
                Assert.True(ui.GetComponentsInChildren<Transform>(true).All(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0));
            }
            finally {PrefabUtility.UnloadPrefabContents(board); PrefabUtility.UnloadPrefabContents(ui);}
        }
    }
}
