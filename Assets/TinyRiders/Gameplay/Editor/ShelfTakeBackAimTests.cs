using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

public class ShelfTakeBackAimTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [Test]
    public void ShelfSurfaceAim_SelectsOlderItem_AndDoesNotSubstituteForEmptyOrFlyingSlots()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var root = new GameObject("Take-back aim regression");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            root.SetActive(false);
            var shelf = root.AddComponent<SortingShelf>();
            var handler = root.AddComponent<InteractionHandler>();
            var spots = new List<Transform>();
            for (int i = 0; i < 3; i++)
            {
                var spot = new GameObject("Slot " + i).transform;
                spot.SetParent(root.transform);
                spot.position = new Vector3(i * 2f, 1f, 0f);
                spots.Add(spot);
            }
            var older = spots[0].gameObject.AddComponent<PlacedOnShelfItemBase>();
            var newest = spots[2].gameObject.AddComponent<PlacedOnShelfItemBase>();
            SetSettled(older, true);
            SetSettled(newest, true);
            Set(shelf, "placingSpots", spots);
            Set(shelf, "_occupants", new[] { older, null, newest });
            ((List<PlacedOnShelfItemBase>)typeof(SortingShelf).GetField("_placeOrder", Private).GetValue(shelf))
                .AddRange(new[] { older, newest });
            Set(handler, "<HasCurrentHit>k__BackingField", true);

            Set(handler, "<CurrentHitPoint>k__BackingField", spots[0].position + Vector3.forward * .3f);
            Assert.AreSame(older, Resolve(shelf, handler), "Aim must beat placement order.");
            Set(handler, "<CurrentHitPoint>k__BackingField", spots[2].position);
            Assert.AreSame(newest, Resolve(shelf, handler));
            Set(handler, "<CurrentHitPoint>k__BackingField", spots[1].position);
            Assert.IsNull(Resolve(shelf, handler), "An empty slot must not take a neighbouring car.");
            Set(handler, "<CurrentHitPoint>k__BackingField", spots[0].position);
            SetSettled(older, false);
            Assert.IsNull(Resolve(shelf, handler), "An item still landing cannot be taken.");
            Assert.IsFalse(shelf.TryTakeBack(older));
            SetSettled(older, true);
            Set(handler, "<HasCurrentHit>k__BackingField", false);
            Assert.IsNull(Resolve(shelf, handler));
            Assert.IsNull(Resolve(shelf, null));

            var child = new GameObject("Car collider");
            child.transform.SetParent(older.transform);
            root.SetActive(true);
            Assert.AreSame(older, new InteractTarget(child).Interactable,
                "Direct car hits must continue to take that exact car.");
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    static PlacedOnShelfItemBase Resolve(SortingShelf shelf, InteractionHandler handler) =>
        (PlacedOnShelfItemBase)typeof(SortingShelf).GetMethod("GetAimedItem", Private)
            .Invoke(shelf, new object[] { handler });

    static void SetSettled(PlacedOnShelfItemBase item, bool value) =>
        Set(item, "<IsSettled>k__BackingField", value);

    static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, Private).SetValue(target, value);
}
