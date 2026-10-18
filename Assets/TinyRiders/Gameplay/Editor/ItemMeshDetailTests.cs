using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class ItemMeshDetailTests
{
    [Test]
    public void EveryCatalogMeshHasThreeLevelsAndUnchangedHighGeometry()
    {
        var set = AssetDatabase.LoadAssetAtPath<ItemLodSet>(ItemDetailBuilder.SetPath);
        var meshes = ItemDetailBuilder.CatalogPrefabs().SelectMany(p => p.GetComponentsInChildren<MeshFilter>(true))
            .Select(f => f.sharedMesh).Where(m => m != null).Distinct().ToArray();
        Assert.Greater(meshes.Length, 0);
        long[] totals = new long[3];
        foreach (var source in meshes)
        {
            Assert.IsTrue(set.TryGet(source, out var entry), source.name);
            Assert.IsNotNull(entry.bakedMesh, source.name);
            Assert.AreEqual(3, entry.bakedMesh.lodCount, source.name);
            CollectionAssert.AreEqual(source.vertices, entry.bakedMesh.vertices, source.name);
            CollectionAssert.AreEqual(source.normals, entry.bakedMesh.normals, source.name);
            CollectionAssert.AreEqual(source.uv, entry.bakedMesh.uv, source.name);
            for (int sub = 0; sub < source.subMeshCount; sub++)
            {
                CollectionAssert.AreEqual(source.GetIndices(sub), entry.GetIndices(0, sub), source.name);
                var ranges = new System.Collections.Generic.List<MeshLodRange>();
                entry.bakedMesh.GetLods(ranges, sub);
                Assert.AreEqual(3, ranges.Count);
                Assert.LessOrEqual(ranges[1].indexCount, ranges[0].indexCount, source.name);
                Assert.LessOrEqual(ranges[2].indexCount, ranges[1].indexCount, source.name);
                if (ranges[0].indexCount > 0) Assert.Greater(ranges[2].indexCount, 0, source.name);
                for (int level = 0; level < 3; level++) totals[level] += ranges[level].indexCount;
            }
        }
        Assert.Less(totals[1], totals[0] * .9, "Medium must actually reduce geometry");
        Assert.Less(totals[2], totals[1] * .9, "Low must actually reduce geometry");
    }

    [Test]
    public void NestedCarsUseBakedMeshesOnSpawnAndRestoreHighAfterLow()
    {
        bool had = PlayerPrefs.HasKey("Item_Detail");
        int saved = PlayerPrefs.GetInt("Item_Detail"), quality = QualitySettings.GetQualityLevel();
        GameObject instance = null;
        try
        {
            ItemDetail.Set(ItemDetailLevel.Low);
            instance = Object.Instantiate(ItemDetailBuilder.CatalogPrefabs().First(p => p.GetComponent<ItemVisualGroup>() != null));
            var group = instance.GetComponent<ItemVisualGroup>();
            group.RefreshDetail((short)ItemDetail.Lod);
            var filters = group.Renderers.Where(r => r.GetComponent<MeshFilter>() != null).ToArray();
            Assert.Greater(filters.Length, 1);
            foreach (var r in filters)
            {
                Assert.AreEqual(3, r.GetComponent<MeshFilter>().sharedMesh.lodCount, r.name);
                Assert.AreEqual(2, r.forceMeshLod, r.name);
            }
            ItemDetail.Set(ItemDetailLevel.High);
            group.RefreshDetail((short)ItemDetail.Lod);
            foreach (var r in filters) Assert.AreEqual(0, r.forceMeshLod, r.name);
        }
        finally
        {
            if (instance != null) Object.DestroyImmediate(instance);
            if (had) PlayerPrefs.SetInt("Item_Detail", saved); else PlayerPrefs.DeleteKey("Item_Detail");
            PlayerPrefs.Save(); QualitySettings.SetQualityLevel(quality, true);
        }
    }
}
