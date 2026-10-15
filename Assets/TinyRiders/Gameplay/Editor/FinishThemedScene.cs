using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TinyRiders;
public static class FinishThemedScene {
 public static void Run() {
 var scene=SceneManager.GetActiveScene();var session=UnityEngine.Object.FindFirstObjectByType<ThemedCollectionSession>();
 if(session==null)throw new Exception("Open themed scene first");if(session.GetComponent<ThemedVisibility>()==null)session.gameObject.AddComponent<ThemedVisibility>();
 session.pileLayout=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/TinyRiders/Gameplay/Data/PileLayout.json");
  if(session.GetComponent<ThemedPerformanceProbe>()==null)session.gameObject.AddComponent<ThemedPerformanceProbe>();
 foreach(var shelf in UnityEngine.Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None)){
  var header=shelf.transform.Find("Replaceable Theme Header");if(header==null)continue;
  var binding=header.GetComponent<ThemeShelfHeader>();if(binding==null)binding=header.gameObject.AddComponent<ThemeShelfHeader>();binding.themeId=shelf.GetComponent<ThemeShelfBinding>().themeId;binding.label=header.GetComponent<TMPro.TMP_Text>();var theme=session.catalog.themes.First(t=>t.id==binding.themeId);binding.label.color=Color.Lerp(theme.color,new Color(.025f,.035f,.055f),.8f);
 }

 ThemedSortingAuthoringSettings.Load();
 ThemedPlayerCalibration.Apply(UnityEngine.Object.FindFirstObjectByType<Player>(),session.worldScale);
 foreach(var e in session.catalog.entries.Where(e=>e.sortingItem!=null)){e.sortingItem.CameraFixed=true;e.sortingItem.HeldPosition=new Vector3(1.6f,-2.1f,7.8f);EditorUtility.SetDirty(e.sortingItem);}
 ThemedPickingBounds.Apply(session.catalog);
 foreach(var interaction in UnityEngine.Object.FindObjectsByType<InteractionHandler>(FindObjectsSortMode.None)){var so=new SerializedObject(interaction);so.FindProperty("triggerInteraction").enumValueIndex=(int)QueryTriggerInteraction.Collide;so.ApplyModifiedPropertiesWithoutUndo();}
 var ordered=session.catalog.themes.Where(t=>t.enabled).SelectMany(t=>UnityEngine.Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None).Where(s=>s.GetComponent<ThemeShelfBinding>()?.themeId==t.id).OrderBy(s=>s.SaveId,StringComparer.Ordinal)).ToArray();
 int perSide=Mathf.Max(1,Mathf.CeilToInt(ordered.Length/4f));float radius=Mathf.Max(250,(perSide-1)*17f+34f);
 for(int i=0;i<ordered.Length;i++){
  int side=i/perSide;int col=i%perSide;float along=(col-(perSide-1)*.5f)*34f;Vector3 pos;float yaw;
  if(side==0){pos=new Vector3(500+along,0,radius);yaw=180;}else if(side==1){pos=new Vector3(500+radius,0,-along);yaw=-90;}else if(side==2){pos=new Vector3(500-along,0,-radius);yaw=0;}else{pos=new Vector3(500-radius,0,along);yaw=90;}
  ordered[i].transform.SetPositionAndRotation(pos,Quaternion.Euler(0,yaw,0));var so=new SerializedObject(ordered[i]);so.FindProperty("jumpPower").floatValue=.07f;so.FindProperty("fullPunchScale").vector3Value=Vector3.one*.003f;so.ApplyModifiedPropertiesWithoutUndo();
 }
 ThemedShelfBatcher.Apply(ordered);
 var room=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Fixed Room Calibration");if(room!=null)room.transform.localScale=Vector3.one*Mathf.Max(14,(radius+24)/20f);
 Directory.CreateDirectory("Assets/TinyRiders/Gameplay/Icons");AssetDatabase.Refresh();
 int icons=0;foreach(var e in session.catalog.entries){if(e.sortingItem==null||e.preview==null||e.sortingItem.ItemImage!=null)continue;var path="Assets/TinyRiders/Gameplay/Icons/"+e.collectibleId+".asset";var icon=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(icon==null){icon=Sprite.Create(e.preview,new Rect(0,0,e.preview.width,e.preview.height),new Vector2(.5f,.5f),100);icon.name=e.collectibleId;AssetDatabase.CreateAsset(icon,path);}e.sortingItem.ItemImage=icon;EditorUtility.SetDirty(e.sortingItem);icons++;}
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 var menu=EditorSceneManager.OpenScene("Assets/00_Project/00_Scenes/MainMenu Scene.unity",OpenSceneMode.Additive);foreach(var m in menu.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MainMenu>(true))){var so=new SerializedObject(m);so.FindProperty("levelScene").stringValue="ThemedSorting";so.ApplyModifiedPropertiesWithoutUndo();}EditorSceneManager.MarkSceneDirty(menu);EditorSceneManager.SaveScene(menu);EditorSceneManager.CloseScene(menu,true);SceneManager.SetActiveScene(scene);AssetDatabase.SaveAssets();Debug.Log("THEMED_SCENE_FINISHED icons="+icons);
 }
}





