using System;
using System.IO;
using System.Linq;
using TMPro;
using ToyShop.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Authors the shared screen without rebuilding any store or menu assets.</summary>
public static class StoreLoadingScreenBuilder
{
    public const string Folder = "Assets/00_Project/04_ِArt/UI/Loading";
    public const string PrefabPath = "Assets/00_Project/02_prefabs/UI/StoreLoadingPresentation.prefab";
    public const string InitializerPath = "Assets/00_Project/00_Scenes/Initializer.unity";
    public const string LoadingPath = "Assets/00_Project/00_Scenes/Loading Scene.unity";

    [MenuItem("Tools/Tiny Riders/Loading Screen/Build Shared Screen")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("Save open scenes before authoring the loading screen.");
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
        AssetDatabase.Refresh();
        ConfigureTexture();
        LocalizationCsvTool.Import(File.ReadAllText("Localization.csv"));
        Localizer.RefreshDatabase();
        var root = BuildPresentation();
        try { PrefabUtility.SaveAsPrefabAsset(root, PrefabPath); }
        finally { Object.DestroyImmediate(root); }
        try
        {
            ConfigureScene(InitializerPath, true);
            ConfigureScene(LoadingPath, false);
            AssetDatabase.SaveAssets();
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        Debug.Log("STORE_LOADING_SCREEN_BUILT: shared prefab, startup activity and async menu handoff, Play progress; no branding.");
    }

    private static void ConfigureTexture()
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(Folder + "/CreativeLoadingBackgroundUniversal.png");
        importer.textureType = TextureImporterType.Default;
        importer.mipmapEnabled = false;
        importer.isReadable = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 2048;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
    }

    private static GameObject BuildPresentation()
    {
        var theme = ToyShopTheme.Current;
        var root = new GameObject("Store Loading Presentation", typeof(RectTransform), typeof(Canvas),
            typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster), typeof(CanvasGroup));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920,1080);
        scaler.matchWidthOrHeight = .5f;
        var background = Rect("Store Render", root.transform);
        background.anchorMin = background.anchorMax = new Vector2(.5f,.5f);
        var image = background.gameObject.AddComponent<UnityEngine.UI.RawImage>();
        image.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/CreativeLoadingBackgroundUniversal.png");
        var fit = background.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
        fit.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent;
        fit.aspectRatio = 16f/9;
        // The opaque fullscreen image also blocks interaction with the incoming menu.
        image.raycastTarget = true;

        var panel = Rect("Loading Panel", root.transform);
        panel.anchorMin = panel.anchorMax = new Vector2(.5f,0);
        panel.pivot = new Vector2(.5f,0);
        panel.anchoredPosition = new Vector2(0,48);
        panel.sizeDelta = new Vector2(620,132);
        var surface = panel.gameObject.AddComponent<UnityEngine.UI.Image>();
        ToyShopStyle.Surface(surface, theme.cream);
        var titleRect = Rect("Loading Label", panel);
        titleRect.anchorMin = new Vector2(0,1); titleRect.anchorMax = new Vector2(1,1);
        titleRect.pivot = new Vector2(.5f,1);
        titleRect.anchoredPosition = new Vector2(-30,-20); titleRect.sizeDelta = new Vector2(-96,44);
        var title = titleRect.gameObject.AddComponent<TextMeshProUGUI>();
        title.font = theme.font; title.fontSize = 32; title.color = theme.navy;
        title.alignment = TextAlignmentOptions.Center;
        title.raycastTarget = false;
        titleRect.gameObject.AddComponent<LocalizedText>().SetKey("loading.label");

        var track = Rect("Progress Track", panel);
        track.anchorMin = new Vector2(0,0); track.anchorMax = new Vector2(1,0);
        track.pivot = new Vector2(.5f,0);
        track.anchoredPosition = new Vector2(0,27); track.sizeDelta = new Vector2(-64,18);
        var trackImage = track.gameObject.AddComponent<UnityEngine.UI.Image>();
        ToyShopStyle.Surface(trackImage, theme.muted, false);
        var fill = Rect("Progress Fill", track);
        fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(.45f,1);
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        ToyShopStyle.Surface(fill.gameObject.AddComponent<UnityEngine.UI.Image>(), theme.mint, false);
        var dots = new CanvasGroup[3];
        for (int i=0;i<dots.Length;i++)
        {
            var dot = Rect("Activity Dot " + (i+1), panel);
            dot.anchorMin = dot.anchorMax = new Vector2(.5f,1);
            dot.anchoredPosition = new Vector2(112+i*14,-43); dot.sizeDelta = new Vector2(7,7);
            var dotImage = dot.gameObject.AddComponent<UnityEngine.UI.Image>();
            dotImage.sprite = theme.circle; dotImage.color = theme.blue; dotImage.raycastTarget = false;
            dots[i] = dot.gameObject.AddComponent<CanvasGroup>();
        }
        var presentation = root.AddComponent<LoadingPresentation>();
        var so = new SerializedObject(presentation);
        so.FindProperty("fill").objectReferenceValue = fill;
        var dotArray = so.FindProperty("activityDots"); dotArray.arraySize = dots.Length;
        for(int i=0;i<dots.Length;i++) dotArray.GetArrayElementAtIndex(i).objectReferenceValue = dots[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        return root;
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static void ConfigureScene(string path, bool startup)
    {
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        // Only the previous loading canvas is replaced; cameras and initialization systems remain authored.
        foreach (var canvas in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Canvas>(true)).ToArray())
            if (canvas && (canvas.transform.parent == null || !canvas.transform.parent.GetComponentInParent<Canvas>()))
                Object.DestroyImmediate(canvas.gameObject);
        var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
        var presentation = root.GetComponent<LoadingPresentation>();
        if (startup)
        {
            var initializer = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<GameInitializer>(true)).Single();
            var bridge = initializer.GetComponent<StartupLoadingPresenter>();
            if (!bridge) bridge = initializer.gameObject.AddComponent<StartupLoadingPresenter>();
            var so = new SerializedObject(bridge);
            so.FindProperty("presentation").objectReferenceValue = presentation;
            so.FindProperty("startupCamera").objectReferenceValue = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).Single();
            so.ApplyModifiedPropertiesWithoutUndo();
            for(int i=initializer.OnInitializeComplete.GetPersistentEventCount()-1;i>=0;i--)
                if (initializer.OnInitializeComplete.GetPersistentMethodName(i) == "LoadScene" ||
                    initializer.OnInitializeComplete.GetPersistentTarget(i) is StartupLoadingPresenter)
                    UnityEventTools.RemovePersistentListener(initializer.OnInitializeComplete,i);
            UnityEventTools.AddPersistentListener(initializer.OnInitializeComplete, bridge.OpenMenu);
            EditorUtility.SetDirty(initializer);
        }
        else
        {
            var loader = root.AddComponent<LoadingScreen>();
            var so = new SerializedObject(loader);
            so.FindProperty("presentation").objectReferenceValue = presentation;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}



