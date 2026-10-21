using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TinyRiders;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Exercise the shipped tutorial with optimized visuals and real placement transactions.
public sealed class CarVisualBatchTutorialTests
{
    const string Backup="CarBatching.Tutorial.";
    static IEnumerator Tick(float seconds)
    { float deadline=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<deadline)yield return null; }
    static IEnumerator Land(PlacedOnShelfItemBase car)
    {
        float deadline=Time.realtimeSinceStartup+15;
        while(!car.IsSettled){Assert.Less(Time.realtimeSinceStartup,deadline,"Car landing");yield return null;}
    }

    [UnityTest,Timeout(180000)]
    public IEnumerator OptimizedCarsSupportWrongPlacementRecoveryAndTutorialCompletion()
    {
        SessionState.SetBool(Backup+"hadStatus",PlayerPrefs.HasKey(TutorialProgress.StatusKey));
        SessionState.SetInt(Backup+"status",PlayerPrefs.GetInt(TutorialProgress.StatusKey));
        SessionState.SetBool(Backup+"background",Application.runInBackground);
        SessionState.SetBool(Backup+"active",true);
        SessionState.SetBool("TinyRiders.Compact.QA.Active",true);
        PlayerPrefs.DeleteKey("TinyRiders.Compact.QA");
        PlayerPrefs.DeleteKey("TinyRiders.Compact.QA.position");
        PlayerPrefs.DeleteKey("TinyRiders.Compact.QA.coins");
        EditorSceneManager.OpenScene(TutorialBuilder.ScenePath);
        yield return new EnterPlayMode();
        yield return Exercise();
        yield return new ExitPlayMode();
    }

    static IEnumerator Exercise()
    {
        Application.runInBackground=true;
        float deadline=Time.realtimeSinceStartup+30;
        TutorialController tutorial=null;
        while((tutorial=Object.FindFirstObjectByType<TutorialController>())==null || tutorial.player==null || Time.frameCount<5)
        { Assert.Less(Time.realtimeSinceStartup,deadline,"Tutorial startup");yield return null; }
        var player=tutorial.player;
        player.Input.SendMessage("OnApplicationFocus",true);
        var first=WorldItemBase.All.First(w=>w.isActiveAndEnabled && w.Data==tutorial.firstCar);
        Assert.NotNull(first.GetComponentInChildren<CarBatchGeometry>(),"Tutorial must use the optimized visual.");
        Assert.True(player.ItemCollector.TryCollectItem(first));
        yield return Tick(.7f);
        var held=player.ItemHandler.CurrentEquipped.GetComponent<ItemVisualGroup>();
        Assert.True(held.Renderers.All(r=>r.forceMeshLod==(short)ItemDetail.Lod));
        held.ShowPrimary(Color.white);yield return null;held.HidePrimary();
        var wrong=tutorial.shelves.First(s=>!s.IsCorrectItem(tutorial.firstCar));
        Assert.True(player.SortingBehaviour.TryGetPlacedItemFromHand(out var misplaced));
        wrong.AddItem(misplaced,0);yield return Land(misplaced);
        tutorial.Evaluate();Assert.AreEqual(0,tutorial.CorrectCount);Assert.True(misplaced.IsWrong);
        Assert.True(wrong.TryTakeBack(misplaced));yield return Tick(.7f);

        for(int i=0;i<3;i++)
        {
            if(i>0)
            {
                var world=WorldItemBase.All.First(w=>w.isActiveAndEnabled && w.gameObject.scene==tutorial.gameObject.scene);
                Assert.True(player.ItemCollector.TryCollectItem(world));yield return Tick(.7f);
            }
            Assert.True(player.SortingBehaviour.TryGetHeldSortable(out var data));
            var shelf=tutorial.shelves.First(s=>s.IsCorrectItem(data) && s.HasFreeSpot);
            int slot=Enumerable.Range(0,shelf.SpotCount).First(shelf.IsSpotFree);
            Assert.True(shelf.CanAccept(data,slot,out var reason),reason);
            Assert.True(player.SortingBehaviour.TryGetPlacedItemFromHand(out var placed));
            shelf.AddItem(placed,slot);tutorial.Evaluate();
            Assert.AreEqual(i+1,tutorial.CorrectCount);
            if(i<2)yield return Land(placed);
            else Assert.False(tutorial.ReadyForGameplay,"The final landing must complete before leaving practice.");
        }
        deadline=Time.realtimeSinceStartup+60;
        while(SceneManager.GetActiveScene().name!="Level" || ThemedCollectionSession.Instance==null || ThemedCollectionSession.Instance.LivePileCount!=1200)
        { Assert.Less(Time.realtimeSinceStartup,deadline,"Tutorial-to-store transition");yield return null; }
        ThemedCollectionSession.Instance.suspendPersistenceForTests=true;
        Assert.AreEqual(2,PlayerPrefs.GetInt(TutorialProgress.StatusKey));
        Assert.AreEqual(600,ThemedCollectionSession.Instance.ActiveDefinitions.Count());
        File.WriteAllText("IntegrationEvidence/CarBatching/tutorial-gameplay.txt",
            "Optimized car pickup/holding/detail/outline, wrong placement/takeback, all three correct placements/landings, and transition to all600 designs/all1200 cars passed.");
    }

    [UnityTearDown]
    public IEnumerator Restore()
    {
        if(EditorApplication.isPlaying)
        {
            if(ThemedCollectionSession.Instance!=null)ThemedCollectionSession.Instance.suspendPersistenceForTests=true;
            yield return new ExitPlayMode();
        }
        if(SessionState.GetBool(Backup+"active",false))
        {
            if(SessionState.GetBool(Backup+"hadStatus",false))PlayerPrefs.SetInt(TutorialProgress.StatusKey,SessionState.GetInt(Backup+"status",0));
            else PlayerPrefs.DeleteKey(TutorialProgress.StatusKey);
            Application.runInBackground=SessionState.GetBool(Backup+"background",false);
        }
        SessionState.SetBool(Backup+"active",false);SessionState.SetBool("TinyRiders.Compact.QA.Active",false);
        PlayerPrefs.DeleteKey("TinyRiders.Compact.QA");PlayerPrefs.DeleteKey("TinyRiders.Compact.QA.position");
        PlayerPrefs.DeleteKey("TinyRiders.Compact.QA.coins");PlayerPrefs.Save();
    }
}
