using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class StoreLoadingScreenTests
{
    private const string Evidence = "IntegrationEvidence/LoadingScreen";

    [SetUp]
    public void SaveUserPreferences()
    {
        SessionState.SetBool("CinematicMenu.HadLanguage", PlayerPrefs.HasKey("Language"));
        SessionState.SetString("CinematicMenu.Language", PlayerPrefs.GetString("Language","en"));
        SessionState.SetBool("CinematicMenu.Background", Application.runInBackground);
    }

    [Test]
    public void AuthoredScenes_SharePresentationAndKeepLoadingPolicy()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(StoreLoadingScreenBuilder.PrefabPath);
        Assert.IsNotNull(prefab);
        Assert.IsNotNull(prefab.GetComponent<LoadingPresentation>());
        Assert.IsNull(prefab.GetComponent<LoadingScreen>(), "The shared presentation must not start a scene load.");
        Assert.AreEqual("loading.label",prefab.GetComponentInChildren<LocalizedText>().Key);
        Assert.AreEqual(1,prefab.GetComponentsInChildren<TMP_Text>(true).Length,"Only Loading is shown, without branding.");
        var texture = prefab.GetComponentInChildren<UnityEngine.UI.RawImage>().texture;
        Assert.LessOrEqual(Mathf.Max(texture.width,texture.height),2048);
        var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
        Assert.IsFalse(importer.mipmapEnabled);
        Assert.AreNotEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach(var path in new[]{StoreLoadingScreenBuilder.InitializerPath,StoreLoadingScreenBuilder.LoadingPath})
            {
                var scene = EditorSceneManager.OpenScene(path);
                var p = Object.FindFirstObjectByType<LoadingPresentation>();
                Assert.AreEqual(StoreLoadingScreenBuilder.PrefabPath,PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(p));
                Assert.IsNull(Object.FindFirstObjectByType<FakeLoadingScreen>());
                if(path==StoreLoadingScreenBuilder.LoadingPath)
                {
                    var loader=Object.FindFirstObjectByType<LoadingScreen>();
                    Assert.AreEqual(5,new SerializedObject(loader).FindProperty("minimumDuration").floatValue);
                    Assert.AreEqual(p.transform.root,loader.transform.root,"The covering canvas survives activation with its loader.");
                }
                else
                {
                    var init=Object.FindFirstObjectByType<GameInitializer>();
                    Assert.AreEqual("OpenMenu",init.OnInitializeComplete.GetPersistentMethodName(0));
                    Assert.IsInstanceOf<StartupLoadingPresenter>(init.OnInitializeComplete.GetPersistentTarget(0));
                }
            }
            var db=Resources.Load<LocalizationDatabase_SO>(LocalizationDatabase_SO.ResourcePath);
            foreach(var language in db.Languages) Assert.IsTrue(db.BuildTable(language.Code).ContainsKey("loading.label"));
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }

    [UnityTest, Timeout(120000)]
    public IEnumerator Startup_IndeterminateAnimation_LocalizedLayouts_AsyncMenu()
    {
        Directory.CreateDirectory(Evidence);
        SessionState.SetBool("CinematicMenu.HadLanguage", PlayerPrefs.HasKey("Language"));
        SessionState.SetString("CinematicMenu.Language", PlayerPrefs.GetString("Language","en"));
        SessionState.SetBool("CinematicMenu.Background", Application.runInBackground);
        PlayerPrefs.DeleteKey("Language");
        SessionState.SetBool("TinyRiders.Compact.QA.Active",true);
        EditorSceneManager.OpenScene(StoreLoadingScreenBuilder.InitializerPath);
        // Hold actual initialization until presentation checks finish; never save this override.
        Object.FindFirstObjectByType<GameInitializer>().enabled=false;
        CinematicMenuTests.SetGameSize(1920,1080);
        yield return new EnterPlayMode();
        Application.runInBackground=true;
        for(int i=0;i<5;i++) yield return null;
        var presentation=Object.FindFirstObjectByType<LoadingPresentation>();
        Assert.IsTrue(presentation.IsIndeterminate);
        Time.timeScale=0;
        var dots=presentation.GetComponentsInChildren<CanvasGroup>().Where(g=>g.gameObject!=presentation.gameObject).ToArray();
        var before=dots.Select(d=>d.alpha).ToArray();
        for(int i=0;i<12;i++) yield return null;
        Assert.IsTrue(dots.Where((d,i)=>Mathf.Abs(d.alpha-before[i])>.01f).Any(),"Loading activity must animate while paused.");
        Time.timeScale=1;
        foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,800),new Vector2Int(1680,720)})
        {
            CinematicMenuTests.SetGameSize(size.x,size.y);
            for(int i=0;i<8;i++) yield return null;
            Canvas.ForceUpdateCanvases();
            var corners=new Vector3[4];
            ((RectTransform)presentation.transform.Find("Loading Panel")).GetWorldCorners(corners);
            Assert.IsTrue(corners.All(p=>p.x>=0 && p.y>=0 && p.x<=Screen.width && p.y<=Screen.height));
            var background=presentation.GetComponentInChildren<UnityEngine.UI.RawImage>();
            background.rectTransform.GetWorldCorners(corners);
            Assert.LessOrEqual(corners[0].x,1); Assert.LessOrEqual(corners[0].y,1);
            Assert.GreaterOrEqual(corners[2].x,Screen.width-1); Assert.GreaterOrEqual(corners[2].y,Screen.height-1);
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(Evidence+"/loading-"+size.x+"x"+size.y+".png"));
            for(int i=0;i<3;i++) yield return null;
        }
        Localizer.SetLanguage("ar");
        for(int i=0;i<8;i++) yield return null;
        var label=presentation.GetComponentInChildren<TMP_Text>();
        label.ForceMeshUpdate();
        Assert.IsNotEmpty(label.text);
        Assert.IsFalse(label.isTextOverflowing);
        Assert.Greater(label.textInfo.characterCount,0);
        ScreenCapture.CaptureScreenshot(Path.GetFullPath(Evidence+"/loading-arabic.png"));
        for(int i=0;i<3;i++) yield return null;
        PlayerPrefs.DeleteKey("Language");
        Localizer.RefreshDatabase();
        Object.FindFirstObjectByType<GameInitializer>().enabled=true;
        float deadline=Time.realtimeSinceStartup+75;
        bool sawLoading=false;
        while((SceneManager.GetActiveScene().name!="MainMenu Scene" || SceneLoader.IsLoading) && Time.realtimeSinceStartup<deadline)
        {
            yield return null;
            sawLoading |= SceneManager.GetSceneByName(SceneLoader.LoadingSceneName).isLoaded;
            Assert.LessOrEqual(Object.FindObjectsByType<LoadingPresentation>(FindObjectsSortMode.None).Count(p=>p.gameObject.activeInHierarchy),1);
            Assert.LessOrEqual(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c=>c.isActiveAndEnabled),1);
        }
        Assert.IsTrue(sawLoading);
        Assert.AreEqual("MainMenu Scene",SceneManager.GetActiveScene().name);
        Assert.IsFalse(SceneLoader.IsLoading);
        Assert.AreEqual(0f,SceneLoader.MinimumDurationOverride);
        for(int i=0;i<5;i++) yield return null;
        Assert.IsNull(Object.FindFirstObjectByType<LoadingPresentation>());
        Assert.IsTrue(LanguagePickerUI.IsOpen,"The loading handoff must preserve first-run language selection.");
        File.WriteAllText(Evidence+"/startup-verification.txt","PASS: one shared screen; indeterminate activity at timeScale zero; full coverage and panel bounds at 16:9, 16:10, ultrawide; Arabic; async startup handoff; no five-second startup minimum; one camera and presentation; first-run language selection.\n");
        var picker=Object.FindFirstObjectByType<LanguagePickerUI>();
        typeof(LanguagePickerUI).GetMethod("Pick",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(picker,new object[]{"en"});
        CinematicMenuTests.SetGameSize(1920,1080);
        for(int i=0;i<5;i++) yield return null;
        SessionState.SetInt("CinematicMenu.PreviousPhysics",(int)Physics.simulationMode);
        SessionState.SetBool("CinematicMenu.PhysicsOverride",true);
        Physics.simulationMode=SimulationMode.Script;
        var menu=Object.FindFirstObjectByType<MainMenu>();
        float playStart=Time.realtimeSinceStartup;
        menu.Play(); menu.Play();
        Assert.AreEqual("Level",SceneLoader.TargetScene);
        Assert.AreEqual(-1f,SceneLoader.MinimumDurationOverride,"Play must use the authored five-second minimum.");
        deadline=Time.realtimeSinceStartup+75;
        float previousProgress=0;
        bool captured=false, sawProgress=false;
        while((SceneManager.GetActiveScene().name!="Level" || SceneLoader.IsLoading) && Time.realtimeSinceStartup<deadline)
        {
            yield return null;
            var active=Object.FindObjectsByType<LoadingPresentation>(FindObjectsSortMode.None);
            Assert.LessOrEqual(active.Length,1);
            if(active.Length==0) continue;
            var p=active[0];
            Assert.IsFalse(p.IsIndeterminate);
            Assert.GreaterOrEqual(p.Progress,previousProgress);
            previousProgress=p.Progress;
            sawProgress |= p.Progress>0 && p.Progress<1;
            if(!captured && p.Progress>.35f && p.Progress<.9f)
            {
                ScreenCapture.CaptureScreenshot(Path.GetFullPath(Evidence+"/play-progress.png"));
                captured=true;
            }
            if(p.GetComponent<CanvasGroup>().alpha<1) Assert.AreEqual(1f,p.Progress,"Only fade after progress completes.");
        }
        Assert.IsTrue(sawProgress);
        Assert.IsTrue(captured);
        Assert.AreEqual("Level",SceneManager.GetActiveScene().name);
        Assert.IsFalse(SceneLoader.IsLoading);
        Assert.GreaterOrEqual(Time.realtimeSinceStartup-playStart,5f);
        for(int i=0;i<3;i++) yield return null;
        Assert.IsNull(Object.FindFirstObjectByType<LoadingPresentation>());
        File.WriteAllText(Evidence+"/play-verification.txt","PASS: repeated Play guarded; real progress is monotonic; authored five-second minimum restored after startup; full progress before fade; covering screen removed on completion. Navigation-only QA isolates pile physics.\n");
        yield return new ExitPlayMode();
    }

    [UnityTearDown]
    public IEnumerator Restore()
    {
        if (EditorApplication.isPlaying) yield return new ExitPlayMode();
        EditorSceneManager.OpenScene(CinematicMenuBuilder.MenuPath);
        // Use the existing QA cleanup to restore language, game-view sizes and background ticking.
        yield return new CinematicMenuTests().Restore();
    }
}



