using System;
using System.Collections.Generic;
using System.Linq;

namespace TinyRiders
{
    /// <summary>The same collection eligibility rules drive spawning and shelf capacity audits.</summary>
    public static class ThemedShelfCapacity
    {
        public static IEnumerable<ThemedCarCatalog.Entry> EligibleDefinitions(ThemedCarCatalog catalog)
        {
            if (catalog == null) return Enumerable.Empty<ThemedCarCatalog.Entry>();
            var themes = new HashSet<string>((catalog.themes ?? Array.Empty<ThemedCarCatalog.Theme>())
                .Where(t => t != null && t.enabled).Select(t => t.id), StringComparer.Ordinal);
            return (catalog.entries ?? Array.Empty<ThemedCarCatalog.Entry>())
                .Where(e => e != null && e.enabled && e.prefab != null && e.sortingItem != null && themes.Contains(e.themeId));
        }

        public static void Validate(ThemedCarCatalog catalog, int copiesPerDesign, IEnumerable<SortingShelf> shelves)
        {
            if (catalog == null || copiesPerDesign < 1) throw new InvalidOperationException("Missing catalog or invalid copy count.");
            var entries = EligibleDefinitions(catalog).ToArray();
            if (entries.Any(e => string.IsNullOrEmpty(e.collectibleId)) || entries.Select(e => e.collectibleId).Distinct().Count() != entries.Length)
                throw new InvalidOperationException("Collection IDs must be present and unique.");
            var expected = entries.GroupBy(e => e.themeId).ToDictionary(g => g.Key, g => g.Count() * copiesPerDesign);
            var actual = new Dictionary<string, int>(StringComparer.Ordinal);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var markers = new HashSet<UnityEngine.Transform>();
            foreach (var shelf in shelves)
            {
                var binding = shelf.GetComponent<ThemeShelfBinding>();
                if (binding == null) continue;
                if (string.IsNullOrEmpty(shelf.SaveId) || !ids.Add(shelf.SaveId))
                    throw new InvalidOperationException("Missing or duplicate shelf ID: " + shelf.SaveId);
                if (string.IsNullOrEmpty(binding.themeId) || shelf.Catagory == null || shelf.Catagory.CatagoryID != binding.themeId)
                    throw new InvalidOperationException("Shelf theme/category mismatch: " + shelf.SaveId);
                for (int i = 0; i < shelf.SpotCount; i++)
                    if (shelf.GetSpot(i) == null || !markers.Add(shelf.GetSpot(i)))
                        throw new InvalidOperationException("Missing or reused placement marker: " + shelf.SaveId);
                if (Enumerable.Range(0, shelf.SpotCount).Select(i => shelf.GetSpot(i).name).Distinct().Count() != shelf.SpotCount)
                    throw new InvalidOperationException("Duplicate slot names: " + shelf.SaveId);
                actual.TryGetValue(binding.themeId, out int count);
                actual[binding.themeId] = count + shelf.SpotCount;
            }
            var errors = new List<string>();
            foreach (string theme in expected.Keys.Union(actual.Keys).OrderBy(t => t, StringComparer.Ordinal))
            {
                expected.TryGetValue(theme, out int cars); actual.TryGetValue(theme, out int slots);
                if (cars != slots) errors.Add(theme + ": " + cars + " cars / " + slots + " slots");
            }
            if (expected.Values.Sum() != actual.Values.Sum()) errors.Add("Total capacity does not match the collection.");
            if (errors.Count != 0) throw new InvalidOperationException("Shelf capacity mismatch: " + string.Join("; ", errors));
        }
    }
}
