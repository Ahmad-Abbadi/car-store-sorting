using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TinyRiders;
using UnityEngine;

public static class VerifyShelfCarFit
{
    static void Verify(PlacedOnShelfItemBase item, ThemeShelfBinding binding)
    {
        var t = item.transform;
        if (!binding.TryFitDisplay(t, out var position, out var scale)) throw new Exception("Fit failed");
        if (Vector3.Distance(t.localScale, scale) > .001f || Vector3.Distance(t.localPosition, position) > .001f)
            throw new Exception("Placement did not use fitted pose: " + item.Data.name);
    }

    public static async Task<object> Main()
    {
        var session = ThemedCollectionSession.Instance;
        session.suspendPersistenceForTests = true; // Leave enabled until Play mode exits; never save these test placements.
        Application.runInBackground = true;
        var shelf = UnityEngine.Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None)
            .First(s => s.GetComponent<ThemeShelfBinding>()?.themeId == "TH_ALIEN" &&
                Enumerable.Range(0, 3).Any(row => Enumerable.Range(row * 5, 5).All(i => s.GetOccupant(i) == null)));
        int firstSpot = Enumerable.Range(0, 3).First(row => Enumerable.Range(row * 5, 5).All(i => shelf.GetOccupant(i) == null)) * 5;
        var binding = shelf.GetComponent<ThemeShelfBinding>();
        var entries = session.ActiveDefinitions.Where(e => e.themeId == binding.themeId).OrderBy(e => e.baseCarId).ToArray();
        var entry = entries.First(e => e.baseCarId == "C007");
        var world = WorldItemBase.All.First(w => w.Data == entry.sortingItem && w.gameObject.activeInHierarchy);
        if (!Player.Instance.ItemCollector.TryCollectItem(world)) throw new Exception("Pickup failed");
        await Task.Delay(500);
        if (!Player.Instance.SortingBehaviour.TryGetPlacedItemFromHand(out var placed)) throw new Exception("Could not create placed form");
        shelf.AddItem(placed, firstSpot);
        await Task.Delay(1000);
        if (shelf.GetOccupant(firstSpot) != placed || !placed.IsSettled) throw new Exception("Animated placement failed");
        Verify(placed, binding);
        int restored = 0;
        for (int i = 1; i < 5; i++)
        {
            if (!shelf.RestoreItem(firstSpot + i, entries[i].sortingItem)) throw new Exception("Saved placement failed");
            Verify(shelf.GetOccupant(firstSpot + i), binding); restored++;
        }
        // Capture the actual shelf without moving the player's camera.
        var view = new GameObject("Shelf fit evidence camera").AddComponent<Camera>();
        var target = new RenderTexture(1600, 900, 24);
        var oldActive = RenderTexture.active;
        Texture2D image = null;
        try
        {
            var marker = shelf.GetSpot(firstSpot + 2);
            var focus = marker.position + Vector3.up * .12f;
            view.transform.position = focus + marker.forward * 1.4f + Vector3.up * .35f;
            view.transform.LookAt(focus);
            view.nearClipPlane = .03f; view.fieldOfView = 50f; view.aspect = 1600f / 900;
            view.targetTexture = target; view.Render(); RenderTexture.active = target;
            image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
            Directory.CreateDirectory("IntegrationEvidence/ShelfCarFit");
            File.WriteAllBytes("IntegrationEvidence/ShelfCarFit/LargerShelfCars.png", image.EncodeToPNG());
            var result = new { animatedPlacement = true, restoredPlacements = restored, car = entry.collectibleId,
                authoredScale = entry.sortingItem.PlacedOnShelfPrefab.transform.localScale.x,
                placedWorldScale = placed.transform.lossyScale.x };
            File.WriteAllText("IntegrationEvidence/ShelfCarFit/verification.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
            return result;
        }
        finally
        {
            RenderTexture.active = oldActive; view.targetTexture = null;
            target.Release(); UnityEngine.Object.DestroyImmediate(target);
            if (image != null) UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(view.gameObject);
        }
    }
}
