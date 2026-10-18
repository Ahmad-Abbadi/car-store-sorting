using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TinyRiders.Quests;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace TinyRiders.Editor
{
    /// <summary>Explicit opt-in live verification. Level, position, wallet and career use fresh QA keys.</summary>
    [InitializeOnLoad]
    public static class CollectorQuestExpansionQA
    {
        const string Active="Quest240QA.Active", PrefixKey="Quest240QA.Prefix";
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        [Serializable] sealed class PrefsSnapshot {public List<Pref> entries=new List<Pref>();}
        [Serializable] sealed class Pref {public string key,value; public bool had,integer;}
        static string Prefix=>SessionState.GetString(PrefixKey,"");
        static CollectorQuestExpansionQA(){SceneManager.sceneLoaded+=Isolate;}

        public static string Prepare()
        {
            if(EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Start QA from the saved Level scene outside Play mode.");
            if(SessionState.GetBool(Active,false)) throw new InvalidOperationException("Finish the previous isolated QA session first.");
            var original=new PrefsSnapshot();
            foreach(var key in new[]{"LevelSave","Coins","Coins_RewardWallet","CollectorQuests","PlayerPositionSave","Language"}
                .Concat(AssetDatabase.FindAssets("t:UpgradeSO").Select(g=>"Upgrade_"+AssetDatabase.LoadAssetAtPath<UpgradeSO>(AssetDatabase.GUIDToAssetPath(g)).SaveId)))
            {
                bool integer=key=="Coins" || key.StartsWith("Upgrade_",StringComparison.Ordinal);
                original.entries.Add(new Pref{key=key,had=PlayerPrefs.HasKey(key),integer=integer,value=integer ? PlayerPrefs.GetInt(key).ToString() : PlayerPrefs.GetString(key,"")});
            }
            SessionState.SetString("Quest240QA.Original",JsonUtility.ToJson(original));
            SessionState.SetString(PrefixKey,"TinyRiders.Quests240.QA."+Guid.NewGuid().ToString("N"));
            var manager=Object.FindFirstObjectByType<CollectorQuestManager>();
            var legacy=new CollectorCareer{xp=20,pinnedId="getting_organized",pinChosen=true,pendingClaim="getting_organized"};
            foreach(var d in manager.definitions.Where(d=>CollectorQuestBuilder.LegacyIds.Contains(d.id)))
                legacy.quests.Add(new CollectorQuestRecord{id=d.id,definitionId=d.id,goal=d.goal,target=d.goal==CollectorQuestGoal.EntireStore ? 1200 : d.target,
                    coins=d.coins,xp=d.xp,complete=d.id=="first_display" || d.id=="getting_organized",claimed=d.id=="first_display",
                    progress=d.id=="first_display" ? 5 : d.id=="getting_organized" ? 25 : 0});
            PlayerPrefs.SetString(Prefix+".level.CollectorQuests",JsonUtility.ToJson(legacy)); PlayerPrefs.Save();
            SessionState.SetBool(Active,true);
            return "Prepared an isolated legacy 8-quest save with a pending claim; production progress will not be written.";
        }

        static void Set(object target,string field,object value)=>target.GetType().GetField(field,Private).SetValue(target,value);
        static void Isolate(Scene scene,LoadSceneMode mode)
        {
            if(!SessionState.GetBool(Active,false) || !Object.FindFirstObjectByType<ThemedCollectionSession>()) return;
            Set(Object.FindFirstObjectByType<LevelSaveManager>(),"saveKey",Prefix+".level");
            Set(Object.FindFirstObjectByType<PlayerSavePosition>(),"saveKey",Prefix+".position");
            var wallet=Object.FindFirstObjectByType<CoinsManager>(); Set(wallet,"saveKey",Prefix+".coins");
            var field=typeof(CoinsManager).GetField("wallet",Private);
            field.SetValue(wallet,Activator.CreateInstance(field.FieldType,true)); Set(wallet,"walletReadable",true);
            typeof(CoinsManager).GetMethod("Awake",Private).Invoke(wallet,null);
            foreach(var probe in Object.FindObjectsByType<ThemedPerformanceProbe>(FindObjectsSortMode.None)) probe.enabled=false;
            var reveal=Object.FindFirstObjectByType<GoldenCarReveal>(); if(reveal) reveal.duration=1.5f; // runtime QA only
        }

        public static string VerifyStartup()
        {
            var manager=CollectorQuestManager.Instance;
            if(!manager || !manager.Ready) throw new Exception("Quest manager not ready");
            if((string)typeof(CollectorQuestManager).GetField("storageKey",Private).GetValue(manager)!=Prefix+".level.CollectorQuests") throw new Exception("Career not isolated");
            if(manager.Quests.Count(q=>!q.Challenge)!=240 || manager.XP!=50 || CoinsManager.Instance.Coins!=5)
                throw new Exception("Legacy migration or pending claim recovery failed");
            if(!manager.Quests.Single(q=>q.Id=="first_display").Claimed || !manager.Quests.Single(q=>q.Id=="getting_organized").Claimed)
                throw new Exception("Legacy claim state lost");
            var board=Object.FindFirstObjectByType<CollectorQuestBoard>(); var handler=Object.FindFirstObjectByType<InteractionHandler>();
            int mask=new SerializedObject(handler).FindProperty("interactableMask").intValue;
            var aim=board.transform.position+Vector3.up*1.6f;
            if(!Physics.Raycast(aim-board.transform.forward*2.5f,board.transform.forward,out var hit,3,mask) || hit.collider.GetComponent<CollectorQuestBoard>()!=board)
                throw new Exception("Board interaction ray failed");
            board.OnInteract(handler);
            if(!board.interfaceUI.IsOpen || Time.timeScale!=0 || !Cursor.visible) throw new Exception("Quest modal failed");
            return "PASS: migrated 8 -> 240 quests; recovered pending claim exactly once (50 XP, 5 coins); board ray and modal work.";
        }

        public static string VerifyPagination()
        {
            var ui=Object.FindFirstObjectByType<CollectorQuestUI>(); ui.SelectCategory(-1); ui.SelectTab(CollectorQuestTab.AllQuests);
            var seen=new HashSet<string>();
            for(int page=0;page<ui.PageCount;page++)
            {
                if(ui.VisibleCardCount>6 || ui.content.childCount>6) throw new Exception("Too many card instances");
                foreach(var card in ui.content.GetComponentsInChildren<CollectorQuestCard>())
                    if(!seen.Add((string)typeof(CollectorQuestCard).GetField("id",Private).GetValue(card))) throw new Exception("Duplicated quest across pages");
                if(page<ui.PageCount-1) ui.nextPage.onClick.Invoke();
            }
            if(seen.Count!=240 || ui.PageCount!=40) throw new Exception("Catalog pagination missing quests");
            ui.SelectTab(CollectorQuestTab.Journey);
            if(ui.VisibleCardCount>6) throw new Exception("Journey does not reveal gradually");
            return "PASS: all 240 quests reachable across 40 spreads; only 6 reusable cards; six category recommendations.";
        }

        public static string Place(string subject,int count,bool family=false)
        {
            var session=ThemedCollectionSession.Instance;
            var definitions=session.ActiveDefinitions.ToDictionary(e=>e.collectibleId);
            var shelves=Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None);
            int placed=0;
            foreach(var record in session.State.instances.Where(r=>r.state=="world" || r.state=="pending").ToArray())
            {
                var d=definitions[record.collectibleId]; if((family ? d.baseCarId : d.themeId)!=subject) continue;
                var shelf=shelves.First(sh=>sh.GetComponent<ThemeShelfBinding>()?.themeId==d.themeId && sh.HasFreeSpot);
                int spot=Enumerable.Range(0,shelf.SpotCount).First(i=>shelf.IsSpotFree(i));
                var world=WorldItemBase.All.FirstOrDefault(w=>ThemedWorldInstance.Read(w)==record.id);
                if(!world){world=Object.Instantiate(d.sortingItem.WorldItem,session.pileOrigin.position+Vector3.up,Quaternion.identity); session.Dropped(world,record.id);}
                if(!Player.Instance.ItemCollector.TryCollectItem(world)) throw new Exception("Real pickup failed");
                if(!Player.Instance.SortingBehaviour.TryGetPlacedItemFromHand(out var item)) throw new Exception("Real placement conversion failed");
                shelf.AddItem(item,spot); placed++;
                if(placed==count) break;
            }
            if(placed!=count) throw new Exception("Insufficient eligible test cars");
            return "Placed "+placed+" real cars; sorted="+session.Sorted;
        }

        public static string RememberForReload()
        {
            var m=CollectorQuestManager.Instance;
            SessionState.SetInt("Quest240QA.ExpectedXP",m.XP); SessionState.SetInt("Quest240QA.ExpectedCoins",CoinsManager.Instance.Coins);
            SessionState.SetInt("Quest240QA.ExpectedClaims",m.Quests.Count(q=>!q.Challenge && q.Claimed));
            SessionState.SetInt("Quest240QA.ExpectedSorted",ThemedCollectionSession.Instance.Sorted);
            m.Pin("family_C015_3"); Object.FindFirstObjectByType<CollectorQuestUI>().Close();
            SceneManager.LoadScene("Level"); return "Reloading isolated expanded career";
        }

        public static string VerifyReload()
        {
            var m=CollectorQuestManager.Instance;
            if(!m.Ready || m.XP!=SessionState.GetInt("Quest240QA.ExpectedXP",-1) || CoinsManager.Instance.Coins!=SessionState.GetInt("Quest240QA.ExpectedCoins",-1)
                || m.Quests.Count(q=>!q.Challenge && q.Claimed)!=SessionState.GetInt("Quest240QA.ExpectedClaims",-1)
                || m.PinnedId!="family_C015_3" || ThemedCollectionSession.Instance.Sorted!=SessionState.GetInt("Quest240QA.ExpectedSorted",-1))
                throw new Exception("Expanded career persistence failed");
            return "PASS: reload preserved XP, coins, all claims, manually pinned family goal and sorted cars without duplicate rewards.";
        }

        public static string Finish()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before finishing QA.");
            var original=JsonUtility.FromJson<PrefsSnapshot>(SessionState.GetString("Quest240QA.Original",""));
            var language=original.entries.Single(e=>e.key=="Language");
            if(language.had) PlayerPrefs.SetString(language.key,language.value); else PlayerPrefs.DeleteKey(language.key);
            foreach(string suffix in new[]{".level",".level.CollectorQuests",".coins",".coins_RewardWallet",".position"}) PlayerPrefs.DeleteKey(Prefix+suffix);
            PlayerPrefs.Save(); SessionState.SetBool(Active,false); Localizer.RefreshDatabase();
            foreach(var entry in original.entries)
                if(PlayerPrefs.HasKey(entry.key)!=entry.had || (entry.had && (entry.integer ? PlayerPrefs.GetInt(entry.key).ToString() : PlayerPrefs.GetString(entry.key,""))!=entry.value))
                    throw new Exception("Production preference changed: "+entry.key);
            return "PASS: original player save, wallet, career, position, upgrades and language match the pre-QA snapshot. Isolated QA data removed.";
        }
    }
}
