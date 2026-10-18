using System.Collections.Generic;
using System.Runtime.InteropServices;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary> Merges resting item meshes into one mesh per grid cell; one draw per cell. </summary>
[DefaultExecutionOrder(100)] // runs after items register and move this frame
public class ItemChunkRenderer : MonoBehaviour
{
    [Tooltip("Only renderers using this material are merged.")]
    [SerializeField] private Material sourceMaterial;

    [Tooltip("Material drawing the merged cells (Template/Items Chunk).")]
    [SerializeField] private Material chunkMaterial;

    [Tooltip("Grid cell size in meters; one mesh and one draw per cell.")]
    [SerializeField, Min(0.5f)] private float cellSize = 4f;

    [Tooltip("Seconds a detached item must stay still before it merges back.")]
    [SerializeField, Min(0f)] private float restSeconds = 0.5f;

    [Tooltip("Max milliseconds per frame spent rebuilding cells during play.")]
    [SerializeField, Min(0.1f)] private float rebuildBudgetMs = 3f;

    [Tooltip("Seconds after load where every dirty cell rebuilds at once.")]
    [SerializeField, Min(0f)] private float startupSeconds = 1.5f;


    private const float LightScale = 2f;     // must match the shader's _LightScale
    private const float MoveEpsilonSqr = 1e-6f;
    private const float TurnEpsilon = 0.1f;  // degrees
    private const int LodCount = 3;

    /// <summary> A item renderer tracked by the system. </summary>
    public sealed class Member
    {
        internal MeshRenderer Renderer;
        internal Mesh Mesh;
        internal Transform Root;    // tween and pose target
        internal Rigidbody Body;
        internal Cell Cell;         // cell holding it, or null
        internal bool Waiting;      // detached, checking for rest
        internal bool Queued;       // attach requested, cell not chosen yet
        internal float StillTime;
        internal Vector3 LastPosition;
        internal Quaternion LastRotation;
        internal Vector3 LastScale;
    }

    /// <summary> One grid cell's merged mesh. </summary>
    internal sealed class Cell
    {
        public Vector3Int Key;
        public readonly List<Member> Members = new List<Member>();
        public MeshFilter Filter;
        public MeshRenderer Renderer;
        public bool Dirty;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Vertex
    {
        public Vector3 Position; // Unity's fixed attribute order: position, color, uv
        public Color32 Light;
        public ushort U, V;      // half floats
    }

    /// <summary> Cached source mesh data read once per mesh. </summary>
    private sealed class SourceMesh
    {
        public Vector3[] Positions;
        public ushort[] U, V;           // half-float UVs
        public int[][] Lods;            // triangle lists per LOD; lower levels may repeat higher ones
        public int[] NormalIndex;       // per vertex, into UniqueNormals
        public Vector3[] UniqueNormals; // faceted items share few normals
        public Vector3[] WorldNormals;
        public Color[] Lights;
        public Color32[] Encoded;
    }

    private static ItemChunkRenderer _instance;

    private readonly Dictionary<Vector3Int, Cell> _cells = new Dictionary<Vector3Int, Cell>();
    private readonly List<Cell> _dirtyCells = new List<Cell>();
    private readonly List<Member> _waiting = new List<Member>();
    private readonly List<Member> _queued = new List<Member>();
    private readonly Dictionary<Mesh, SourceMesh> _sources = new Dictionary<Mesh, SourceMesh>();
    private readonly System.Diagnostics.Stopwatch _clock = new System.Diagnostics.Stopwatch();
    private VertexAttributeDescriptor[] _layout;
    private Vertex[] _vertexBuffer = new Vertex[0];
    private ushort[] _shortIndices = new ushort[0];
    private int[] _intIndices = new int[0];
    private readonly List<int> _bases = new List<int>(); // first vertex of each member in the cell
    private float _startTime;

    /// <summary> Number of merged cells with geometry. </summary>
    public int CellCount => _cells.Count;

    private void Awake()
    {
        _instance = this;
        _startTime = Time.time;
        _layout = new[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, 2),
        };
    }

