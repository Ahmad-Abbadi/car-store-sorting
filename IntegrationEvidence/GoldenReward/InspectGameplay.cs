using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using TinyRiders;
using UnityEngine.Rendering.Universal;

public static class InspectGoldenGameplay
{
    static void Capture(Camera source, Vector3 eye, Vector3 target, float fov, string name)
    {
        var go=new GameObject("Golden Reward QA Camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(source);camera.enabled=false;
        if(source.TryGetComponent<UniversalAdditionalCameraData>(out var sourceData))
        {
            var data=go.AddComponent<UniversalAdditionalCameraData>();data.renderPostProcessing=sourceData.renderPostProcessing;data.volumeLayerMask=sourceData.volumeLayerMask;data.antialiasing=sourceData.antialiasing;
        }
        camera.transform.position=eye;camera.transform.LookAt(target);camera.fieldOfView=fov;
        var rt=new RenderTexture(1800,1200,24);camera.targetTexture=rt;var previous=RenderTexture.active;
        try
        {
            camera.Render();RenderTexture.active=rt;var image=new Texture2D(1800,1200,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1800,1200),0,0);image.Apply();File.WriteAllBytes("IntegrationEvidence/GoldenReward/"+name+".png",image.EncodeToPNG());UnityEngine.Object.Destroy(image);
        }
        finally{RenderTexture.active=previous;camera.targetTexture=null;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(go);}
    }
    public static string Main()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Inspect the running game.");
        var session=ThemedCollectionSession.Instance;session.suspendPersistenceForTests=true;
        var reward=UnityEngine.Object.FindFirstObjectByType<GoldenCarReveal>();
        var log=new List<string>();
        log.Add("Actual gameplay frame="+Time.frameCount+" sorted="+session.Sorted+" total="+session.Total+" rewardVisible="+reward.trophy.activeSelf+" glass="+reward.cover.gameObject.activeSelf);
        Capture(Camera.main,reward.cameraViewpoint.position,reward.cover.parent.GetComponentInChildren<Renderer>().bounds.center+Vector3.up*.14f,35,"empty-in-game");
        Capture(Camera.main,reward.transform.position+new Vector3(-1.2f,1.65f,2f),reward.transform.position+Vector3.up*.8f,50,"shop-context");
        var capsule=Player.Instance.Motor.Capsule;float radius=capsule.radius;float height=capsule.height;
        Vector3[] offsets={new Vector3(0,0,1f),new Vector3(-1f,0,0),new Vector3(1f,0,0)};
        foreach(var offset in offsets)
        {
            var foot=reward.transform.position+offset+Vector3.up*.05f;
            var hits=Physics.OverlapCapsule(foot+Vector3.up*radius,foot+Vector3.up*(height-radius),radius,~0,QueryTriggerInteraction.Ignore)
                .Where(c=>!c.transform.IsChildOf(Player.Instance.transform))
                .Where(c=>!(session.roomLayout!=null && session.roomLayout.walkThroughLooseCars && c.GetComponentInParent<WorldItemBase>()!=null)).ToArray();
            log.Add("Standing clearance offset="+offset+" playerRadius="+radius+" blockers="+string.Join(",",hits.Select(c=>c.name)));
            if(hits.Length>0)throw new InvalidOperationException("Walk clearance blocked: "+log.Last());
        }
        var pedestal=reward.pedestal.GetComponent<Renderer>().bounds;
        var stand=reward.cover.parent.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
        log.Add("Pedestal contact delta="+(stand.min.y-pedestal.max.y)+" stationWidth="+stand.size.x+" pedestalWidth="+pedestal.size.x);
        if(Mathf.Abs(stand.min.y-pedestal.max.y)>.001f || stand.size.x>pedestal.size.x)throw new InvalidOperationException("Station must be supported by the retained pedestal.");
        File.WriteAllLines("IntegrationEvidence/GoldenReward/gameplay-inspection.txt",log);
        return string.Join("\n",log);
    }
}
