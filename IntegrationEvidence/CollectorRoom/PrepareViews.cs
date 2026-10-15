using System;
using System.Linq;
using TinyRiders;
using UnityEngine;
public static class PrepareViews
{
    public static string Entrance()
    {
        CoinsManager.Instance.Remove(CoinsManager.Instance.Coins);
        Player.Instance.Motor.SetPosition(ThemedCollectionSession.Instance.roomLayout.spawn);Player.Instance.Look.SetLook(0,0);return "Entrance";
    }
    public static string Overhead()
    {
        GameObject.Find("Store Ceiling").GetComponent<Renderer>().enabled=false;
        var camera=new GameObject("Collector Overview Camera").AddComponent<Camera>();camera.CopyFrom(Camera.main);camera.enabled=false;
        camera.transform.SetPositionAndRotation(new Vector3(500,45,0),Quaternion.Euler(90,0,0));camera.orthographic=true;camera.orthographicSize=24;camera.farClipPlane=100;return "Overhead camera ready";
    }
    public static string FillShelf()
    {
        GameObject.Find("Store Ceiling").GetComponent<Renderer>().enabled=true;
        var session=ThemedCollectionSession.Instance;
        var shelf=UnityEngine.Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None).Single(s=>s.SaveId=="COMPACT-TH_FARM-S002");
        for(int i=0;i<18;i++)
        {
            var world=WorldItemBase.All.First(w=>session.Definition(w.Data)?.themeId=="TH_FARM");
            if(!Player.Instance.ItemCollector.TryCollectItem(world)||!Player.Instance.SortingBehaviour.TryGetPlacedItemFromHand(out var placed))throw new Exception("Could not fill display");
            shelf.AddItem(placed,i);
        }
        Player.Instance.Motor.SetPosition(new Vector3(493.5f,.05f,-17.47f));Player.Instance.Look.SetLook(-90,16);return "18 cars sorted into Country Patch display";
    }
}
