using System;
using System.Linq;
using TinyRiders;
using UnityEngine;
public static class CrowdedViews
{
 public static string Entrance(){CoinsManager.Instance.Remove(CoinsManager.Instance.Coins);Player.Instance.Motor.SetPosition(ThemedCollectionSession.Instance.roomLayout.spawn);Player.Instance.Look.SetLook(0,12);return "Entrance";}
 public static string Floor(){Player.Instance.Motor.SetPosition(new Vector3(500,.05f,-7.8f));Player.Instance.Look.SetLook(20,32);return "Dense floor view";}
 public static string Approach(){var shelf=UnityEngine.Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None).Single(s=>s.SaveId=="COMPACT-TH_PHOTOGRAPHY-S001");Player.Instance.Motor.SetPosition(shelf.transform.position+shelf.transform.forward*1.65f+Vector3.up*.05f);Player.Instance.Look.SetLook(shelf.transform.eulerAngles.y+180,18);return "Shelf approach";}
 public static string Overhead(){GameObject.Find("Store Ceiling").GetComponent<Renderer>().enabled=false;var camera=new GameObject("Crowded Overview Camera").AddComponent<Camera>();camera.CopyFrom(Camera.main);camera.enabled=false;camera.transform.SetPositionAndRotation(new Vector3(500,30,0),Quaternion.Euler(90,0,0));camera.orthographic=true;camera.orthographicSize=14.5f;camera.farClipPlane=100;return "Overview";}
 public static string Fill(){GameObject.Find("Store Ceiling").GetComponent<Renderer>().enabled=true;var s=ThemedCollectionSession.Instance;var shelf=UnityEngine.Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None).Single(v=>v.SaveId=="COMPACT-TH_PHOTOGRAPHY-S001");for(int i=0;i<18;i++){var w=WorldItemBase.All.First(w=>s.Definition(w.Data)?.themeId=="TH_PHOTOGRAPHY");if(!Player.Instance.ItemCollector.TryCollectItem(w)||!Player.Instance.SortingBehaviour.TryGetPlacedItemFromHand(out var placed))throw new Exception("Fill failed");shelf.AddItem(placed,i);}return Approach();}
}
