using System.Collections;
using System.Collections.Generic;
using TinyRiders;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>Combines distant, settled cars into small view culled cells.</summary>
public sealed class CarSpatialRenderer : MonoBehaviour
{
    private const float CellSize = 4.5f;
    private const float FullDetailDistance = 5f;
    private const float CheckInterval = 0.12f;

    private sealed class Car
    {
        public WorldItemBase world;
        public PlacedOnShelfItemBase placed;
        public ItemVisualGroup visual;
        public Transform transform;
        public Mesh proxy;
        public bool included;
        public bool wrong;
    }

    private sealed class Cell
    {
        public Vector2Int key;
        public readonly List<Car> cars = new List<Car>();
        public Mesh mesh;
        public Mesh wrongMesh;
        public bool near;
        public bool visible;
        public bool dirty = true;
    }

    private readonly Dictionary<Vector2Int, Cell> cells = new Dictionary<Vector2Int, Cell>();
    private readonly HashSet<WorldItemBase> knownWorld = new HashSet<WorldItemBase>();
    private readonly HashSet<PlacedOnShelfItemBase> knownPlaced = new HashSet<PlacedOnShelfItemBase>();
    private readonly Plane[] planes = new Plane[6];
    private CarSpatialCatalog catalog;
    private Material wrongMaterial;
    private Camera view;
    private float nextCheck;
    private float nextDiscover;

    public int ProxiedCars { get; private set; }
    public int CombinedCells { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        SceneManager.sceneLoaded -= Install;
        SceneManager.sceneLoaded += Install;
#endif
    }

