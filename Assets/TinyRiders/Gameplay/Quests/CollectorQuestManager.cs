using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TinyRiders.Quests
{
    [DisallowMultipleComponent]
    public sealed class CollectorQuestManager : MonoBehaviour
    {
        public const string SaveKey = "CollectorQuests";
        public static CollectorQuestManager Instance { get; private set; }
        public CollectorQuestDefinition[] definitions = Array.Empty<CollectorQuestDefinition>();
        public event Action Changed;
        public event Action<CollectorQuestView> Completed;
        public event Action<IReadOnlyList<CollectorQuestView>> CompletionBatch;
        public event Action<int> RankedUp;
        public IReadOnlyList<CollectorQuestView> Quests { get; private set; } = Array.Empty<CollectorQuestView>();
        public bool Ready { get; private set; }
        public int XP => engine?.Career.xp ?? 0;
        public int Rank => CollectorQuestProgress.RankIndex(XP);
        public string PinnedId => engine?.Career.pinnedId;
        public int ClaimableCount => Quests.Count(q => q.Complete && !q.Claimed);
        CollectorQuestProgress engine;
        ThemedCollectionSession session;
        SortingShelf[] shelves;
        Dictionary<string,CollectorQuestDefinition> definitionById;
        bool dirty, readable = true, resetting;
        string lastSaved;
        string storageKey=SaveKey;

        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void LoadCareer()
        {
            var career = new CollectorCareer();
            if (PlayerPrefs.HasKey(storageKey))
            {
                try
                {
                    career = JsonUtility.FromJson<CollectorCareer>(PlayerPrefs.GetString(storageKey));
                    if (career == null || career.version != 1 || career.quests == null || string.IsNullOrEmpty(career.runId))
                        throw new InvalidOperationException("Unsupported quest save");
                    if (career.quests.Any(q=>q==null || string.IsNullOrEmpty(q.id))) throw new InvalidOperationException("Invalid quest record");
                }
                catch (Exception e) { readable=false; Debug.LogWarning("Collector career retained without modification: " + e.Message); }
            }
            engine = new CollectorQuestProgress(career ?? new CollectorCareer(), definitions);
            definitionById=definitions.ToDictionary(d=>d.id,StringComparer.Ordinal);
        }

        IEnumerator Start()
        {
            session = ThemedCollectionSession.Instance;
            if (!session) yield break;
            while (!session.IsReady) yield return null;
            yield return null; // shelf restoration and all other Start methods have completed
            var coordinator=FindFirstObjectByType<LevelSaveManager>();
            // Existing sorting tests isolate their level save in sceneLoaded, after Awake.
            if(coordinator && coordinator.StorageKey!="LevelSave") storageKey=coordinator.StorageKey+".CollectorQuests";
            LoadCareer();
            if(!readable) yield break;
            shelves = FindObjectsByType<SortingShelf>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            session.Changed += MarkDirty;
            UpgradeManager.OnPurchased += OnPurchased;
            SortingShelf.OnShelfFilled += OnShelfFilled;
            RecoverClaim();
            Ready = true;
            Refresh(false);
        }

        void OnPurchased(UpgradeSO _) => MarkDirty();
        void OnShelfFilled(SortingShelf _) => MarkDirty();
        void MarkDirty() => dirty = true;
        void LateUpdate() { if (Ready && dirty && !resetting) Refresh(true); }
        void OnDestroy()
        {
            if (session) session.Changed -= MarkDirty;
            UpgradeManager.OnPurchased -= OnPurchased;
            SortingShelf.OnShelfFilled -= OnShelfFilled;
            if (Instance == this) Instance=null;
        }

        CollectorSnapshot Snapshot()
        {
            var active=session.ActiveDefinitions.ToDictionary(e=>e.collectibleId,StringComparer.Ordinal);
            var s=new CollectorSnapshot{designs=active.Count,discovered=session.State.discovered.Count(active.ContainsKey)};
            // One traversal supplies global, theme and family counts, even for automatic bulk sorting.
            foreach(var record in session.State.instances)
            {
                if(!active.TryGetValue(record.collectibleId,out var d)) continue;
                int sorted=record.state=="sorted" ? 1 : 0;
                s.total++; s.sorted+=sorted;
                s.themes.TryGetValue(d.themeId,out var t); s.themes[d.themeId]=(t.sorted+sorted,t.total+1);
                s.families.TryGetValue(d.baseCarId,out var f); s.families[d.baseCarId]=(f.sorted+sorted,f.total+1);
            }
            foreach(var shelf in shelves)
                if(shelf && shelf.SpotCount>0 && shelf.TryGetComponent<ThemeShelfBinding>(out var binding) && s.themes.ContainsKey(binding.themeId))
                {s.shelfCapacity++; if(shelf.IsCompleted) s.fullShelves++;}
            if(UpgradeManager.Instance)
                foreach(var u in UpgradeManager.Instance.Upgrades.Where(u=>u))
                {
                    s.upgradeLevels[u.SaveId]=(u.Level,u.MaxLevel); s.totalUpgradeLevels+=u.Level; s.upgradeCapacity+=u.MaxLevel;
                    if(u.Level>0) s.upgrades++;
                }
            foreach(var theme in session.State.completedThemes) if(s.themes.ContainsKey(theme)) s.completedThemes.Add(theme);
            return s;
        }

        void Refresh(bool celebrate)
        {
            dirty=false;
            var before = new HashSet<string>(engine.Career.quests.Where(q=>q.complete).Select(q=>q.id));
            engine.Evaluate(Snapshot());
            Publish(); Save();
            if(celebrate)
            {
                var finished=Quests.Where(q=>q.Complete && !before.Contains(q.Id)).ToArray();
                if(finished.Length>0) CompletionBatch?.Invoke(Array.AsReadOnly(finished));
                foreach(var q in finished) Completed?.Invoke(q);
            }
        }

        void Publish()
        { Quests=Array.AsReadOnly(engine.Career.quests.Select(q=>new CollectorQuestView(q)).ToArray()); Changed?.Invoke(); }

        void Save()
        {
            if(!readable || resetting || (session && session.suspendPersistenceForTests)) return;
            string json=JsonUtility.ToJson(engine.Career);
            if(json==lastSaved) return;
            PlayerPrefs.SetString(storageKey,json); PlayerPrefs.Save(); lastSaved=json;
        }

        public bool Pin(string id)
        {
            if(!Ready || resetting || !engine.Pin(id)) return false;
            Publish(); Save(); return true;
        }

        public bool Claim(string id)
        {
            if(!Ready || resetting || !CoinsManager.Instance) return false;
            if(!string.IsNullOrEmpty(engine.Career.pendingClaim) && !RecoverClaim()) return false;
            var quest=engine.Career.quests.FirstOrDefault(q=>q.id==id && q.complete && !q.claimed);
            if(quest==null) return false;
            int rank=Rank;
            // Write intent before the wallet transaction. A reload completes the same receipt.
            engine.Career.pendingClaim=id; Save();
            if(!RecoverClaim()) return false;
            Refresh(false);
            if(Rank>rank) RankedUp?.Invoke(Rank);
            return true;
        }

        bool RecoverClaim()
        {
            string id=engine.Career.pendingClaim;
            if(string.IsNullOrEmpty(id)) return true;
            var q=engine.Career.quests.FirstOrDefault(x=>x.id==id && x.complete && !x.claimed);
            if(q==null) {engine.Career.pendingClaim=null; Save(); return true;}
            if(!CoinsManager.Instance || !CoinsManager.Instance.AddRewardOnce("collector/"+id,q.coins)) return false;
            engine.FinishClaim(id); Save(); return true;
        }

        public bool ReplaceChallenge(string id)
        {
            if(!Ready || resetting || !engine.ReplaceChallenge(id, Snapshot())) return false;
            Publish(); Save(); return true;
        }

        public void PrepareReplay()
        {
            if(!readable || engine==null) return;
            RecoverClaim(); engine.Replay(); Save(); resetting=true;
        }

        public void PrepareFullReset() { resetting=true; PlayerPrefs.DeleteKey(storageKey); }

        public string Title(CollectorQuestView q)
        {
            var definition=Definition(q);
            string fallback=definition ? definition.title : q.Goal==CollectorQuestGoal.SortedCars ? "Fresh Displays" :
                q.Goal==CollectorQuestGoal.DiscoveredDesigns ? "New Discoveries" : "Theme Spotlight";
            return Localizer.FormatText(Localizer.GetOr(definition && !string.IsNullOrEmpty(definition.titleKey) ? definition.titleKey : "quest."+q.DefinitionId+".title",fallback),
                q.Target,SubjectName(q));
        }

        public string Description(CollectorQuestView q)
        {
            string fallback;
            switch(q.Goal)
            {
                case CollectorQuestGoal.SortedCars: fallback=q.Challenge ? "Correctly shelve {0} additional cars" : "Correctly shelve {0} cars"; break;
                case CollectorQuestGoal.FullShelves: fallback="Fill {0} shelf correctly"; break;
                case CollectorQuestGoal.OwnedUpgrades: fallback="Own any upgrade"; break;
                case CollectorQuestGoal.DiscoveredDesigns: fallback="Discover {0} additional car designs"; break;
                case CollectorQuestGoal.CompletedThemes: fallback="Complete {0} themes"; break;
                case CollectorQuestGoal.EntireStore: fallback="Correctly shelve the entire collection"; break;
                case CollectorQuestGoal.ThemeSortedCars: fallback="Correctly shelve {0} cars from the {1} theme"; break;
                case CollectorQuestGoal.FamilySortedCars: fallback="Correctly shelve {0} {1} cars across themes"; break;
                case CollectorQuestGoal.UpgradeLevel: fallback="Reach level {0} of {1}"; break;
                case CollectorQuestGoal.TotalUpgradeLevels: fallback="Own {0} total upgrade levels"; break;
                default: fallback="Complete the {1} theme"; break;
            }
            var definition=Definition(q);
            if(definition && !string.IsNullOrEmpty(definition.description)) fallback=definition.description;
            string text=Localizer.GetOr(definition && !string.IsNullOrEmpty(definition.descriptionKey) ? definition.descriptionKey : "quest."+q.DefinitionId+".desc",fallback);
            return Localizer.FormatText(text,q.Target,SubjectName(q));
        }
        CollectorQuestDefinition Definition(CollectorQuestView q) => definitionById!=null && definitionById.TryGetValue(q.DefinitionId,out var d) ? d : null;
        public string SubjectName(CollectorQuestView q)
        {
            if(q.Goal==CollectorQuestGoal.ThemeSortedCars || q.Goal==CollectorQuestGoal.SpecificTheme)
                return session ? session.ThemeName(q.Challenge ? q.ThemeId : q.SubjectId) : q.SubjectId;
            if(q.Goal==CollectorQuestGoal.FamilySortedCars) return Localizer.GetOr("category."+q.SubjectId,q.SubjectId);
            if(q.Goal==CollectorQuestGoal.UpgradeLevel)
                return UpgradeManager.Instance?.Upgrades.FirstOrDefault(u=>u.SaveId==q.SubjectId)?.DisplayName ?? q.SubjectId;
            return string.Empty;
        }
        public static string RankName(int rank) => Localizer.GetOr("quest.rank."+rank,
            new[]{"Rookie","Organizer","Collector","Specialist","Master Collector","Curator","Collector Legend","Hall of Fame"}[Math.Clamp(rank,0,7)]);
    }
}
