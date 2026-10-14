using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Keeps the existing sorting shelves, save IDs, item definitions and seven saved spots.
// Only the display and its physical layout are replaced.
public static class TRShelfSceneIntegration
{
    public const float CompactShelfScale = 0.48f;
    const string ArtPrefab = "Assets/TinyRiders/Art/Shelves/BaseShelf_01/Prefabs/TR_Shelf_Base_01.prefab";
    const string LevelPath = "Assets/00_Project/00_Scenes/Level.unity";
    const string BackupPath = "Assets/TinyRiders/Art/Shelves/BaseShelf_01/Backup/Level_before_shelf_replacement.unity";
    static readonly string[] SpotNames = {
        "Slot_R03_C02", "Slot_R03_C05",
        "Slot_R02_C01", "Slot_R02_C03", "Slot_R02_C06",
        "Slot_R01_C02", "Slot_R01_C05"
    };

    [MenuItem("Tools/Tiny Riders/Base Shelf/Replace Level Shelves")]
    public static void ReplaceLevelShelves()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before replacing shelves.");
        var scene = SceneManager.GetSceneByPath(LevelPath);
        bool opened = !scene.isLoaded;
        Scene previous = SceneManager.GetActiveScene();
        if (opened) scene = EditorSceneManager.OpenScene(LevelPath, OpenSceneMode.Additive);
        try {
            Directory.CreateDirectory(Path.GetDirectoryName(BackupPath));
            if (!File.Exists(BackupPath) && !EditorSceneManager.SaveScene(scene, BackupPath, true))
                throw new IOException("Could not back up the loaded Level scene.");
            int count = Apply(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save Level scene.");
            Debug.Log("TR_SHELF_LEVEL_REPLACED: " + count + " sorting shelves; backup: " + BackupPath);
        }
        finally {
            if (opened) EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }
    }