    private static void Install(Scene scene, LoadSceneMode mode)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (scene.name != "Level") return;
        var session = FindFirstObjectByType<ThemedCollectionSession>();
        if (session != null && session.GetComponent<CarSpatialRenderer>() == null)
            session.gameObject.AddComponent<CarSpatialRenderer>();
#endif
    }

    private IEnumerator Start()
    {
        catalog = Resources.Load<CarSpatialCatalog>("CarSpatialCatalog");
        if (catalog == null || catalog.material == null) { enabled = false; yield break; }
        wrongMaterial = new Material(catalog.material) { name = "Distant wrong car" };
        wrongMaterial.SetColor("_ProxyTint", new Color(1f, 0.2f, 0.2f));
        while (ThemedCollectionSession.Instance == null || !ThemedCollectionSession.Instance.IsReady)
            yield return null;
        Discover();
    }

    private static Vector2Int Key(Vector3 point) =>
        new Vector2Int(Mathf.FloorToInt(point.x / CellSize), Mathf.FloorToInt(point.z / CellSize));

    private void Add(Car car)
    {
        Vector2Int key = Key(car.transform.position);
        if (!cells.TryGetValue(key, out var cell))
        {
            cell = new Cell { key = key };
            cells.Add(key, cell);
        }
        cell.cars.Add(car);
        cell.dirty = true;
    }

    private void Discover()
    {
        // Shelf placement and throws move objects after their first registration.
        // Keep the cell membership at the settled pose so they remain batchable.
        var moved = new List<(Cell cell, Car car)>();
        foreach (var cell in cells.Values)
            foreach (var car in cell.cars)
                if (car.transform == null ||
                    ((car.world == null || car.world.Frozen) && (car.placed == null || car.placed.IsSettled) &&
                     Key(car.transform.position) != cell.key))
                    moved.Add((cell, car));
        foreach (var (cell, car) in moved)
        {
            cell.cars.Remove(car);
            cell.dirty = true;
            car.included = false;
            if (car.transform != null) Add(car);
        }
        knownWorld.RemoveWhere(item => item == null);
        knownPlaced.RemoveWhere(item => item == null);
        foreach (var item in WorldItemBase.All)
        {
            if (item == null || knownWorld.Contains(item) || item.GetComponent<ThemedWorldInstance>() == null ||
                !catalog.TryGet(item.Data, out var proxy)) continue;
            var visual = item.GetComponent<ItemVisualGroup>();
            if (visual == null) continue;
            knownWorld.Add(item);
            Add(new Car { world = item, transform = item.transform, visual = visual, proxy = proxy });
        }
        foreach (var item in FindObjectsByType<PlacedOnShelfItemBase>(FindObjectsSortMode.None))
        {
            if (item == null || knownPlaced.Contains(item) || item.Data == null ||
                !catalog.TryGet(item.Data, out var proxy)) continue;
            var visual = item.GetComponent<ItemVisualGroup>();
            if (visual == null) continue;
            knownPlaced.Add(item);
            Add(new Car { placed = item, transform = item.transform, visual = visual, proxy = proxy });
        }
    }

    private void RefreshCells()
    {
        if (view == null) view = Camera.main;
        if (view == null) return;
        GeometryUtility.CalculateFrustumPlanes(view, planes);
        Vector3 eye = view.transform.position;
        foreach (var cell in cells.Values)
        {
            float x = (cell.key.x + .5f) * CellSize;
            float z = (cell.key.y + .5f) * CellSize;
            float dx = Mathf.Max(0, Mathf.Abs(eye.x - x) - CellSize * .5f);
            float dz = Mathf.Max(0, Mathf.Abs(eye.z - z) - CellSize * .5f);
            cell.near = dx * dx + dz * dz < FullDetailDistance * FullDetailDistance;
            cell.visible = GeometryUtility.TestPlanesAABB(planes,
                new Bounds(new Vector3(x, 2f, z), new Vector3(CellSize, 8f, CellSize)));
        }
    }

    private static bool Eligible(Car car, Vector2Int key)
    {
        if (car.visual == null || car.transform == null || !car.visual.gameObject.activeInHierarchy ||
            car.visual.IsOutlined || Key(car.transform.position) != key) return false;
        if (car.world != null)
            return car.world.Frozen && !car.world.ThrowControlled && car.world.PowerOwner == null;
        return car.placed != null && car.placed.IsSettled && !car.placed.IsFlashingWrong;
    }

    private static Mesh Combine(List<CombineInstance> parts)
    {
        if (parts.Count == 0) return null;
        var mesh = new Mesh { name = "Distant car cell", indexFormat = IndexFormat.UInt32 };
        mesh.CombineMeshes(parts.ToArray(), true, true, false);
        mesh.UploadMeshData(true);
        return mesh;
    }

    private void Rebuild(Cell cell)
    {
        var parts = new List<CombineInstance>(cell.cars.Count);
        var wrongParts = new List<CombineInstance>();
        foreach (var car in cell.cars)
            if (car.included && car.transform != null)
                (car.wrong ? wrongParts : parts).Add(new CombineInstance { mesh = car.proxy, subMeshIndex = 0,
                    transform = car.transform.localToWorldMatrix });
        Mesh replacement = Combine(parts);
        Mesh wrongReplacement = Combine(wrongParts);
        if (cell.mesh != null) Destroy(cell.mesh);
        if (cell.wrongMesh != null) Destroy(cell.wrongMesh);
        cell.mesh = replacement;
        cell.wrongMesh = wrongReplacement;
        cell.dirty = false;
    }

    private void LateUpdate()
    {
        if (catalog == null) return;
        if (Time.unscaledTime >= nextCheck)
        {
            nextCheck = Time.unscaledTime + CheckInterval;
            if (Time.unscaledTime >= nextDiscover)
            {
                nextDiscover = Time.unscaledTime + 2f;
                Discover();
            }
            RefreshCells();
        }
        if (view == null) return;
        ProxiedCars = 0;
        CombinedCells = 0;
        Cell rebuild = null;
        foreach (var cell in cells.Values)
        {
            foreach (var car in cell.cars)
            {
                if (car.visual == null)
                {
                    if (car.included) { car.included = false; cell.dirty = true; }
                    continue;
                }
                bool include = Eligible(car, cell.key);
                bool wrong = include && car.placed != null && car.placed.IsWrong;
                if (car.included != include) { car.included = include; cell.dirty = true; }
                if (car.wrong != wrong) { car.wrong = wrong; cell.dirty = true; }
                bool proxyVisible = include && !cell.near;
                car.visual.SetFar(proxyVisible);
                if (proxyVisible) ProxiedCars++;
            }
            if (cell.dirty && rebuild == null) rebuild = cell;
            if (cell.near || !cell.visible) continue;
            if (cell.dirty)
            {
                foreach (var car in cell.cars)
                    if (car.included)
                        Graphics.DrawMesh(car.proxy, car.transform.localToWorldMatrix,
                            car.wrong ? wrongMaterial : catalog.material, 0,
                            view, 0, null, ShadowCastingMode.Off, false);
            }
            else if (cell.mesh != null)
            {
                Graphics.DrawMesh(cell.mesh, Matrix4x4.identity, catalog.material, 0,
                    view, 0, null, ShadowCastingMode.Off, false);
                CombinedCells++;
            }
            if (!cell.dirty && cell.wrongMesh != null)
            {
                Graphics.DrawMesh(cell.wrongMesh, Matrix4x4.identity, wrongMaterial, 0,
                    view, 0, null, ShadowCastingMode.Off, false);
                CombinedCells++;
            }
        }
        // One cell per frame avoids a startup stall and keeps pickup rebuilds local.
        if (rebuild != null) Rebuild(rebuild);
    }

    private void OnDisable()
    {
        foreach (var cell in cells.Values)
        {
            foreach (var car in cell.cars)
                if (car.visual != null) car.visual.SetFar(false);
            if (cell.mesh != null) Destroy(cell.mesh);
            if (cell.wrongMesh != null) Destroy(cell.wrongMesh);
            cell.mesh = null;
            cell.wrongMesh = null;
        }
        if (wrongMaterial != null) Destroy(wrongMaterial);
    }
}
