void Require(bool ok,string m){if(!ok)throw new System.InvalidOperationException(m);}
var session=TinyRiders.ThemedCollectionSession.Instance;
var save=UnityEngine.Object.FindFirstObjectByType<LevelSaveManager>();
var reward=UnityEngine.Object.FindFirstObjectByType<TinyRiders.GoldenCarReveal>();
Require(session.IsReady&&session.Total==1200&&session.LivePileCount==1200,"Invalid-data startup stalled.");
Require(!save.HasRestoredProgress&&!save.StoreCompleted&&!reward.IsRevealed,"Invalid payload unlocked reward or counted as restored.");
LevelSaveManager.MarkDirty();save.Save();
Require(UnityEngine.PlayerPrefs.GetString(save.StorageKey)==UnityEditor.SessionState.GetString("SaveQA.bad",""),"Original invalid payload was overwritten.");
var p=UnityEngine.Object.FindFirstObjectByType<PlayerSavePosition>();
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
string key=(string)typeof(PlayerSavePosition).GetField("saveKey",flags).GetValue(p);
UnityEngine.PlayerPrefs.SetString(key,"{broken");
typeof(PlayerSavePosition).GetMethod("Load",flags).Invoke(p,null);p.Save();
Require(UnityEngine.PlayerPrefs.GetString(key)=="{broken","Invalid pose was deleted or overwritten.");
Require(UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>(UnityEngine.FindObjectsSortMode.None).All(t=>UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0),"Missing runtime script.");
Require(session.ActiveDefinitions.All(e=>e.sortingItem.WorldItem&&e.sortingItem.HandItem&&e.sortingItem.PlacedOnShelfPrefab),"Missing prefab.");
foreach(string k in UnityEditor.SessionState.GetString("SaveQA.stringKeys","").Split((char)124)){
 Require(UnityEngine.PlayerPrefs.HasKey(k)==UnityEditor.SessionState.GetBool("SaveQA.had."+k,false),"Original key presence changed: "+k);
 if(UnityEngine.PlayerPrefs.HasKey(k))Require(UnityEngine.PlayerPrefs.GetString(k)==UnityEditor.SessionState.GetString("SaveQA.value."+k,""),"Original value changed: "+k);
}
System.IO.File.AppendAllText("IntegrationEvidence/SaveReturningPlayer/result.txt","PASS invalid startup: no stall, no false golden unlock, no welcome; missing payload and malformed pose retained without overwrite. All production store/pose/wallet/quest values match backups; no missing runtime scripts or required car prefabs.\n");
return "Invalid-data retention and original-data checks passed.";
