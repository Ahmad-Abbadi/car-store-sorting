using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ToyShop.UI.Editor
{
    /// <summary>Reapplies the shared theme to existing player UI without replacing controller objects.</summary>
    public static class ToyShopUIBuilder
    {
        private const string Art = "Assets/00_Project/04_ِArt/UI/";
        public const string ThemePath = "Assets/00_Project/03_scriptableObjects/Resources/ToyShopTheme.asset";

        [MenuItem("Tools/Tiny Riders/Apply Toy Shop UI Theme")]
        public static void ApplyAll()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop play mode before authoring UI.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save scene changes before applying the UI theme.");

            var theme = AssetDatabase.LoadAssetAtPath<ToyShopTheme>(ThemePath);
            if (!theme) { theme = ScriptableObject.CreateInstance<ToyShopTheme>(); AssetDatabase.CreateAsset(theme, ThemePath); }
            theme.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Art + "Fonts/Fredoka-SemiBold SDF.asset");
            theme.rounded = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "Sprites/UI_Flat_Rounded.png");
            theme.circle = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "Sprites/UI_Circle.png");
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();

            var prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/00_Project/02_prefabs/UI" })
                .Select(AssetDatabase.GUIDToAssetPath).Concat(new[] {
                    "Assets/00_Project/03_scriptableObjects/Resources/LanguagePicker.prefab",
                    "Assets/00_Project/02_prefabs/Player.prefab"
                }).Distinct().ToArray();
            foreach (string path in prefabs)
            {
                if (path.Contains("Empty Canvas") || path.Contains("Screen fader")) continue;
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (path.EndsWith("Player.prefab"))
                    {
                        var canvases = root.GetComponentsInChildren<Canvas>(true);
                        if (canvases.Length == 0) continue;
                        foreach (var canvas in canvases)
                            if (canvas.renderMode != RenderMode.WorldSpace) ApplyRoot(canvas.gameObject);
                    }
                    else ApplyRoot(root);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }

            Scene active = SceneManager.GetActiveScene();
            string[] scenes = { "Assets/00_Project/00_Scenes/MainMenu Scene.unity", "Assets/00_Project/00_Scenes/Loading Scene.unity",
                "Assets/00_Project/00_Scenes/Level.unity", "Assets/TinyRiders/Scenes/ThemedSorting.unity" };
            foreach (string path in scenes)
            {
                Scene scene = SceneManager.GetSceneByPath(path);
                bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    foreach (var root in scene.GetRootGameObjects())
                        foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
                            if (canvas.renderMode != RenderMode.WorldSpace && !canvas.GetComponent<ScreenFader>()) ApplyRoot(canvas.gameObject);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
            if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
            AssetDatabase.SaveAssets();
            Debug.Log("Toy shop UI applied to player scenes and shared UI prefabs.");
        }

        public static void ApplyRoot(GameObject root)
        {
            ToyShopStyle.Apply(root);
            var menuButtons = root.transform.Find("Menu Card/Buttons");
            if (menuButtons && menuButtons.TryGetComponent(out UnityEngine.UI.VerticalLayoutGroup layout))
                layout.spacing = ToyShopTheme.Current.spacing;
            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour is PeriodicShake || behaviour is SweepHighlighter || behaviour is TextWordPulse) behaviour.enabled = false;
                if (behaviour is CoinsCounterUI || behaviour is ItemsCounterUI)
                {
                    var settings = new SerializedObject(behaviour);
                    settings.FindProperty("punchScale").vector3Value = new Vector3(.045f, .045f, 0);
                    settings.FindProperty("punchDuration").floatValue = .2f;
                    settings.FindProperty("punchVibrato").intValue = 2;
                    settings.FindProperty("countDuration").floatValue = .22f;
                    settings.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            foreach (var image in root.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                string n = image.name;
                if (n == "Frame" || n == "Menu Card" || (n == "Window" && image.rectTransform.sizeDelta.x > 0))
                    if (!image.GetComponent<ToyShopPanelEntrance>()) image.gameObject.AddComponent<ToyShopPanelEntrance>();
            }
            // Confirmation panels retain their existing immediate activation/close semantics.
            foreach (var rect in root.GetComponentsInChildren<RectTransform>(true))
                if (rect.name == "Confirm_Panel" && !rect.GetComponent<ToyShopPanelEntrance>()) rect.gameObject.AddComponent<ToyShopPanelEntrance>();
            foreach (var touch in root.GetComponentsInChildren<TouchButton>(true))
                foreach (var icon in touch.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                    icon.color = icon.transform == touch.transform ? ToyShopTheme.Current.cream : ToyShopTheme.Current.navy;
            EditorUtility.SetDirty(root);
        }
    }
}
