using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Imports and assigns the licensed replacement library through Unity's serialization APIs.</summary>
public static class PlayfulAudioBuilder
{
    public const string Root = "Assets/00_Project/Audio/PackageSelection/";
    private static readonly string[] ProjectRoots = { "Assets/00_Project", "Assets/TinyRiders" };
    private static readonly List<string> Changes = new List<string>();

    private static AudioClip Clip(string name, bool music = false)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + (music ? "Music/" : "SFX/") + name + ".wav");
        if (!clip) throw new InvalidOperationException("Missing replacement clip: " + name);
        return clip;
    }

    private static GameSound Sound(string name, float volume) => new GameSound(Clip(name), volume, 1f);
    private static List<GameSound> Variations(string prefix, int count, float volume) =>
        Enumerable.Range(1, count).Select(i => Sound(prefix + i, volume)).ToList();

    [MenuItem("Tools/Tiny Riders/Audio/Apply Playful Audio")]
    public static void Apply()
    {
        Apply(true);
    }

    [MenuItem("Tools/Tiny Riders/Audio/Apply Action Sounds and Button Audio")]
    public static void ApplyActionSounds() => Apply(false);

    private static void Apply(bool updateMusic)
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before assigning audio.");
        Changes.Clear();
        ConfigureImports(updateMusic);
        var library = AssetDatabase.LoadAssetAtPath<GameplaySounds_SO>("Assets/00_Project/03_scriptableObjects/GameplaySounds.asset");
        Undo.RecordObject(library, "Replace gameplay audio");
        library.StepsSounds = Variations("Footstep_", 4, .28f);
        library.JumpSound = Sound("Jump", .22f);
        library.CollectingSounds = Variations("Pickup_", 3, .48f);
        library.ThrowingSounds = Variations("Throw_", 3, .35f);
        library.ShelfPlacementSounds = Variations("Place_", 3, .5f);
        library.WrongSFX = Sound("Refuse", .24f);
        library.WrongSectionSFX = Sound("WrongSection", .4f);
        library.CompleteShelfSFX = Sound("Complete", .5f);
        library.UIClick = Sound("UIClick", .25f);
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssetIfDirty(library);
        Changes.Add("Replaced shared gameplay library; reset pitch and cutoffs; populated WrongSectionSFX.");

        // Only inspect prefabs that serialize audio, not the third-party or generated art libraries.
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", ProjectRoots))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string text = File.ReadAllText(path);
            if (!text.Contains("Clip:") && !text.Contains("m_audioClip:") && !text.Contains("_MusicSource:") && !text.Contains("m_OnClick:")) continue;
            var root = PrefabUtility.LoadPrefabContents(path);
            try { if (ReplaceRoot(root, path, false, updateMusic)) PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        Scene active = SceneManager.GetActiveScene();
        foreach (var settings in EditorBuildSettings.scenes.Where(s => s.enabled))
        {
            Scene scene = SceneManager.GetSceneByPath(settings.path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(settings.path, OpenSceneMode.Additive);
            bool changed = false;
            foreach (var root in scene.GetRootGameObjects()) changed |= ReplaceRoot(root, settings.path, scene.name == "MainMenu Scene", updateMusic);
            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
        if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
        Directory.CreateDirectory("IntegrationEvidence/AudioPackageRefresh");
        File.WriteAllLines("IntegrationEvidence/AudioPackageRefresh/assignments.txt", Changes);
        Debug.Log("Playful audio applied. " + Changes.Count + " assignment records.");
    }

    private static void ConfigureImports(bool updateMusic)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { Root.TrimEnd('/') }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            bool music = path.Contains("/Music/");
            if (music && !updateMusic) continue;
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            importer.forceToMono = !music;
            // Gains were measured offline; don't let importer normalization undo them.
            var serialized = new SerializedObject(importer);
            var normalize = serialized.FindProperty("m_Normalize");
            if (normalize != null) { normalize.boolValue = false; serialized.ApplyModifiedPropertiesWithoutUndo(); }
            importer.loadInBackground = false;
            var sample = importer.defaultSampleSettings;
            sample.preloadAudioData = true;
            sample.loadType = music ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            sample.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
            sample.quality = .85f;
            sample.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings = sample;
            // WebGL has no separate AudioImporter override in this Editor. Unity's Web
            // build pipeline encodes AAC using these defaults. Do not use Streaming.
            importer.SaveAndReimport();
        }
    }

    private static bool ReplaceRoot(GameObject root, string path, bool menu, bool updateMusic)
    {
        bool changed = false;
        foreach (var button in root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
        {
            if (button.GetComponent<UIButtonAudioFeedback>()) continue;
            UIButtonAudioFeedback.Ensure(button);
            Changes.Add(path + ": button click " + button.name);
            changed = true;
        }
        foreach (var manager in root.GetComponentsInChildren<SimpleAudioManager>(true))
        {
            if (!updateMusic) continue;
            var so = new SerializedObject(manager);
            so.FindProperty("_menuMusic").objectReferenceValue = Clip("Menu_LifeIsSimple", true);
            so.FindProperty("_gameplayMusic").objectReferenceValue = Clip("Gameplay_LifeIsSimple", true);
            so.FindProperty("musicFadeDuration").floatValue = 1f;
            var source = so.FindProperty("_MusicSource").objectReferenceValue as AudioSource;
            var template = so.FindProperty("_audioSourcePrefab").objectReferenceValue as GameObject;
            so.ApplyModifiedPropertiesWithoutUndo();
            if (template && template.TryGetComponent<AudioSource>(out var effectsSource))
            {
                effectsSource.clip = null;
                effectsSource.playOnAwake = false;
                effectsSource.loop = false;
                EditorUtility.SetDirty(effectsSource);
                if (PrefabUtility.IsPartOfPrefabInstance(effectsSource)) PrefabUtility.RecordPrefabInstancePropertyModifications(effectsSource);
            }
            if (source)
            {
                source.clip = Clip(menu ? "Menu_LifeIsSimple" : "Gameplay_LifeIsSimple", true);
                source.volume = .18f;
                source.pitch = 1;
                source.loop = true;
                source.playOnAwake = false;
                source.spatialBlend = 0;
                EditorUtility.SetDirty(source);
                if (PrefabUtility.IsPartOfPrefabInstance(source)) PrefabUtility.RecordPrefabInstancePropertyModifications(source);
            }
            EditorUtility.SetDirty(manager);
            if (PrefabUtility.IsPartOfPrefabInstance(manager)) PrefabUtility.RecordPrefabInstancePropertyModifications(manager);
            Changes.Add(path + ": music manager " + manager.name);
            changed = true;
        }
        foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (!component || component is SimpleAudioManager) continue;
            var so = new SerializedObject(component);
            var it = so.GetIterator();
            bool modified = false;
            while (it.Next(true))
            {
                if (it.type != "GameSound") continue;
                var clip = it.FindPropertyRelative("Clip");
                // Optional empty sounds stay optional, avoiding doubled pickup feedback.
                if (clip == null || !clip.objectReferenceValue) continue;
                string name = Replacement(it.propertyPath + " " + clip.objectReferenceValue.name);
                Changes.Add(path + ": " + component.name + "/" + it.propertyPath + " -> " + name);
                clip.objectReferenceValue = Clip(name);
                it.FindPropertyRelative("Pitch").floatValue = 1f;
                it.FindPropertyRelative("Volume").floatValue = .4f;
                it.FindPropertyRelative("Cutoff").vector2Value = new Vector2(0, Clip(name).length);
                modified = true;
            }
            if (modified)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(component);
                if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                changed = true;
            }
        }
        foreach (var source in root.GetComponentsInChildren<AudioSource>(true))
        {
            if (source.loop && !updateMusic) continue;
            if (!source.clip || AssetDatabase.GetAssetPath(source.clip).StartsWith(Root, StringComparison.Ordinal)) continue;
            Changes.Add(path + ": direct AudioSource " + source.name + " -> " + source.clip.name);
            source.clip = source.loop ? Clip(menu ? "Menu_LifeIsSimple" : "Gameplay_LifeIsSimple", true) : Clip(Replacement(source.clip.name));
            source.pitch = 1;
            source.volume = source.loop ? .18f : .4f;
            EditorUtility.SetDirty(source);
            if (PrefabUtility.IsPartOfPrefabInstance(source)) PrefabUtility.RecordPrefabInstancePropertyModifications(source);
            changed = true;
        }
        return changed;
    }

    private static string Replacement(string key)
    {
        key = key.ToLowerInvariant();
        if (key.Contains("wrongsection") || key.Contains("wrong_section")) return "WrongSection";
        if (key.Contains("wrong") || key.Contains("refuse") || key.Contains("cant")) return "Refuse";
        if (key.Contains("step")) return "Footstep_1";
        if (key.Contains("jump")) return "Jump";
        if (key.Contains("complete") || key.Contains("level up")) return "Complete";
        if (key.Contains("pickup") || key.Contains("collect")) return "Pickup_1";
        if (key.Contains("throw")) return "Throw_1";
        if (key.Contains("shelf") || key.Contains("place")) return "Place_1";
        if (key.Contains("hurt")) return "Hurt";
        if (key.Contains("sweep") || key.Contains("swoosh") || key.Contains("whoosh")) return "Sweep";
        if (key.Contains("shake")) return "Shake";
        return "UIClick";
    }
}
