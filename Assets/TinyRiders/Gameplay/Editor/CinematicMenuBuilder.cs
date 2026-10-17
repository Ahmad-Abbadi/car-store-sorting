using System;
using System.IO;
using System.Linq;
using TinyRiders;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Authors a visual-only snapshot; never runs a collection session or reads player saves.</summary>
public static class CinematicMenuBuilder
{
    public const string MenuPath = "Assets/00_Project/00_Scenes/MainMenu Scene.unity";
    private const string LevelPath = "Assets/00_Project/00_Scenes/Level.unity";
    [Serializable] private class Pile { public ThemedInstanceRecord[] placements; }

    [MenuItem("Tools/Tiny Riders/Cinematic Menu/Rebuild From Store")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
        var menu = SceneManager.GetSceneByPath(MenuPath);
        if (!menu.isLoaded) menu = EditorSceneManager.OpenScene(MenuPath, OpenSceneMode.Additive);
        Directory.CreateDirectory("IntegrationEvidence/CinematicMenu");
        var backup = "IntegrationEvidence/CinematicMenu/MainMenu_before_cinematics.unity";
        if (!File.Exists(backup)) EditorSceneManager.SaveScene(menu, backup, true);
        var level = SceneManager.GetSceneByPath(LevelPath);
        bool openedLevel = !level.isLoaded;
        if (openedLevel) level = EditorSceneManager.OpenScene(LevelPath, OpenSceneMode.Additive);
        var generated = level.GetRootGameObjects().Single(g => g.name == "Car Store Generated");
        var session = generated.GetComponentInChildren<ThemedCollectionSession>(true);
        SceneManager.SetActiveScene(level);
        var sky = RenderSettings.skybox;
        var ambient = RenderSettings.ambientLight;
        var ambientMode = RenderSettings.ambientMode;
        var ambientIntensity = RenderSettings.ambientIntensity;
        var fog = RenderSettings.fog;
        SceneManager.SetActiveScene(menu);
        RenderSettings.skybox = sky;
        RenderSettings.ambientMode = ambientMode;
        RenderSettings.ambientLight = ambient;
        RenderSettings.ambientIntensity = ambientIntensity;
        RenderSettings.fog = fog;
        foreach (var old in menu.GetRootGameObjects().Where(g => g.name == "Cinematic Store" || g.name == "Cinematic Cameras"))
            Object.DestroyImmediate(old);
        var stage = new GameObject("Cinematic Store");
        stage.SetActive(false);
        // A scene snapshot retains nested prefab instances and their overrides. Instantiate
        // on a scene hierarchy expands them and needlessly duplicates every shelf mesh node.
        const string scratchPath = "Assets/TinyRiders/Gameplay/Editor/CinematicMenuStageScratch.unity";
        if (File.Exists(scratchPath)) throw new InvalidOperationException("Cinematic menu scratch scene already exists: " + scratchPath);
        GameObject copy;
        Scene scratch = default;
        try
        {
            EditorSceneManager.SaveScene(level, scratchPath, true);
            AssetDatabase.ImportAsset(scratchPath);
            scratch = EditorSceneManager.OpenScene(scratchPath, OpenSceneMode.Additive);
            copy = scratch.GetRootGameObjects().Single(g => g.name == "Car Store Generated");
            SceneManager.MoveGameObjectToScene(copy, menu);
            copy.transform.SetParent(stage.transform, true);
        }
        finally
        {
            if (scratch.isLoaded) EditorSceneManager.CloseScene(scratch, true);
            AssetDatabase.DeleteAsset(scratchPath);
            SceneManager.SetActiveScene(menu);
        }
        copy.name = "Store Visual Snapshot";
        StripGameplay(copy);
        foreach (var light in level.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Light>(true)))
        {
            if (light.transform.IsChildOf(generated.transform)) continue;
            var lightCopy = Object.Instantiate(light.gameObject, stage.transform);
            StripGameplay(lightCopy);
        }
        PopulateCars(copy, stage.transform, session);
        stage.SetActive(true);
        BuildCameras(menu);
        PolishMenu(menu);
        if (openedLevel) EditorSceneManager.CloseScene(level, true);
        ShowroomLightingBuilder.Apply(menu);
        ShowroomLightingBuilder.BakeReflection(menu);
        EditorSceneManager.MarkSceneDirty(menu);
        EditorSceneManager.SaveScene(menu);
        Debug.Log("CINEMATIC_MENU_BUILT: visual-only store, six shots, 48 second loop.");
    }

    private static void StripGameplay(GameObject root)
    {
        if (PrefabUtility.IsPartOfPrefabInstance(root))
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
        foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true).Reverse())
            if (behaviour != null && !(behaviour is TMP_Text) && !(behaviour is UnityEngine.Rendering.Volume)
                && !(behaviour is UnityEngine.Rendering.Universal.UniversalAdditionalLightData)) Object.DestroyImmediate(behaviour);
        foreach (var c in root.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        foreach (var c in root.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(c);
        foreach (var c in root.GetComponentsInChildren<Camera>(true)) Object.DestroyImmediate(c);
        foreach (var c in root.GetComponentsInChildren<AudioListener>(true)) Object.DestroyImmediate(c);
        foreach (var c in root.GetComponentsInChildren<AudioSource>(true)) Object.DestroyImmediate(c);
    }

    private static void PopulateCars(GameObject snapshot, Transform parent, ThemedCollectionSession session)
    {
        var cars = new GameObject("Staged Collection Cars").transform;
        cars.SetParent(parent, false);
        var definitions = session.catalog.entries.Where(e => e.enabled && e.sortingItem != null).ToArray();
        var shelves = session.GetComponentsInChildren<SortingShelf>(true);
        foreach (var shelf in shelves)
        {
            var theme = shelf.GetComponent<ThemeShelfBinding>().themeId;
            var choices = definitions.Where(e => e.themeId == theme).ToArray();
            var clone = snapshot.GetComponentsInChildren<Transform>(true).First(t => t.name == shelf.name);
            var slots = clone.GetComponentsInChildren<Transform>(false).Where(t => t.name.StartsWith("Slot_R", StringComparison.Ordinal))
                .OrderBy(t => t.name, StringComparer.Ordinal).ToArray();
            for (int i = 0; i < slots.Length; i++)
            {
                // A varied, partly stocked store. Fill the featured shelf for the detail shots.
                if (i % 3 != 0 && shelf.name != "Barbershop - 2") continue;
                if (choices.Length == 0) continue;
                var car = (GameObject)PrefabUtility.InstantiatePrefab(choices[i % choices.Length].sortingItem.PlacedOnShelfPrefab.gameObject, cars);
                car.name = "Display " + choices[i % choices.Length].displayName;
                car.transform.SetPositionAndRotation(slots[i].position, slots[i].rotation);
                StripGameplay(car);
            }
        }
        if (session.pileLayout == null) return;
        var pile = JsonUtility.FromJson<Pile>(session.pileLayout.text);
        var lookup = definitions.ToDictionary(e => e.collectibleId);
        foreach (var pose in pile.placements)
        {
            if (!lookup.TryGetValue(pose.collectibleId, out var entry)) continue;
            var car = (GameObject)PrefabUtility.InstantiatePrefab(entry.sortingItem.WorldItem.gameObject, cars);
            car.name = "Floor display " + entry.displayName;
            car.transform.SetPositionAndRotation(session.pileOrigin.position + pose.position, Quaternion.Euler(pose.euler));
            StripGameplay(car);
        }
    }

    private static void BuildCameras(Scene menu)
    {
        var camera = menu.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true)).Single();
        camera.nearClipPlane = .02f;
        camera.farClipPlane = 120;
        camera.useOcclusionCulling = false;
        camera.clearFlags = CameraClearFlags.Skybox;
        var brain = camera.GetComponent<CinemachineBrain>() ?? camera.gameObject.AddComponent<CinemachineBrain>();
        brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
        brain.IgnoreTimeScale = true;
        brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 2);
        var root = new GameObject("Cinematic Cameras");
        var tour = root.AddComponent<MenuCinematicTour>();
        var serialized = new SerializedObject(tour);
        serialized.FindProperty("brain").objectReferenceValue = brain;
        var shots = serialized.FindProperty("shots"); shots.arraySize = 6;
        string[] names = { "01 Store Overview", "02 Aisle Glide", "03 Shelf Close-up", "04 Car Detail", "05 Floor-level View", "06 Wide Closing View" };
        var positions = new[] { new Vector3(500,2.5f,-9.5f), new Vector3(500.4f,1.65f,-5), new Vector3(498.25f,1.35f,-1.3f), new Vector3(497.15f,1.43f,-1.05f), new Vector3(500.6f,.46f,-2), new Vector3(500,2.65f,7.5f) };
        var ends = new[] { new Vector3(500.3f,2.4f,-8.9f), new Vector3(500.4f,1.65f,-3.8f), new Vector3(498.05f,1.35f,-.8f), new Vector3(497.08f,1.38f,-.84f), new Vector3(500.9f,.52f,-1.4f), new Vector3(499.7f,2.55f,6.9f) };
        var targets = new[] { new Vector3(500,1.1f,3), new Vector3(500,1.1f,8), new Vector3(496.4f,1.1f,-.86f), new Vector3(496.24f,1.26f,-.86f), new Vector3(498.5f,.25f,1), new Vector3(500,1,-7) };
        float[] lenses = { 64, 60, 48, 36, 60, 66 };
        for (int i = 0; i < 6; i++)
        {
            var group = new GameObject(names[i]).transform; group.SetParent(root.transform, false);
            var start = new GameObject("Start Pose").transform; start.SetParent(group, false);
            var end = new GameObject("End Pose").transform; end.SetParent(group, false);
            start.position = positions[i]; end.position = ends[i];
            start.rotation = Quaternion.LookRotation(targets[i] - positions[i]) * Quaternion.Euler(0,-10,0);
            end.rotation = Quaternion.LookRotation(targets[i] - ends[i]) * Quaternion.Euler(0,-10,0);
            var vcam = new GameObject("Cinemachine Camera").AddComponent<CinemachineCamera>();
            vcam.transform.SetParent(group, false);
            vcam.transform.SetPositionAndRotation(start.position,start.rotation);
            vcam.Priority = i == 0 ? 20 : 0;
            var lens = vcam.Lens; lens.FieldOfView = lenses[i]; lens.NearClipPlane = .02f; lens.FarClipPlane = 120; vcam.Lens = lens;
            var shot = shots.GetArrayElementAtIndex(i);
            shot.FindPropertyRelative("name").stringValue = names[i];
            shot.FindPropertyRelative("camera").objectReferenceValue = vcam;
            shot.FindPropertyRelative("start").objectReferenceValue = start;
            shot.FindPropertyRelative("end").objectReferenceValue = end;
            shot.FindPropertyRelative("duration").floatValue = 8;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        camera.transform.SetPositionAndRotation(positions[0], Quaternion.LookRotation(targets[0]-positions[0])*Quaternion.Euler(0,-10,0));
        var main = menu.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MainMenu>(true)).Single();
        var mainSo = new SerializedObject(main);
        mainSo.FindProperty("cinematicTour").objectReferenceValue = tour;
        mainSo.FindProperty("menuCamera").objectReferenceValue = camera;
        mainSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void PolishMenu(Scene menu)
    {
        var canvas = menu.GetRootGameObjects().Single(g => g.name == "Main Menu Canvas");
        var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
        // Preserve the established scale mode and reference resolution.
        var width = scaler.referenceResolution.x;
        var oldShade = canvas.transform.Find("Cinematic Side Shade");
        if (oldShade != null) Object.DestroyImmediate(oldShade.gameObject);
        var shade = new GameObject("Cinematic Side Shade", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        shade.transform.SetParent(canvas.transform, false); shade.transform.SetAsFirstSibling();
        var shadeRect = shade.GetComponent<RectTransform>();
        shadeRect.anchorMin = Vector2.zero; shadeRect.anchorMax = new Vector2(0,1);
        shadeRect.pivot = new Vector2(0,.5f); shadeRect.sizeDelta = new Vector2(width*.055f+450,0); shadeRect.anchoredPosition = Vector2.zero;
        var shadeImage = shade.GetComponent<UnityEngine.UI.Image>(); shadeImage.color = new Color(.025f,.055f,.085f,.36f); shadeImage.raycastTarget = false;
        var title = canvas.transform.Find("Title").GetComponent<TMP_Text>();
        var card = canvas.transform.Find("Menu Card").GetComponent<RectTransform>();
        card.anchorMin = card.anchorMax = new Vector2(0,.47f);
        card.pivot = new Vector2(0,.5f); card.anchoredPosition = new Vector2(width*.055f,0);
        card.sizeDelta = new Vector2(424,440);
        var backing = card.GetComponent<UnityEngine.UI.Image>(); backing.sprite = null;
        backing.color = new Color(.025f,.055f,.085f,.78f);
        title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0,.47f);
        title.rectTransform.pivot = new Vector2(0,0);
        title.rectTransform.anchoredPosition = new Vector2(width*.055f,250);
        title.rectTransform.sizeDelta = new Vector2(460,160);
        title.text = "TINY\nRIDERS"; title.fontSize = 76; title.alignment = TextAlignmentOptions.BottomLeft;
        title.color = new Color(.98f,.94f,.82f); title.textWrappingMode = TextWrappingModes.NoWrap;
        foreach (var button in card.GetComponentsInChildren<UnityEngine.UI.Button>(true))
        {
            var image = button.GetComponent<UnityEngine.UI.Image>();
            image.sprite = null;
            image.color = button.name == "Button_Play" ? new Color(.92f,.65f,.27f) : new Color(.12f,.19f,.24f,.94f);
            foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
            { label.color = button.name == "Button_Play" ? new Color(.055f,.1f,.13f) : new Color(.97f,.95f,.89f); label.fontSize = 25; }
            var colors = button.colors; colors.highlightedColor = new Color(1,.94f,.78f); colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(.75f,.8f,.85f); button.colors = colors;
        }
        canvas.transform.Find("Reset_Group").SetAsLastSibling();
    }
}
