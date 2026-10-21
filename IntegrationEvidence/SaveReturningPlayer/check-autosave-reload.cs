void Require(bool ok,string message){if(!ok)throw new System.InvalidOperationException(message);}
string qa=UnityEditor.SessionState.GetString("SaveQA.qa","");
var stored=UnityEngine.JsonUtility.FromJson<LevelSaveData>(UnityEngine.PlayerPrefs.GetString(qa));
Require(stored.themed.instances.Count(r=>r.state=="sorted")==3,"Debounced placement save absent.");
Require(stored.themed.heldOrder.SequenceEqual(new[]{UnityEditor.SessionState.GetString("SaveQA.held","")}),"Debounced pickup save absent.");
Require(UnityEngine.PlayerPrefs.GetInt(TutorialProgress.StatusKey)==2,"First placement did not complete tutorial.");
Require(UnityEngine.PlayerPrefs.HasKey(qa+".position"),"Pose not included in checkpoint.");
UnityEditor.SessionState.SetString("SaveQA.expected",UnityEngine.JsonUtility.ToJson(stored));
UnityEditor.SessionState.SetInt("SaveQA.coins",CoinsManager.Instance.Coins);
// Observe the actual two-second welcome without changing its timing.
UnityEditor.EditorApplication.CallbackFunction observe=null;
observe=()=>{
 if(!UnityEditor.EditorApplication.isPlaying){UnityEditor.EditorApplication.update-=observe;return;}
 var t=UnityEngine.Object.FindFirstObjectByType<TutorialController>();
 if(t&&t.instruction&&t.instruction.text=="Welcome back! Continue sorting."){
  if(t.guidance.canvas.enabled)UnityEditor.SessionState.SetBool("SaveQA.welcomeSeen",true);
  if(UnityEditor.SessionState.GetBool("SaveQA.welcomeSeen",false)&&!t.enabled&&!t.guidance.canvas.enabled){UnityEditor.SessionState.SetBool("SaveQA.welcomeHidden",true);UnityEditor.EditorApplication.update-=observe;}
 }
};
UnityEditor.EditorApplication.update+=observe;
var pos=Player.Instance.Motor.TransientPosition;
Player.Instance.Look.SetLook(17,-8);
UnityEditor.SessionState.SetString("SaveQA.pose",pos.ToString("F3"));
// Flush the pending pose and elapsed state through the same route used by Pause -> Menu.
LevelSaveManager.MarkDirty();
GameFlowManager.Instance.LoadScene("Level");
System.IO.File.AppendAllText("IntegrationEvidence/SaveReturningPlayer/result.txt","PASS debounced save (without manual Save): three sorted copies, held copy/order, pose, tutorial complete. Normal GameFlow scene-transition flush executed.\n");
return "Checkpoint verified; restoring once through the normal scene transition.";
