UnityEngine.Physics.simulationMode=(UnityEngine.SimulationMode)UnityEditor.SessionState.GetInt("ToyCarSort.Physics",0);
if(UnityEditor.SessionState.GetBool("ToyCarSort.HadLanguage",false)) UnityEngine.PlayerPrefs.SetString("Language",UnityEditor.SessionState.GetString("ToyCarSort.Language","en"));
else UnityEngine.PlayerPrefs.DeleteKey("Language");
if(UnityEditor.SessionState.GetBool("ToyCarSort.HadTutorial",false)) UnityEngine.PlayerPrefs.SetInt(TutorialProgress.StatusKey,UnityEditor.SessionState.GetInt("ToyCarSort.Tutorial",0));
else UnityEngine.PlayerPrefs.DeleteKey(TutorialProgress.StatusKey);
UnityEditor.SessionState.SetBool("TinyRiders.Compact.QA.Active",UnityEditor.SessionState.GetBool("ToyCarSort.QA",false));
UnityEngine.PlayerPrefs.Save();
if(UnityEditor.SessionState.GetBool("CinematicMenu.SizeSaved",false)) {
    var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public;
    var type=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
    type.GetProperty("selectedSizeIndex",flags).SetValue(UnityEditor.EditorWindow.GetWindow(type),UnityEditor.SessionState.GetInt("CinematicMenu.PreviousSize",0));
    var sizesType=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSizes");
    var sizes=typeof(UnityEditor.ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
    var groupType=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSizeGroupType");
    var group=sizesType.GetMethod("GetGroup").Invoke(sizes,new[]{System.Enum.Parse(groupType,"Standalone")});
    int count=(int)group.GetType().GetMethod("GetCustomCount").Invoke(group,null);
    for(int i=count-1;i>=UnityEditor.SessionState.GetInt("CinematicMenu.PreviousCustomCount",count);i--) group.GetType().GetMethod("RemoveCustomSize").Invoke(group,new object[]{i});
    UnityEditor.SessionState.SetBool("CinematicMenu.SizeSaved",false);
}
return "Restored language, tutorial, lighting/detail preferences, physics and Game view settings.";
