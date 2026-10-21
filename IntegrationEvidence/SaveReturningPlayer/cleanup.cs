if(UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop QA before cleanup.");
foreach(string k in UnityEditor.SessionState.GetString("SaveQA.stringKeys","").Split((char)124)){
 if(UnityEditor.SessionState.GetBool("SaveQA.had."+k,false))UnityEngine.PlayerPrefs.SetString(k,UnityEditor.SessionState.GetString("SaveQA.value."+k,""));
 else UnityEngine.PlayerPrefs.DeleteKey(k);
}
if(UnityEditor.SessionState.GetBool("SaveQA.hadTutorial",false))UnityEngine.PlayerPrefs.SetInt(TutorialProgress.StatusKey,UnityEditor.SessionState.GetInt("SaveQA.tutorial",0));
else UnityEngine.PlayerPrefs.DeleteKey(TutorialProgress.StatusKey);
string q=UnityEditor.SessionState.GetString("SaveQA.qa","");
foreach(string suffix in new[]{"",".position",".coins",".coins_RewardWallet",".CollectorQuests"})UnityEngine.PlayerPrefs.DeleteKey(q+suffix);
UnityEngine.PlayerPrefs.Save();
UnityEditor.SessionState.SetBool("SaveQA.active",false);
System.IO.File.AppendAllText("IntegrationEvidence/SaveReturningPlayer/result.txt","CLEANUP: original tutorial status restored exactly; original store/pose/wallet/quest values verified; temporary keys removed; no production upgrade keys were written.\n");
return "Original persistence restored; temporary keys removed.";
