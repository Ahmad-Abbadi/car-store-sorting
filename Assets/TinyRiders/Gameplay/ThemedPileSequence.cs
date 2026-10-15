using System;
using System.Collections.Generic;
using System.Linq;

namespace TinyRiders
{
    /// <summary>The same deterministic, theme-balanced sequence is used by the bake and runtime.</summary>
    public static class ThemedPileSequence
    {
        public static uint Rank(string id, int seed)
        {
            unchecked
            {
                uint h = (uint)seed;
                foreach (char c in id) h = (h ^ c) * 16777619;
                h ^= h >> 16; h *= 0x7feb352d; h ^= h >> 15; h *= 0x846ca68b;
                return h ^ (h >> 16);
            }
        }

        public static IEnumerable<ThemedCarCatalog.Entry> Designs(IEnumerable<ThemedCarCatalog.Entry> entries, int seed)
        {
            var groups = entries.GroupBy(e => e.themeId)
                .OrderBy(g => Rank(g.Key, seed)).ThenBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => g.OrderBy(e => Rank(e.collectibleId, seed))
                    .ThenBy(e => e.collectibleId, StringComparer.Ordinal).ToArray()).ToArray();
            int rounds = groups.Length == 0 ? 0 : groups.Max(g => g.Length);
            for (int round = 0; round < rounds; round++)
                foreach (var group in groups)
                    if (round < group.Length) yield return group[round];
        }

        public static IEnumerable<string> InstanceIds(IEnumerable<ThemedCarCatalog.Entry> entries, int copies, int seed)
        {
            var designs = Designs(entries, seed).ToArray();
            for (int copy = 1; copy <= copies; copy++)
                foreach (var entry in designs)
                    yield return "TRWORLD-" + seed + ":" + entry.collectibleId + ":" + copy.ToString("D4");
        }
    }
}
