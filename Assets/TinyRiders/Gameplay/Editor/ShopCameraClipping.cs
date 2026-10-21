using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Room-sized viewing range, shared by gameplay and cinematic generators.</summary>
public static class ShopCameraClipping
{
    // The shop's longest dimension is 38 m. Allow the entire room and its
    // cinematic endpoints, without wasting depth range on distant legacy rooms.
    public const float FarDistance = 80f;

    public static void Apply(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var camera in root.GetComponentsInChildren<Camera>(true))
            {
                camera.farClipPlane = FarDistance;
                EditorUtility.SetDirty(camera);
                PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
            }
            foreach (var camera in root.GetComponentsInChildren<CinemachineCamera>(true))
            {
                var lens = camera.Lens; lens.FarClipPlane = FarDistance; camera.Lens = lens;
                EditorUtility.SetDirty(camera);
                PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
            }
        }
    }
}