    private void OnEnable()
    {
        ItemDetail.OnChanged += ApplyDetail;
    }

    private void OnDisable()
    {
        ItemDetail.OnChanged -= ApplyDetail;
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
        foreach (Cell cell in _cells.Values)
            if (cell.Filter != null) Destroy(cell.Filter.sharedMesh);
    }

    // ------------------------------------------------------------------ public API

    /// <summary> Starts tracking a item renderer; null if it can't be merged. </summary>
    public static Member Register(MeshRenderer renderer, Transform root, bool atRest)
    {
        if (_instance == null || renderer == null) return null;
        if (!_instance.CanMerge(renderer, out Mesh mesh)) return null;

        Member member = new Member { Renderer = renderer, Mesh = mesh, Root = root != null ? root : renderer.transform };
        member.Body = member.Root.GetComponentInParent<Rigidbody>();

        if (atRest) _instance.Queue(member);
        else _instance.StartWaiting(member);
        return member;
    }

    /// <summary> Stops tracking; drops it from its cell. </summary>
    public static void Unregister(Member member)
    {
        if (_instance == null || member == null) return;
        _instance.RemoveFromCell(member);
        _instance.StopWaiting(member);
        _instance.Dequeue(member);
    }

    /// <summary> Shows the item on its own renderer until it rests again. </summary>
    public static void Detach(Member member)
    {
        if (_instance == null || member == null || member.Waiting) return;
        _instance.Dequeue(member);
        _instance.RemoveFromCell(member);
        _instance.StartWaiting(member);
    }

    /// <summary> Refreshes the cached Rigidbody after one is added or removed. </summary>
    public static void RefreshBody(Member member, Rigidbody body)
    {
        if (member != null) member.Body = body;
    }

    // ------------------------------------------------------------------ loop

    private void LateUpdate()
    {
        CheckWaiting();
        AssignQueued();
        RebuildDirty();
    }

    /// <summary> Forces the player's chosen detail level on every cell. </summary>
    private void ApplyDetail()
    {
        short lod = (short)ItemDetail.Lod;
        foreach (Cell cell in _cells.Values) cell.Renderer.forceMeshLod = lod;
    }

    /// <summary> Queues detached items that stayed still long enough. </summary>
    private void CheckWaiting()
    {
        float dt = Time.deltaTime;
        for (int i = _waiting.Count - 1; i >= 0; i--)
        {
            Member m = _waiting[i];
            if (m.Renderer == null)
            {
                SwapRemove(_waiting, i);
                m.Waiting = false;
                continue;
            }

            Transform t = m.Renderer.transform;
            t.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
            Vector3 scale = t.lossyScale;

            bool still = (position - m.LastPosition).sqrMagnitude < MoveEpsilonSqr
                && Quaternion.Angle(rotation, m.LastRotation) < TurnEpsilon
                && (scale - m.LastScale).sqrMagnitude < MoveEpsilonSqr
                && !DOTween.IsTweening(m.Root)
                && (m.Body == null || m.Body.isKinematic || m.Body.IsSleeping());

            m.LastPosition = position;
            m.LastRotation = rotation;
            m.LastScale = scale;
            m.StillTime = still ? m.StillTime + dt : 0f;
            if (m.StillTime < restSeconds) continue;

            SwapRemove(_waiting, i);
            m.Waiting = false;
            Queue(m);
        }
    }

    /// <summary> Puts queued items into the cell under their current bounds. </summary>
    private void AssignQueued()
    {
        for (int i = 0; i < _queued.Count; i++)
        {
            Member m = _queued[i];
            m.Queued = false;
            if (m.Renderer == null) continue;

            Cell cell = GetCell(m.Renderer.bounds.center);
            m.Cell = cell;
            cell.Members.Add(m);
            MarkDirty(cell);
        }
        _queued.Clear();
    }

