using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TinyRiders;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public sealed class CarVisualBatchTests
{
    struct Vertex
    {
        public Vector3 position, normal;
        public Vector4 tangent;
        public Color color;
        public Vector4[] uv;
        public double[] sortKey;
    }
    static int Compare(Vertex a, Vertex b)
    {
        for(int i=0;i<a.sortKey.Length;i++){int result=a.sortKey[i].CompareTo(b.sortKey[i]);if(result!=0)return result;}
        return 0;
    }
    static void Append(double[] key,ref int offset,Vector4 vector)
    { for(int i=0;i<4;i++)key[offset++]=Math.Round(vector[i],3); }
    static double[] SortKey(Vertex v)
    {
        var key=new double[16+v.uv.Length*4];int offset=0;
        Append(key,ref offset,v.position);Append(key,ref offset,v.normal);Append(key,ref offset,v.tangent);Append(key,ref offset,v.color);
        foreach(var uv in v.uv)Append(key,ref offset,uv);
        return key;
    }

    static Dictionary<Material, List<Vertex>> Read(GameObject root, int level)
    {
        var set = AssetDatabase.LoadAssetAtPath<ItemLodSet>(ItemDetailBuilder.SetPath);
        var result = new Dictionary<Material, List<Vertex>>();
        foreach (var r in root.GetComponentsInChildren<MeshRenderer>().Where(r => r.enabled))
        {
            var filter = r.GetComponent<MeshFilter>(); if (filter == null) continue;
            var source = filter.sharedMesh;
            bool baked = set.TryGet(source, out var entry) && entry.bakedMesh != null;
            var mesh = baked ? entry.bakedMesh : source;
            var matrix = root.transform.worldToLocalMatrix * r.transform.localToWorldMatrix;
            var normalMatrix = matrix.inverse.transpose;
            var ps = mesh.vertices; var ns = mesh.normals; var ts = mesh.tangents; var cs = mesh.colors;
            var uvs = Enumerable.Range(0, 8).Select(c => {var u=new List<Vector4>();mesh.GetUVs(c,u);return u;}).ToArray();
            var cache=new Vertex[ps.Length];
            for (int slot = 0; slot < r.sharedMaterials.Length && mesh.subMeshCount > 0; slot++)
            {
                int sub = Math.Min(slot, mesh.subMeshCount - 1);
                var mat = r.sharedMaterials[slot];
                if (!result.TryGetValue(mat, out var list)) result.Add(mat, list = new List<Vertex>());
                foreach (int i in baked ? entry.GetIndices(level, sub) : mesh.GetIndices(sub, Math.Min(level, mesh.lodCount - 1), true))
                {
                    if(cache[i].sortKey==null)
                    {
                        Vector3 t = ts.Length > i ? matrix.MultiplyVector((Vector3)ts[i]).normalized : Vector3.zero;
                        var channels=new Vector4[7];int channelIndex=0;
                        for(int channel=0;channel<8;channel++)if(channel!=3)channels[channelIndex++]=uvs[channel].Count>i?uvs[channel][i]:Vector4.zero;
                        var vertex=new Vertex { position = matrix.MultiplyPoint3x4(ps[i]), normal = ns.Length > i ? normalMatrix.MultiplyVector(ns[i]).normalized : Vector3.zero,
                            tangent = new Vector4(t.x,t.y,t.z,ts.Length > i ? ts[i].w * (matrix.determinant < 0 ? -1 : 1) : 0),
                            color = cs.Length > i ? cs[i] : Color.white,uv=channels };
                        vertex.sortKey=SortKey(vertex);cache[i]=vertex;
                    }
                    list.Add(cache[i]);
                }
            }
        }
        return result;
    }

    internal static void AssertEquivalent(GameObject source, GameObject batched)
    {
        for (int level = 0; level < 3; level++)
        {
            var expected = Read(source, level); var actual = Read(batched, level);
            CollectionAssert.AreEquivalent(expected.Keys, actual.Keys, source.name + " material assignments LOD" + level);
            foreach (var material in expected.Keys)
            {
                expected[material].Sort(Compare); actual[material].Sort(Compare);
                var a = expected[material].ToArray(); var b = actual[material].ToArray();
                Assert.AreEqual(a.Length,b.Length,source.name+" / "+material.name+" triangle count LOD"+level);
                float positionError=0,normalError=0,tangentError=0,colorError=0,uvError=0;
                for(int i=0;i<a.Length;i++)
                {
                    positionError=Math.Max(positionError,(a[i].position-b[i].position).magnitude);
                    normalError=Math.Max(normalError,(a[i].normal-b[i].normal).magnitude);
                    tangentError=Math.Max(tangentError,(a[i].tangent-b[i].tangent).magnitude);
                    colorError=Math.Max(colorError,((Vector4)a[i].color-(Vector4)b[i].color).magnitude);
                    for(int c=0;c<a[i].uv.Length;c++) uvError=Math.Max(uvError,(a[i].uv[c]-b[i].uv[c]).magnitude);
                }
                string context=source.name+" / "+material.name+" LOD"+level;
                Assert.Less(positionError,.0005f,context+" positions"); Assert.Less(normalError,.0005f,context+" normals");
                Assert.Less(tangentError,.0005f,context+" tangents"); Assert.Less(colorError,.0005f,context+" colors");
                Assert.Less(uvError,.0005f,context+" UVs");
            }
        }
    }

    [TestCase(0),TestCase(1),TestCase(2),TestCase(3),TestCase(4),TestCase(5),TestCase(6),TestCase(7),TestCase(8),TestCase(9),TestCase(10),TestCase(11),TestCase(12),TestCase(13),TestCase(14),Timeout(180000)]
    public void GeneratedCarsPreserveEveryDetailLevelAndMaterial(int modelIndex)
    {
        var catalog = AssetDatabase.LoadAssetAtPath<ThemedCarCatalog>(ItemDetailBuilder.CatalogPath);
        var models=catalog.entries.Where(e=>e.enabled).GroupBy(e=>e.baseCarId).OrderBy(g=>g.Key,StringComparer.Ordinal).ToArray();
        Assert.AreEqual(15,models.Length);var model=models[modelIndex];Assert.AreEqual(40,model.Count());
        int before=0,after=0;
        foreach(var e in model)
        {
            string path=CarVisualBatchBuilder.Root+"/"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(e.prefab))+"/Visual.prefab";
            var generated=AssetDatabase.LoadAssetAtPath<GameObject>(path); Assert.NotNull(generated,path);
            AssertEquivalent(e.prefab,generated);
            before+=e.prefab.GetComponentsInChildren<MeshRenderer>().Sum(r=>r.sharedMaterials.Length);
            after+=generated.GetComponentsInChildren<MeshRenderer>().Sum(r=>r.sharedMaterials.Length);
        }
        Assert.Less(after,before,"Batches must reduce material draws");
        Debug.Log("CAR_BATCH_GEOMETRY "+model.Key+": all40 themes and all3 LODs verified");
    }

    [Test]
    public void GlassAndDecalsKeepSourceTransformsBoundsAndSeparateSorting()
    {
        var catalog=AssetDatabase.LoadAssetAtPath<ThemedCarCatalog>(ItemDetailBuilder.CatalogPath);
        foreach(var e in catalog.entries.Where(e=>e.enabled))
        {
            var generated=AssetDatabase.LoadAssetAtPath<GameObject>(CarVisualBatchBuilder.Root+"/"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(e.prefab))+"/Visual.prefab");
            foreach(var source in e.prefab.GetComponentsInChildren<MeshRenderer>().Where(r=>r.sharedMaterials.Any(m=>m.renderQueue>=2500)))
            {
                string path=AnimationUtility.CalculateTransformPath(source.transform,e.prefab.transform);
                var target=generated.transform.Find(path).GetComponent<MeshRenderer>(); Assert.NotNull(target,path);
                bool sliced=AssetDatabase.GetAssetPath(target.GetComponent<MeshFilter>().sharedMesh).StartsWith(CarVisualBatchBuilder.Root+"/",StringComparison.Ordinal);
                // Incompatible renderers retain ALL authored slots; compatible mixed renderers keep their transparent slots.
                var expected=sliced?source.sharedMaterials.Where(m=>m.renderQueue>=2500).ToArray():source.sharedMaterials;
                CollectionAssert.AreEqual(expected,target.sharedMaterials,path);
                Assert.AreEqual(source.transform.localPosition,target.transform.localPosition,path);
                Assert.AreEqual(source.transform.localRotation,target.transform.localRotation,path);
                Assert.AreEqual(source.transform.localScale,target.transform.localScale,path);
                Assert.AreEqual(source.GetComponent<MeshFilter>().sharedMesh.bounds,target.GetComponent<MeshFilter>().sharedMesh.bounds,path);
            }
        }
    }

    [Test]
    public void VertexCompactionPreservesExactBuffersBoundsAndVerticesUsedOnlyByLowLod()
    {
        Mesh source = new Mesh(), compact = null;
        try
        {
            source.vertices = new[] { Vector3.zero, Vector3.one * 100, Vector3.right, Vector3.up, Vector3.forward, Vector3.one };
            source.normals = Enumerable.Repeat(Vector3.up, 6).ToArray();
            source.tangents = Enumerable.Range(0, 6).Select(i => new Vector4(i, .25f, -.75f, -1)).ToArray();
            source.colors32 = Enumerable.Range(0, 6).Select(i => new Color32((byte)i, 37, 121, 255)).ToArray();
            source.SetUVs(4, Enumerable.Range(0, 6).Select(i => new Vector4(i, -i, .125f, 1)).ToArray());
            source.SetIndices(new[] { 0, 2, 3, 0, 2, 4, 0, 2, 5 }, MeshTopology.Triangles, 0);
            source.lodCount = 3;
            source.SetLods(Enumerable.Range(0, 3).Select(i => new MeshLodRange { indexStart = (uint)(i * 3), indexCount = 3 }).ToArray(), 0);
            Bounds bounds = source.bounds;
            byte[][] bytes;
            using (var data = Mesh.AcquireReadOnlyMeshData(source))
                bytes = Enumerable.Range(0, source.vertexBufferCount).Select(s => data[0].GetVertexData<byte>(s).ToArray()).ToArray();
            var strides = Enumerable.Range(0, source.vertexBufferCount).Select(source.GetVertexBufferStride).ToArray();
            compact = (Mesh)typeof(CarVisualBatchBuilder).GetMethod("CompactUnusedVertices", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { source });
            Assert.AreEqual(5, compact.vertexCount); Assert.AreEqual(bounds, compact.bounds); Assert.AreEqual(3, compact.lodCount);
            using (var data = Mesh.AcquireReadOnlyMeshData(compact))
            for (int stream = 0; stream < bytes.Length; stream++)
            {
                var expected = new[] { 0, 2, 3, 4, 5 }.SelectMany(i => bytes[stream].Skip(i * strides[stream]).Take(strides[stream])).ToArray();
                CollectionAssert.AreEqual(expected, data[0].GetVertexData<byte>(stream).ToArray());
            }
            for (int level = 0; level < 3; level++)
                CollectionAssert.AreEqual(new[] { 0, 1, level + 2 }, compact.GetIndices(0, level, true));
        }
        finally { if (source != null) Object.DestroyImmediate(source); if (compact != null) Object.DestroyImmediate(compact); }
    }

    [Test]
    public void CurrentGeneratedMeshesHaveNoVerticesUnusedByAllDetailLevels()
    {
        var meshes = ItemDetailBuilder.CatalogPrefabs().SelectMany(p => p.GetComponentsInChildren<MeshFilter>(true))
            .Select(f => f.sharedMesh).Where(m => m != null && AssetDatabase.GetAssetPath(m).StartsWith(CarVisualBatchBuilder.Root + "/", StringComparison.Ordinal)).Distinct();
        foreach (var mesh in meshes)
        {
            var used = new HashSet<int>();
            for (int sub = 0; sub < mesh.subMeshCount; sub++)
            for (int level = 0; level < mesh.lodCount; level++)
                used.UnionWith(mesh.GetIndices(sub, level, true));
            Assert.AreEqual(mesh.vertexCount, used.Count, mesh.name);
        }
    }

    [Test]
    public void FlattenedOutlineRetainsAllNativeLodTriangles()
    {
        var mesh=new Mesh(); mesh.vertices=new[]{Vector3.zero,Vector3.right,Vector3.up,Vector3.one};
        mesh.subMeshCount=2; mesh.SetIndices(new[]{0,1,2,0,1,2,0,1,2},MeshTopology.Triangles,0);
        mesh.SetIndices(new[]{0,2,3,0,2,3,0,2,3},MeshTopology.Triangles,1); mesh.lodCount=3;
        for(int s=0;s<2;s++)mesh.SetLods(new[]{new MeshLodRange{indexStart=0,indexCount=3},new MeshLodRange{indexStart=3,indexCount=3},new MeshLodRange{indexStart=6,indexCount=3}},s);
        try
        {
            var flattened=(Mesh)typeof(SharedOutline).GetMethod("GetOutlineMesh",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{mesh});
            Assert.AreEqual(3,flattened.lodCount); Assert.AreEqual(1,flattened.subMeshCount);
            for(int level=0;level<3;level++) CollectionAssert.AreEqual(mesh.GetIndices(0,level,true).Concat(mesh.GetIndices(1,level,true)),flattened.GetIndices(0,level,true));
        }
        finally { Object.DestroyImmediate(mesh); }
    }
}
