void Require(bool ok,string message){if(!ok)throw new System.InvalidOperationException(message);}
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var session=TinyRiders.ThemedCollectionSession.Instance;
var save=UnityEngine.Object.FindFirstObjectByType<LevelSaveManager>();
string qa=UnityEditor.SessionState.GetString("SaveQA.qa","");
Require(session!=null&&session.IsReady&&session.Total==1200&&session.LivePileCount==1200,"Fresh population not ready.");
Require(save.StorageKey==qa&&!save.HasRestoredProgress&&session.Sorted==0&&!save.StoreCompleted,"Fresh state incorrect.");
var tutorial=UnityEngine.Object.FindFirstObjectByType<TutorialController>();
Require((bool)typeof(TutorialController).GetField("storeRunning",flags).GetValue(tutorial),"Fresh inline tutorial absent.");
Require(session.ActiveDefinitions.All(e=>e.sortingItem.WorldItem&&e.sortingItem.HandItem&&e.sortingItem.PlacedOnShelfPrefab),"Missing wrapper.");
var upgrades=UnityEngine.Object.FindFirstObjectByType<UpgradeManager>();
UnityEditor.SessionState.SetString("SaveQA.upgrades",string.Join("|",upgrades.Upgrades.Select(u=>u.SaveId+"="+u.Level)));
// All subsequent scene reloads use the same temporary keys before Start restores.
UnityEngine.SceneManagement.SceneManager.sceneLoaded+=(scene,mode)=>{
 if(scene.name!="Level")return;
 var s=UnityEngine.Object.FindFirstObjectByType<LevelSaveManager>();typeof(LevelSaveManager).GetField("saveKey",flags).SetValue(s,qa);
 var p=UnityEngine.Object.FindFirstObjectByType<PlayerSavePosition>();typeof(PlayerSavePosition).GetField("saveKey",flags).SetValue(p,qa+".position");
 var c=UnityEngine.Object.FindFirstObjectByType<CoinsManager>();typeof(CoinsManager).GetField("saveKey",flags).SetValue(c,qa+".coins");
 var wallet=typeof(CoinsManager).GetField("wallet",flags);
 string json=UnityEngine.PlayerPrefs.GetString(qa+".coins_RewardWallet","{\"version\":1,\"coins\":0,\"receipts\":[]}");
 object loaded=UnityEngine.JsonUtility.FromJson(json,wallet.FieldType);wallet.SetValue(c,loaded);
 typeof(CoinsManager).GetProperty("Coins").SetValue(c,(int)wallet.FieldType.GetField("coins").GetValue(loaded));
 foreach(var probe in UnityEngine.Object.FindObjectsByType<TinyRiders.ThemedPerformanceProbe>(UnityEngine.FindObjectsSortMode.None))probe.enabled=false;
};
var definition=session.ActiveDefinitions.First();
var shelf=UnityEngine.Object.FindObjectsByType<SortingShelf>(UnityEngine.FindObjectsSortMode.None).First(s=>s.GetComponent<TinyRiders.ThemeShelfBinding>()?.themeId==definition.themeId);
for(int n=0;n<3;n++){
 var world=WorldItemBase.All.First(w=>w.gameObject.activeInHierarchy&&session.Definition(w.Data)?.themeId==definition.themeId);
 Require(Player.Instance.ItemCollector.TryCollectItem(world),"Pickup failed.");
 Require(Player.Instance.SortingBehaviour.TryGetHeldSortable(out var data),"Missing held sortable.");
 Require(shelf.CanAccept(data,n,out var reason),reason);
 Require(Player.Instance.SortingBehaviour.TryGetPlacedItemFromHand(out var placed),"Hand transfer failed.");
 shelf.AddItem(placed,n);
}
Require(session.Sorted==3&&session.LivePileCount==1197,"Three placements failed.");
var held=WorldItemBase.All.First(w=>w.gameObject.activeInHierarchy&&TinyRiders.ThemedWorldInstance.Read(w)!=null);
UnityEditor.SessionState.SetString("SaveQA.held",TinyRiders.ThemedWorldInstance.Read(held));
Require(Player.Instance.ItemCollector.TryCollectItem(held),"Fourth pickup failed.");
Require(session.LivePileCount==1196,"Held car not removed from pile.");
System.IO.File.WriteAllText("IntegrationEvidence/SaveReturningPlayer/result.txt","PASS fresh: 1200 copies, 600 definitions, normal inline tutorial, no golden unlock; required prefabs present.\nPASS normal pickup and three placements into a partial shelf; fourth car held.\n");
UnityEditor.SessionState.SetString("SaveQA.shelf",shelf.SaveId);
return "Fresh checks and 3 ordinary placements + held pickup passed; waiting for the scheduled checkpoint (no manual Save).";
