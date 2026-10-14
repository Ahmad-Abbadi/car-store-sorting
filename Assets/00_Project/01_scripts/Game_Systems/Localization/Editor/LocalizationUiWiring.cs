#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary> Menu tool that puts LocalizedText on the menu, HUD and panel labels. </summary>
public static class LocalizationUiWiring
{
    private const string PrefabRoot = "Assets/00_Project/02_prefabs/";
    private const string MainMenuScenePath = "Assets/00_Project/00_Scenes/MainMenu Scene.unity";

    private struct Entry
    {
        public string Where;   // prefab path under PrefabRoot, or "scene:Name"
        public string Suffix;  // hierarchy path suffix of the label
        public string Key;
        public string English;
    }

    private static readonly Entry[] Entries =
    {
        P("UI/Pause Panel Canvas.prefab", "Panel/Frame/Header/Title", "pause.title", "Paused"),
        P("UI/Pause Panel Canvas.prefab", "Panel/Frame/Content/Button_Continue/Text (TMP)", "ui.continue", "Continue"),
        P("UI/Pause Panel Canvas.prefab", "Panel/Frame/Content/Button_Restart/Text (TMP)", "ui.restart", "Restart"),
        P("UI/Pause Panel Canvas.prefab", "Panel/Frame/Content/Button_Quit/Text (TMP)", "ui.quit", "Quit"),
        P("UI/Pause Panel Canvas.prefab", "Sensitivity_Row/Label", "pause.sensitivity", "Sensitivity"),
        P("UI/Pause Panel Canvas.prefab", "Reset_Group/Button_Reset/Text (TMP)", "ui.reset_progress", "Reset Progress"),
        P("UI/Pause Panel Canvas.prefab", "Reset_Group/Confirm_Panel/Message", "ui.reset_message", "Reset all progress?\nThis can't be undone."),
        P("UI/Pause Panel Canvas.prefab", "Reset_Group/Confirm_Panel/Button_Yes/Text (TMP)", "ui.yes", "Yes"),
        P("UI/Pause Panel Canvas.prefab", "Reset_Group/Confirm_Panel/Button_No/Text (TMP)", "ui.no", "No"),
        P("UI/Controls Hint Canvas.prefab", "Pause Row/Label", "controls.pause", "Pause"),
        P("UI/Controls Hint Canvas.prefab", "Place Row/Label", "controls.place", "Place"),
        P("UI/Controls Hint Canvas.prefab", "Jump Row/Label", "controls.jump", "Jump"),
        P("UI/Controls Hint Canvas.prefab", "Collect Row/Label", "controls.collect", "Collect"),
        P("UI/Controls Hint Canvas.prefab", "Switch Row/Label", "controls.switch", "Switch"),
        P("UI/Upgrades_UI/Upgrade Panel Canvas.prefab", "Header/Title", "upgrade.panel_title", "Upgrades"),

        S("Level", "Lose Panel  Canvas  /Panel/Pause_Panel/Button_Continue/Text (TMP)", "ui.continue", "Continue"),
        S("Level", "Lose Panel  Canvas  /Panel/Pause_Panel/Button_Restart/Text (TMP)", "ui.restart", "Restart"),
        S("Level", "Lose Panel  Canvas  /Panel/Pause_Panel/Button_Quit/Text (TMP)", "ui.quit", "Quit"),

        S("MainMenu Scene", "Main Menu Canvas/Menu Card/Buttons/Button_Play/Text (TMP)", "menu.play", "Play"),
        S("MainMenu Scene", "Main Menu Canvas/Reset_Group/Button_Reset/Theme label Text (TMP)", "ui.reset_progress", "Reset Progress"),
        S("MainMenu Scene", "Main Menu Canvas/Reset_Group/Confirm_Panel/Message", "ui.reset_message", "Reset all progress?\nThis can't be undone."),
        S("MainMenu Scene", "Main Menu Canvas/Reset_Group/Confirm_Panel/Button_Yes/Theme label Text (TMP)", "ui.yes", "Yes"),
        S("MainMenu Scene", "Main Menu Canvas/Reset_Group/Confirm_Panel/Button_No/Theme label Text (TMP)", "ui.no", "No"),
    };

    // Strings set from code with fixed keys; no label carries them.
    private static readonly string[,] CodeStrings =
    {
        { "language.title", "Language" },
        { "menu.details", "Details: {0}" },
        { "menu.detail.high", "High" },
        { "menu.detail.medium", "Medium" },
        { "menu.detail.low", "Low" },
        { "prompt.hold", "Hold" },
        { "prompt.place", "Place" },
        { "upgrade.max", "MAX" },
        { "feedback.inventory_full", "Inventory Full" },
        { "feedback.wrong_section", "Wrong Section" },
    };

    /// <summary> Every label key with its English source text. </summary>
    public static IEnumerable<KeyValuePair<string, string>> AllStrings()
    {
        foreach (Entry entry in Entries) yield return new KeyValuePair<string, string>(entry.Key, entry.English);
        for (int i = 0; i < CodeStrings.GetLength(0); i++)
            yield return new KeyValuePair<string, string>(CodeStrings[i, 0], CodeStrings[i, 1]);
    }

