using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using TinyRiders;
using UnityEngine;
public static class ExerciseShelves
{
    public static string Run()
    {
        new GameObject("Room Shelf Verification").AddComponent<CollectorShelfProbe>().Begin();return "Checking all 80 shelves";
    }
    public static string Upgrades()
    {
        var manager=UpgradeManager.Instance;var wallet=CoinsManager.Instance;int balance=wallet.Coins;var results=new JArray();
        foreach(var upgrade in manager.Upgrades)
        {
            string key="Upgrade_"+upgrade.SaveId;bool existed=PlayerPrefs.HasKey(key);int saved=PlayerPrefs.GetInt(key);int level=upgrade.Level;
            try
            {
                upgrade.SetLevel(0);int cost=upgrade.NextCost;wallet.Add(cost);int before=wallet.Coins;
                bool bought=manager.TryPurchase(upgrade);
                results.Add(new JObject{["upgrade"]=upgrade.SaveId,["passed"]=bought&&upgrade.Level==1&&wallet.Coins==before-cost});
            }
            finally
            {
                upgrade.SetLevel(level);if(existed)PlayerPrefs.SetInt(key,saved);else PlayerPrefs.DeleteKey(key);
                if(wallet.Coins>balance)wallet.Remove(wallet.Coins-balance);else wallet.Add(balance-wallet.Coins);
            }
        }
        PlayerPrefs.Save();string json=results.ToString();File.WriteAllText("IntegrationEvidence/CollectorRoom/upgrades.json",json);return json;
    }
}
public class CollectorShelfProbe:MonoBehaviour
{
    public void Begin(){StartCoroutine(Check());}
    IEnumerator Check()
    {
        var s=ThemedCollectionSession.Instance;var player=Player.Instance;var result=new JArray();bool passed=true;
        var shelves=UnityEngine.Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None).OrderBy(v=>v.SaveId).ToArray();
        foreach(var shelf in shelves)
        {
            string theme=shelf.GetComponent<ThemeShelfBinding>().themeId;
            var world=WorldItemBase.All.First(w=>s.Definition(w.Data)?.themeId==theme);string id=ThemedWorldInstance.Read(world);
            var original=world.transform.position;var rotation=world.transform.rotation;
            player.Motor.SetPosition(shelf.transform.position+shelf.transform.forward*1.35f+Vector3.up*.05f);player.Look.SetLook(shelf.transform.eulerAngles.y+180,10);
            bool collected=player.ItemCollector.TryCollectItem(world);yield return new WaitForSecondsRealtime(.15f);
            bool placed=collected&&player.SortingBehaviour.TryGetHeldSortable(out var item)&&shelf.CanAccept(item,0,out _);
            if(placed&&player.SortingBehaviour.TryGetPlacedItemFromHand(out var model))shelf.AddItem(model,0);else placed=false;
            yield return new WaitForSecondsRealtime(.4f);
            bool taken=placed&&shelf.TryTakeBack(shelf.GetOccupant(0));yield return new WaitForSecondsRealtime(.15f);
            if(taken)
            {
                typeof(ItemsHandler).GetMethod("ExecuteThrow",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(player.ItemHandler,new object[]{0f});
                yield return new WaitForSecondsRealtime(.1f);
                var returned=WorldItemBase.All.First(w=>ThemedWorldInstance.Read(w)==id);
                returned.transform.SetPositionAndRotation(original,rotation);
                if(returned.Body!=null){returned.Body.isKinematic=true;returned.Body.position=original;returned.Body.rotation=rotation;}
                typeof(WorldItemBase).GetProperty("RestPosition",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(returned,original);
            }
            bool ok=collected&&placed&&taken;passed&=ok;result.Add(new JObject{["shelf"]=shelf.SaveId,["passed"]=ok});
            File.WriteAllText("IntegrationEvidence/CollectorRoom/shelf_progress.json",result.ToString());if(!ok)break;
        }
        File.WriteAllText("IntegrationEvidence/CollectorRoom/shelf_interactions.json",new JObject{["passed"]=passed&&result.Count==80,["shelves"]=result,["sortedAfterTakeBack"]=s.Sorted,["liveCars"]=s.LivePileCount}.ToString());
        player.Motor.SetPosition(s.roomLayout.spawn);player.Look.SetLook(0,0);Destroy(gameObject);
    }
}
