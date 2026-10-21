
if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop play mode before applying branding.");
if (Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)) throw new Exception("Unsaved scene changes; preserve these before applying branding.");
System.Action apply = () => {
    var setup = UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
    var log = new System.Text.StringBuilder();
    try {
        ToyCarSortBranding.ConfigureLogo();
        PlayerSettings.productName = "Toy Car Sort";
        LocalizationCsvTool.Import(System.IO.File.ReadAllText("Localization.csv"));
        Localizer.RefreshDatabase();
        foreach (var path in new[]{StoreLoadingScreenBuilder.PrefabPath,"Assets/00_Project/02_prefabs/Systems/Upgrade Box.prefab"}) {
            var root = PrefabUtility.LoadPrefabContents(path);
            try {
                if(path==StoreLoadingScreenBuilder.PrefabPath) ToyCarSortBranding.ApplyLoading(root);
                foreach(var t in root.GetComponentsInChildren<TMPro.TMP_Text>(true)) {
                    if(System.Text.RegularExpressions.Regex.IsMatch(t.text ?? "",@"TINY\s+RIDERS",System.Text.RegularExpressions.RegexOptions.IgnoreCase)) t.text=System.Text.RegularExpressions.Regex.Replace(t.text,@"TINY\s+RIDERS","TOY CAR SORT",System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    var localized=t.GetComponent<LocalizedText>();
                    if(localized && localized.Key=="sign.series") t.text="TOY CAR SORT  /  MK.02";
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);
                log.AppendLine("Updated prefab: "+path);
            } finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        foreach(var path in EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path)) {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,UnityEditor.SceneManagement.OpenSceneMode.Single);
            int changed=0;
            foreach(var root in scene.GetRootGameObjects()) {
                if(root.name=="Main Menu Canvas") { ToyCarSortBranding.ApplyMenu(root); changed++; }
                foreach(var t in root.GetComponentsInChildren<TMPro.TMP_Text>(true)) {
                    if(!System.Text.RegularExpressions.Regex.IsMatch(t.text ?? "",@"TINY\s+RIDERS",System.Text.RegularExpressions.RegexOptions.IgnoreCase)) continue;
                    t.text=System.Text.RegularExpressions.Regex.Replace(t.text,@"TINY\s+RIDERS","TOY CAR SORT",System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if(PrefabUtility.IsPartOfPrefabInstance(t)) PrefabUtility.RecordPrefabInstancePropertyModifications(t);
                    EditorUtility.SetDirty(t); changed++;
                }
            }
            if(changed>0) { UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene); }
            log.AppendLine(path+": "+changed+" direct changes; shared prefab branding inherited.");
        }
        AssetDatabase.SaveAssets();
        log.AppendLine("PASS: branding applied.");
    } catch(Exception e) { log.AppendLine("FAIL: "+e); Debug.LogException(e); }
    finally { UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(setup); System.IO.File.WriteAllText("IntegrationEvidence/ToyCarSort/apply-result.txt",log.ToString()); }
};
apply();
return System.IO.File.ReadAllText("IntegrationEvidence/ToyCarSort/apply-result.txt");