    // Called by the store generator as well, so regenerating Level retains the new shelves.
    public static int Apply(Scene scene)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArtPrefab);
        if (prefab == null) throw new FileNotFoundException("New shelf art prefab is missing", ArtPrefab);
        var sections = scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Section>(true))
            .OrderBy(s => s.name, StringComparer.Ordinal).ToArray();
        if (sections.Length != 15) throw new InvalidOperationException("Expected 15 model sections; found " + sections.Length);
        int count = 0;
        foreach (var section in sections) {
            var shelves = section.GetComponentsInChildren<SortingShelf>(true)
                .Where(s => s.transform.parent == section.transform)
                .OrderBy(s => s.transform.GetSiblingIndex()).ToArray();
            bool compact = shelves.Length == 5;
            if (!compact && shelves.Length != 10) throw new InvalidOperationException(section.name + " must have five Standard shelves or ten legacy shelves.");
            int index = Array.IndexOf(sections, section);
            if (!compact) {
                if (index < 5) section.transform.position = new Vector3(500f - 37.4f, 0, -37f + index * 18.5f);
                else if (index < 10) section.transform.position = new Vector3(500f + 37.4f, 0, -37f + (index - 5) * 18.5f);
                else if (index < 13) section.transform.position = new Vector3(500f + (index - 11) * 19.5f, 0, 48f);
                else section.transform.position = new Vector3(500f + (index == 13 ? -19.5f : 19.5f), 0, -48f);
            }

            for (int i = 0; i < shelves.Length; i++) {
                var shelf = shelves[i];
                bool inner = i >= 5;
                shelf.transform.localPosition = new Vector3(((i % 5) - 2) * (compact ? 1.8f : 3.7f), 0, inner ? -7f : 0f);
                shelf.transform.localRotation = Quaternion.identity;
                shelf.transform.localScale = Vector3.one * (compact ? CompactShelfScale : 1f);
                Transform art = shelf.transform.Find("TR_Shelf_Base_01");
                if (art == null) {
                    Transform oldRack = shelf.transform.Find("Display Rack");
                    if (oldRack != null) UnityEngine.Object.DestroyImmediate(oldRack.gameObject);
                    art = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, scene)).transform;
                    art.SetParent(shelf.transform, false);
                }
                art.localPosition = Vector3.zero;
                art.localRotation = Quaternion.Euler(0, inner ? 0 : 180, 0);
                art.localScale = Vector3.one;
                float face = inner ? 1f : -1f;
                foreach (var renderer in art.GetComponentsInChildren<Renderer>(true)) renderer.gameObject.layer = 2;
                foreach (var collider in art.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
                // The old sorting prefab includes a small presentation slab. Hide its mesh;
                // the new cabinet supplies the visible body and these simple solid blockers.
                Transform oldSlab = shelf.transform.Find("ShelfObject");
                if (oldSlab != null) {
                    foreach (var renderer in oldSlab.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
                    foreach (var collider in oldSlab.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                }
                EnsureBodyCollider(shelf.transform, "Shelf Plinth Collision",
                    new Vector3(0, .29f, 0), new Vector3(3.55f, .58f, .86f));
                EnsureBodyCollider(shelf.transform, "Shelf Back Collision",
                    new Vector3(0, 1.82f, -face * .34f), new Vector3(3.48f, 3.48f, .15f));

                var markers = art.GetComponentsInChildren<Transform>(true)
                    .Where(t => SpotNames.Contains(t.name)).ToDictionary(t => t.name);
                if (markers.Count != 7) throw new InvalidOperationException("Shelf art is missing seven required markers.");
                for (int spot = 0; spot < 7; spot++) {
                    Transform target = shelf.GetSpot(spot);
                    if (target == null) throw new InvalidOperationException(shelf.name + " is missing gameplay spot " + spot);
                    target.SetPositionAndRotation(markers[SpotNames[spot]].position, markers[SpotNames[spot]].rotation);
                }
                // Keep the color label and variant text, on the low plinth instead of across the art.
                MoveLabel(shelf.transform.Find("Color Label Board"), new Vector3(0, .31f, face * .51f), inner);
                MoveLabel(shelf.transform.Find("Color Swatch"), new Vector3(-.56f, .31f, face * .56f), inner);
                var variantLabel = shelf.GetComponentsInChildren<CarShelfLabel>(true).FirstOrDefault();
                if (variantLabel != null) MoveLabel(variantLabel.transform, new Vector3(.15f, .31f, face * .57f), inner);
                MoveLabel(shelf.transform.Find("Label"), new Vector3(1.08f, .31f, face * .60f), inner);
                Transform surface = shelf.transform.Find("Car Placement Surface");
                if (surface == null) {
                    var surfaceObject = new GameObject("Car Placement Surface");
                    surface = surfaceObject.transform;
                    surface.SetParent(shelf.transform, false);
                }
                surface.gameObject.layer = 8;
                surface.localPosition = new Vector3(0, 1.82f, face * .49f);
                surface.localRotation = Quaternion.identity;
                var box = surface.GetComponent<BoxCollider>();
                if (box == null) box = surface.gameObject.AddComponent<BoxCollider>();
                box.size = new Vector3(3.48f, 3.24f, .08f);
                if (surface.GetComponent<SortingShelfPlacementSurface>() == null)
                    surface.gameObject.AddComponent<SortingShelfPlacementSurface>();
                PrefabUtility.RecordPrefabInstancePropertyModifications(shelf.transform);
                count++;
            }
        }
        if (count != 75 && count != 150) throw new InvalidOperationException("Expected 75 Standard shelves or 150 legacy shelves; found " + count);
        return count;
    }

    static void MoveLabel(Transform t, Vector3 localPosition, bool inner)
    {
        if (t == null) return;
        t.localPosition = localPosition;
        t.localRotation = Quaternion.Euler(0, inner ? 180 : 0, 0);
    }

    static void EnsureBodyCollider(Transform shelf, string name, Vector3 position, Vector3 size)
    {
        Transform child = shelf.Find(name);
        if (child == null) {
            var obj = new GameObject(name);
            child = obj.transform;
            child.SetParent(shelf, false);
        }
        child.localPosition = position;
        child.localRotation = Quaternion.identity;
        var box = child.GetComponent<BoxCollider>();
        if (box == null) box = child.gameObject.AddComponent<BoxCollider>();
        box.size = size;
    }
}
