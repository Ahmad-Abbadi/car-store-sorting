#if CAR_RENDER_BENCHMARK || UNITY_WEBGL
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using TinyRiders;
using UnityEngine;
using UnityEngine.SceneManagement;

// Dormant local diagnostics. Ordinary players return before registering callbacks or touching state.
[DefaultExecutionOrder(30000)]
public sealed class CarRenderingBenchmark : MonoBehaviour
{
    [DllImport("__Internal")] private static extern void CarBatchReport(string json);
    private Camera view;
    private Vector3 eye, aim;
    private bool motion;
    private float viewStart;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (!Uri.TryCreate(Application.absoluteURL, UriKind.Absolute, out var url) ||
            !string.Equals(url.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
            !url.Query.TrimStart('?').Split('&').Contains("carBenchmark=1")) return;
        SceneManager.sceneLoaded += (scene, mode) =>
        {
            // Let normal initialization finish, then bypass menu/tutorial only for this explicit local QA run.
            if (scene.name == "MainMenu Scene") { SceneManager.LoadSceneAsync("Level"); return; }
            if (scene.name != "Level") return;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            foreach (var type in new[] { typeof(LevelSaveManager), typeof(PlayerSavePosition), typeof(CoinsManager) })
                foreach (var obj in UnityEngine.Object.FindObjectsByType(type, FindObjectsSortMode.None))
                    type.GetField("saveKey", flags)?.SetValue(obj, "CarRenderingBenchmark." + type.Name);
            var session = UnityEngine.Object.FindFirstObjectByType<ThemedCollectionSession>();
            if (session != null)
            {
                session.suspendPersistenceForTests = true;
                session.completeStoreWhenSorted = false;
                new GameObject("Car Rendering Benchmark").AddComponent<CarRenderingBenchmark>();
            }
        };
    }

    private IEnumerator Start()
    {
        Application.runInBackground = true;
        while (ThemedCollectionSession.Instance == null || !ThemedCollectionSession.Instance.IsReady || Player.Instance == null)
            yield return null;
        ItemDetailLevel targetQuality = Application.absoluteURL.Contains("quality=low") ? ItemDetailLevel.Low : ItemDetailLevel.Medium;
        if (ItemDetail.Current != targetQuality) ItemDetail.Set(targetQuality);
        var session = ThemedCollectionSession.Instance;
        if (Player.Instance.Input != null) Player.Instance.Input.enabled = false;
        if (Player.Instance.Look != null) Player.Instance.Look.enabled = false;
        view = Camera.main;
        foreach (var behaviour in view.GetComponents<Behaviour>())
            if (behaviour.GetType().Name == "CinemachineBrain") behaviour.enabled = false;
        bool shelves = Application.absoluteURL.Contains("scenario=shelves");
        motion = Application.absoluteURL.Contains("scenario=motion");
        if (shelves)
        {
            foreach (var shelf in FindObjectsByType<SortingShelf>(FindObjectsSortMode.None).OrderBy(s => s.SaveId))
            {
                for (int slot = 0; slot < shelf.SpotCount; slot++)
                {
                    var world = WorldItemBase.All.FirstOrDefault(w => w != null && w.gameObject.activeInHierarchy &&
                        w.Data is SortableItem_SO data && shelf.CanAccept(data, slot, out _));
                    if (world == null) continue;
                    if (!Player.Instance.ItemCollector.TryCollectItem(world)) throw new Exception("Benchmark pickup failed");
                    if (!Player.Instance.SortingBehaviour.TryGetPlacedItemFromHand(out var placed)) throw new Exception("Benchmark placement failed");
                    shelf.AddItem(placed, slot);
                }
                yield return null;
            }
        }
        Vector3 center = session.roomLayout != null ? session.roomLayout.origin : session.pileOrigin.position;
        eye = center + new Vector3(0, 1.7f, -10);
        aim = center + new Vector3(0, .25f, 3);
        if (shelves) { eye = center + new Vector3(0, 1.7f, 0); aim = center + new Vector3(0, 1.2f, 12); }
        viewStart = Time.realtimeSinceStartup;
        yield return new WaitForSecondsRealtime(5);
        var frames = new List<float>();
        float until = Time.realtimeSinceStartup + 15;
        while (Time.realtimeSinceStartup < until) { frames.Add(Time.unscaledDeltaTime * 1000); yield return null; }
        frames.Sort();
        var spatial = FindFirstObjectByType<CarSpatialRenderer>();
        CarBatchReport(JsonUtility.ToJson(new Report {
            scenario = shelves ? "shelves" : motion ? "motion" : "pile", samples = frames.Count, averageFrameMs = frames.Average(),
            p95FrameMs = frames[Mathf.Min(frames.Count - 1, (int)(frames.Count * .95f))],
            p99FrameMs = frames[Mathf.Min(frames.Count - 1, (int)(frames.Count * .99f))],
            maxFrameMs = frames[frames.Count - 1],
            totalCars = session.Total, worldCars = WorldItemBase.All.Count, sortedCars = session.Sorted,
            frozenWorldCars = WorldItemBase.All.Count(w => w != null && w.Frozen),
            placedCars = FindObjectsByType<PlacedOnShelfItemBase>(FindObjectsSortMode.None).Length,
            proxiedCars = spatial != null ? spatial.ProxiedCars : 0,
            combinedCells = spatial != null ? spatial.CombinedCells : 0,
            activeMeshRenderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Count(r => r.enabled && r.gameObject.activeInHierarchy),
            allocatedBytes = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong(),
            quality = ItemDetail.Current.ToString(), width = Screen.width, height = Screen.height,
            eye = eye, aim = aim
        }));
    }
    private void LateUpdate()
    {
        if (view == null) return;
        Vector3 position = eye;
        Vector3 target = aim;
        if (motion)
        {
            float t = Time.realtimeSinceStartup - viewStart;
            position += new Vector3(2f * Mathf.Sin(t * .65f), 0, 2f * Mathf.Sin(t * .43f));
            target += new Vector3(5f * Mathf.Sin(t * .9f), 0, 0);
        }
        view.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position));
    }
    [Serializable] private class Report
    {
        public string scenario, quality;
        public int samples, totalCars, worldCars, sortedCars, width, height;
        public int frozenWorldCars, placedCars, proxiedCars, combinedCells, activeMeshRenderers;
        public float averageFrameMs, p95FrameMs, p99FrameMs, maxFrameMs;
        public long allocatedBytes;
        public Vector3 eye, aim;
    }
}
#endif