    private static Entry P(string prefab, string suffix, string key, string english) =>
        new Entry { Where = prefab, Suffix = suffix, Key = key, English = english };

    private static Entry S(string scene, string suffix, string key, string english) =>
        new Entry { Where = "scene:" + scene, Suffix = suffix, Key = key, English = english };

    [MenuItem("Tools/Localization/Wire UI Labels (prefabs + open scenes)")]
    public static void WireMenu()
    {
        int count = 0;
        var byPrefab = new Dictionary<string, List<Entry>>();
        foreach (Entry entry in Entries)
        {
            if (entry.Where.StartsWith("scene:")) continue;
            if (!byPrefab.TryGetValue(entry.Where, out List<Entry> list)) byPrefab[entry.Where] = list = new List<Entry>();
            list.Add(entry);
        }

        foreach (var pair in byPrefab)
        {
            string path = PrefabRoot + pair.Key;
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (Entry entry in pair.Value) count += Apply(root.transform, entry) ? 1 : 0;
                if (pair.Key == "UI/Pause Panel Canvas.prefab") AddPauseLanguageButton(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        count += WireScene("Level", saveScene: false);
        count += WireMainMenu();
        AssetDatabase.SaveAssets();
        Debug.Log($"Localization wiring: {count} labels linked.");
    }

    /// <summary> Wires a scene that is already open; saves nothing. </summary>
    private static int WireScene(string sceneName, bool saveScene)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.isLoaded) return 0;
        return WireLoadedScene(scene, saveScene);
    }

    private static int WireLoadedScene(Scene scene, bool saveScene)
    {
        int count = 0;
        foreach (Entry entry in Entries)
        {
            if (entry.Where != "scene:" + scene.name) continue;
            bool found = false;
            foreach (GameObject root in scene.GetRootGameObjects())
                if (Apply(root.transform, entry, warn: false)) { count++; found = true; break; }
            if (!found) Debug.LogWarning($"Localization wiring: label '{entry.Suffix}' not found in {entry.Where}.");
        }

        if (saveScene && count > 0) EditorSceneManager.SaveScene(scene);
        return count;
    }

    /// <summary> Opens the main menu next to the current scene, wires it, saves it and closes it. </summary>
    private static int WireMainMenu()
    {
        Scene scene = SceneManager.GetSceneByName("MainMenu Scene");
        bool wasOpen = scene.isLoaded;
        if (!wasOpen) scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Additive);

        int count = WireLoadedScene(scene, saveScene: false);
        AddMenuLanguageButton(scene);
        EditorSceneManager.SaveScene(scene);
        if (!wasOpen) EditorSceneManager.CloseScene(scene, true);
        return count;
    }

    private static GameObject LanguageButtonPrefab() =>
        AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "UI/Language Button.prefab");

    /// <summary> Adds the flag button to the pause menu's round-button row once. </summary>
    private static void AddPauseLanguageButton(Transform root)
    {
        Transform row = FindDeep(root, "Audio Buttons");
        if (row == null || row.Find("Language Button") != null) return;

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(LanguageButtonPrefab(), row);
        instance.transform.SetAsLastSibling();

        var rect = (RectTransform)row;
        float spacing = row.TryGetComponent(out UnityEngine.UI.HorizontalLayoutGroup layout) ? layout.spacing : 30f;
        rect.sizeDelta = new Vector2(row.childCount * 170f + (row.childCount - 1) * spacing, rect.sizeDelta.y);
    }

    /// <summary> Adds the flag button to the top-right of the main menu canvas once. </summary>
    private static void AddMenuLanguageButton(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform canvas = FindDeep(root.transform, "Main Menu Canvas");
            if (canvas == null || canvas.Find("Language Button") != null) continue;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(LanguageButtonPrefab(), canvas);
            var rect = (RectTransform)instance.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-40f, -40f);
            return;
        }
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform child in root)
        {
            Transform found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary> Finds the label under root by path suffix and sets its key. </summary>
    private static bool Apply(Transform root, Entry entry, bool warn = true)
    {
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            string path = PathOf(text.transform, root);
            if (path != entry.Suffix && !path.EndsWith("/" + entry.Suffix)) continue;

            var label = text.GetComponent<LocalizedText>();
            if (label == null) label = text.gameObject.AddComponent<LocalizedText>();
            var so = new SerializedObject(label);
            so.FindProperty("key").stringValue = entry.Key;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(label);
            return true;
        }
        if (warn) Debug.LogWarning($"Localization wiring: label '{entry.Suffix}' not found in {entry.Where}.");
        return false;
    }

    private static string PathOf(Transform t, Transform root)
    {
        string path = t.name;
        for (Transform p = t.parent; p != null; p = p.parent)
        {
            path = p.name + "/" + path;
            if (p == root) break;
        }
        return path;
    }
}
#endif
