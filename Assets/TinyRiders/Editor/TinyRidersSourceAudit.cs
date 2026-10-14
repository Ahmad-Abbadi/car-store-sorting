using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TinyRiders.Editor
{
    public static class TinyRidersSourceAudit
    {
        const string Package = "Assets/ithappy/Modular_Racing_Cars";

        [Serializable] public class Audit { public string unityVersion; public string packageRoot; public Car[] cars; }
        [Serializable] public class Car { public string name, prefabPath, guid; public Part[] parts; public Render[] renderers; public float[] bounds; }
        [Serializable] public class Part { public string name, prefabPath, guid; public bool active; }
        [Serializable] public class Render { public string name, meshPath, meshGuid; public long meshFileId; public string[] materials; public float[] uvRange; }

        [MenuItem("Tiny Riders/Audit Source Package")]
        public static void Run()
        {
            var audit = new Audit { unityVersion = Application.unityVersion, packageRoot = Package, cars = new Car[15] };
            for (int i = 1; i <= 15; i++)
            {
                string name = "Car_" + i.ToString("00");
                string path = Package + "/Prefabs/" + name + "/" + name + ".prefab";
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var car = new Car { name = name, prefabPath = path, guid = AssetDatabase.AssetPathToGUID(path) };
                if (source != null)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
                    var parts = new List<Part>();
                    foreach (Transform child in instance.transform)
                    {
                        var nestedRoot = PrefabUtility.GetNearestPrefabInstanceRoot(child.gameObject);
                        var nestedSource = nestedRoot == null ? null : PrefabUtility.GetCorrespondingObjectFromOriginalSource(nestedRoot);
                        string partPath = nestedSource == null ? "" : AssetDatabase.GetAssetPath(nestedSource);
                        parts.Add(new Part { name = child.name, prefabPath = partPath, guid = AssetDatabase.AssetPathToGUID(partPath), active = child.gameObject.activeSelf });
                    }
                    car.parts = parts.ToArray();
                    var renderers = new List<Render>();
                    bool hasBounds = false;
                    Bounds bounds = new Bounds();
                    foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                    {
                        if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                        if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; } else bounds.Encapsulate(renderer.bounds);
                        var filter = renderer.GetComponent<MeshFilter>();
                        Mesh mesh = filter != null ? filter.sharedMesh : null;
                        string meshGuid = ""; long meshFileId = 0;
                        if (mesh != null) AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out meshGuid, out meshFileId);
                        var mats = new List<string>();
                        foreach (var mat in renderer.sharedMaterials) mats.Add(mat == null ? "<null>" : mat.name + "|" + AssetDatabase.GetAssetPath(mat));
                        var rr = new Render { name = renderer.name, meshPath = mesh == null ? "" : AssetDatabase.GetAssetPath(mesh), meshGuid = meshGuid, meshFileId = meshFileId, materials = mats.ToArray() };
                        if (mesh != null && mesh.uv.Length > 0)
                        {
                            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
                            foreach (var uv in mesh.uv) { minX = Mathf.Min(minX, uv.x); minY = Mathf.Min(minY, uv.y); maxX = Mathf.Max(maxX, uv.x); maxY = Mathf.Max(maxY, uv.y); }
                            rr.uvRange = new[] { minX, minY, maxX, maxY };
                        }
                        renderers.Add(rr);
                    }
                    car.renderers = renderers.ToArray();
                    car.bounds = new[] { bounds.center.x, bounds.center.y, bounds.center.z, bounds.size.x, bounds.size.y, bounds.size.z };
                    UnityEngine.Object.DestroyImmediate(instance);
                }
                audit.cars[i - 1] = car;
            }
            string output = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "TinyRidersProduction", "source_audit_raw.json");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, JsonUtility.ToJson(audit, true));
            Debug.Log("Tiny Riders source audit saved: " + output);
        }
    }
}
