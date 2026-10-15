using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using TinyRiders;
using UnityEngine;
public static class CrowdedAccessAudit
{
    public static string Run()
    {
        var s=ThemedCollectionSession.Instance;var l=s.roomLayout;var player=Player.Instance;
        var cars=WorldItemBase.All.Where(w=>w.gameObject.activeInHierarchy).ToArray();
        var shelves=UnityEngine.Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None);
        bool Free(Vector3 p,float radius=.3f)
        {
            return !Physics.OverlapCapsule(p+Vector3.up*.65f,p+Vector3.up*1.15f,radius,1,QueryTriggerInteraction.Ignore).Any(c=>c.name!="Store Floor"&&c.GetComponentInParent<Player>()==null);
        }
        // Flood-fill positions the current capsule can stand at, disregarding loose cars only.
        var nodes=new Dictionary<Vector2Int,Vector3>();
        for(int x=-23;x<=23;x++)for(int z=-25;z<=25;z++)
        {var p=l.origin+new Vector3(x*.5f,.05f,z*.5f);if(Free(p))nodes[new Vector2Int(x,z)]=p;}
        var start=nodes.OrderBy(p=>Vector3.Distance(p.Value,l.spawn)).First().Key;
        var visited=new HashSet<Vector2Int>{start};var queue=new Queue<Vector2Int>();queue.Enqueue(start);
        while(queue.Count>0)
        {
            var key=queue.Dequeue();foreach(var delta in new[]{Vector2Int.left,Vector2Int.right,Vector2Int.up,Vector2Int.down})
            {var next=key+delta;if(nodes.ContainsKey(next)&&visited.Add(next))queue.Enqueue(next);}
        }
        var shelfFailures=new JArray();
        foreach(var shelf in shelves)
        {
            var stand=shelf.transform.position+shelf.transform.forward*1.1f+Vector3.up*.05f;
            if(!Free(stand,.6f)||!visited.Any(key=>Vector3.Distance(nodes[key],stand)<.8f))shelfFailures.Add(shelf.SaveId+": approach");
            var eye=stand+Vector3.up*1.45f;
            for(int slot=0;slot<shelf.SpotCount;slot++)if(Vector3.Distance(eye,shelf.GetSpot(slot).position)>3)shelfFailures.Add(shelf.SaveId+":"+slot);
        }
        var originals=cars.SelectMany(w=>w.GetComponentsInChildren<Collider>()).ToDictionary(c=>c,c=>c.enabled);
        var remaining=new HashSet<WorldItemBase>(cars);int passes=0;int collisionFailures=0;
        var controller=player.Motor.CharacterController;
        foreach(var col in originals.Keys)if(controller.IsColliderValidForCollisions(col)||!Physics.GetIgnoreCollision(player.Motor.Capsule,col))collisionFailures++;
        try
        {
            while(remaining.Count>0&&passes++<10)
            {
                int before=remaining.Count;
                foreach(var car in remaining.ToArray())
                {
                    var targets=car.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger).Select(c=>c.bounds.center).ToArray();
                    var options=visited.Select(key=>nodes[key]).Where(p=>Vector2.Distance(new Vector2(p.x,p.z),new Vector2(car.transform.position.x,car.transform.position.z))<2.1f).OrderBy(p=>(p-car.transform.position).sqrMagnitude).ToArray();
                    bool reachable=false;
                    foreach(var stand in options)
                    {
                        var eye=stand+Vector3.up*1.45f;
                        foreach(var target in targets)
                        {
                            var ray=target-eye;if(ray.magnitude>3)continue;
                            if(Physics.Raycast(eye,ray.normalized,out var hit,3,(1<<8)|1,QueryTriggerInteraction.Collide)&&hit.collider.GetComponentInParent<WorldItemBase>()==car){reachable=true;break;}
                        }
                        if(reachable)break;
                    }
                    if(reachable){foreach(var col in car.GetComponentsInChildren<Collider>())col.enabled=false;remaining.Remove(car);}
                }
                if(remaining.Count==before)break;
            }
        }
        finally{foreach(var pair in originals)if(pair.Key!=null)pair.Key.enabled=pair.Value;Physics.SyncTransforms();}
        var heights=cars.Select(w=>{var a=w.GetComponentsInChildren<Renderer>();var b=a[0].bounds;foreach(var r in a)b.Encapsulate(r.bounds);return b.max.y;}).ToArray();
        bool passed=cars.Length==1200&&remaining.Count==0&&shelfFailures.Count==0&&collisionFailures==0;
        var result=new JObject{["passed"]=passed,["cars"]=cars.Length,["connectedFloorNodes"]=visited.Count,["totalStandableNodes"]=nodes.Count,["shelfFailures"]=shelfFailures,["reachableCars"]=1200-remaining.Count,["unreachableIds"]=new JArray(remaining.Select(w=>ThemedWorldInstance.Read(w))),["pickupPasses"]=passes,["looseCarCollisionFailures"]=collisionFailures,["maxArtHeight"]=heights.Max(),["carsBelow08"]=heights.Count(h=>h<=.8f),["frame"]=Time.frameCount};
        File.WriteAllText("IntegrationEvidence/CrowdedRoom/access_audit.json",result.ToString());return result.ToString();
    }
}

