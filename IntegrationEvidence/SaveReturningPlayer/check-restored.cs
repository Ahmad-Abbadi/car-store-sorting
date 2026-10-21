void Require(bool ok,string message){if(!ok)throw new System.InvalidOperationException(message);}
string qa=UnityEditor.SessionState.GetString("SaveQA.qa","");
var session=TinyRiders.ThemedCollectionSession.Instance;
var save=UnityEngine.Object.FindFirstObjectByType<LevelSaveManager>();
var expected=UnityEngine.JsonUtility.FromJson<LevelSaveData>(UnityEditor.SessionState.GetString("SaveQA.expected",""));
Require(session.IsReady&&save.HasRestoredProgress&&save.StorageKey==qa,"Restore not ready or isolated.");
Require(session.Total==1200&&session.Sorted==3&&session.LivePileCount==1196,"Population mismatch.");
Require(session.State.instances.Select(r=>r.id).Distinct().Count()==1200,"Duplicate record IDs.");
Require(expected.themed.instances.OrderBy(r=>r.id).Select(r=>r.id+"|"+r.collectibleId+"|"+r.state+"|"+r.shelfId+"|"+r.slotId).SequenceEqual(session.State.instances.OrderBy(r=>r.id).Select(r=>r.id+"|"+r.collectibleId+"|"+r.state+"|"+r.shelfId+"|"+r.slotId)),"Saved identities/states/slots changed.");
var shelves=UnityEngine.Object.FindObjectsByType<SortingShelf>(UnityEngine.FindObjectsSortMode.None);
foreach(var r in expected.themed.instances.Where(r=>r.state=="sorted")){
 var shelf=shelves.Single(s=>s.SaveId==r.shelfId);
 int slot=Enumerable.Range(0,shelf.SpotCount).Single(i=>shelf.GetSpot(i).name==r.slotId);
 Require(TinyRiders.ThemedWorldInstance.Read(shelf.GetOccupant(slot))==r.id,"Shelf occupant identity changed.");
}
var ids=WorldItemBase.All.Where(w=>w.gameObject.activeInHierarchy).Select(w=>TinyRiders.ThemedWorldInstance.Read(w)).Concat(shelves.SelectMany(s=>Enumerable.Range(0,s.SpotCount).Select(i=>s.GetOccupant(i))).Where(i=>i!=null).Select(i=>TinyRiders.ThemedWorldInstance.Read(i))).Concat(Enumerable.Range(0,Player.Instance.StashHandler.Inventory.Count).Select(i=>Player.Instance.StashHandler.Inventory.InstanceIdAt(i))).ToArray();
Require(ids.Length==1200&&ids.Distinct().Count()==1200,"Physical duplicate/loss.");
Require(Player.Instance.StashHandler.Inventory.InstanceIdAt(0)==UnityEditor.SessionState.GetString("SaveQA.held",""),"Held order/ID lost.");
Require(!save.StoreCompleted,"Golden reward unlocked on a partial store.");
Require(UnityEngine.PlayerPrefs.GetInt(TutorialProgress.StatusKey)==2,"Tutorial completion lost.");
var t=UnityEngine.Object.FindFirstObjectByType<TutorialController>();
Require(!t.enabled&&t.Step!=TutorialController.Lesson.Pickup,"Tutorial replayed.");
Require(UnityEditor.SessionState.GetBool("SaveQA.welcomeSeen",false)&&UnityEditor.SessionState.GetBool("SaveQA.welcomeHidden",false),"Welcome was not shown and dismissed.");
Require(string.Join("|",UpgradeManager.Instance.Upgrades.Select(u=>u.SaveId+"="+u.Level))==UnityEditor.SessionState.GetString("SaveQA.upgrades",""),"Upgrade levels changed.");
Require(CoinsManager.Instance.Coins==UnityEditor.SessionState.GetInt("SaveQA.coins",-1),"Wallet changed on restore.");
Require(UnityEngine.Mathf.Abs(Player.Instance.Look.Yaw-17)<.01f&&UnityEngine.Mathf.Abs(Player.Instance.Look.Pitch+8)<.01f,"Transition pose not restored.");
System.IO.File.AppendAllText("IntegrationEvidence/SaveReturningPlayer/result.txt","PASS reload: same 1200 physical IDs, 1196 world + 3 exact shelf slots + 1 held copy/order; upgrades/wallet and tutorial preserved; welcome displayed and faded; no golden unlock on partial store; look restored.\n");
return "Restoration checks passed.";
