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
        public string subjectId, seriesId;
        public CollectorQuestCategory category;
        public int stageOrder, authoredOrder;
        public bool available = true;
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
        public int sorted, total, discovered, designs, fullShelves, upgrades, shelfCapacity, totalUpgradeLevels, upgradeCapacity;
        public readonly Dictionary<string, (int sorted, int total)> themes = new Dictionary<string, (int, int)>();
        public readonly Dictionary<string, (int sorted, int total)> families = new Dictionary<string, (int, int)>();
        public readonly Dictionary<string, (int level, int max)> upgradeLevels = new Dictionary<string, (int, int)>();
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
        public string SubjectId { get; }
        public string SeriesId { get; }
        public CollectorQuestCategory Category { get; }
        public int AuthoredOrder { get; }
        public bool Available { get; }
        public CollectorQuestView(CollectorQuestRecord q)
        {
            Id=q.id; DefinitionId=q.definitionId; ThemeId=q.themeId; Goal=q.goal;
            Challenge=q.challenge; Complete=q.complete; Claimed=q.claimed;
            Progress=q.progress; Target=q.target; Coins=q.coins; XP=q.xp;
            SubjectId=q.subjectId; SeriesId=q.seriesId; Category=q.category; AuthoredOrder=q.authoredOrder; Available=q.available;
        }
    }

    /// <summary>Deterministic progress evaluation, independent of scene lifecycle and rendering.</summary>
    public sealed class CollectorQuestProgress
    {
        public CollectorCareer Career { get; }
        readonly IReadOnlyList<CollectorQuestDefinition> definitions;
        readonly Dictionary<string,CollectorQuestDefinition> byId;
        public CollectorQuestProgress(CollectorCareer career, IReadOnlyList<CollectorQuestDefinition> definitions)
        { Career = career; this.definitions = definitions; byId=definitions.ToDictionary(d=>d.id,StringComparer.Ordinal); }

        public void Evaluate(CollectorSnapshot s)
        {
            Career.sortedHighWater = Math.Max(Career.sortedHighWater, s.sorted);
            var existing=new HashSet<string>(Career.quests.Select(q=>q.id),StringComparer.Ordinal);
            foreach (var d in definitions)
                if (existing.Add(d.id))
                    Career.quests.Add(new CollectorQuestRecord { id=d.id, definitionId=d.id, goal=d.goal, target=d.target, coins=d.coins, xp=d.xp });
            foreach (var q in Career.quests)
            {
                if (!q.challenge)
                {
                    if(!byId.TryGetValue(q.definitionId,out var d)) {q.available=q.complete; continue;}
                    // Add presentation metadata without replacing saved achievements or rewards.
                    q.subjectId=d.subjectId; q.seriesId=d.seriesId; q.category=d.category;
                    q.stageOrder=d.stageOrder; q.authoredOrder=d.authoredOrder;
                    int capacity=Capacity(d,s);
                    int target=d.targetFraction>0 ? (int)Math.Ceiling(capacity*(double)d.targetFraction) : d.goal==CollectorQuestGoal.EntireStore ? capacity : d.target;
                    if(d.seriesId=="legacy" && (d.goal==CollectorQuestGoal.SortedCars || d.goal==CollectorQuestGoal.CompletedThemes))
                        target=Math.Min(target,capacity);
                    q.available=q.complete || (capacity>0 && target>0 && target<=capacity);
                    if(!q.complete) q.target=target;
                }
                else q.category=q.goal==CollectorQuestGoal.DiscoveredDesigns ? CollectorQuestCategory.Discoveries :
                    q.goal==CollectorQuestGoal.SpecificTheme ? CollectorQuestCategory.Themes : CollectorQuestCategory.Sorting;
                if(q.complete || !q.available) continue;
                int value = Value(q, s);
                q.progress = Math.Clamp(value - q.baseline, 0, Math.Max(0, q.target));
                if (q.target > 0 && q.progress >= q.target) q.complete = true;
            }
            EnsureChallenges(s);
            if(Career.pinnedId!=null && !Career.quests.Any(q=>q.id==Career.pinnedId && !q.claimed && q.available))
            { Career.pinnedId=null; Career.pinChosen=false; }
            if (!Career.pinChosen)
                Career.pinnedId = CollectorQuestPresentation.Select(Career.quests.Select(q=>new CollectorQuestView(q)),CollectorQuestTab.Journey)
                    .FirstOrDefault(q=>!q.Complete)?.Id;
        }

        static int Capacity(CollectorQuestDefinition d,CollectorSnapshot s)
        {
            switch(d.goal)
            {
                case CollectorQuestGoal.SortedCars: case CollectorQuestGoal.EntireStore: return s.total;
                case CollectorQuestGoal.DiscoveredDesigns: return s.designs;
                case CollectorQuestGoal.FullShelves: return s.shelfCapacity>0 ? s.shelfCapacity : s.fullShelves;
                case CollectorQuestGoal.OwnedUpgrades: return s.upgradeLevels.Count>0 ? s.upgradeLevels.Count : s.upgrades;
                case CollectorQuestGoal.CompletedThemes: return s.themes.Count;
                case CollectorQuestGoal.ThemeSortedCars: return s.themes.TryGetValue(d.subjectId ?? "",out var t) ? t.total : 0;
                case CollectorQuestGoal.FamilySortedCars: return s.families.TryGetValue(d.subjectId ?? "",out var f) ? f.total : 0;
                case CollectorQuestGoal.UpgradeLevel: return s.upgradeLevels.TryGetValue(d.subjectId ?? "",out var u) ? u.max : 0;
                case CollectorQuestGoal.TotalUpgradeLevels: return s.upgradeCapacity;
                default: return 0;
            }
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
                case CollectorQuestGoal.ThemeSortedCars: return s.themes.TryGetValue(q.subjectId ?? "",out var theme) ? theme.sorted : 0;
                case CollectorQuestGoal.FamilySortedCars: return s.families.TryGetValue(q.subjectId ?? "",out var family) ? family.sorted : 0;
                case CollectorQuestGoal.UpgradeLevel: return s.upgradeLevels.TryGetValue(q.subjectId ?? "",out var upgrade) ? upgrade.level : 0;
                case CollectorQuestGoal.TotalUpgradeLevels: return s.totalUpgradeLevels;
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
            var added=Career.quests[Career.quests.Count-1];
            added.category=goal==CollectorQuestGoal.DiscoveredDesigns ? CollectorQuestCategory.Discoveries :
                goal==CollectorQuestGoal.SpecificTheme ? CollectorQuestCategory.Themes : CollectorQuestCategory.Sorting;
        }

        public bool Pin(string id)
        {
            if (id != null && !Career.quests.Any(q => q.id == id && !q.claimed && q.available)) return false;
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

        public static readonly int[] RankThresholds = {0,100,250,500,1000,2500,5000,10000};
        public static int RankIndex(int xp) => xp>=10000 ? 7 : xp>=5000 ? 6 : xp>=2500 ? 5 : xp>=1000 ? 4 : xp>=500 ? 3 : xp>=250 ? 2 : xp>=100 ? 1 : 0;
    }
}