    /// <summary> Rebuilds dirty cells; unlimited at startup, time-budgeted after. </summary>
    private void RebuildDirty()
    {
        if (_dirtyCells.Count == 0) return;

        bool startup = Time.time - _startTime < startupSeconds;
        _clock.Restart();

        int done = 0;
        while (done < _dirtyCells.Count)
        {
            Rebuild(_dirtyCells[done]);
            done++;
            if (!startup && _clock.Elapsed.TotalMilliseconds >= rebuildBudgetMs) break;
        }
        _dirtyCells.RemoveRange(0, done);
    }

    // ------------------------------------------------------------------ state changes

    private void Queue(Member m)
    {
        if (m.Queued) return;
        m.Queued = true;
        _queued.Add(m);
    }

    private void Dequeue(Member m)
    {
        if (!m.Queued) return;
        m.Queued = false;
        _queued.Remove(m);
    }

    private void StartWaiting(Member m)
    {
        if (m.Renderer != null) m.Renderer.enabled = true;
        if (m.Waiting) return;

        m.Waiting = true;
        m.StillTime = 0f;
        m.LastPosition = new Vector3(float.NaN, 0f, 0f); // first check never counts as still
        _waiting.Add(m);
    }

    private void StopWaiting(Member m)
    {
        if (!m.Waiting) return;
        m.Waiting = false;
        _waiting.Remove(m);
    }

    private void RemoveFromCell(Member m)
    {
        if (m.Cell == null) return;
        m.Cell.Members.Remove(m);
        MarkDirty(m.Cell);
        m.Cell = null;
    }

    private void MarkDirty(Cell cell)
    {
        if (cell.Dirty) return;
        cell.Dirty = true;
        _dirtyCells.Add(cell);
    }

    /// <summary> Rebuilds every cell, e.g. after the light probes changed. </summary>
    private void MarkAllDirty()
    {
        foreach (Cell cell in _cells.Values) MarkDirty(cell);
    }

    // ------------------------------------------------------------------ building

    /// <summary> True for single-material renderers using the source material. </summary>
    private bool CanMerge(MeshRenderer renderer, out Mesh mesh)
    {
        mesh = null;
        if (sourceMaterial == null || chunkMaterial == null) return false;

        Material[] materials = renderer.sharedMaterials;
        if (materials.Length != 1 || materials[0] != sourceMaterial) return false;

        MeshFilter filter = renderer.GetComponent<MeshFilter>();
        if (filter == null || filter.sharedMesh == null) return false;

        mesh = ItemDetail.Original(filter.sharedMesh); // cells build from the source mesh
        return mesh.isReadable && mesh.subMeshCount == 1;
    }

    private Cell GetCell(Vector3 position)
    {
        Vector3Int key = new Vector3Int(Mathf.FloorToInt(position.x / cellSize), 0, Mathf.FloorToInt(position.z / cellSize));
        if (_cells.TryGetValue(key, out Cell cell)) return cell;

        GameObject go = new GameObject($"ItemCell {key.x},{key.z}");
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3((key.x + 0.5f) * cellSize, 0f, (key.z + 0.5f) * cellSize);

        cell = new Cell { Key = key, Filter = go.AddComponent<MeshFilter>(), Renderer = go.AddComponent<MeshRenderer>() };
        cell.Renderer.sharedMaterial = chunkMaterial;
        cell.Renderer.shadowCastingMode = ShadowCastingMode.Off;
        cell.Renderer.receiveShadows = false;
        cell.Renderer.lightProbeUsage = LightProbeUsage.Off;
        cell.Renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        cell.Renderer.motionVectorGenerationMode = MotionVectorGenerationMode.Camera;
        cell.Renderer.enabled = false;

        _cells.Add(key, cell);
        return cell;
    }

