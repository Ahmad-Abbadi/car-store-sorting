void Require(bool ok,string m){if(!ok)throw new System.InvalidOperationException(m);}
var session=TinyRiders.ThemedCollectionSession.Instance;
var save=UnityEngine.Object.FindFirstObjectByType<LevelSaveManager>();
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
string previous=UnityEngine.PlayerPrefs.GetString(save.StorageKey);
var held=session.State.heldOrder;
session.State.heldOrder=null; // inject capture failure in temporary runtime state only
try{
 for(int n=0;n<4;n++)save.Save();
 Require(UnityEngine.PlayerPrefs.GetString(save.StorageKey)==previous,"Failed checkpoint replaced the prior save.");
 Require((int)typeof(LevelSaveManager).GetField("_saveFailures",flags).GetValue(save)==3,"Retries not bounded.");
}finally{session.State.heldOrder=held;}
LevelSaveManager.MarkDirty();save.Save();
Require((int)typeof(LevelSaveManager).GetField("_saveFailures",flags).GetValue(save)==0,"Later meaningful progress did not recover persistence.");
// Player can continue using ordinary held-item placement after both reloads.
var definition=session.Definition(Player.Instance.ItemHandler.CurrentEquipped.Data);
var shelf=UnityEngine.Object.FindObjectsByType<SortingShelf>(UnityEngine.FindObjectsSortMode.None).First(s=>s.IsCorrectItem(definition.sortingItem)&&s.HasFreeSpot);
int spot=Enumerable.Range(0,shelf.SpotCount).First(i=>shelf.GetOccupant(i)==null&&shelf.CanAccept(definition.sortingItem,i,out _));
Require(Player.Instance.SortingBehaviour.TryGetPlacedItemFromHand(out var placed),"Returning player hand transfer failed.");
shelf.AddItem(placed,spot);
Require(session.Sorted==4,"Returning player could not continue sorting.");
System.IO.File.AppendAllText("IntegrationEvidence/SaveReturningPlayer/result.txt","PASS second reload: no loss/duplicates; returning player continues normal sorting. Injected capture failure retained previous JSON, stopped at 3 attempts, and recovered on next meaningful checkpoint.\n");
Require(session.DebugSortAll(),"Existing completion fixture could not sort the isolated collection.");
Require(session.Sorted==1200&&save.StoreCompleted&&session.LivePileCount==0,"Completion fixture invalid.");
return "Repeated load, resumed sorting and bounded capture-failure recovery passed. Existing development fixture prepared a completed temporary collection.";
