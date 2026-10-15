using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using TinyRiders;
using Newtonsoft.Json.Linq;
public static class PreviewCompleteCollection
{
 public static string Prepare(){SessionState.SetBool("TinyRiders.QA.Active",true);return "Temporary QA save isolation enabled";}
 public static string Inspect()
 {
  var session=ThemedCollectionSession.Instance;
  if(session==null)throw new Exception("No running collection session");
  var cars=WorldItemBase.All.Where(w=>w!=null&&w.gameObject.activeInHierarchy).Select(w=>w.GetComponentInChildren<ThemedCarIdentity>()).Where(i=>i!=null).ToArray();
  var report=new JObject{["frame"]=Time.frameCount,["designs"]=session.ActiveDefinitions.Count(),["physicalCars"]=session.Total,["liveCars"]=session.LivePileCount,["liveThemes"]=cars.Select(c=>c.themeId).Distinct().Count(),["liveShapes"]=cars.Select(c=>c.baseCarId).Distinct().Count(),["liveUniqueDesigns"]=cars.Select(c=>c.collectibleId).Distinct().Count(),["capacity"]=session.Capacity};
  File.WriteAllText("IntegrationEvidence/complete_collection_live.json",report.ToString());return report.ToString();
 }
 public static string Finish(){if(ThemedCollectionSession.Instance!=null)ThemedCollectionSession.Instance.suspendPersistenceForTests=true;SessionState.SetBool("TinyRiders.QA.Active",false);return "Preview stopped saving";}
 public static string Cleanup(){PlayerPrefs.DeleteKey("TinyRiders.Integration.QA.Temp");PlayerPrefs.DeleteKey("TinyRiders.Integration.QA.Temp.position");PlayerPrefs.DeleteKey("TinyRiders.Integration.QA.Temp.coins");return "Temporary preview saves removed";}
}
