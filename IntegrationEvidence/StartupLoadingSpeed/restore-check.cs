void Require(bool ok, string message) { if (!ok) throw new System.InvalidOperationException(message); }
var session = TinyRiders.ThemedCollectionSession.Instance;
Require(session != null && session.IsReady && !SceneLoader.IsLoading, "Restored level is not ready.");
Require(session.Total == 1200 && session.LivePileCount == 1199 && session.Sorted == 1, "Saved population did not restore.");
string id = UnityEditor.SessionState.GetString("StartupSpeed.SortedId", "");
var record = session.State.instances.Single(r => r.id == id);
Require(record.state == "sorted" && record.shelfId == UnityEditor.SessionState.GetString("StartupSpeed.ShelfId", "") && record.slotId == UnityEditor.SessionState.GetString("StartupSpeed.SlotId", ""), "Sorted identity or slot changed during restoration.");
var shelf = UnityEngine.Object.FindObjectsByType<SortingShelf>(UnityEngine.FindObjectsSortMode.None).Single(s => s.SaveId == record.shelfId);
int slot = Enumerable.Range(0, shelf.SpotCount).Single(i => shelf.GetSpot(i).name == record.slotId);
Require(TinyRiders.ThemedWorldInstance.Read(shelf.GetOccupant(slot)) == id, "Saved shelf occupant did not restore.");
var saved = UnityEngine.JsonUtility.FromJson<LevelSaveData>(UnityEngine.PlayerPrefs.GetString("StartupSpeed.QA.20261021"));
Require(saved.themed.instances.OrderBy(r => r.id).Select(r => r.id + ":" + r.collectibleId + ":" + r.state).SequenceEqual(session.State.instances.OrderBy(r => r.id).Select(r => r.id + ":" + r.collectibleId + ":" + r.state)), "Copy IDs, variants, or states changed during restore.");
Require(UnityEngine.PlayerPrefs.GetString(UnityEditor.SessionState.GetString("StartupSpeed.UserLevelKey", ""), "<absent>") == UnityEditor.SessionState.GetString("StartupSpeed.UserLevel", ""), "User level save changed.");
Require(UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>(UnityEngine.FindObjectsSortMode.None).All(t => UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0), "Missing runtime script.");
System.IO.File.AppendAllText("IntegrationEvidence/StartupLoadingSpeed/sorting-result.txt", "PASS: one reload restores all 1200 copy IDs, variant IDs and states; 1199 world cars plus the same sorted copy in its exact shelf slot; production level-save value unchanged; no missing runtime scripts.\n");
return "Saved progress and population restoration passed; production level save unchanged.";
