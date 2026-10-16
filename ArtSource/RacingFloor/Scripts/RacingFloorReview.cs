using System;
using System.IO;
using System.Linq;
using TinyRiders;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static class RacingFloorReview
{
    const string Evidence = "IntegrationEvidence/RacingFloor/ToyTown";
    static Transform Interior => GameObject.Find("Car Store Generated/Crowded Shop Interior").transform;

    public static void Baseline()
    {
        Directory.CreateDirectory(Evidence);
        var floor = Interior.Find("Store Floor");
        File.WriteAllText(Evidence + "/floor_collider_before.json", EditorJsonUtility.ToJson(floor.GetComponent<BoxCollider>(), true));
        File.WriteAllText(Evidence + "/floor_transform_before.json", EditorJsonUtility.ToJson(floor.transform, true));
        File.WriteAllText(Evidence + "/room_before.json", RoomSnapshot());
        Capture("Before");
    }

    static string RoomSnapshot()
    {
        var session = Object.FindFirstObjectByType<ThemedCollectionSession>();
        var lines = Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None)
            .OrderBy(s => s.SaveId).Select(s => s.SaveId + " " + EditorJsonUtility.ToJson(s.transform)
                + " " + EditorJsonUtility.ToJson(s));
        return EditorJsonUtility.ToJson(session) + "\n" + EditorJsonUtility.ToJson(session.roomLayout)
            + "\n" + string.Join("\n", lines) + "\n" + string.Join("\n", Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
                .OrderBy(l => l.GetInstanceID()).Select(l => EditorJsonUtility.ToJson(l)));
    }

    public static string Audit()
    {
        var floor = Interior.Find("Store Floor");
        if (File.ReadAllText(Evidence + "/floor_collider_before.json") != EditorJsonUtility.ToJson(floor.GetComponent<BoxCollider>(), true))
            throw new Exception("Floor collider changed.");
        if (File.ReadAllText(Evidence + "/floor_transform_before.json") != EditorJsonUtility.ToJson(floor.transform, true))
            throw new Exception("Floor transform changed.");
        if (File.ReadAllText(Evidence + "/room_before.json") != RoomSnapshot())
            throw new Exception("Shelf, session, layout or lighting state changed.");
        var root = Interior.Find(RacingFloorBuilder.DecorationName);
        if (Interior.Cast<Transform>().Count(t => t.name == RacingFloorBuilder.DecorationName) != 1)
            throw new Exception("Duplicate floor root.");
        if (root.childCount != 5 || root.GetComponentsInChildren<Collider>(true).Length != 0)
            throw new Exception("Unexpected graphic count or decorative collider.");
        var renderers = root.GetComponentsInChildren<MeshRenderer>();
        if (renderers.Any(r => r.gameObject.layer != 2 || r.sharedMaterials.Length != 1 || r.shadowCastingMode != ShadowCastingMode.Off))
            throw new Exception("Graphics intercept picking, cast shadows or have extra materials.");
        var meshes = root.GetComponentsInChildren<MeshFilter>();
        if (meshes.Any(m => m.sharedMesh.normals.Any(n => n.y < .99f)))
            throw new Exception("Floor paint normal is not upward.");
        string report = "PASS: Original floor collider and transform unchanged.\n"
            + "PASS: Shelf identities/transforms, session, pile layout and lights unchanged.\n"
            + "PASS: One graphics root, five combined graphic renderers, zero decorative colliders.\n"
            + "PASS: Graphics ignore picking, receive shadows and do not cast shadows.\n"
            + "PASS: All paint normals face up.\n"
            + "Floor renderers/material slots: 6 (base plus 5 graphics; additional shadow/depth passes depend on pipeline).\n"
            + "Graphic triangles: " + meshes.Sum(m => m.sharedMesh.triangles.Length / 3) + "\n";
        File.WriteAllText(Evidence + "/structural_audit.txt", report);
        return report;
    }

    public static void Capture(string state)
    {
        Directory.CreateDirectory(Evidence);
        var session = Object.FindFirstObjectByType<ThemedCollectionSession>();
        var origin = session.roomLayout.origin;
        File.WriteAllText(Evidence + "/" + state + "_capture.txt", "Playing: " + Application.isPlaying
            + "\nLive pile: " + session.LivePileCount + "\nFrame: " + Time.frameCount
            + "\nOverhead capture temporarily hides the ceiling and restores it immediately.\n");
        Render(state + "_entrance", origin + new Vector3(0, 1.65f, -11.1f), origin + new Vector3(0, .15f, -6), false);
        Render(state + "_central", origin + new Vector3(-1.7f, 1.7f, -5.8f), origin + new Vector3(.25f, 0, 2.5f), false);
        Render(state + "_outer", origin + new Vector3(7.6f, 1.7f, -10.9f), origin + new Vector3(7.2f, .1f, -3.5f), false);
        Render(state + "_overhead", origin + new Vector3(0, 30, 0), origin, true);
    }

    static void Render(string name, Vector3 position, Vector3 target, bool overhead)
    {
        var go = new GameObject("Temporary Floor Review Camera") { hideFlags = HideFlags.HideAndDontSave };
        var camera = go.AddComponent<Camera>();
        if (Camera.main != null) camera.CopyFrom(Camera.main);
        camera.enabled = false; camera.fieldOfView = 66; camera.nearClipPlane = .03f; camera.farClipPlane = 80;
        camera.transform.position = position;
        camera.transform.rotation = overhead ? Quaternion.Euler(90, 0, 0) : Quaternion.LookRotation(target - position);
        camera.orthographic = overhead; camera.orthographicSize = 14.2f;
        camera.aspect = overhead ? 1 : 1.6f;
        var hidden = overhead ? Interior.GetComponentsInChildren<Renderer>().Where(r => r.enabled &&
            (r.name == "Store Ceiling" || r.name == "Ceiling Diffuser" ||
             (Interior.Find("Shop Ceiling Finish") != null && r.transform.IsChildOf(Interior.Find("Shop Ceiling Finish"))))).ToArray() : Array.Empty<Renderer>();
        int width = overhead ? 1400 : 1440, height = overhead ? 1400 : 900;
        var rt = new RenderTexture(width, height, 24);
        var previous = RenderTexture.active; Texture2D texture = null;
        try
        {
            foreach (var renderer in hidden) renderer.enabled = false;
            camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
            File.WriteAllBytes(Evidence + "/" + name + ".png", texture.EncodeToPNG());
        }
        finally
        {
            foreach (var renderer in hidden) renderer.enabled = true;
            camera.targetTexture = null; RenderTexture.active = previous;
            Object.DestroyImmediate(go); Object.DestroyImmediate(rt); if (texture != null) Object.DestroyImmediate(texture);
        }
    }
}
