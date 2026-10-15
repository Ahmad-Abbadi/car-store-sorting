using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using TinyRiders;
using UnityEngine;
public static class RoomGameplayAudit
{
    public static string Run()
    {
        var s=ThemedCollectionSession.Instance;
        if(s==null||!Application.isPlaying)throw new Exception("Run in isolated Play mode");
        var l=s.roomLayout;var cars=WorldItemBase.All.Where(w=>w.gameObject.activeInHierarchy).ToArray();
        var shelves=UnityEngine.Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None);
        var blocked=new List<string>();int samples=0;
        Action<float,float> walk=(x,z)=>
        {
            var p=l.origin+new Vector3(x,0,z);
            // A 1.8m diameter sweep verifies minimum clear route width, not just the 0.6m player.
            var hits=Physics.OverlapCapsule(p+Vector3.up*.94f,p+Vector3.up*1.0f,.9f,~(1<<2),QueryTriggerInteraction.Ignore)
                .Where(c=>c.GetComponentInParent<Player>()==null && c.gameObject.name!="Store Floor").ToArray();
            if(hits.Length>0)blocked.Add(x+","+z+":"+string.Join(",",hits.Select(c=>c.name).Distinct())); samples++;
        };
        Physics.SyncTransforms();
        for(float z=-21;z<=21;z+=.5f){walk(0,z);walk(-15.8f,z);walk(15.8f,z);}
        foreach(float z in new[]{-21f,-10.5f,0,10.5f,21f})for(float x=-15.5f;x<=15.5f;x+=.5f)walk(x,z);
        var shelfFailures=new List<string>();
        foreach(var shelf in shelves)
        {
            var eye=shelf.transform.position+shelf.transform.forward*1.35f+Vector3.up*1.5f;
            var stand=eye-l.origin-Vector3.up*1.5f;walk(stand.x,stand.z);
            for(int i=0;i<shelf.SpotCount;i++)if(Vector3.Distance(eye,shelf.GetSpot(i).position)>3f)shelfFailures.Add(shelf.SaveId+":"+i);
        }
        // Simulate progressively picking reachable stock by temporarily removing its pick colliders.
        // Ray origins stand in the two walkable strips beside each mat. No cars or records are removed.
        var originals=cars.SelectMany(c=>c.GetComponentsInChildren<Collider>()).ToDictionary(c=>c,c=>c.enabled);
        var remaining=new HashSet<WorldItemBase>(cars);int passes=0;
        try
        {
            while(remaining.Count>0&&passes++<8)
            {
                int before=remaining.Count;
                foreach(var car in remaining.ToArray())
                {
                    var target=car.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger).Select(c=>c.bounds.center).FirstOrDefault();
                    var p=car.transform.position-l.origin;
                    var area=l.sections.SelectMany(v=>v.sortingAreas).OrderBy(b=>(new Vector2(b.center.x-p.x,b.center.z-p.z)).sqrMagnitude).First();
                    foreach(float side in new[]{-1f,1f})
                    {
                        var eye=l.origin+new Vector3(area.center.x+side*2.1f,1.5f,p.z);
                        var direction=target-eye;
                        if(direction.magnitude>3)continue;
                        if(Physics.Raycast(eye,direction.normalized,out var hit,3,1<<8,QueryTriggerInteraction.Collide)&&hit.collider.GetComponentInParent<WorldItemBase>()==car)
                        {
                            foreach(var c in car.GetComponentsInChildren<Collider>())c.enabled=false;
                            remaining.Remove(car);break;
                        }
                    }
                }
                if(before==remaining.Count)break;
            }
        }
        finally{foreach(var pair in originals)if(pair.Key!=null)pair.Key.enabled=pair.Value;Physics.SyncTransforms();}
        var mix=new JArray();
        for(int i=0;i<8;i++)
        {
            var center=l.origin+l.sections[i].center;
            var sectionCars=cars.Where(c=>Mathf.Sign(c.transform.position.x-500)==Mathf.Sign(center.x-500)&&Mathf.Abs(c.transform.position.z-center.z)<4.6f).ToArray();
            mix.Add(new JObject{["section"]=i+1,["cars"]=sectionCars.Length,["themes"]=sectionCars.Select(c=>s.Definition(c.Data).themeId).Distinct().Count()});
        }
        var result=new JObject{["passed"]=blocked.Count==0&&remaining.Count==0&&shelfFailures.Count==0&&cars.Length==1200,["liveCars"]=cars.Length,["walkSamples"]=samples,["minimumClearWidth"]=1.8,["blockedWalkSamples"]=new JArray(blocked),["shelfSlotsBeyondReach"]=new JArray(shelfFailures),["reachableByProgressiveRaycast"]=cars.Length-remaining.Count,["unreachableCars"]=new JArray(remaining.Select(c=>ThemedWorldInstance.Read(c))),["pickupPasses"]=passes,["sections"]=mix,["frame"]=Time.frameCount};
        File.WriteAllText("IntegrationEvidence/CollectorRoom/gameplay_access_audit.json",result.ToString());return result.ToString();
    }
}
