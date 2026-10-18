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
            var s = new CollectorSnapshot { sorted=session.Sorted, total=session.Total, discovered=session.UniqueDiscovered,
                designs=session.ActiveDefinitions.Count(), fullShelves=shelves.Count(x=>x && x.IsCompleted),
                upgrades=UpgradeManager.Instance ? UpgradeManager.Instance.Upgrades.Count(x=>x && x.Level>0) : 0 };
            foreach(var theme in session.ActiveDefinitions.Select(x=>x.themeId).Distinct()) s.themes[theme]=session.ThemeCounts(theme);
            foreach(var theme in session.State.completedThemes) if(s.themes.ContainsKey(theme)) s.completedThemes.Add(theme);
            return s;
        }

        void Refresh(bool celebrate)
        {
            dirty=false;
            var before = new HashSet<string>(engine.Career.quests.Where(q=>q.complete).Select(q=>q.id));
            engine.Evaluate(Snapshot());
            Publish(); Save();
            if(celebrate) foreach(var q in Quests.Where(q=>q.Complete && !before.Contains(q.Id))) Completed?.Invoke(q);
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
            if(!readable) return;
            RecoverClaim(); engine.Replay(); Save(); resetting=true;
        }

        public void PrepareFullReset() { resetting=true; PlayerPrefs.DeleteKey(storageKey); }

        public string Title(CollectorQuestView q)
        {
            var definition=definitions.FirstOrDefault(d=>d.id==q.DefinitionId);
            string fallback=definition ? definition.title : q.Goal==CollectorQuestGoal.SortedCars ? "Fresh Displays" :
                q.Goal==CollectorQuestGoal.DiscoveredDesigns ? "New Discoveries" : "Theme Spotlight";
            return Localizer.GetOr("quest."+q.DefinitionId+".title",fallback);
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
                default: fallback="Complete the {1} theme"; break;
            }
            var definition=definitions.FirstOrDefault(d=>d.id==q.DefinitionId);
            if(definition && !string.IsNullOrEmpty(definition.description)) fallback=definition.description;
            string text=Localizer.GetOr("quest."+q.DefinitionId+".desc",fallback);
            return Localizer.FormatText(text,q.Target,session ? session.ThemeName(q.ThemeId) : q.ThemeId);
        }
        public static string RankName(int rank) => Localizer.GetOr("quest.rank."+rank,
            new[]{"Rookie","Organizer","Collector","Specialist","Master Collector"}[Math.Clamp(rank,0,4)]);
    }
}
