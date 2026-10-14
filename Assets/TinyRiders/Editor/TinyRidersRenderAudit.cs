using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TinyRiders.Editor
{
    public static class TinyRidersRenderAudit
    {
        static readonly string Project = Directory.GetParent(Application.dataPath).FullName;

        public static Texture2D RenderIconTexture(GameObject prefab)
        {
            var preview = new PreviewRenderUtility();
            GameObject instance = null;
            try
            {
                instance = UnityEngine.Object.Instantiate(prefab);
                preview.AddSingleGO(instance);
                bool hasBounds = false;
                var bounds = new Bounds();
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
                {
                    if (!renderer.enabled) continue;
                    if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                    else bounds.Encapsulate(renderer.bounds);
                }
                if (!hasBounds) throw new InvalidOperationException("No active icon renderer: " + prefab.name);
                var camera = preview.camera;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.90f, 0.92f, 0.94f, 1f);
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(bounds.extents.magnitude * 0.92f, 2.2f);
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;
                camera.transform.position = bounds.center + new Vector3(1f, 0.55f, 1.2f).normalized * Mathf.Max(bounds.extents.magnitude * 3f, 8f);
                camera.transform.LookAt(bounds.center);
                preview.lights[0].intensity = 1.35f;
                preview.lights[0].transform.rotation = Quaternion.Euler(35f, -35f, 0f);
                preview.lights[1].intensity = 0.65f;
                preview.BeginStaticPreview(new Rect(0, 0, 128, 128));
                camera.Render();
                var rendered = preview.EndStaticPreview();
                var copy = new Texture2D(rendered.width, rendered.height, TextureFormat.RGBA32, false);
                copy.SetPixels32(rendered.GetPixels32());
                copy.Apply();
                UnityEngine.Object.DestroyImmediate(rendered);
                return copy;
            }
            finally
            {
                preview.Cleanup();
                if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        public static void Render(string commaSeparatedIds)
        {
            foreach (string raw in commaSeparatedIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string id = raw.Trim();
                var parts = id.Split('-');
                if (parts.Length != 4) throw new InvalidOperationException("Invalid QA ID: " + id);
                string path = "Assets/TinyRiders/Prefabs/Variants/" + parts[1] + "/" + parts[2] + "/" + id + ".prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) throw new InvalidOperationException("Missing QA prefab: " + path);
                RenderOne(prefab, id, "front", new Vector3(0.35f, 0.38f, 1f));
                RenderOne(prefab, id, "side", new Vector3(1f, 0.25f, 0f));
                RenderOne(prefab, id, "rear", new Vector3(-0.3f, 0.38f, -1f));
            }
        }

        public static void RenderAllCustom()
        {
            for (int car = 1; car <= 15; car++)
                Render("TR-C" + car.ToString("D3") + "-CUS-L01");
        }

        static void RenderOne(GameObject prefab, string id, string view, Vector3 direction)
        {
            var preview = new PreviewRenderUtility();
            GameObject instance = null;
            try
            {
                instance = UnityEngine.Object.Instantiate(prefab);
                preview.AddSingleGO(instance);
                bool hasBounds = false;
                var bounds = new Bounds();
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
                {
                    if (!renderer.enabled) continue;
                    if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                    else bounds.Encapsulate(renderer.bounds);
                }
                if (!hasBounds) throw new InvalidOperationException("No active QA renderer: " + id);
                var camera = preview.camera;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.86f, 0.88f, 0.90f);
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(bounds.extents.magnitude * 0.94f, 2.7f);
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;
                camera.transform.position = bounds.center + direction.normalized * Mathf.Max(bounds.extents.magnitude * 3f, 8f);
                camera.transform.LookAt(bounds.center);
                preview.lights[0].intensity = 1.45f;
                preview.lights[0].transform.rotation = Quaternion.Euler(35f, -35f, 0f);
                preview.lights[1].intensity = 0.7f;
                preview.BeginStaticPreview(new Rect(0, 0, 512, 512));
                camera.Render();
                var texture = preview.EndStaticPreview();
                string directory = Path.Combine(Project, "TinyRidersProduction", "Reports", "QA");
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, id + "_" + view + ".png"), texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
            }
            finally
            {
                preview.Cleanup();
                if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
            }
        }
    }
}
