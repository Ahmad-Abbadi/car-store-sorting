using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TinyRiders;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class DetailSelectionPlayTests
{
    const string Backup = "Details.Validation.";
    static readonly string[] IntKeys = { "Item_Detail", "Lighting_Theme", "TinyRiders.Compact.QA.coins", TutorialProgress.StatusKey };
    static readonly string[] StringKeys = { "TinyRiders.Compact.QA", "TinyRiders.Compact.QA.position" };
    static string Evidence => ItemDetailBuilder.Evidence;
    static void Note(string text) => File.AppendAllText(Evidence + "/runtime.txt", text + "\n");

    [UnityTest, Timeout(180000)]
    public IEnumerator DetailsChangeGeometryAndSharpnessWithoutChangingLighting()
    {
        Directory.CreateDirectory(Evidence);
        File.AppendAllText(Evidence + "/runtime.txt", "\nRun " + System.DateTime.UtcNow.ToString("O") + "\n");
        SessionState.SetString(Backup + "scene", SceneManager.GetActiveScene().path);
        SessionState.SetInt(Backup + "quality", QualitySettings.GetQualityLevel());
        SessionState.SetBool(Backup + "background", Application.runInBackground);
        SessionState.SetBool(Backup + "isolation", SessionState.GetBool("TinyRiders.Compact.QA.Active", false));
        foreach (var key in IntKeys)
        {
            SessionState.SetBool(Backup + "had." + key, PlayerPrefs.HasKey(key));
            SessionState.SetInt(Backup + key, PlayerPrefs.GetInt(key));
        }
        foreach (var key in StringKeys)
        {
            SessionState.SetBool(Backup + "had." + key, PlayerPrefs.HasKey(key));
            SessionState.SetString(Backup + key, PlayerPrefs.GetString(key));
        }
        SessionState.SetBool(Backup + "active", true);
        SessionState.SetBool("TinyRiders.Compact.QA.Active", true);
        PlayerPrefs.DeleteKey("TinyRiders.Compact.QA");
        PlayerPrefs.DeleteKey("TinyRiders.Compact.QA.position");
        PlayerPrefs.DeleteKey("TinyRiders.Compact.QA.coins");
        PlayerPrefs.SetInt("Item_Detail", (int)ItemDetailLevel.Low);
        PlayerPrefs.Save();
        if (SceneManager.GetActiveScene().isDirty) EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        EditorSceneManager.OpenScene(ShowroomLightingBuilder.LevelPath);
        yield return new EnterPlayMode();
        yield return ExerciseRuntime();
        yield return new ExitPlayMode();
    }

    static IEnumerator ExerciseRuntime()
    {
        Application.runInBackground = true;
        var errors = new List<string>();
        Application.LogCallback log = (message, trace, type) =>
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            // Other Editor clients can time out while a scene loads; that is not a render error.
            if (message == "Failed to handle /api/exec request: Main thread operation timed out after 5000ms") return;
            errors.Add(message);
        };
        Application.logMessageReceived += log;
        LogAssert.ignoreFailingMessages = true;
        while (ThemedCollectionSession.Instance == null || Player.Instance == null || Time.frameCount < 10
            || ThemedCollectionSession.Instance.LivePileCount != 1200) yield return null;
        ThemedCollectionSession.Instance.suspendPersistenceForTests = true;
        Assert.AreEqual(ItemDetailLevel.Low, ItemDetail.Current);
        AssertGroup(WorldItemBase.All.First().GetComponent<ItemVisualGroup>(), 2);
        Note("PASS direct gameplay launch restored Low before interacting; full 1200-car pile");

        var firstFrames = new HashSet<int>();
        Action<ScriptableRenderContext, Camera> firstFrame = (context, camera) =>
        {
            if (!Application.isPlaying || camera.cameraType != CameraType.Game) return;
            var scene = camera.gameObject.scene;
            if (scene.name != "Level" && scene.name != "MainMenu Scene" || firstFrames.Contains(scene.handle)) return;
            var groups = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ItemVisualGroup>(true)).ToArray();
            var display = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ItemDetailDisplay>(true)).FirstOrDefault();
            var renderers = display != null ? display.GetComponentsInChildren<MeshRenderer>(true)
                : groups.SelectMany(g => g.Renderers).ToArray();
            if (renderers.Length == 0) return;
            firstFrames.Add(scene.handle);
            foreach (var r in renderers.Where(r => r.enabled && r.gameObject.activeInHierarchy && r.GetComponent<MeshFilter>()?.sharedMesh != null))
                Assert.AreEqual((short)ItemDetail.Lod, r.forceMeshLod, "First frame: " + r.name);
            Note("PASS first rendered frame " + scene.name + " " + ItemDetail.Current);
        };
        RenderPipelineManager.beginCameraRendering += firstFrame;
        try
        {
            yield return CaptureMatrix(false);
            LightingThemeSettings.Set(LightingTheme.Bright);
            ItemDetail.Set(ItemDetailLevel.High);
            var world = WorldItemBase.All.First(w => w.gameObject.activeInHierarchy && w.Data is SortableItem_SO);
            var data = (SortableItem_SO)world.Data;
            Assert.IsTrue(Player.Instance.ItemCollector.TryCollectItem(world));
            yield return new WaitForSecondsRealtime(.8f);
            var held = Player.Instance.ItemHandler.CurrentEquipped;
            Assert.IsNotNull(held);
            AssertGroup(held.GetComponent<ItemVisualGroup>(), 0);
            ItemDetail.Set(ItemDetailLevel.Low);
            AssertGroup(held.GetComponent<ItemVisualGroup>(), 2);
            var shelf = Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None)
                .First(s => s.CanAccept(data, 0, out _));
            Assert.IsTrue(Player.Instance.SortingBehaviour.TryGetPlacedItemFromHand(out var placed));
            shelf.AddItem(placed, 0);
            yield return null;
            AssertGroup(placed.GetComponent<ItemVisualGroup>(), 2);
            ItemDetail.Set(ItemDetailLevel.High);
            AssertGroup(placed.GetComponent<ItemVisualGroup>(), 0);
            Note("PASS held and shelved car geometry updates immediately");

            var another = WorldItemBase.All.First(w => w.gameObject.activeInHierarchy && w.Data is SortableItem_SO);
            Assert.IsTrue(Player.Instance.ItemCollector.TryCollectItem(another));
            yield return new WaitForSecondsRealtime(.8f);
            ItemDetail.Set(ItemDetailLevel.Low);
            typeof(ItemsHandler).GetMethod("ExecuteThrow", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(Player.Instance.ItemHandler, new object[] { 0f });
            yield return new WaitForSecondsRealtime(.5f);
            foreach (var group in ItemVisualGroup.All) AssertGroup(group, 2);
            Note("PASS dropped and newly spawned world cars retain selected Low geometry");

            var menuLoad = SceneManager.LoadSceneAsync("MainMenu Scene");
            while (!menuLoad.isDone) yield return null;
            yield return null;
            var display = Object.FindFirstObjectByType<ItemDetailDisplay>();
            Assert.IsNotNull(display);
            Assert.AreEqual(1, Object.FindObjectsByType<ItemDetailDisplay>(FindObjectsSortMode.None).Length);
            foreach (var r in display.GetComponentsInChildren<MeshRenderer>(true)) Assert.AreEqual(2, r.forceMeshLod);
            Object.FindFirstObjectByType<MenuCinematicTour>().StopTour();
            yield return CaptureMatrix(true);

            var menu = Object.FindFirstObjectByType<MainMenu>();
            var serialized = new SerializedObject(menu);
            var button = (UnityEngine.UI.Button)serialized.FindProperty("itemDetailButton").objectReferenceValue;
            var label = (TMPro.TMP_Text)serialized.FindProperty("ItemDetails").objectReferenceValue;
            for (int i = 0; i < 3; i++)
            {
                var next = (ItemDetailLevel)(((int)ItemDetail.Current + 1) % 3);
                button.onClick.Invoke();
                Assert.AreEqual(next, ItemDetail.Current);
                Assert.IsTrue(label.text.Contains(next.ToString()), label.text);
                Assert.AreEqual((int)next, PlayerPrefs.GetInt("Item_Detail"));
            }
            Note("PASS actual menu button cycles labels, saved value, and mesh levels");
            LightingThemeSettings.Set(LightingTheme.Bright);
            ItemDetail.Set(ItemDetailLevel.High);
            // Validate the returning-player menu route; the tutorial has a separate live fixture.
            PlayerPrefs.SetInt(TutorialProgress.StatusKey, 2);
            menu.Play();
            float loadDeadline = Time.realtimeSinceStartup + 60;
            while (SceneLoader.IsLoading || SceneManager.GetActiveScene().name != "Level")
            { Assert.Less(Time.realtimeSinceStartup, loadDeadline, "Menu-to-store load"); yield return null; }
            yield return null;
            Assert.AreEqual(ItemDetailLevel.High, ItemDetail.Current);
            while (ThemedCollectionSession.Instance == null || ThemedCollectionSession.Instance.LivePileCount < 1100) yield return null;
            ThemedCollectionSession.Instance.suspendPersistenceForTests = true;
            foreach (var group in ItemVisualGroup.All) AssertGroup(group, 0);
            Note("PASS actual Play-button scene transition retained High");
            // MarkDone precedes the loading screen's final scene-unload cleanup.
            while (SceneManager.GetSceneByName(SceneLoader.LoadingSceneName).isLoaded) yield return null;
            menuLoad = SceneManager.LoadSceneAsync("MainMenu Scene");
            while (!menuLoad.isDone) yield return null;
            yield return null;
            var tour = Object.FindFirstObjectByType<MenuCinematicTour>();
            Assert.IsNotNull(tour, "Menu tour missing after completed loading-screen cleanup");
            tour.StopTour();
            yield return SamplePerformance();
            CollectionAssert.IsEmpty(errors, "Unexpected runtime/rendering errors");
        }
        finally
        {
            RenderPipelineManager.beginCameraRendering -= firstFrame;
            Application.logMessageReceived -= log;
            LogAssert.ignoreFailingMessages = false;
        }
    }

    static void AssertGroup(ItemVisualGroup group, int level)
    {
        Assert.IsNotNull(group);
        foreach (var r in group.Renderers.Where(r => r.GetComponent<MeshFilter>()?.sharedMesh != null))
        {
            Assert.AreEqual(3, r.GetComponent<MeshFilter>().sharedMesh.lodCount, r.name);
            Assert.AreEqual(level, r.forceMeshLod, r.name);
        }
    }

    static string LightingState(ShowroomLightingController c)
    {
        var asset = (UniversalRenderPipelineAsset)QualitySettings.renderPipeline;
        return string.Join("|", RenderSettings.ambientSkyColor, RenderSettings.ambientEquatorColor, RenderSettings.ambientGroundColor,
            c.keyLight.intensity, c.keyLight.color, c.keyLight.transform.rotation, c.keyLight.shadowStrength,
            string.Join(";", c.ceilingLights.Select(l => l.color + ":" + l.intensity + ":" + l.range)),
            string.Join(";", c.surfaces.Select(s => s.renderer.sharedMaterial.GetInstanceID())),
            c.volume.sharedProfile.GetInstanceID(), c.reflectionProbe.customBakedTexture.GetInstanceID(),
            asset.supportsHDR, asset.additionalLightsRenderingMode, asset.maxAdditionalLightsCount,
            asset.supportsMainLightShadows, asset.supportsSoftShadows, asset.mainLightShadowmapResolution, asset.shadowDistance);
    }

    static IEnumerator CaptureMatrix(bool menu)
    {
        var controller = Object.FindFirstObjectByType<ShowroomLightingController>();
        bool capture = !SessionState.GetBool(Backup + "skipCaptures", false);
        foreach (var theme in new[] { LightingTheme.Bright, LightingTheme.Dark })
        {
            LightingThemeSettings.Set(theme);
            string lighting = null;
            foreach (var level in new[] { ItemDetailLevel.High, ItemDetailLevel.Medium, ItemDetailLevel.Low })
            {
                ItemDetail.Set(level);
                yield return null; yield return null;
                if (lighting == null) lighting = LightingState(controller);
                Assert.AreEqual(lighting, LightingState(controller), "Details changed lighting");
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TinyRiders/ThemedCollection/Textures/Themes/TH_ALIEN/C001_ThemeDecals.png");
                Assert.AreEqual((int)level, texture.activeMipmapLimit);
                if (!capture) continue;
                if (menu) yield return CaptureMenuShots("Details/" + theme + "/" + level);
                else
                {
                    ShowroomLightingBuilder.Capture(Camera.main, "Details/" + theme + "/Gameplay-" + level + ".png");
                    var target = WorldItemBase.All.First(w => w.gameObject.activeInHierarchy).GetComponent<ItemVisualGroup>();
                    var bounds = target.Renderers.First(r => r.enabled).bounds;
                    foreach (var r in target.Renderers.Where(r => r.enabled)) bounds.Encapsulate(r.bounds);
                    var cam = Camera.main; var position = cam.transform.position; var rotation = cam.transform.rotation; float fov = cam.fieldOfView;
                    try
                    {
                        cam.transform.position = bounds.center + new Vector3(.9f, .55f, 1) * bounds.size.magnitude * 1.4f;
                        cam.transform.LookAt(bounds.center); cam.fieldOfView = 40;
                        ShowroomLightingBuilder.Capture(cam, "Details/" + theme + "/Closeup-" + level + ".png");
                    }
                    finally { cam.transform.SetPositionAndRotation(position, rotation); cam.fieldOfView = fov; }
                }
            }
            Note("PASS " + (menu ? "menu" : "gameplay") + " " + theme
                + " High/Medium/Low; identical lighting, correct active texture mip limits");
        }
    }

    static IEnumerator CaptureMenuShots(string folder)
    {
        var scene = SceneManager.GetActiveScene();
        var camera = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true)).Single();
        var position = camera.transform.position; var rotation = camera.transform.rotation; float fov = camera.fieldOfView;
        try
        {
            foreach (Transform shot in scene.GetRootGameObjects().Single(g => g.name == "Cinematic Cameras").transform)
            {
                var pose = shot.Find("Start Pose");
                camera.transform.SetPositionAndRotation(pose.position, pose.rotation);
                camera.fieldOfView = shot.GetComponentInChildren<Unity.Cinemachine.CinemachineCamera>().Lens.FieldOfView;
                ShowroomLightingBuilder.Capture(camera, folder + "/" + shot.name.Substring(0, 2) + ".png");
                yield return null;
            }
        }
        finally { if (camera != null) { camera.transform.SetPositionAndRotation(position, rotation); camera.fieldOfView = fov; } }
    }

    static IEnumerator SamplePerformance()
    {
        foreach (var level in new[] { ItemDetailLevel.High, ItemDetailLevel.Medium, ItemDetailLevel.Low })
        {
            ItemDetail.Set(level);
            for (int i = 0; i < 60; i++) yield return null;
            var times = new List<float>(); var triangles = new List<int>();
            for (int i = 0; i < 120; i++)
            {
                yield return null; times.Add(Time.unscaledDeltaTime * 1000); triangles.Add(UnityStats.triangles);
            }
            Note(level + " meanMs=" + times.Average() + " medianMs=" + times.OrderBy(v => v).ElementAt(60)
                + " renderedTriangles=" + triangles.OrderBy(v => v).ElementAt(60)
                + " allocatedMB=" + UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024));
        }
    }

    [UnityTearDown]
    public IEnumerator Restore()
    {
        if (EditorApplication.isPlaying) yield return new ExitPlayMode();
        if (!SessionState.GetBool(Backup + "active", false)) yield break;
        foreach (var key in IntKeys)
            if (SessionState.GetBool(Backup + "had." + key, false)) PlayerPrefs.SetInt(key, SessionState.GetInt(Backup + key, 0));
            else PlayerPrefs.DeleteKey(key);
        foreach (var key in StringKeys)
            if (SessionState.GetBool(Backup + "had." + key, false)) PlayerPrefs.SetString(key, SessionState.GetString(Backup + key, ""));
            else PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
        SessionState.SetBool("TinyRiders.Compact.QA.Active", SessionState.GetBool(Backup + "isolation", false));
        Application.runInBackground = SessionState.GetBool(Backup + "background", false);
        QualitySettings.SetQualityLevel(SessionState.GetInt(Backup + "quality", 3), true);
        string scene = SessionState.GetString(Backup + "scene", CinematicMenuBuilder.MenuPath);
        EditorSceneManager.OpenScene(string.IsNullOrEmpty(scene) ? CinematicMenuBuilder.MenuPath : scene);
        SessionState.SetBool(Backup + "active", false);
        SessionState.SetBool(Backup + "skipCaptures", false);
    }
}
