using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TinyRiders;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class CinematicMenuTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    const string Evidence = "IntegrationEvidence/CinematicMenu";
    static UnityEngine.UI.Button Button(string name) => Object.FindFirstObjectByType<MainMenu>()
        .GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(b => b.name == name);
    static IEnumerator Frames(int count = 5) { for (int i = 0; i < count; i++) yield return null; }

    static void OpenMenu()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path == CinematicMenuBuilder.MenuPath) return;
        Assert.IsFalse(scene.isDirty, "Save the current scene before running menu tests.");
        EditorSceneManager.OpenScene(CinematicMenuBuilder.MenuPath);
    }

    static void AssertFreshTour()
    {
        var tour = Object.FindFirstObjectByType<MenuCinematicTour>();
        Assert.That(tour.CurrentShot, Is.InRange(0,tour.ShotCount-1));
        Assert.AreEqual(0,tour.CompletedLoops);
        var shots = (MenuCinematicTour.Shot[])typeof(MenuCinematicTour).GetField("shots",Private).GetValue(tour);
        Assert.AreEqual(20,shots[tour.CurrentShot].camera.Priority.Value);
        Assert.AreEqual(1,shots.Count(s => s.camera.Priority.Value == 20));
    }

    static void IsolateNavigationPhysics()
    {
        // This fixture checks UI and scene ownership, not the initial pile's collision solver.
        SessionState.SetInt("CinematicMenu.PreviousPhysics",(int)Physics.simulationMode);
        SessionState.SetBool("CinematicMenu.PhysicsOverride",true);
        Physics.simulationMode = SimulationMode.Script;
    }

    [UnityTest, Timeout(120000)]
    public IEnumerator LoadingHandoff_UsesOneCameraAndListener()
    {
        SessionState.SetBool("CinematicMenu.HadLanguage",PlayerPrefs.HasKey("Language"));
        SessionState.SetString("CinematicMenu.Language",PlayerPrefs.GetString("Language","en"));
        SessionState.SetBool("CinematicMenu.Background",Application.runInBackground);
        SessionState.SetBool("TinyRiders.Compact.QA.Active",true);
        PlayerPrefs.SetString("Language","en");
        OpenMenu();
        SetGameSize(1280,720);
        yield return new EnterPlayMode();
        Application.runInBackground = true;
        yield return Frames(5);
        var menu = Object.FindFirstObjectByType<MainMenu>();
        IsolateNavigationPhysics();
        menu.Play(); menu.Play();
        float deadline = Time.realtimeSinceStartup + 65;
        bool sawLoading = false;
        while ((SceneManager.GetActiveScene().name != "Level" || SceneLoader.IsLoading) && Time.realtimeSinceStartup < deadline)
        {
            yield return null;
            sawLoading |= SceneManager.GetSceneByName(SceneLoader.LoadingSceneName).isLoaded;
            Assert.LessOrEqual(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c => c.isActiveAndEnabled),1);
            Assert.LessOrEqual(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(c => c.isActiveAndEnabled),1);
        }
        Assert.IsTrue(sawLoading);
        Assert.AreEqual("Level",SceneManager.GetActiveScene().name);
        Assert.IsFalse(SceneLoader.IsLoading);
        Assert.IsNull(Object.FindFirstObjectByType<MenuCinematicTour>());
        yield return SceneManager.LoadSceneAsync("MainMenu Scene");
        yield return Frames(5);
        Assert.AreEqual(1,Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c => c.isActiveAndEnabled));
        Assert.AreEqual(1,Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(c => c.isActiveAndEnabled));
        AssertFreshTour();
        yield return new ExitPlayMode();
        File.WriteAllText(Evidence + "/handoff-verification.txt","PASS: guarded Play; one active camera and listener during loading; cinematic cleanup; menu camera restored on return.\n");
    }

    [UnityTest, Timeout(240000)]
    public IEnumerator Tour_MenuControls_LoadingAndReturn()
    {
        Directory.CreateDirectory(Evidence);
        SessionState.SetBool("CinematicMenu.HadLanguage", PlayerPrefs.HasKey("Language"));
        SessionState.SetString("CinematicMenu.Language", PlayerPrefs.GetString("Language", "en"));
        SessionState.SetBool("CinematicMenu.Background", Application.runInBackground);
        SessionState.SetBool("TinyRiders.Compact.QA.Active", true);
        PlayerPrefs.DeleteKey("Language");
        OpenMenu();
        SetGameSize(1280,720);
        yield return new EnterPlayMode();
        Application.runInBackground = true;
        yield return Frames(10);
        Assert.IsTrue(LanguagePickerUI.IsOpen, "First-run language selection must appear.");
        var picker = Object.FindFirstObjectByType<LanguagePickerUI>();
        picker.Close(); yield return Frames();
        Assert.IsTrue(LanguagePickerUI.IsOpen, "First-run selection must require a choice.");
        typeof(LanguagePickerUI).GetMethod("Pick", Private).Invoke(picker, new object[] { "en" });
        yield return Frames();
        Assert.IsFalse(LanguagePickerUI.IsOpen);
        var tour = Object.FindFirstObjectByType<MenuCinematicTour>();
        var stage = GameObject.Find("Cinematic Store");
        Assert.AreEqual(1, Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length);
        Assert.AreEqual(1, Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length);
        Assert.AreEqual(6, tour.GetComponentsInChildren<CinemachineCamera>(true).Length);
        Assert.IsNull(Object.FindFirstObjectByType<ThemedCollectionSession>());
        Assert.IsNull(Object.FindFirstObjectByType<Player>());
        Assert.IsEmpty(stage.GetComponentsInChildren<Collider>(true));
        Assert.IsTrue(stage.GetComponentsInChildren<MonoBehaviour>(true).All(b => b is TMPro.TMP_Text ||
            b is UnityEngine.Rendering.Volume || b is UnityEngine.Rendering.Universal.UniversalAdditionalLightData ||
            b is ItemDetailDisplay),"The backdrop may contain visual components, but no gameplay session or player logic.");
        // Language selection may take part of the first round; observe two fresh, full rounds.
        var authored=(MenuCinematicTour.Shot[])typeof(MenuCinematicTour).GetField("shots",Private).GetValue(tour);
        Assert.AreEqual(6,authored.Length);
        Assert.IsTrue(authored.All(s=>Mathf.Approximately(s.duration,8)),"Saved shot durations must remain eight seconds.");
        tour.enabled = false;
        tour.enabled = true;
        var firstPositions = new Vector3[tour.ShotCount]; var sampled = new bool[tour.ShotCount]; var moved = new bool[tour.ShotCount];
        var elapsedField = typeof(MenuCinematicTour).GetField("elapsed", Private);
        int initialFrame = Time.frameCount;
        float deadline = Time.realtimeSinceStartup + 125;
        int initialLoops = tour.CompletedLoops;
        int observedRound = initialLoops, previousShot = -1;
        var roundViews = new System.Collections.Generic.HashSet<int>();
        while (tour.CompletedLoops < initialLoops + 2 && Time.realtimeSinceStartup < deadline)
        {
            int shot = tour.CurrentShot;
            if (tour.CompletedLoops != observedRound)
            {
                Assert.AreEqual(tour.ShotCount,roundViews.Count,"A round must show every view.");
                roundViews.Clear(); observedRound = tour.CompletedLoops;
            }
            if (shot != previousShot)
            {
                Assert.IsTrue(roundViews.Add(shot),"A view must not repeat within its round.");
                previousShot = shot;
            }
            float elapsed = (float)elapsedField.GetValue(tour);
            float progress=elapsed/authored[shot].duration;
            if (progress > .35f && progress < .55f && !sampled[shot])
            {
                sampled[shot] = true; firstPositions[shot] = Camera.main.transform.position;
                if(shot<6) ScreenCapture.CaptureScreenshot(Path.GetFullPath(Evidence + "/shot-" + (shot + 1).ToString("D2") + ".png"));
            }
            if (progress > .75f && sampled[shot] && Vector3.Distance(firstPositions[shot], Camera.main.transform.position) > .015f)
                moved[shot] = true;
            yield return null;
        }
        Assert.Greater(Time.frameCount, initialFrame + 100, "The player loop must advance.");
        Assert.AreEqual(tour.ShotCount,roundViews.Count,"The final observed round must be complete.");
        Assert.GreaterOrEqual(tour.CompletedLoops, initialLoops + 2, "Two complete real-time loops must finish.");
        Assert.IsTrue(sampled.All(v => v), "All six shots must appear.");
        Assert.IsTrue(moved.All(v => v), "Every shot must move the rendered camera after its blend.");
        var detail = ItemDetail.Current;
        Button("Button_ItemDetail").onClick.Invoke(); Assert.AreNotEqual(detail, ItemDetail.Current);
        Button("Button_ItemDetail").onClick.Invoke(); Button("Button_ItemDetail").onClick.Invoke();
        Assert.AreEqual(detail, ItemDetail.Current);
        bool sfx = SimpleAudioManager.SfxMuted, music = SimpleAudioManager.MusicMuted;
        Button("Button_MuteSFX").onClick.Invoke(); Assert.AreNotEqual(sfx, SimpleAudioManager.SfxMuted);
        Button("Button_MuteSFX").onClick.Invoke();
        Button("Button_MuteMusic").onClick.Invoke(); Assert.AreNotEqual(music, SimpleAudioManager.MusicMuted);
        Button("Button_MuteMusic").onClick.Invoke();
        Button("Button_Reset").onClick.Invoke();
        var confirm = Object.FindFirstObjectByType<MainMenu>().transform.Find("Reset_Group/Confirm_Panel").gameObject;
        Assert.IsTrue(confirm.activeSelf);
        Button("Button_No").onClick.Invoke(); Assert.IsFalse(confirm.activeSelf);
        Button("Language Button").onClick.Invoke(); yield return Frames(); Assert.IsTrue(LanguagePickerUI.IsOpen);
        Object.FindFirstObjectByType<LanguagePickerUI>().Close(); yield return Frames(); Assert.IsFalse(LanguagePickerUI.IsOpen);
        foreach (var size in new[] { new Vector2Int(1280,720), new Vector2Int(1280,800), new Vector2Int(1680,720) })
        {
            SetGameSize(size.x,size.y);
            yield return Frames(15);
            Canvas.ForceUpdateCanvases();
            foreach (var button in Object.FindFirstObjectByType<MainMenu>().GetComponentsInChildren<UnityEngine.UI.Button>())
            {
                var corners = new Vector3[4]; ((RectTransform)button.transform).GetWorldCorners(corners);
                Assert.IsTrue(corners.All(p => p.x >= -1 && p.y >= -1 && p.x <= Screen.width + 1 && p.y <= Screen.height + 1), button.name + " must fit " + size);
            }
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(Evidence + "/aspect-" + size.x + "x" + size.y + ".png"));
            yield return Frames();
        }
        var menu = Object.FindFirstObjectByType<MainMenu>();
        IsolateNavigationPhysics();
        menu.Play(); menu.Play();
        Assert.IsFalse(Button("Button_Play").interactable);
        Assert.AreEqual("Level", SceneLoader.TargetScene);
        Assert.IsTrue(SceneLoader.IsLoading);
        deadline = Time.realtimeSinceStartup + 55;
        while ((SceneManager.GetActiveScene().name != "Level" || SceneLoader.IsLoading) && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.AreEqual("Level", SceneManager.GetActiveScene().name);
        Assert.IsFalse(SceneLoader.IsLoading);
        Assert.IsNull(Object.FindFirstObjectByType<MenuCinematicTour>());
        Assert.IsNull(GameObject.Find("Cinematic Store"));
        Assert.IsNotNull(Player.Instance);
        yield return SceneManager.LoadSceneAsync("MainMenu Scene");
        yield return Frames(5);
        AssertFreshTour();
        Assert.IsTrue(Button("Button_Play").interactable);
        File.WriteAllText(Evidence + "/verification.txt", "PASS: two complete shuffled rounds of six moving views; first-run language selection; detail cycle; both mute controls; reset cancellation; language reopening; 16:9, 16:10 and ultrawide controls; guarded Play through Loading Scene into Level with physics isolated; cinematic cleanup; fresh tour on return.\n");
        yield return new ExitPlayMode();
    }

    // Fixed sizes make screen-space UI validation independent of the Editor window dimensions.
    public static void SetGameSize(int width, int height)
    {
        var assembly = typeof(Editor).Assembly;
        var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
        var instance = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
        var groupType = assembly.GetType("UnityEditor.GameViewSizeGroupType");
        var group = sizesType.GetMethod("GetGroup").Invoke(instance, new[] { Enum.Parse(groupType,"Standalone") });
        var groupClass = group.GetType();
        if (!SessionState.GetBool("CinematicMenu.SizeSaved",false))
            SessionState.SetInt("CinematicMenu.PreviousCustomCount",(int)groupClass.GetMethod("GetCustomCount").Invoke(group,null));
        string label = "Cinematic QA " + width + "x" + height;
        var sizeType = assembly.GetType("UnityEditor.GameViewSize");
        var sizeKind = assembly.GetType("UnityEditor.GameViewSizeType");
        var size = Activator.CreateInstance(sizeType, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null,
            new object[] { Enum.Parse(sizeKind,"FixedResolution"),width,height,label },null);
        groupClass.GetMethod("AddCustomSize").Invoke(group,new[] { size });
        int count = (int)groupClass.GetMethod("GetTotalCount").Invoke(group,null);
        var viewType = assembly.GetType("UnityEditor.GameView");
        var view = EditorWindow.GetWindow(viewType);
        if (!SessionState.GetBool("CinematicMenu.SizeSaved",false))
        {
            SessionState.SetInt("CinematicMenu.PreviousSize",(int)viewType.GetProperty("selectedSizeIndex",Private|BindingFlags.Public).GetValue(view));
            SessionState.SetBool("CinematicMenu.SizeSaved",true);
        }
        viewType.GetProperty("selectedSizeIndex",Private|BindingFlags.Public).SetValue(view,count-1);
    }

    [UnityTearDown]
    public IEnumerator Restore()
    {
        if (EditorApplication.isPlaying) yield return new ExitPlayMode();
        if (SessionState.GetBool("CinematicMenu.PhysicsOverride",false))
        {
            Physics.simulationMode=(SimulationMode)SessionState.GetInt("CinematicMenu.PreviousPhysics",0);
            SessionState.SetBool("CinematicMenu.PhysicsOverride",false);
        }
        if (SessionState.GetBool("CinematicMenu.HadLanguage",false)) PlayerPrefs.SetString("Language",SessionState.GetString("CinematicMenu.Language","en"));
        else PlayerPrefs.DeleteKey("Language");
        Application.runInBackground = SessionState.GetBool("CinematicMenu.Background",false);
        SessionState.SetBool("TinyRiders.Compact.QA.Active",false);
        if (SessionState.GetBool("CinematicMenu.SizeSaved",false))
        {
            var type = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            type.GetProperty("selectedSizeIndex",Private|BindingFlags.Public).SetValue(EditorWindow.GetWindow(type),SessionState.GetInt("CinematicMenu.PreviousSize",0));
            var sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
            var sizes = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
            var groupType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeGroupType");
            var group = sizesType.GetMethod("GetGroup").Invoke(sizes,new[] { Enum.Parse(groupType,"Standalone") });
            int count = (int)group.GetType().GetMethod("GetCustomCount").Invoke(group,null);
            for (int i = count-1; i >= SessionState.GetInt("CinematicMenu.PreviousCustomCount",count); i--)
                group.GetType().GetMethod("RemoveCustomSize").Invoke(group,new object[] { i });
            SessionState.SetBool("CinematicMenu.SizeSaved",false);
        }
        OpenMenu();
    }
}