    private SourceMesh GetSource(Mesh mesh)
    {
        if (_sources.TryGetValue(mesh, out SourceMesh source)) return source;

        ItemLodSet.Entry entry = null;
        if (ItemDetail.LodSet != null) ItemDetail.LodSet.TryGet(mesh, out entry);
        Mesh geometry = entry != null && entry.bakedMesh != null ? entry.bakedMesh : mesh;
        Vector3[] positions = geometry.vertices;
        Vector3[] normals = geometry.normals;
        Vector2[] uvs = geometry.uv;
        int count = positions.Length;

        source = new SourceMesh
        {
            Positions = positions,
            U = new ushort[count],
            V = new ushort[count],
            Lods = new int[LodCount][],
            NormalIndex = new int[count],
        };

        // LOD0 is the mesh itself; empty baked levels reuse the level above.
        for (int level = 0; level < LodCount; level++)
            source.Lods[level] = entry != null ? entry.GetIndices(level) : mesh.GetIndices(0);

        // Group identical normals so lighting is evaluated once per facet direction.
        var unique = new List<Vector3>();
        var lookup = new Dictionary<Vector3Int, int>();
        for (int i = 0; i < count; i++)
        {
            Vector3 n = i < normals.Length ? normals[i] : Vector3.up;
            Vector3Int key = Vector3Int.RoundToInt(n * 1000f);
            if (!lookup.TryGetValue(key, out int index))
            {
                index = unique.Count;
                unique.Add(n);
                lookup.Add(key, index);
            }
            source.NormalIndex[i] = index;

            Vector2 uv = i < uvs.Length ? uvs[i] : Vector2.zero;
            source.U[i] = Mathf.FloatToHalf(uv.x);
            source.V[i] = Mathf.FloatToHalf(uv.y);
        }

        source.UniqueNormals = unique.ToArray();
        source.WorldNormals = new Vector3[unique.Count];
        source.Lights = new Color[unique.Count];
        source.Encoded = new Color32[unique.Count];

        _sources.Add(mesh, source);
        return source;
    }

