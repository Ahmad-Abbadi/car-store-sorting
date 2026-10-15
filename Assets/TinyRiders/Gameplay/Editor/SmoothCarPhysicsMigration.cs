using System;
using System.IO;
using System.Linq;
using TinyRiders;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using KinematicCharacterController;

public static class SmoothCarPhysicsMigration
{
    const string Evidence = "IntegrationEvidence/SmoothCarPhysics";

    [MenuItem("Tools/Tiny Riders/Sorting/Apply Smooth Capsule Physics")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
        Directory.CreateDirectory(Evidence);
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != CompactCollectionBuilder.ScenePath)
            scene = EditorSceneManager.OpenScene(CompactCollectionBuilder.ScenePath);
        if (!File.Exists(Evidence + "/Level_before_capsules.unity"))
            EditorSceneManager.SaveScene(scene, Evidence + "/Level_before_capsules.unity", true);
        var session = UnityEngine.Object.FindFirstObjectByType<ThemedCollectionSession>();
        if (session == null || session.roomLayout == null) throw new InvalidOperationException("Collection layout missing.");
        const string materialPath = CompactCollectionBuilder.Root + "/LooseCar.physicMaterial";
        var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(materialPath);
        if (material == null)
        {
            material = new PhysicsMaterial("Loose Car");
            AssetDatabase.CreateAsset(material, materialPath);
        }
        material.dynamicFriction = .35f; material.staticFriction = .4f; material.bounciness = 0f;
        material.frictionCombine = PhysicsMaterialCombine.Minimum;
        material.bounceCombine = PhysicsMaterialCombine.Minimum;
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssetIfDirty(material);
        int converted = 0;
        foreach (var entry in session.catalog.entries.Where(e => e.enabled))
        {
            var existing = entry.sortingItem.WorldItem;
            if (existing.TryGetComponent<CapsuleCollider>(out var ready) && ready.sharedMaterial == material &&
                existing.GetComponent<Rigidbody>().angularDamping == 4f)
            { converted++; continue; }
            string path = AssetDatabase.GetAssetPath(entry.sortingItem.WorldItem);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var box = root.GetComponent<BoxCollider>();
                var capsule = root.GetComponent<CapsuleCollider>();
                if (capsule == null)
                {
                    if (box == null) throw new InvalidOperationException("Missing body bounds: " + path);
                    var bounds = new Bounds(box.center, box.size);
                    UnityEngine.Object.DestroyImmediate(box);
                    capsule = root.AddComponent<CapsuleCollider>();
                    LooseCarPhysics.Fit(capsule, bounds);
                }
                capsule.sharedMaterial = material;
                var body = root.GetComponent<Rigidbody>();
                LooseCarPhysics.Configure(body); body.isKinematic = true;
                PrefabUtility.SaveAsPrefabAsset(root, path); converted++;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        var layout = session.roomLayout;
        layout.walkThroughLooseCars = false;
        layout.revision = 4;
        var pile = CrowdedCollectorRoomBuilder.Bake(layout, session.catalog, session.seed);
        CrowdedCollectorRoomBuilder.Validate(layout, pile, session.catalog);
        var pilePath = AssetDatabase.GetAssetPath(session.pileLayout);
        File.WriteAllText(pilePath, JsonUtility.ToJson(pile, true)); AssetDatabase.ImportAsset(pilePath);
        var player = UnityEngine.Object.FindFirstObjectByType<Player>();
        player.Motor.InteractiveRigidbodyHandling = true;
        player.Motor.RigidbodyInteractionType = RigidbodyInteractionType.SimulatedDynamic;
        player.Motor.SimulatedCharacterMass = 60f;
        EditorUtility.SetDirty(player.Motor);
        PrefabUtility.RecordPrefabInstancePropertyModifications(player.Motor);
        foreach (var interaction in UnityEngine.Object.FindObjectsByType<InteractionHandler>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var serialized = new SerializedObject(interaction);
            serialized.FindProperty("triggerInteraction").intValue = (int)QueryTriggerInteraction.Collide;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(interaction);
        }
        EditorUtility.SetDirty(layout); AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        File.WriteAllText(Evidence + "/migration.json", JsonUtility.ToJson(new Report { prefabs = converted, cars = pile.placements.Length, revision = layout.revision }, true));
        Debug.Log("SMOOTH_CAPSULE_PHYSICS_APPLIED " + converted);
    }
    [Serializable] class Report { public int prefabs, cars, revision; }
}
