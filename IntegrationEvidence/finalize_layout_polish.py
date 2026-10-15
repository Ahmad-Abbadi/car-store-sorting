from pathlib import Path
p=Path('Assets/TinyRiders/Gameplay/Editor/FinishThemedScene.cs')
s=p.read_text(encoding='utf-8')
needle=' var shelfCountPlaceholderNeverUsed=0;'
insert='''
 ThemedSortingAuthoringSettings.Load();
 var ordered=session.catalog.themes.Where(t=>t.enabled).SelectMany(t=>UnityEngine.Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None).Where(s=>s.GetComponent<ThemeShelfBinding>()?.themeId==t.id).OrderBy(s=>s.SaveId,StringComparer.Ordinal)).ToArray();
 int perSide=Mathf.Max(1,Mathf.CeilToInt(ordered.Length/4f));float radius=Mathf.Max(250,(perSide-1)*17f+34f);
 for(int i=0;i<ordered.Length;i++){
  int side=i/perSide;int col=i%perSide;float along=(col-(perSide-1)*.5f)*34f;Vector3 pos;float yaw;
  if(side==0){pos=new Vector3(500+along,0,radius);yaw=180;}else if(side==1){pos=new Vector3(500+radius,0,-along);yaw=-90;}else if(side==2){pos=new Vector3(500-along,0,-radius);yaw=0;}else{pos=new Vector3(500-radius,0,along);yaw=90;}
  ordered[i].transform.SetPositionAndRotation(pos,Quaternion.Euler(0,yaw,0));var so=new SerializedObject(ordered[i]);so.FindProperty("jumpPower").floatValue=.07f;so.FindProperty("fullPunchScale").vector3Value=Vector3.one*.003f;so.ApplyModifiedPropertiesWithoutUndo();
 }
 var room=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Fixed Room Calibration");if(room!=null)room.transform.localScale=Vector3.one*Mathf.Max(14,(radius+24)/20f);
'''
s=s.replace(' Directory.CreateDirectory("Assets/TinyRiders/Gameplay/Icons");',insert+' Directory.CreateDirectory("Assets/TinyRiders/Gameplay/Icons");')
p.write_text(s,encoding='utf-8')
p=Path('Assets/TinyRiders/Gameplay/Editor/ThemedSortingBuilder.cs');s=p.read_text(encoding='utf-8').replace(' public static void Build()',' [MenuItem("Tools/Tiny Riders/Sorting/Build Themed Showroom")]\n public static void Build()');p.write_text(s,encoding='utf-8')