    /// <summary> Rebuilds a cell mesh from its members with baked probe lighting. </summary>
    private void Rebuild(Cell cell)
    {
        cell.Dirty = false;
        List<Member> members = cell.Members;
        members.RemoveAll(m => m.Renderer == null);

        int levels = ItemDetail.LodSet != null ? LodCount : 1;
        int vertexCount = 0, indexCount = 0;
        var lodCounts = new int[LodCount];
        foreach (Member m in members)
        {
            SourceMesh s = GetSource(m.Mesh);
            vertexCount += s.Positions.Length;
            for (int l = 0; l < levels; l++) lodCounts[l] += s.Lods[l].Length;
        }
        for (int l = 0; l < levels; l++) indexCount += lodCounts[l];

        Mesh old = cell.Filter.sharedMesh;
        if (vertexCount == 0)
        {
            cell.Filter.sharedMesh = null;
            cell.Renderer.enabled = false;
            if (old != null) Destroy(old);
            return;
        }

        bool wide = vertexCount > 65535;
        if (_vertexBuffer.Length < vertexCount) _vertexBuffer = new Vertex[vertexCount];
        if (wide && _intIndices.Length < indexCount) _intIndices = new int[indexCount];
        if (!wide && _shortIndices.Length < indexCount) _shortIndices = new ushort[indexCount];

        Vector3 origin = cell.Filter.transform.position;
        float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;

        int v = 0;
        _bases.Clear();
        foreach (Member m in members)
        {
            SourceMesh s = GetSource(m.Mesh);
            Matrix4x4 mx = m.Renderer.localToWorldMatrix;
            _bases.Add(v);

            // Light once per facet normal.
            for (int k = 0; k < s.UniqueNormals.Length; k++) s.WorldNormals[k] = mx.MultiplyVector(s.UniqueNormals[k]).normalized;
            SampleLight(m.Renderer).Evaluate(s.WorldNormals, s.Lights);
            for (int k = 0; k < s.Lights.Length; k++)
            {
                Color c = s.Lights[k];
                s.Encoded[k] = new Color32(Encode(c.r), Encode(c.g), Encode(c.b), 255);
            }

            float ox = mx.m03 - origin.x, oy = mx.m13 - origin.y, oz = mx.m23 - origin.z;
            Vector3[] positions = s.Positions;
            int count = positions.Length;
            for (int i = 0; i < count; i++)
            {
                Vector3 p = positions[i];
                float x = mx.m00 * p.x + mx.m01 * p.y + mx.m02 * p.z + ox;
                float y = mx.m10 * p.x + mx.m11 * p.y + mx.m12 * p.z + oy;
                float z = mx.m20 * p.x + mx.m21 * p.y + mx.m22 * p.z + oz;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
                if (z < minZ) minZ = z;
                if (z > maxZ) maxZ = z;

                ref Vertex vert = ref _vertexBuffer[v + i];
                vert.Position = new Vector3(x, y, z);
                vert.Light = s.Encoded[s.NormalIndex[i]];
                vert.U = s.U[i];
                vert.V = s.V[i];
            }

            v += count;
        }

        // Index buffer holds every LOD back to back: [LOD0 of all][LOD1 of all][LOD2 of all].
        int idx = 0;
        for (int l = 0; l < levels; l++)
        {
            for (int k = 0; k < members.Count; k++)
            {
                int[] src = GetSource(members[k].Mesh).Lods[l];
                int b = _bases[k];
                if (wide) for (int i = 0; i < src.Length; i++) _intIndices[idx + i] = src[i] + b;
                else for (int i = 0; i < src.Length; i++) _shortIndices[idx + i] = (ushort)(src[i] + b);
                idx += src.Length;
            }
        }

        const MeshUpdateFlags Flags = MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds
            | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontResetBoneBounds;

        Mesh mesh = new Mesh { name = $"ItemCell {cell.Key.x},{cell.Key.z}" };
        mesh.SetVertexBufferParams(vertexCount, _layout);
        mesh.SetVertexBufferData(_vertexBuffer, 0, 0, vertexCount, 0, Flags);
        mesh.SetIndexBufferParams(indexCount, wide ? IndexFormat.UInt32 : IndexFormat.UInt16);
        if (wide) mesh.SetIndexBufferData(_intIndices, 0, 0, indexCount, Flags);
        else mesh.SetIndexBufferData(_shortIndices, 0, 0, indexCount, Flags);

        Vector3 min = new Vector3(minX, minY, minZ), max = new Vector3(maxX, maxY, maxZ);
        Bounds bounds = new Bounds((min + max) * 0.5f, max - min);
        mesh.subMeshCount = 1;
        mesh.SetSubMesh(0, new SubMeshDescriptor(0, indexCount) { bounds = bounds, vertexCount = vertexCount }, Flags);
        if (levels > 1)
        {
            var ranges = new MeshLodRange[levels];
            uint start = 0;
            for (int l = 0; l < levels; l++)
            {
                ranges[l] = new MeshLodRange { indexStart = start, indexCount = (uint)lodCounts[l] };
                start += (uint)lodCounts[l];
            }
            mesh.lodCount = levels;
            mesh.SetLods(ranges, 0, Flags);
        }
        mesh.bounds = bounds;
        mesh.UploadMeshData(true); // frees the CPU copy

        cell.Filter.sharedMesh = mesh;
        cell.Renderer.forceMeshLod = (short)(levels > 1 ? ItemDetail.Lod : 0);
        cell.Renderer.enabled = true;
        if (old != null) Destroy(old);

        // Hide merged items only once the cell draws them.
        foreach (Member m in members) m.Renderer.enabled = false;
    }

    /// <summary> Probe lighting at the renderer, as Baked Lit samples it. </summary>
    private static SphericalHarmonicsL2 SampleLight(Renderer renderer)
    {
        LightProbes.GetInterpolatedProbe(renderer.bounds.center, renderer, out SphericalHarmonicsL2 sh);
        return sh;
    }

    private static byte Encode(float light) => (byte)Mathf.Clamp(Mathf.RoundToInt(light / LightScale * 255f), 0, 255);

    private static void SwapRemove(List<Member> list, int index)
    {
        int last = list.Count - 1;
        list[index] = list[last];
        list.RemoveAt(last);
    }
}
