using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TinyRiders
{
    /// <summary>Authors only the reward display, preserving the rest of the shop.</summary>
    public static class GoldenCarDisplayBuilder
    {
        const string Folder = "Assets/TinyRiders/Gameplay/GoldenCar";

        [MenuItem("Tools/Tiny Riders/Collector Shop/Install Golden Car Display")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != CompactCollectionBuilder.ScenePath) throw new InvalidOperationException("Open Level first.");
            var existing = Object.FindFirstObjectByType<GoldenCarReveal>();
            if (existing != null) { Selection.activeGameObject = existing.gameObject; return; }
            var counter = GameObject.Find("Collector Counter");
            if (counter == null) throw new InvalidOperationException("Collector Counter is missing.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TinyRidersPremium/Prefabs/Candidates/M04_Wedge.prefab");
            if (prefab == null) throw new InvalidOperationException("Wedge sports car asset is missing.");
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/TinyRiders/Gameplay", "GoldenCar");
            var gold = Material("Trophy Gold", new Color(1f, .64f, .13f), .85f, .82f);
            var dark = Material("Pedestal Teal", new Color(.025f, .09f, .10f), .25f, .45f);
            var velvet = Material("Reveal Velvet", new Color(.12f, .035f, .055f), .08f, .38f);
            var glow = Material("Warm Rim", new Color(1f, .73f, .27f), .5f, .65f);
            glow.EnableKeyword("_EMISSION"); glow.SetColor("_EmissionColor", new Color(1f, .55f, .1f) * 1.4f);
            var root = new GameObject("Golden Car Display");
            Undo.RegisterCreatedObjectUndo(root, "Install Golden Car Display");
            root.transform.SetParent(counter.transform.parent, true);
            // In the existing pile exclusion, front-right of the counter, with 1.2m clearance to the wall shelves.
            root.transform.position = new Vector3(counter.transform.position.x + .55f, 0, counter.transform.position.z + 1.7f);
            var controller = root.AddComponent<GoldenCarReveal>();
            controller.pedestal = Shape("Trophy Pedestal", PrimitiveType.Cylinder, root.transform,
                new Vector3(0, .48f, 0), new Vector3(.83f, .48f, .83f), dark, true).transform;
            Shape("Gold Foot", PrimitiveType.Cylinder, root.transform, new Vector3(0, .07f, 0), new Vector3(.9f, .035f, .9f), gold);
            Shape("Illuminated Rim", PrimitiveType.Cylinder, root.transform, new Vector3(0, .965f, 0), new Vector3(.88f, .013f, .88f), glow);
            Shape("Velvet Display Pad", PrimitiveType.Cylinder, root.transform, new Vector3(0, .985f, 0), new Vector3(.83f, .01f, .83f), dark);

            var trophy = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
            PrefabUtility.UnpackPrefabInstance(trophy, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            trophy.name = "Golden Wedge Trophy";
            // Keep meshes and authored details only: no collectible identity, physics or interaction.
            foreach (var component in trophy.GetComponentsInChildren<Component>(true))
                if (!(component is Transform) && !(component is MeshFilter) && !(component is MeshRenderer)) Object.DestroyImmediate(component);
            trophy.transform.localPosition = Vector3.zero;
            trophy.transform.localRotation = Quaternion.Euler(0, -18, 0) * prefab.transform.localRotation;
            var renderers = trophy.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
            trophy.transform.localScale *= .72f / Mathf.Max(bounds.size.x, bounds.size.z);
            bounds = renderers[0].bounds;
            foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
            trophy.transform.position += root.transform.position + Vector3.up * (1.01f - bounds.min.y) - new Vector3(bounds.center.x, 0, bounds.center.z);
            foreach (var r in renderers)
                r.sharedMaterials = r.sharedMaterials.Select(m => m != null && m.name.StartsWith("M04_Paint", StringComparison.Ordinal) ? gold : m).ToArray();
            controller.trophy = trophy;

            var cover = new GameObject("Velvet Reveal Cover", typeof(MeshFilter), typeof(MeshRenderer));
            cover.transform.SetParent(root.transform, false);
            cover.transform.localPosition = new Vector3(0, 1f, 0);
            var cloth = CoverMesh();
            string meshPath = Folder + "/RevealCover.asset";
            var savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (savedMesh == null) { AssetDatabase.CreateAsset(cloth, meshPath); savedMesh = cloth; }
            else { EditorUtility.CopySerialized(cloth, savedMesh); Object.DestroyImmediate(cloth); }
            cover.GetComponent<MeshFilter>().sharedMesh = savedMesh;
            cover.GetComponent<MeshRenderer>().sharedMaterial = velvet;
            controller.cover = cover.transform;

            var sign = new GameObject("Golden Car Plaque", typeof(TextMeshPro));
            sign.transform.SetParent(root.transform, false);
            sign.transform.localPosition = new Vector3(0, .73f, .421f);
            sign.transform.localRotation = Quaternion.Euler(0, 180, 0);
            var text = sign.GetComponent<TextMeshPro>();
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/00_Project/04_ِArt/UI/Fonts/Fredoka-SemiBold SDF.asset");
            text.fontSize = .65f; text.color = new Color(1f, .83f, .46f);
            text.alignment = TextAlignmentOptions.Center; text.rectTransform.sizeDelta = new Vector2(.78f, .22f);
            text.text = "GOLDEN CAR\n<size=55%>Sort all cars to reveal</size>";
            controller.placard = text;

            var lamp = new GameObject("Golden Car Spotlight", typeof(Light));
            lamp.transform.SetParent(root.transform, false);
            lamp.transform.localPosition = new Vector3(-.3f, 2.35f, .5f);
            lamp.transform.LookAt(root.transform.position + Vector3.up * 1.12f);
            var light = lamp.GetComponent<Light>(); light.type = LightType.Spot;
            light.color = new Color(1, .85f, .6f); light.range = 4; light.spotAngle = 65; light.intensity = .25f;
            controller.displayLight = light;
            var view = new GameObject("Golden Car Camera Viewpoint").transform;
            view.SetParent(root.transform, false);
            view.localPosition = new Vector3(-.9f, 1.75f, 1.8f);
            view.LookAt(root.transform.position + Vector3.up * .8f);
            controller.cameraViewpoint = view;
            trophy.SetActive(false);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = root;
            Debug.Log("GOLDEN_CAR_DISPLAY_READY: beside counter; decorative trophy excluded from sorting.");
        }

        static Material Material(string name, Color color, float metallic, float smoothness)
        {
            string path = Folder + "/" + name.Replace(" ", "") + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.color = color; m.SetFloat("_Metallic", metallic); m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Cull", 0); EditorUtility.SetDirty(m); return m;
        }

        static GameObject Shape(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material, bool solid = false)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!solid) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static Mesh CoverMesh()
        {
            const int rings = 12, segments = 48;
            var vertices = new Vector3[(rings + 1) * (segments + 1)];
            var triangles = new int[rings * segments * 6];
            for (int y = 0; y <= rings; y++)
            {
                float angle = y / (float)rings * Mathf.PI * .5f;
                float radius = Mathf.Sin(angle);
                for (int x = 0; x <= segments; x++)
                {
                    float a = x / (float)segments * Mathf.PI * 2;
                    float fold = 1 + .025f * Mathf.Cos(a * 16) * radius;
                    vertices[y * (segments + 1) + x] = new Vector3(
                        Mathf.Cos(a) * radius * .45f * fold,
                        .44f * Mathf.Sqrt(Mathf.Max(0, Mathf.Cos(angle))),
                        Mathf.Sin(a) * radius * .35f * fold);
                }
            }
            int index = 0;
            for (int y = 0; y < rings; y++) for (int x = 0; x < segments; x++)
            {
                int a = y * (segments + 1) + x, b = a + segments + 1;
                triangles[index++] = a; triangles[index++] = a + 1; triangles[index++] = b;
                triangles[index++] = a + 1; triangles[index++] = b + 1; triangles[index++] = b;
            }
            var mesh = new Mesh { name = "Draped Velvet Cover", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
    }
}
