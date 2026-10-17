using System.Linq;
using NUnit.Framework;
using TinyRiders;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class ShelfCarFitTests
{
    [Test, Timeout(180000)]
    public void All600Designs_FillSlotWithoutClipping_AndRepeatedFittingIsStable()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var root = new GameObject("Shelf fit test");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            var binding = root.AddComponent<ThemeShelfBinding>();
            binding.clearance = ExactThemeShelfBuilder.Clearance * .48f;
            var slot = new GameObject("Slot").transform;
            slot.SetParent(root.transform, false);
            slot.localScale = Vector3.one * .48f;
            root.transform.rotation = Quaternion.Euler(0, 37, 0);
            var catalog = AssetDatabase.LoadAssetAtPath<ThemedCarCatalog>(CompactCollectionBuilder.CatalogPath);
            var entries = catalog.entries.Where(e => e.sortingItem != null).ToArray();
            Assert.AreEqual(600, entries.Length);
            foreach (var entry in entries)
            {
                var item = Object.Instantiate(entry.sortingItem.PlacedOnShelfPrefab);
                try
                {
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(item.gameObject, scene);
                    item.transform.SetParent(slot, true);
                    var initial = item.transform.localScale;
                    // Simulate a car arriving with an arbitrary flight rotation.
                    item.transform.localRotation = Quaternion.Euler(45, 120, 10);
                    Assert.True(binding.TryFitDisplay(item.transform, out var position, out var scale), entry.collectibleId);
                    Assert.Greater(scale.x / initial.x, 1.02f, entry.collectibleId + " should be larger than its authored shelf form");
                    item.transform.SetLocalPositionAndRotation(position, Quaternion.identity);
                    item.transform.localScale = scale;
                    Vector3 minimum = Vector3.one * float.PositiveInfinity, maximum = Vector3.one * float.NegativeInfinity;
                    foreach (var renderer in item.GetComponentsInChildren<Renderer>(true))
                    {
                        if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                        var b = renderer.localBounds;
                        for (int i = 0; i < 8; i++)
                        {
                            var sign = new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1);
                            var point = Vector3.Scale(slot.lossyScale, slot.InverseTransformPoint(renderer.transform.TransformPoint(b.center + Vector3.Scale(b.extents, sign))));
                            minimum = Vector3.Min(minimum, point); maximum = Vector3.Max(maximum, point);
                        }
                    }
                    var c = binding.clearance;
                    Assert.GreaterOrEqual(minimum.x, -c.x * .5f); Assert.LessOrEqual(maximum.x, c.x * .5f);
                    Assert.GreaterOrEqual(minimum.z, -c.z * .5f); Assert.LessOrEqual(maximum.z, c.z * .5f);
                    Assert.That(minimum.y, Is.EqualTo(.005f).Within(.00001f)); Assert.LessOrEqual(maximum.y, c.y);
                    var size = maximum - minimum;
                    Assert.GreaterOrEqual(Mathf.Max(size.x / c.x, size.y / c.y, size.z / c.z), .93f, "Should use available slot space");
                    Assert.True(binding.TryFitDisplay(item.transform, out var againPosition, out var againScale));
                    Assert.Less(Vector3.Distance(scale, againScale), .0001f);
                    Assert.Less(Vector3.Distance(position, againPosition), .0001f);
                }
                finally { Object.DestroyImmediate(item.gameObject); }
            }
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
}
