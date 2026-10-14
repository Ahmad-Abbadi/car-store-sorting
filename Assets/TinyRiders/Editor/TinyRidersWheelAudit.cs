using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TinyRiders.Editor
{
    public static class TinyRidersWheelAudit
    {
        [Serializable] class Report { public Wheel[] mounted; public Wheel[] supplied; }
        [Serializable] class Wheel { public string name, path; public float[] bounds, rootPosition, rootRotation, rootScale; public int renderers; }

        static Transform Find(Transform root, string name)
        {
            foreach (Transform child in root)
            {
                if (child.name == name) return child;
                var found = Find(child, name);
                if (found != null) return found;
            }
            return null;
        }

        static Wheel Describe(Transform root, string path)
        {
            var output = new Wheel { name = root.name, path = path };
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            output.renderers = renderers.Length;
            bool hasBounds = false;
            var bounds = new Bounds();
            foreach (var renderer in renderers)
            {
                if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            output.bounds = new[] { bounds.size.x, bounds.size.y, bounds.size.z };
            output.rootPosition = new[] { root.localPosition.x, root.localPosition.y, root.localPosition.z };
            output.rootRotation = new[] { root.localEulerAngles.x, root.localEulerAngles.y, root.localEulerAngles.z };
            output.rootScale = new[] { root.localScale.x, root.localScale.y, root.localScale.z };
            return output;
        }

        public static void InspectSaved(string variantId)
        {
            string carId = variantId.Substring(3, 4);
            string build = variantId.Substring(8, 3);
            string path = "Assets/TinyRiders/Prefabs/Variants/" + carId + "/" + build + "/" + variantId + ".prefab";
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null) throw new InvalidOperationException("Missing prefab " + path);
            var output = new List<Wheel>();
            string supplier = "Car_" + int.Parse(carId.Substring(1)).ToString("00");
            foreach (string position in new[] { "FL", "FR", "RL", "RR" })
            {
                var original = Find(root.transform, supplier + "_Wheel_" + position);
                var replacement = Find(root.transform, "TR_Wheel_" + position);
                if (original != null) output.Add(Describe(original, "original " + position));
                if (replacement != null)
                {
                    var source = PrefabUtility.GetCorrespondingObjectFromSource(replacement.gameObject);
                    output.Add(Describe(replacement, "replacement " + position + " source=" + AssetDatabase.GetAssetPath(source) +
                        " delta=" + Vector3.Distance(replacement.position, original.position) +
                        " angle=" + Quaternion.Angle(replacement.rotation, original.rotation)));
                }
            }
            string target = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                "TinyRidersProduction", "Reports", "saved_wheel_inspection.json");
            File.WriteAllText(target, JsonUtility.ToJson(new Report { mounted = output.ToArray(), supplied = new Wheel[0] }, true));
        }

        [MenuItem("Tiny Riders/Audit Alternate Wheel Fit")]
        public static void Run()
        {
            const string package = "Assets/ithappy/Modular_Racing_Cars/Prefabs";
            var mounted = new List<Wheel>();
            var supplied = new List<Wheel>();
            for (int i = 1; i <= 15; i++)
            {
                string car = "Car_" + i.ToString("00");
                string path = package + "/" + car + "/" + car + ".prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (string position in new[] { "FL", "FR", "RL", "RR" })
                    {
                        var wheel = Find(root.transform, car + "_Wheel_" + position);
                        if (wheel == null) throw new InvalidOperationException("Missing mounted wheel: " + car + " " + position);
                        mounted.Add(Describe(wheel, path));
                    }
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            for (int i = 1; i <= 16; i++)
            {
                string name = "Wheel_" + i.ToString("00");
                string path = package + "/Wheels/" + name + ".prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                try { supplied.Add(Describe(root.transform, path)); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            string output = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                "TinyRidersProduction", "Reports", "wheel_fit_audit.json");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, JsonUtility.ToJson(new Report { mounted = mounted.ToArray(), supplied = supplied.ToArray() }, true));
            Debug.Log("Tiny Riders wheel audit saved: " + output);
        }
    }
}
