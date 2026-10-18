using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TinyRiders.Quests
{
    [Serializable]
    public sealed class CollectorQuestRecord
    {
        public string id, definitionId, themeId;
        public CollectorQuestGoal goal;
        public bool challenge, complete, claimed;
        public int target, progress, baseline, coins, xp;
    }

    [Serializable]
    public sealed class CollectorCareer
    {
        public int version = 1, xp, sortedHighWater;
        public string runId = Guid.NewGuid().ToString("N"), pinnedId, pendingClaim;
        public bool pinChosen;
        public List<CollectorQuestRecord> quests = new List<CollectorQuestRecord>();
    }

    public sealed class CollectorSnapshot
    {
        public int sorted, total, discovered, designs, fullShelves, upgrades;
        public readonly Dictionary<string, (int sorted, int total)> themes = new Dictionary<string, (int, int)>();
        public readonly HashSet<string> completedThemes = new HashSet<string>();
    }

    /// <summary>Immutable UI projection. Runtime save records never escape the manager.</summary>
    public sealed class CollectorQuestView
    {
        public string Id { get; }
        public string DefinitionId { get; }
        public string ThemeId { get; }
        public CollectorQuestGoal Goal { get; }
        public bool Challenge { get; }
        public bool Complete { get; }
        public bool Claimed { get; }
        public int Progress { get; }
        public int Target { get; }
        public int Coins { get; }
        public int XP { get; }
        public CollectorQuestView(CollectorQuestRecord q)
        {
            Id=q.id; DefinitionId=q.definitionId; ThemeId=q.themeId; Goal=q.goal;
            Challenge=q.challenge; Complete=q.complete; Claimed=q.claimed;
            Progress=q.progress; Target=q.target; Coins=q.coins; XP=q.xp;
        }
    }

    /// <summary>Deterministic progress evaluation, independent of scene lifecycle and rendering.</summary>
    public sealed class CollectorQuestProgress
    {
        public CollectorCareer Career { get; }
        readonly IReadOnlyList<CollectorQuestDefinition> definitions;
        public CollectorQuestProgress(CollectorCareer career, IReadOnlyList<CollectorQuestDefinition> definitions)
        { Career = career; this.definitions = definitions; }

        public void Evaluate(CollectorSnapshot s)
        {
            Career.sortedHighWater = Math.Max(Career.sortedHighWater, s.sorted);
            foreach (var d in definitions)
                if (!Career.quests.Any(q => q.id == d.id))
                    Career.quests.Add(new CollectorQuestRecord { id=d.id, definitionId=d.id, goal=d.goal, target=d.target, coins=d.coins, xp=d.xp });
            foreach (var q in Career.quests.Where(q => !q.complete))
            {
                int value = Value(q, s);
                if (!q.challenge)
                {
                    var definition=definitions.FirstOrDefault(d => d.id == q.definitionId);
                    if(!definition) continue; // retired content keeps its career record
                    if (q.goal == CollectorQuestGoal.SortedCars || q.goal == CollectorQuestGoal.EntireStore)
                        q.target = Math.Min(definition.target, s.total);
                    if (q.goal == CollectorQuestGoal.EntireStore) q.target = s.total;
                    if (q.goal == CollectorQuestGoal.CompletedThemes)
                        q.target = Math.Min(definition.target, s.themes.Count);
                }
                q.progress = Math.Clamp(value - q.baseline, 0, Math.Max(0, q.target));
                if (q.target > 0 && q.progress >= q.target) q.complete = true;
            }
            EnsureChallenges(s);
            if(Career.pinnedId!=null && !Career.quests.Any(q=>q.id==Career.pinnedId && !q.claimed))
            { Career.pinnedId=null; Career.pinChosen=false; }
            if (!Career.pinChosen)
                Career.pinnedId = Career.quests.FirstOrDefault(q => !q.challenge && !q.complete)?.id;
        }

        static int Value(CollectorQuestRecord q, CollectorSnapshot s)
        {
            switch(q.goal)
            {
                case CollectorQuestGoal.SortedCars: case CollectorQuestGoal.EntireStore: return s.sorted;
                case CollectorQuestGoal.FullShelves: return s.fullShelves;
                case CollectorQuestGoal.OwnedUpgrades: return s.upgrades;
                case CollectorQuestGoal.DiscoveredDesigns: return s.discovered;
                case CollectorQuestGoal.CompletedThemes: return s.completedThemes.Count;
                case CollectorQuestGoal.SpecificTheme:
                    return s.themes.TryGetValue(q.themeId ?? "", out var t) && t.total > 0 && t.sorted == t.total ? 1 : 0;
                default: return 0;
            }
        }

        public void EnsureChallenges(CollectorSnapshot s, string avoidTheme = null)
        {
            AddChallenge(CollectorQuestGoal.SortedCars, "challenge.sort", Math.Min(10, s.total-Career.sortedHighWater), Career.sortedHighWater, 3, 15);
            AddChallenge(CollectorQuestGoal.DiscoveredDesigns, "challenge.discover", Math.Min(5, s.designs-s.discovered), s.discovered, 5, 25);
            var theme = s.themes.Where(t => t.Value.total > 0 && t.Value.sorted < t.Value.total && !s.completedThemes.Contains(t.Key))
                .OrderBy(t => t.Key==avoidTheme).ThenByDescending(t => (float)t.Value.sorted/t.Value.total).ThenBy(t => t.Key, StringComparer.Ordinal).FirstOrDefault();
            if (theme.Key != null) AddChallenge(CollectorQuestGoal.SpecificTheme, "challenge.theme", 1, 0, 8, 40, theme.Key);
        }

        void AddChallenge(CollectorQuestGoal goal, string definitionId, int target, int baseline, int coins, int xp, string theme = null)
        {
            if (target <= 0 || Career.quests.Any(q => q.challenge && !q.claimed && q.goal == goal)) return;
            Career.quests.Add(new CollectorQuestRecord { id=Career.runId+"/"+Guid.NewGuid().ToString("N"),
                definitionId=definitionId, goal=goal, target=target, baseline=baseline, coins=coins, xp=xp, challenge=true, themeId=theme });
        }

        public bool Pin(string id)
        {
            if (id != null && !Career.quests.Any(q => q.id == id && !q.claimed)) return false;
            Career.pinChosen = true; Career.pinnedId = id; return true;
        }

        public bool ReplaceChallenge(string id, CollectorSnapshot s)
        {
            var q=Career.quests.FirstOrDefault(q=>q.id==id && q.challenge && !q.complete && !q.claimed);
            if(q==null) return false;
            bool pinned=Career.pinnedId==id;
            Career.quests.Remove(q);
            EnsureChallenges(s, q.themeId);
            if(pinned) Career.pinnedId=Career.quests.FirstOrDefault(x=>x.challenge && !x.claimed && x.goal==q.goal)?.id;
            return true;
        }

        public bool FinishClaim(string id)
        {
            var q=Career.quests.FirstOrDefault(q=>q.id==id && q.complete && !q.claimed);
            if(q==null) return false;
            q.claimed=true; Career.xp += q.xp; Career.pendingClaim=null;
            if(Career.pinnedId==id) { Career.pinChosen=false; Career.pinnedId=null; }
            return true;
        }

        public void Replay()
        {
            Career.quests.RemoveAll(q=>q.challenge);
            Career.runId=Guid.NewGuid().ToString("N"); Career.sortedHighWater=0;
            Career.pendingClaim=null;
            if(Career.pinnedId!=null && !Career.quests.Any(q=>q.id==Career.pinnedId))
            { Career.pinnedId=null; Career.pinChosen=false; }
        }

        public static int RankIndex(int xp) => xp>=1000 ? 4 : xp>=500 ? 3 : xp>=250 ? 2 : xp>=100 ? 1 : 0;
    }
}
