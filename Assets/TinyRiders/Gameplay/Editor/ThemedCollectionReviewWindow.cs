using System.Collections.Generic;
using TinyRiders;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public sealed class ThemedCollectionReviewWindow : EditorWindow
{
    const string UIPath = "Assets/TinyRiders/Gameplay/Review/CollectionNavigation.uxml";
    CollectionReviewPanel panel;
    ThemedCollectionReviewCamera rig;

    [MenuItem("Tools/Tiny Riders/Review/Collection Navigator")]
    public static void Open()
    {
        var window = GetWindow<ThemedCollectionReviewWindow>("Car Collection");
        window.minSize = new Vector2(280, 650);
        window.Show();
    }
    public void CreateGUI() { Rebuild(); }
    void OnEnable() { EditorApplication.hierarchyChanged += Rebuild; EditorApplication.playModeStateChanged += PlayChanged; }
    void OnDisable() { EditorApplication.hierarchyChanged -= Rebuild; EditorApplication.playModeStateChanged -= PlayChanged; panel?.Dispose(); }
    void PlayChanged(PlayModeStateChange state) { EditorApplication.delayCall += Rebuild; }
    void Rebuild()
    {
        panel?.Dispose(); panel = null; rootVisualElement.Clear();
        rig = Object.FindFirstObjectByType<ThemedCollectionReviewCamera>();
        if (rig == null) { rootVisualElement.Add(new Label("Open ThemedCollectionReview to browse all cars.")); return; }
        AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UIPath).CloneTree(rootVisualElement);
        rootVisualElement.AddToClassList("editor-review");
        panel = new CollectionReviewPanel(rootVisualElement, rig, FrameSceneView);
    }
    void FrameSceneView()
    {
        if (EditorApplication.isPlaying) return;
        var view = SceneView.lastActiveSceneView;
        if (view == null) return;
        var camera = rig.GetComponent<Camera>();
        view.AlignViewToObject(rig.transform);
        view.orthographic = camera.orthographic;
        if (camera.orthographic) view.size = camera.orthographicSize;
        view.Repaint();
    }

    [MenuItem("Tools/Tiny Riders/Review/Update Visibility and Navigation")]
    public static void Setup()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Review setup requires Edit mode.");
        var rig = Object.FindFirstObjectByType<ThemedCollectionReviewCamera>();
        if (rig == null || rig.gameObject.scene.path != ThemedCollectionReviewBuilder.ScenePath)
            throw new System.InvalidOperationException("Open the collection review scene first.");
        rig.ConfigureVisibility();
        var settingsPath = "Assets/TinyRiders/Gameplay/Review/CollectionReviewPanelSettings.asset";
        var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(settingsPath);
        if (settings == null) { settings = ScriptableObject.CreateInstance<PanelSettings>(); AssetDatabase.CreateAsset(settings, settingsPath); }
        settings.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/TinyRiders/Gameplay/Review/CollectionNavigationTheme.tss");
        settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        settings.referenceResolution = new Vector2Int(1600,1000);
        EditorUtility.SetDirty(settings);
        var existing = Object.FindFirstObjectByType<ThemedCollectionReviewNavigation>();
        var ui = existing != null ? existing.gameObject : new GameObject("Collection Navigator - All 600 Cars Visible");
        var document = ui.GetComponent<UIDocument>() ?? ui.AddComponent<UIDocument>();
        document.panelSettings = settings;
        document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UIPath);
        var navigation = ui.GetComponent<ThemedCollectionReviewNavigation>() ?? ui.AddComponent<ThemedCollectionReviewNavigation>();
        navigation.navigation = rig;
        RenderSettings.fog = false;
        rig.ShowTheme(rig.initialTheme);
        EditorSceneManager.MarkSceneDirty(rig.gameObject.scene);
        EditorSceneManager.SaveScene(rig.gameObject.scene);
        AssetDatabase.SaveAssets();
        Open();
        ReviewSceneClipping.ApplyAll();
    }
}

[InitializeOnLoad]
public static class ReviewSceneClipping
{
    static readonly Dictionary<SceneView, SceneView.CameraSettings> previous = new Dictionary<SceneView, SceneView.CameraSettings>();
    static ReviewSceneClipping() { SceneView.duringSceneGui += Apply; EditorSceneManager.activeSceneChangedInEditMode += Changed; }
    static void Changed(Scene before, Scene after) { ApplyAll(); }
    public static void ApplyAll() { foreach (SceneView view in SceneView.sceneViews) Apply(view); }
    static void Apply(SceneView view)
    {
        if (SceneManager.GetActiveScene().path == ThemedCollectionReviewBuilder.ScenePath)
        {
            if (!previous.ContainsKey(view)) previous.Add(view, JsonUtility.FromJson<SceneView.CameraSettings>(JsonUtility.ToJson(view.cameraSettings)));
            var settings = view.cameraSettings;
            settings.dynamicClip = false; settings.nearClip = .1f; settings.farClip = ThemedCollectionReviewCamera.ReviewDistance;
            settings.occlusionCulling = false; view.cameraSettings = settings;
            view.camera.farClipPlane = ThemedCollectionReviewCamera.ReviewDistance;
            view.camera.useOcclusionCulling = false;
        }
        else if (previous.TryGetValue(view, out var settings)) { view.cameraSettings = settings; previous.Remove(view); }
    }
}
