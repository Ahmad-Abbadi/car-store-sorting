#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary> Menu tool that builds the language picker prefabs and links the flag sprites. </summary>
public static class LanguageUiBuilder
{
    private const string UiFolder = "Assets/00_Project/02_prefabs/UI";
    private const string RowPath = UiFolder + "/Language Row.prefab";
    private const string ButtonPath = UiFolder + "/Language Button.prefab";
    private const string PickerPath = "Assets/00_Project/03_scriptableObjects/Resources/LanguagePicker.prefab";
    private const string FlagFolder = "Assets/00_Project/04_ِArt/UI/Flags";
    private const string SpriteFolder = "Assets/00_Project/04_ِArt/UI/Sprites";
    private const string FontFolder = "Assets/00_Project/04_ِArt/UI/Fonts";

    private static readonly Color FrameColor = new Color32(0x33, 0x36, 0x4B, 0xFF);
    private static readonly Color HeaderColor = new Color32(0x1E, 0x20, 0x2E, 0xFF);
    private static readonly Color ButtonColor = new Color32(0x4A, 0x4E, 0x6A, 0xFF);
    private static readonly Color CloseColor = new Color32(0xE1, 0x26, 0x2E, 0xFF);

    [MenuItem("Tools/Localization/Build Language UI")]
    public static void Build()
    {
        LinkFlags();
        Scene scene = EditorSceneManager.NewPreviewScene();
        try
        {
            BuildRow(scene);
            BuildPicker(scene);
            BuildButton(scene);
            Debug.Log("Language UI built: " + RowPath + ", " + PickerPath + ", " + ButtonPath);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
        AssetDatabase.SaveAssets();
    }

    /// <summary> Imports the flag PNGs as sprites and assigns them to the database languages. </summary>
    public static void LinkFlags()
    {
        LocalizationDatabase_SO db = LocalizationCsvTool.LoadOrCreateDatabase();
        var serialized = new SerializedObject(db);
        SerializedProperty list = serialized.FindProperty("languages");

        for (int i = 0; i < list.arraySize; i++)
        {
            SerializedProperty entry = list.GetArrayElementAtIndex(i);
            string code = entry.FindPropertyRelative("Code").stringValue;
            string path = FlagFolder + "/Flag_" + code + ".png";
            if (!File.Exists(path)) continue;

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.textureType != TextureImporterType.Sprite || importer.mipmapEnabled)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            entry.FindPropertyRelative("Flag").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(db);
    }

    private static void BuildRow(Scene scene)
    {
        GameObject root = NewUi("Language Row", scene, null, new Vector2(330f, 140f));
        var bg = root.AddComponent<Image>();
        bg.sprite = Sprite("UI_ButtonLip");
        bg.type = Image.Type.Sliced;
        bg.color = ButtonColor;
        var button = root.AddComponent<Button>();
        button.targetGraphic = bg;

        GameObject flagObject = NewUi("Flag", scene, root.transform, new Vector2(110f, 78f));
        var flagRect = (RectTransform)flagObject.transform;
        flagRect.anchorMin = flagRect.anchorMax = new Vector2(0f, 0.5f);
        flagRect.pivot = new Vector2(0.5f, 0.5f);
        flagRect.anchoredPosition = new Vector2(75f, 5f);
        var flag = flagObject.AddComponent<Image>();
        flag.preserveAspect = true;
        flag.raycastTarget = false;

        TMP_Text label = NewLabel("Label", scene, root.transform, 44f, "Fredoka-SemiBold SDF Material");
        var labelRect = (RectTransform)label.transform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(140f, 14f);
        labelRect.offsetMax = new Vector2(-12f, 0f);
        label.enableAutoSizing = true;
        label.fontSizeMin = 22f;
        label.fontSizeMax = 44f;
        label.textWrappingMode = TextWrappingModes.NoWrap;

        var ui = root.AddComponent<LanguageButtonUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("button").objectReferenceValue = button;
        so.FindProperty("background").objectReferenceValue = bg;
        so.FindProperty("flag").objectReferenceValue = flag;
        so.FindProperty("label").objectReferenceValue = label;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, RowPath);
    }

