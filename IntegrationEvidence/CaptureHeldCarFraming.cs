using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TinyRiders;
using UnityEngine;

public static class CaptureHeldCarFraming
{
    public static async Task<object> VerifyMotion()
    {
        Application.runInBackground = true;
        while (ThemedCollectionSession.Instance == null || Player.Instance == null || Time.frameCount < 10 || ThemedCollectionSession.Instance.LivePileCount != 1200)
            await Task.Delay(25);
        var player = Player.Instance;
        var session = ThemedCollectionSession.Instance;
        session.suspendPersistenceForTests = true;
        var camera = Camera.main;
        player.StashHandler.LoadFromSave(Array.Empty<ItemDataSO>(), 0);
        player.ItemHandler.Unequip();
        player.ItemCollector.TryCollectItem(session.ActiveDefinitions.First(e => e.themeId == "TH_JUNGLE" && e.baseCarId == "C013").sortingItem);
        player.Motor.SetPosition(session.roomLayout.spawn);
        player.Look.SetLook(0, 12);
        var previous = player.StateMachine.CurrentState;
        player.StateMachine.enabled = false;
        int renderedFrames = 0;
        string failure = null;
        Action<UnityEngine.Rendering.ScriptableRenderContext, Camera> validate = (context, view) =>
        {
            if (view != camera || failure != null) return;
            try { HeldCarFramingTests.AssertVisible(player.ItemHandler.CurrentEquipped, camera); renderedFrames++; }
            catch (Exception error) { failure = error.Message; }
        };
        UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += validate;
        var stages = new List<object>();
        try
        {
            foreach (var state in new BasePlayerState[] { player.GetComponentInChildren<PlayerState_Moving>(), player.GetComponentInChildren<PlayerState_Sprint>() })
            {
                player.StateMachine.ChangeState(state);
                var start = player.transform.position;
                float until = Time.realtimeSinceStartup + .8f;
                while (Time.realtimeSinceStartup < until)
                {
                    var input = new KinematicCharacterController.Examples.PlayerCharacterInputs { MoveAxisForward = 1, CameraRotation = Quaternion.identity };
                    player.Character.SetInputs(ref input);
                    await Task.Delay(20);
                }
                float distance = Vector3.Distance(start, player.transform.position);
                if (distance < .1f) throw new Exception("Movement did not advance: " + state.GetType().Name);
                stages.Add(new { state = state.GetType().Name, distance, fov = camera.fieldOfView });
            }
            if (failure != null) throw new Exception(failure);
            if (renderedFrames < 5) throw new Exception("Too few rendered frames for verification");
            var result = new { renderedFrames, stages, passed = true };
            File.WriteAllText("IntegrationEvidence/held_framing_movement.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
            return result;
        }
        finally
        {
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= validate;
            var idle = new KinematicCharacterController.Examples.PlayerCharacterInputs { CameraRotation = Quaternion.identity };
            player.Character.SetInputs(ref idle);
            player.StateMachine.ChangeState(previous); player.StateMachine.enabled = true;
        }
    }

    public static async Task<string[]> Main()
    {
        Application.runInBackground = true;
        while (ThemedCollectionSession.Instance == null || Player.Instance == null || Time.frameCount < 10)
            await Task.Delay(25);
        var player = Player.Instance;
        var session = ThemedCollectionSession.Instance;
        session.suspendPersistenceForTests = true;
        var entries = session.ActiveDefinitions.ToArray();
        var cars = new[] {
            entries.First(e => e.themeId == "TH_ROYAL" && e.baseCarId == "C009"),
            entries.First(e => e.themeId == "TH_JUNGLE" && e.baseCarId == "C013"),
            entries.OrderByDescending(e => Mathf.Max(e.displaySize.x, e.displaySize.z) / Mathf.Max(.001f, Mathf.Min(e.displaySize.x, e.displaySize.z))).First(),
            entries.OrderByDescending(e => e.displaySize.y / Mathf.Max(e.displaySize.x, e.displaySize.z)).First()
        };
        var camera = Camera.main;
        player.Motor.SetPosition(session.roomLayout.spawn);
        player.Look.SetLook(0, 12);
        var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
            .Where(c => c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
        var oldCameras = canvases.Select(c => c.worldCamera).ToArray();
        var oldDistances = canvases.Select(c => c.planeDistance).ToArray();
        var files = new List<string>();
        Directory.CreateDirectory("IntegrationEvidence/HeldCarFraming");
        try
        {
            foreach (var entry in cars)
            {
                player.StashHandler.LoadFromSave(Array.Empty<ItemDataSO>(), 0);
                player.ItemHandler.Unequip();
                if (!player.ItemCollector.TryCollectItem(entry.sortingItem)) throw new Exception("Capture pickup failed");
                float until = Time.realtimeSinceStartup + .5f;
                while (Time.realtimeSinceStartup < until) await Task.Yield();
                foreach (var dimensions in new[] { new Vector2Int(1280,720), new Vector2Int(1280,800), new Vector2Int(1680,720) })
                {
                    camera.aspect = (float)dimensions.x / dimensions.y;
                    var hand = player.ItemHandler.CurrentEquipped;
                    var pose = player.ItemHandler.GetHeldPose(hand);
                    hand.transform.SetLocalPositionAndRotation(pose.Position, pose.Rotation); hand.transform.localScale = pose.Scale;
                    HeldCarFramingTests.AssertVisible(hand, camera);
                    foreach (var canvas in canvases)
                    { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = camera.nearClipPlane + .01f; }
                    var target = new RenderTexture(dimensions.x, dimensions.y, 24);
                    var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active;
                    Texture2D image = null;
                    try
                    {
                        camera.targetTexture = target; Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                        image = new Texture2D(dimensions.x, dimensions.y, TextureFormat.RGB24, false);
                        image.ReadPixels(new Rect(0,0,dimensions.x,dimensions.y),0,0); image.Apply();
                        string path = "IntegrationEvidence/HeldCarFraming/" + entry.collectibleId + "_" + dimensions.x + "x" + dimensions.y + ".png";
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
            return files.ToArray();
        }
        finally
        {
            camera.ResetAspect();
            for (int i=0;i<canvases.Length;i++)
            { canvases[i].renderMode=RenderMode.ScreenSpaceOverlay;canvases[i].worldCamera=oldCameras[i];canvases[i].planeDistance=oldDistances[i]; }
        }
    }
}
