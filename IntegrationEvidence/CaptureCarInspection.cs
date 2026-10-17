using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using TinyRiders;
using UnityEngine;

public static class CaptureCarInspection
{
    public static async Task<object> Main()
    {
        var player = Player.Instance;
        var session = ThemedCollectionSession.Instance;
        if (player == null || session == null) throw new Exception("Enter Play mode first");
        Application.runInBackground = true;
        var stash = player.StashHandler;
        var items = Enumerable.Range(0, stash.Inventory.Count).Select(stash.Inventory.GetItemAt).ToArray();
        var ids = Enumerable.Range(0, stash.Inventory.Count).Select(stash.Inventory.InstanceIdAt).ToList();
        int selection = stash.SelectedIndex;
        var originalHandData = player.ItemHandler.CurrentEquipped != null ? player.ItemHandler.CurrentEquipped.Data : null;
        float yaw = player.Look.Yaw, pitch = player.Look.Pitch;
        bool suspended = session.suspendPersistenceForTests;
        session.suspendPersistenceForTests = true;
        var camera = Camera.main;
        var entries = session.ActiveDefinitions.ToArray();
        var cars = new[] {
            entries.First(e => e.themeId == "TH_PIRATE" && e.baseCarId == "C002"),
            entries.OrderByDescending(e => e.displaySize.y / Mathf.Max(e.displaySize.x, e.displaySize.z)).First(),
            entries.OrderByDescending(e => Mathf.Max(e.displaySize.x, e.displaySize.z) / Mathf.Max(.001f, Mathf.Min(e.displaySize.x, e.displaySize.z))).First()
        };
        var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
            .Where(c => c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
        var oldCameras = canvases.Select(c => c.worldCamera).ToArray();
        var oldDistances = canvases.Select(c => c.planeDistance).ToArray();
        var files = new List<string>();
        int startFrame = Time.frameCount;
        Directory.CreateDirectory("IntegrationEvidence/CarInspection");
        try
        {
            player.Look.SetLook(0, 12);
            foreach (var entry in cars)
            {
                stash.LoadFromSave(new ItemDataSO[] { entry.sortingItem }, 0);
                player.ItemHandler.Equip_ItemData(entry.sortingItem);
                float until = Time.realtimeSinceStartup + .7f;
                while (Time.realtimeSinceStartup < until) await Task.Yield();
                foreach (bool underside in new[] { false, true })
                {
                    typeof(ItemsHandler).GetField("_inspectionRotation", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                        .SetValue(player.ItemHandler, underside ? Quaternion.Euler(180, 60, 0) : Quaternion.identity);
                    var hand = player.ItemHandler.CurrentEquipped;
                    var pose = player.ItemHandler.GetHeldPose(hand);
                    hand.transform.SetLocalPositionAndRotation(pose.Position, pose.Rotation);
                    hand.transform.localScale = pose.Scale;
                    HeldCarFramingTests.AssertVisible(hand, camera, !underside);
                    foreach (var canvas in canvases)
                    { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = camera.nearClipPlane + .01f; }
                    var target = new RenderTexture(1280, 720, 24);
                    var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active;
                    Texture2D image = null;
                    try
                    {
                        camera.targetTexture = target; Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                        image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                        image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
                        string path = "IntegrationEvidence/CarInspection/" + entry.collectibleId + (underside ? "_underside" : "_default") + ".png";
                        File.WriteAllBytes(path, image.EncodeToPNG()); files.Add(path);
                    }
                    finally
                    {
                        camera.targetTexture = oldTarget; RenderTexture.active = oldActive;
                        target.Release(); UnityEngine.Object.DestroyImmediate(target);
                        if (image != null) UnityEngine.Object.DestroyImmediate(image);
                        foreach (var canvas in canvases) canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    }
                }
            }
            return new { framesAdvanced = Time.frameCount - startFrame, files };
        }
        finally
        {
            stash.LoadFromSave(items, selection); stash.Inventory.SetInstanceIds(ids);
            if (originalHandData != null) player.ItemHandler.Equip_ItemData(originalHandData);
            else player.ItemHandler.Unequip();
            player.Look.SetLook(yaw, pitch);
            session.suspendPersistenceForTests = suspended;
            for (int i = 0; i < canvases.Length; i++)
            { canvases[i].renderMode = RenderMode.ScreenSpaceOverlay; canvases[i].worldCamera = oldCameras[i]; canvases[i].planeDistance = oldDistances[i]; }
        }
    }
}
