using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Draws distant resting cars as instanced toy-car silhouettes while keeping their pickup colliders.</summary>
public sealed class CarVisualCulling : MonoBehaviour
{
    [SerializeField, Min(5)] private float fullDetailRadius = 12f;

    private const int ColorCount = 11; // ten TinyRiders colorways plus wrong placement red
    private readonly List<Matrix4x4>[] batches = new List<Matrix4x4>[ColorCount];
    private readonly Matrix4x4[] drawBuffer = new Matrix4x4[1023];
    private Material[] materials;
    private Mesh proxy;
    private Camera cameraToUse;

    private static readonly Color[] Colors = {
        new Color(0.86f, 0.12f, 0.16f), new Color(1f, 0.43f, 0.08f),
        new Color(0.96f, 0.66f, 0.10f), new Color(0.65f, 0.88f, 0.16f),
        new Color(0.05f, 0.60f, 0.30f), new Color(0.04f, 0.62f, 0.58f),
        new Color(0.06f, 0.74f, 0.92f), new Color(0.09f, 0.25f, 0.78f),
        new Color(0.49f, 0.23f, 0.76f), new Color(0.91f, 0.14f, 0.58f),
        new Color(0.95f, 0.15f, 0.12f),
    };

    private void Awake()
    {
        proxy = BuildProxy();
        materials = new Material[ColorCount];
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        for (int i = 0; i < ColorCount; i++)
        {
            batches[i] = new List<Matrix4x4>(160);
            materials[i] = new Material(shader) { color = Colors[i], enableInstancing = true };
        }
    }

    private void LateUpdate()
    {
        if (cameraToUse == null) cameraToUse = Camera.main;
        if (cameraToUse == null) return;
        foreach (List<Matrix4x4> batch in batches) batch.Clear();
        Vector3 eye = cameraToUse.transform.position;
        float threshold = fullDetailRadius * fullDetailRadius;
        IReadOnlyList<ItemVisualGroup> groups = ItemVisualGroup.All;
        for (int i = 0; i < groups.Count; i++)
        {
            ItemVisualGroup group = groups[i];
            if (group == null || !group.gameObject.activeInHierarchy) continue;
            if (group.GetComponent<HandItemBase>() != null) continue;
            Vector3 position = group.transform.position;
            bool distant = (position - eye).sqrMagnitude > threshold;
            group.SetFar(distant);
            if (!distant) continue;
            int color = ColorIndex(group);
            batches[color].Add(Matrix4x4.TRS(position, group.transform.rotation, Vector3.one));
        }
        for (int color = 0; color < ColorCount; color++)
        {
            List<Matrix4x4> matrices = batches[color];
            for (int offset = 0; offset < matrices.Count; offset += 1023)
            {
                int count = Mathf.Min(1023, matrices.Count - offset);
                matrices.CopyTo(offset, drawBuffer, 0, count);
                Graphics.DrawMeshInstanced(proxy, 0, materials[color], drawBuffer, count, null,
                    ShadowCastingMode.Off, false, 0, cameraToUse);
            }
        }
    }

    private static int ColorIndex(ItemVisualGroup group)
    {
        PlacedOnShelfItemBase placed = group.GetComponent<PlacedOnShelfItemBase>();
        if (placed != null && placed.IsWrong) return 10;
        WorldItemBase world = group.GetComponent<WorldItemBase>();
        string id = world != null && world.Data != null ? world.Data.SaveId :
            placed != null && placed.Data != null ? placed.Data.SaveId : group.name;
        int livery = id.LastIndexOf("-L0", System.StringComparison.Ordinal);
        int number = livery >= 0 && livery + 3 < id.Length ? id[livery + 3] - '1' : 0;
        number = Mathf.Clamp(number, 0, 4);
        return id.Contains("-CUS-") ? 5 + number : number;
    }

    private static Mesh BuildProxy()
    {
        var vertices = new List<Vector3>(48);
        var triangles = new List<int>(72);
        AddBox(vertices, triangles, new Vector3(0, 0.11f, 0), new Vector3(0.34f, 0.13f, 0.56f));
        AddBox(vertices, triangles, new Vector3(0, 0.22f, -0.035f), new Vector3(0.27f, 0.11f, 0.28f));
        Mesh mesh = new Mesh { name = "Distant Toy Car" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void AddBox(List<Vector3> vertices, List<int> triangles, Vector3 center, Vector3 size)
    {
        int start = vertices.Count;
        Vector3 half = size * 0.5f;
        vertices.Add(center + new Vector3(-half.x, -half.y, -half.z));
        vertices.Add(center + new Vector3(half.x, -half.y, -half.z));
        vertices.Add(center + new Vector3(half.x, half.y, -half.z));
        vertices.Add(center + new Vector3(-half.x, half.y, -half.z));
        vertices.Add(center + new Vector3(-half.x, -half.y, half.z));
        vertices.Add(center + new Vector3(half.x, -half.y, half.z));
        vertices.Add(center + new Vector3(half.x, half.y, half.z));
        vertices.Add(center + new Vector3(-half.x, half.y, half.z));
        int[] faces = { 0, 2, 1, 0, 3, 2, 4, 5, 6, 4, 6, 7, 0, 4, 7, 0, 7, 3,
            1, 2, 6, 1, 6, 5, 3, 7, 6, 3, 6, 2, 0, 1, 5, 0, 5, 4 };
        foreach (int index in faces) triangles.Add(start + index);
    }

    private void OnDestroy()
    {
        if (proxy != null) Destroy(proxy);
        if (materials != null) foreach (Material material in materials) if (material != null) Destroy(material);
    }
}
