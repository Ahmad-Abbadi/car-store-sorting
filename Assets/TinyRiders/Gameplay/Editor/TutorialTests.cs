using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class TutorialTests
{
    [SetUp]
    public void PreservePreferences()
    {
        SessionState.SetBool("TutorialTest.HadStatus",PlayerPrefs.HasKey(TutorialProgress.StatusKey));
        SessionState.SetInt("TutorialTest.Status",PlayerPrefs.GetInt(TutorialProgress.StatusKey));
        SessionState.SetBool("TutorialTest.Background",Application.runInBackground);
    }
    [Test]
    public void Routing_FreshInterruptedCompletedSkippedAndExistingPlayers()
    {
        var keys = new HashSet<string>();
        Assert.IsTrue(TutorialProgress.ShouldShow(keys.Contains));
        keys.Add("Coins"); // An unrelated preference must not hide onboarding.
        Assert.IsTrue(TutorialProgress.ShouldShow(keys.Contains));
        keys.Add(TutorialProgress.StatusKey);
        Assert.IsFalse(TutorialProgress.ShouldShow(keys.Contains));
        keys.Remove(TutorialProgress.StatusKey); keys.Add("LevelSave");
        Assert.IsFalse(TutorialProgress.ShouldShow(keys.Contains));
        Assert.AreEqual("Level",TutorialProgress.Destination("Level",true));
        Assert.IsTrue(TutorialProgress.ConsumeReplay());
        Assert.IsFalse(TutorialProgress.ConsumeReplay());
        Assert.AreEqual("Level",TutorialProgress.StoreScene);
        var deleted=new HashSet<string>();
        typeof(ResetProgressButton).GetMethod("DeleteProgressKeys",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)
            .Invoke(null,new object[]{new System.Action<string>(k=>deleted.Add(k))});
        Assert.Contains(TutorialProgress.StatusKey,deleted.ToList());
    }

    [Test]
    public void Completion_RequiresThreeCarsToFinishLandingWithoutAnExitStep()
    {
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            var root=new GameObject("Completion regression");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
            root.SetActive(false);
            var controller=root.AddComponent<TutorialController>();
            var shelf=root.AddComponent<SortingShelf>(); controller.shelves=new[]{shelf};
            var cars=Enumerable.Range(0,3).Select(i=>
            {
                var go=new GameObject("Car "+i); go.transform.SetParent(root.transform);
                return go.AddComponent<PlacedOnShelfItemBase>();
            }).ToArray();
            const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            typeof(SortingShelf).GetField("placingSpots",flags).SetValue(shelf,cars.Select(c=>c.transform).ToList());
            typeof(SortingShelf).GetField("_occupants",flags).SetValue(shelf,cars);
            typeof(TutorialController).GetField("<CorrectCount>k__BackingField",flags).SetValue(controller,3);
            Assert.IsFalse(controller.ReadyForGameplay,"Do not cut off the final placement animation.");
            foreach(var car in cars) typeof(PlacedOnShelfItemBase).GetField("<IsSettled>k__BackingField",flags).SetValue(car,true);
            Assert.IsTrue(controller.ReadyForGameplay,"No door or player-position condition may be required.");
            typeof(TutorialController).GetField("<CorrectCount>k__BackingField",flags).SetValue(controller,2);
            Assert.IsFalse(controller.ReadyForGameplay);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    [Test]
    public void AuthoredScene_UsesTheStoresShelfMeshesMaterialsAndSurfaceTextures()
    {
        var setup=EditorSceneManager.GetSceneManagerSetup();
        try
        {
            EditorSceneManager.OpenScene("Assets/00_Project/00_Scenes/Level.unity");
            var originals=Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None);
            var artwork=new[]{"TH_FIRE","TH_ICE"}.Select(theme=>ShelfArt(originals.First(s=>s.GetComponent<TinyRiders.ThemeShelfBinding>()?.themeId==theme))).ToArray();
            var materials=new[]{"Store Floor","Gallery wall","Teal dado","Store Ceiling"}.ToDictionary(n=>n,n=>Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).First(r=>r.name==n).sharedMaterial);
            EditorSceneManager.OpenScene(TutorialBuilder.ScenePath);
            var c=Object.FindFirstObjectByType<TutorialController>();
            for(int i=0;i<2;i++) CollectionAssert.AreEqual(artwork[i],ShelfArt(c.shelves[i]),"Shelf art must match Level exactly.");
            foreach(var pair in materials)
                foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name==pair.Key))
                    Assert.AreSame(pair.Value,r.sharedMaterial,pair.Key);
            Assert.IsNotNull(GameObject.Find("Tutorial Racing Floor Graphics/Toy Town Background"));
            Assert.IsNull(GameObject.Find("Exit door"));
            Assert.IsNull(GameObject.Find("Exit threshold"));
            Assert.IsNull(GameObject.Find("Closed upper display"));
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }

    static string[] ShelfArt(SortingShelf shelf) => shelf.GetComponentsInChildren<MeshRenderer>(true)
        .Select(r=>r.name+"|"+AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>()?.sharedMesh)+"|"+
            string.Join(";",r.sharedMaterials.Select(m=>AssetDatabase.GetAssetPath(m)))).OrderBy(s=>s).ToArray();

    [Test]
    public void AuthoredScene_IsSmallIsolatedAndLocalized()
    {
        var setup=EditorSceneManager.GetSceneManagerSetup();
        try
        {
            EditorSceneManager.OpenScene(TutorialBuilder.ScenePath);
            var c=Object.FindFirstObjectByType<TutorialController>();
            Assert.IsNotNull(c); Assert.AreEqual(2,c.shelves.Length);
            Assert.AreEqual(3,c.shelves.Sum(s=>s.SpotCount));
            Assert.AreEqual(3,Object.FindObjectsByType<WorldItemBase>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length);
            Assert.IsFalse(c.extraCars.activeSelf);
            foreach(var component in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                Assert.IsFalse(component is LevelSaveManager || component is PlayerSavePosition || component is CoinsManager || component is UpgradeManager || component is ResetProgressButton || component is SortingProgressTracker || component is TinyRiders.ThemedCollectionSession || component is TinyRiders.Quests.CollectorQuestManager);
            Assert.IsNotNull(c.skipButton);
            Assert.AreEqual(1,Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length);
            var data=Resources.Load<RuntimeLocalizationData>("RuntimeLocalizationData");
            foreach(var table in data.tables) foreach(var row in TutorialLocalization.Rows)
                Assert.IsFalse(string.IsNullOrEmpty(table.GetEntry(row[0])?.LocalizedValue),table.name+": "+row[0]);
            Assert.IsTrue(EditorBuildSettings.scenes.Any(s=>s.enabled && s.path==TutorialBuilder.ScenePath));
        }
        finally
        {
            if(setup.Length>0 && setup.All(s=>!string.IsNullOrEmpty(s.path))) EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.OpenScene(TutorialBuilder.ScenePath);
        }
    }

    [UnityTearDown]
    public IEnumerator RestorePreferences()
    {
        if(EditorApplication.isPlaying) yield return new ExitPlayMode();
        if(SessionState.GetBool("TutorialTest.HadStatus",false)) PlayerPrefs.SetInt(TutorialProgress.StatusKey,SessionState.GetInt("TutorialTest.Status",0));
        else PlayerPrefs.DeleteKey(TutorialProgress.StatusKey);
        PlayerPrefs.Save(); Application.runInBackground=SessionState.GetBool("TutorialTest.Background",false);
    }
}


