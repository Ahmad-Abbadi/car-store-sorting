using System.Linq;
using ByteBrewSDK;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Idempotent authoring helper; saves only the startup scene and ByteBrew settings.</summary>
public static class ByteBrewIntegrationSetup
{
    public const string StartupScene = "Assets/00_Project/00_Scenes/Initializer.unity";

    [MenuItem("Tools/Toy Car Sort/Analytics/Configure ByteBrew")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new System.InvalidOperationException("Configure ByteBrew outside Play mode.");
        var settings = AssetDatabase.LoadAssetAtPath<ByteBrewSettings>("Assets/ByteBrewSDK/Resources/ByteBrewSettings.asset");
        if (!settings) throw new System.InvalidOperationException("Wait for ByteBrew settings to finish importing.");
        settings.webEnabled = true;
        settings.androidEnabled = false;
        settings.iosEnabled = false;
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssetIfDirty(settings);

        var previous = SceneManager.GetActiveScene();
        var scene = SceneManager.GetSceneByPath(StartupScene);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(StartupScene, OpenSceneMode.Additive);
        try
        {
            var sdk = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<ByteBrew>(true)).SingleOrDefault();
            if (!sdk)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ByteBrewSDK/Prefabs/ByteBrew.prefab");
                sdk = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, scene)).GetComponent<ByteBrew>();
            }
            var initializer = sdk.GetComponent<ByteBrewInitializer>();
            if (!initializer) initializer = sdk.gameObject.AddComponent<ByteBrewInitializer>();
            var startup = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<GameInitializer>(true)).Single();
            var serialized = new SerializedObject(startup);
            var systems = serialized.FindProperty("systems");
            bool present = Enumerable.Range(0, systems.arraySize).Any(i => systems.GetArrayElementAtIndex(i).objectReferenceValue == initializer);
            if (!present)
            {
                systems.InsertArrayElementAtIndex(systems.arraySize);
                systems.GetArrayElementAtIndex(systems.arraySize - 1).objectReferenceValue = initializer;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        finally
        {
            if (previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
    }
}
