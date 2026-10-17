using System.IO;
using TinyRiders;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class CaptureGoldenCarDisplay
{
    public static string Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return "Wait for tests to finish.";
        var reward = Object.FindFirstObjectByType<GoldenCarReveal>();
        bool visible = reward.trophy.activeSelf;
        bool covered = reward.cover.gameObject.activeSelf;
        string label = reward.placard.text;
        float intensity = reward.displayLight.intensity;
        var cameraObject = new GameObject("Golden Car Evidence Camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.CopyFrom(Camera.main);
        camera.transform.SetPositionAndRotation(reward.cameraViewpoint.position, reward.cameraViewpoint.rotation);
        camera.fieldOfView = 43;
        cameraObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        var target = new RenderTexture(1280, 960, 24);
        var previous = RenderTexture.active;
        var image = new Texture2D(1280, 960, TextureFormat.RGB24, false);
        try
        {
            Directory.CreateDirectory("IntegrationEvidence/GoldenCar");
            camera.targetTexture = target;
            void Capture(string name)
            {
                camera.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1280, 960), 0, 0); image.Apply();
                File.WriteAllBytes("IntegrationEvidence/GoldenCar/" + name + ".png", image.EncodeToPNG());
            }
            Capture("concealed");
            reward.trophy.SetActive(true); reward.cover.gameObject.SetActive(false);
            reward.placard.text = "GOLDEN CAR\n<size=55%>COLLECTION COMPLETE</size>";
            reward.placard.ForceMeshUpdate(); reward.displayLight.intensity = 3;
            Capture("revealed");
        }
        finally
        {
            reward.trophy.SetActive(visible); reward.cover.gameObject.SetActive(covered);
            reward.placard.text = label; reward.placard.ForceMeshUpdate(); reward.displayLight.intensity = intensity;
            camera.targetTexture = null; RenderTexture.active = previous;
            target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(image); Object.DestroyImmediate(cameraObject);
        }
        return "IntegrationEvidence/GoldenCar/revealed.png";
    }
}