    private static void BuildPicker(Scene scene)
    {
        var rowPrefab = AssetDatabase.LoadAssetAtPath<LanguageButtonUI>(RowPath);

        GameObject root = NewUi("LanguagePicker", scene, null, Vector2.zero);
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();

        GameObject backdrop = NewUi("Backdrop", scene, root.transform, Vector2.zero);
        Stretch((RectTransform)backdrop.transform);
        backdrop.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        GameObject window = NewUi("Window", scene, root.transform, new Vector2(760f, 820f));
        var windowImage = window.AddComponent<Image>();
        windowImage.sprite = Sprite("UI_RoundedBordered");
        windowImage.type = Image.Type.Sliced;
        windowImage.color = FrameColor;

        GameObject header = NewUi("Header", scene, window.transform, new Vector2(-10f, 120f));
        var headerRect = (RectTransform)header.transform;
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = new Vector2(0f, -5f);
        header.AddComponent<Image>().color = HeaderColor;

        TMP_Text title = NewLabel("Title", scene, header.transform, 70f, "Fredoka-SemiBold SDF - Outline");
        Stretch((RectTransform)title.transform);
        title.text = "Language";
        title.gameObject.AddComponent<LocalizedText>();
        var titleKey = new SerializedObject(title.GetComponent<LocalizedText>());
        titleKey.FindProperty("key").stringValue = "language.title";
        titleKey.ApplyModifiedPropertiesWithoutUndo();

        GameObject close = NewUi("Close Button", scene, window.transform, new Vector2(100f, 100f));
        var closeRect = (RectTransform)close.transform;
        closeRect.anchorMin = closeRect.anchorMax = Vector2.one;
        closeRect.pivot = Vector2.one;
        closeRect.anchoredPosition = new Vector2(-18f, -14f);
        var closeImage = close.AddComponent<Image>();
        closeImage.sprite = Sprite("UI_ButtonLip");
        closeImage.type = Image.Type.Sliced;
        closeImage.color = CloseColor;
        var closeButton = close.AddComponent<Button>();
        closeButton.targetGraphic = closeImage;
        GameObject x = NewUi("X", scene, close.transform, new Vector2(58f, 58f));
        ((RectTransform)x.transform).anchoredPosition = new Vector2(0f, 6f);
        var xImage = x.AddComponent<Image>();
        xImage.sprite = Sprite("UI_CloseX");
        xImage.raycastTarget = false;

        GameObject list = NewUi("List", scene, window.transform, Vector2.zero);
        var listRect = (RectTransform)list.transform;
        Stretch(listRect);
        listRect.offsetMin = new Vector2(40f, 40f);
        listRect.offsetMax = new Vector2(-40f, -160f);
        var grid = list.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(330f, 140f);
        grid.spacing = new Vector2(20f, 20f);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;

        var picker = root.AddComponent<LanguagePickerUI>();
        var so = new SerializedObject(picker);
        so.FindProperty("window").objectReferenceValue = window.transform;
        so.FindProperty("listRoot").objectReferenceValue = listRect;
        so.FindProperty("buttonPrefab").objectReferenceValue = rowPrefab;
        so.FindProperty("closeButton").objectReferenceValue = closeButton;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, PickerPath);
    }

    private static void BuildButton(Scene scene)
    {
        GameObject root = NewUi("Language Button", scene, null, new Vector2(170f, 170f));
        var bg = root.AddComponent<Image>();
        bg.sprite = Sprite("UI_ButtonLip");
        bg.type = Image.Type.Sliced;
        bg.color = ButtonColor;
        var button = root.AddComponent<Button>();
        button.targetGraphic = bg;
        var layout = root.AddComponent<LayoutElement>();
        layout.preferredWidth = 170f;
        layout.preferredHeight = 170f;

        GameObject icon = NewUi("Flag", scene, root.transform, new Vector2(124f, 84f));
        ((RectTransform)icon.transform).anchoredPosition = new Vector2(0f, 5f);
        var flag = icon.AddComponent<Image>();
        flag.preserveAspect = true;
        flag.raycastTarget = false;

        var languageButton = root.AddComponent<LanguageButton>();
        var so = new SerializedObject(languageButton);
        so.FindProperty("flag").objectReferenceValue = flag;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, ButtonPath);
    }

    private static GameObject NewUi(string name, Scene scene, Transform parent, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        SceneManager.MoveGameObjectToScene(go, scene);
        if (parent != null) go.transform.SetParent(parent, false);
        ((RectTransform)go.transform).sizeDelta = size;
        return go;
    }

    private static TMP_Text NewLabel(string name, Scene scene, Transform parent, float size, string materialName)
    {
        GameObject go = NewUi(name, scene, parent, Vector2.zero);
        var text = go.AddComponent<TextMeshProUGUI>();
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontFolder + "/Fredoka-SemiBold SDF.asset");
        text.font = font;
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        Material material = materialName.EndsWith("Outline")
            ? AssetDatabase.LoadAssetAtPath<Material>(FontFolder + "/Fredoka-SemiBold SDF - Outline.mat")
            : null;
        if (material != null) text.fontSharedMaterial = material;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + "/" + name + ".png");
}
#endif
