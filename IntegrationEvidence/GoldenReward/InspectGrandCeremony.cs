using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using TinyRiders;

public static class InspectGrandCeremony
{
    public static object Main()
    {
        var reward=UnityEngine.Object.FindFirstObjectByType<GoldenCarReveal>();
        var ceremony=reward.ceremony;
        Bounds MeshBounds(Transform root)
        {
            var points=root.GetComponentsInChildren<MeshFilter>(true).SelectMany(f=>
            {
                var b=f.sharedMesh.bounds;
                return Enumerable.Range(0,8).Select(i=>f.transform.TransformPoint(new Vector3(
                    (i&1)==0?b.min.x:b.max.x,(i&2)==0?b.min.y:b.max.y,(i&4)==0?b.min.z:b.max.z)));
            }).ToArray();
            var bounds=new Bounds(points[0],Vector3.zero);foreach(var p in points)bounds.Encapsulate(p);return bounds;
        }
        var car=MeshBounds(reward.trophy.transform);
        var stand=MeshBounds(reward.transform.Find("Grand Exhibit Installation"));
        var baseBounds=reward.pedestal.GetComponent<Renderer>().bounds;
        var blockers=UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None)
            .Where(c=>c.enabled && !c.isTrigger && !c.transform.IsChildOf(reward.transform) &&
                c.GetComponentInParent<Player>()==null && c.GetComponentInParent<WorldItemBase>()==null &&
                c.bounds.max.y>.3f && c.bounds.min.y<2.3f).ToArray();
        float clearance=blockers.Select(c=>
        {
            var p=c.bounds.ClosestPoint(new Vector3(reward.transform.position.x,c.bounds.center.y,reward.transform.position.z));
            return Vector2.Distance(new Vector2(p.x,p.z),new Vector2(reward.transform.position.x,reward.transform.position.z))-1.53f;
        }).Min();
        int cameraCollisionSamples=0;
        for(int i=0;i<ceremony.shots.Length-1;i++)for(int s=0;s<=100;s++)
        {
            var p=Vector3.Lerp(ceremony.shots[i].viewpoint.position,ceremony.shots[i+1].viewpoint.position,s/100f);
            if(blockers.Any(c=>c.bounds.SqrDistance(p)<.12f*.12f))cameraCollisionSamples++;
        }
        return new {
            position=reward.transform.position.ToString(),duration=reward.duration,
            carSize=car.size.ToString("F5"),stationSize=stand.size.ToString("F5"),baseSize=baseBounds.size.ToString(),
            contactDelta=stand.min.y-baseBounds.max.y,minimumWalkway=clearance,cameraCollisionSamples,
            textCount=reward.GetComponentsInChildren<TMPro.TMP_Text>(true).Length,
            allPrefabsConnected=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(reward.trophy)==GoldenCarDisplayBuilder.CarPath,
            variants=ceremony.effects.Select(e=>new {e.effect.name,source=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(e.effect),
                systems=e.effect.GetComponentsInChildren<ParticleSystem>(true).Length,
                shaders=e.effect.GetComponentsInChildren<ParticleSystemRenderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Select(m=>m.shader.name).Distinct().ToArray()}).ToArray(),
            rootNames=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Select(g=>g.name).ToArray()
        };
    }
}
