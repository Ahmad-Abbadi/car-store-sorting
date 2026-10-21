var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var save=UnityEngine.Object.FindFirstObjectByType<LevelSaveManager>();
var position=UnityEngine.Object.FindFirstObjectByType<PlayerSavePosition>();
var coins=UnityEngine.Object.FindFirstObjectByType<CoinsManager>();
string[] stringKeys={save.StorageKey,(string)typeof(PlayerSavePosition).GetField("saveKey",flags).GetValue(position),(string)typeof(CoinsManager).GetField("saveKey",flags).GetValue(coins)+"_RewardWallet",save.StorageKey+".CollectorQuests"};
foreach(string key in stringKeys){UnityEditor.SessionState.SetBool("SaveQA.had."+key,UnityEngine.PlayerPrefs.HasKey(key));UnityEditor.SessionState.SetString("SaveQA.value."+key,UnityEngine.PlayerPrefs.GetString(key,""));}
UnityEditor.SessionState.SetString("SaveQA.stringKeys",string.Join("|",stringKeys));
UnityEditor.SessionState.SetBool("SaveQA.hadTutorial",UnityEngine.PlayerPrefs.HasKey(TutorialProgress.StatusKey));
UnityEditor.SessionState.SetInt("SaveQA.tutorial",UnityEngine.PlayerPrefs.GetInt(TutorialProgress.StatusKey));
UnityEngine.PlayerPrefs.DeleteKey(TutorialProgress.StatusKey);
string qa="SaveReturning.QA.20261021";
typeof(LevelSaveManager).GetField("saveKey",flags).SetValue(save,qa);
typeof(PlayerSavePosition).GetField("saveKey",flags).SetValue(position,qa+".position");
typeof(CoinsManager).GetField("saveKey",flags).SetValue(coins,qa+".coins");
foreach(var probe in UnityEngine.Object.FindObjectsByType<TinyRiders.ThemedPerformanceProbe>(UnityEngine.FindObjectsSortMode.None))probe.enabled=false;
UnityEditor.SessionState.SetString("SaveQA.qa",qa);
UnityEditor.SessionState.SetBool("SaveQA.active",true);
return "Fresh QA keys isolated in unsaved scene; original store, pose, wallet, quests and tutorial state backed up.";
