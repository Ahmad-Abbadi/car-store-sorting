var lines=new System.Collections.Generic.List<string>();
var tests=new PlayfulAudioTests();
tests.GameplayLibrary_HasVariationsAndCompleteUncutSounds();lines.Add("PASS replacement assignments, UI click, pitch and full cutoffs");
tests.PlayableSceneDependencies_UseOnlyTheSelectedPackageAudio();lines.Add("PASS all playable scene audio dependencies use replacement library");
tests.ReplacementImports_PreserveRatesAndUseMonoEffectsStereoMusic();lines.Add("PASS all 23 imports: original rates, mono PCM effects and stereo music");
tests.FrequentActions_DoNotRepeatThePreviousVariation();lines.Add("PASS no consecutive pickup/throw/placement repeats; refusal distinct and quieter");
var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();int total=0;
foreach(var entry in UnityEditor.EditorBuildSettings.scenes.Where(s=>s.enabled))
{
    var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(entry.path);bool opened=!scene.IsValid()||!scene.isLoaded;
    if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(entry.path,UnityEditor.SceneManagement.OpenSceneMode.Additive);
    try
    {
        var buttons=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.UI.Button>(true)).ToArray();
        foreach(var button in buttons)if(!button.GetComponent<UIButtonAudioFeedback>())throw new System.Exception("Button missing click audio: "+entry.path+"/"+button.name);
        total+=buttons.Length;lines.Add("PASS "+scene.name+": "+buttons.Length+" buttons including inactive panels");
    }
    finally{if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
}
UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);
lines.Add("INFO "+total+" scene button instances covered; UI factories rebind shared component after resetting callbacks");
UnityEditor.SessionState.SetBool("ActionAudio_OldSaveDuringPlay",Unity.Cinemachine.Editor.SaveDuringPlay.Enabled);
Unity.Cinemachine.Editor.SaveDuringPlay.Enabled=false;
System.IO.File.WriteAllLines("IntegrationEvidence/AudioPackageRefresh/action-asset-checks.txt",lines);
return string.Join("\n",lines);
