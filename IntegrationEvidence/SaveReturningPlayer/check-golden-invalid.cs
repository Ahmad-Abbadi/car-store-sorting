void Require(bool ok,string m){if(!ok)throw new System.InvalidOperationException(m);}
var session=TinyRiders.ThemedCollectionSession.Instance;
var save=UnityEngine.Object.FindFirstObjectByType<LevelSaveManager>();
var reward=UnityEngine.Object.FindFirstObjectByType<TinyRiders.GoldenCarReveal>();
Require(session.IsReady&&save.HasRestoredProgress&&save.StoreCompleted&&session.Sorted==1200&&session.LivePileCount==0,"Completed store not restored.");
Require(reward.IsRevealed&&!reward.IsRevealing&&reward.trophy.activeSelf,"Earned golden display not restored directly.");
var shelves=UnityEngine.Object.FindObjectsByType<SortingShelf>(UnityEngine.FindObjectsSortMode.None);
var ids=shelves.SelectMany(s=>Enumerable.Range(0,s.SpotCount).Select(i=>TinyRiders.ThemedWorldInstance.Read(s.GetOccupant(i)))).ToArray();
Require(ids.Length==1200&&ids.All(id=>!string.IsNullOrEmpty(id))&&ids.Distinct().Count()==1200,"Completed shelf population changed.");
Require(session.State.completedThemes.Count==40,"Completed theme records lost.");
Require(session.ValidateSave(session.Capture(),out var validReason),validReason);
var invalid=UnityEngine.JsonUtility.FromJson<TinyRiders.ThemedSessionSave>(UnityEngine.JsonUtility.ToJson(session.Capture()));
invalid.instances.Add(invalid.instances[0]);
Require(!session.ValidateSave(invalid,out _),"Duplicate payload accepted.");
invalid.instances.RemoveAt(invalid.instances.Count-1);invalid.instances[0].state="lost";
Require(!session.ValidateSave(invalid,out _),"Unknown state accepted.");
Require(!session.ValidateSave(null,out _),"Missing payload accepted.");
System.IO.File.AppendAllText("IntegrationEvidence/SaveReturningPlayer/result.txt","PASS earned golden reward restores immediately without replaying reveal; 1200 unique saved shelf copies and 40 completed themes. Duplicate identities, unknown states and missing payload rejected before mutation.\n");
// Simulate a corrupt stored payload at the next launch. Bypass the normal pre-exit
// checkpoint deliberately so the injected external data is what startup reads.
string bad="{\"version\":3,\"scene\":\"Level\",\"completed\":true,\"themed\":null}";
UnityEngine.PlayerPrefs.SetString(save.StorageKey,bad);UnityEngine.PlayerPrefs.Save();
UnityEditor.SessionState.SetString("SaveQA.bad",bad);
UnityEngine.SceneManagement.SceneManager.LoadScene("Level");
return "Golden restoration passed. Checking startup with a missing collection payload and completion=true.";
