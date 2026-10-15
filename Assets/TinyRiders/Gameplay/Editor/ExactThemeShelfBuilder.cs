using System;
using System.IO;
using System.Linq;
using TinyRiders;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Targeted shelf-only migration. Does not rebuild the room or rebake its pile.</summary>
public static class ExactThemeShelfBuilder
{
    public const string SourcePath = "Assets/TinyRiders/Art/Shelves/BaseShelf_01/Prefabs/TR_Shelf_Base_01.prefab";
    public const string PrefabPath = "Assets/TinyRiders/Gameplay/Compact/TR_Shelf_15.prefab";
    public const int SlotsPerShelf = 15;
    public static readonly Vector3 Clearance = new Vector3(.564f, .54f, .648f);
    public const string Evidence = "IntegrationEvidence/ExactShelfCapacity";

    public static GameObject EnsurePrefab()
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (existing != null) return existing;
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
        try
        {
            // Keep a true variant of the original art, with only interior-layout overrides.
            for (int row = 1; row <= 3; row++)
            {
                var transforms = instance.GetComponentsInChildren<Transform>(true);
                for (int column = 1; column <= 5; column++)
                {
                    var divider = transforms.Single(t => t.name == $"Divider_{row:00}_{column:00}");
                    // Unity prefab variants cannot delete source objects; inactive dividers do not render or batch.
                    if (column == 5) divider.gameObject.SetActive(false);
                    else { var p = divider.localPosition; p.x = 1.5f - column * .6f; divider.localPosition = p; }
                    PrefabUtility.RecordPrefabInstancePropertyModifications(divider);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(divider.gameObject);
                }
                for (int column = 1; column <= 6; column++)
                {
                    var marker = transforms.Single(t => t.name == $"Slot_R{row:00}_C{column:00}");
                    if (column == 6) marker.gameObject.SetActive(false);
                    else { var p = marker.localPosition; p.x = 1.5f - (column - .5f) * .6f; marker.localPosition = p; }
                    PrefabUtility.RecordPrefabInstancePropertyModifications(marker);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(marker.gameObject);
                }
            }
            return PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
        }
        finally { Object.DestroyImmediate(instance); }
    }

    public static Transform[] Markers(GameObject art) => art.GetComponentsInChildren<Transform>(false)
        .Where(t => t.name.StartsWith("Slot_R", StringComparison.Ordinal)).OrderBy(t => t.name, StringComparer.Ordinal).ToArray();

    public static void ValidateScene()
    {
        var session = Object.FindFirstObjectByType<ThemedCollectionSession>();
        if (session == null) throw new InvalidOperationException("Collection session missing.");
        ThemedShelfCapacity.Validate(session.catalog, session.copiesPerDesign,
            Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None));
    }

    [MenuItem("Tools/Tiny Riders/Collector Shop/Match Shelf Slots To Cars")]
    public static void Apply()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != CompactCollectionBuilder.ScenePath)
            throw new InvalidOperationException("Open Level in Edit mode first.");
        var session = Object.FindFirstObjectByType<ThemedCollectionSession>();
        var shelves = Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None)
            .Where(s => s.GetComponent<ThemeShelfBinding>() != null).OrderBy(s => s.SaveId, StringComparer.Ordinal).ToArray();
        var expected = ThemedShelfCapacity.EligibleDefinitions(session.catalog).GroupBy(e => e.themeId)
            .ToDictionary(g => g.Key, g => g.Count() * session.copiesPerDesign);
        if (expected.Count != 40 || shelves.Length != 80 || expected.Any(g => g.Value != 30 || shelves.Count(s => s.GetComponent<ThemeShelfBinding>().themeId == g.Key) != 2))
            throw new InvalidOperationException("This migration requires two shelves and 30 cars per theme.");
        Directory.CreateDirectory(Evidence);
        EditorSceneManager.SaveScene(scene, Evidence + "/Level_before_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity", true);
        var prefab = EnsurePrefab();
        foreach (var shelf in shelves)
        {
            var old = shelf.transform.Find("TR_Shelf_Base_01");
            if (old == null) throw new InvalidOperationException("Missing shelf art: " + shelf.SaveId);
            var art = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            art.name = "TR_Shelf_Base_01"; art.transform.SetParent(shelf.transform, false);
            art.transform.localPosition = old.localPosition; art.transform.localRotation = old.localRotation; art.transform.localScale = old.localScale;
            foreach (var t in art.GetComponentsInChildren<Transform>(true)) { t.gameObject.layer = 2; PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject); }
            foreach (var collider in art.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
            var markers = Markers(art);
            if (markers.Length != SlotsPerShelf) throw new InvalidOperationException("Expected 15 active shelf markers.");
            var so = new SerializedObject(shelf); var spots = so.FindProperty("placingSpots"); spots.arraySize = markers.Length;
            for (int i = 0; i < markers.Length; i++) spots.GetArrayElementAtIndex(i).objectReferenceValue = markers[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            var binding = shelf.GetComponent<ThemeShelfBinding>(); binding.clearance = Vector3.Scale(Clearance, art.transform.localScale); EditorUtility.SetDirty(binding);
            Object.DestroyImmediate(old.gameObject);
        }
        ValidateScene();
        ThemedShelfBatcher.Apply(shelves, PrefabPath);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        CrowdedCollectorRoomBuilder.Audit();
        File.WriteAllText(Evidence + "/capacity.json", Newtonsoft.Json.JsonConvert.SerializeObject(new {
            cars = expected.Values.Sum(), slots = shelves.Sum(s => s.SpotCount), shelves = shelves.Length,
            themes = expected.OrderBy(g => g.Key).Select(g => new { theme = g.Key, cars = g.Value,
                slots = shelves.Where(s => s.GetComponent<ThemeShelfBinding>().themeId == g.Key).Sum(s => s.SpotCount) })
        }, Newtonsoft.Json.Formatting.Indented));
        Debug.Log("EXACT_SHELF_CAPACITY_READY: 40 themes / 80 shelves / 1200 cars / 1200 slots");
    }
}
