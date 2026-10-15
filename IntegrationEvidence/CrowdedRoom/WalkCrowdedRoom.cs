using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
public static class WalkCrowdedRoom
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Play mode required");
        new GameObject("Room Walk Verification").AddComponent<CrowdedWalkProbe>().Begin();
        return "Walking 12 routes through the existing player input and motor";
    }
}
public class CrowdedWalkProbe:MonoBehaviour
{
    public void Begin(){StartCoroutine(Walk());}
    IEnumerator Walk()
    {
        var player=Player.Instance;var original=player.Motor.TransientPosition;float yaw=player.Look.Yaw,pitch=player.Look.Pitch;
        // Feed the existing movement stick because an unfocused Editor consumes synthetic keyboard events.
        var touch=TouchInput.Instance;bool touchEnabled=touch.enabled;touch.enabled=false;
        var move=typeof(TouchInput).GetField("_move",BindingFlags.Instance|BindingFlags.NonPublic);
        var routes=new List<Vector3[]>();var result=new JArray();
        foreach(float x in new[]{0f,-10.3f,10.3f,-7.5f,7.5f,-5.4f,5.4f,-2.7f,2.7f})routes.Add(new[]{new Vector3(500+x,.05f,-10.9f),new Vector3(500+x,.05f,11f)});
        foreach(float z in new[]{-9f,9f,11f})routes.Add(new[]{new Vector3(489.7f,.05f,z),new Vector3(510.3f,.05f,z)});
        var stock=WorldItemBase.All.ToDictionary(w=>w,w=>w.transform.position);bool passed=true;
        try
        {
            foreach(var route in routes)
            {
                player.Motor.SetPosition(route[0]);player.Motor.BaseVelocity=Vector3.zero;
                var delta=route[1]-route[0];player.Look.SetLook(Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg,0);
                float start=Time.realtimeSinceStartup;yield return null;
                while(Vector2.Distance(new Vector2(player.Motor.TransientPosition.x,player.Motor.TransientPosition.z),new Vector2(route[1].x,route[1].z))>.35f&&Time.realtimeSinceStartup-start<30)
                {
                    move.SetValue(touch,Vector2.up);yield return null;
                }
                move.SetValue(touch,Vector2.zero);yield return null;
                float error=Vector2.Distance(new Vector2(player.Motor.TransientPosition.x,player.Motor.TransientPosition.z),new Vector2(route[1].x,route[1].z));
                bool ok=error<.5f;passed&=ok;
                result.Add(new JObject{["start"]=route[0].ToString(),["end"]=route[1].ToString(),["endError"]=error,["seconds"]=Time.realtimeSinceStartup-start,["passed"]=ok});
                File.WriteAllText("IntegrationEvidence/CrowdedRoom/walk_progress.json",result.ToString());
                if(!ok)break;
            }
        }
        finally
        {
            move.SetValue(touch,Vector2.zero);touch.enabled=touchEnabled;player.Motor.SetPosition(original);player.Look.SetLook(yaw,pitch);
            float displacement=stock.Max(p=>Vector3.Distance(p.Key.transform.position,p.Value));
            File.WriteAllText("IntegrationEvidence/CrowdedRoom/walk_validation.json",new JObject{["passed"]=passed&&result.Count==12&&displacement<.01f,["routes"]=result,["jumpUsed"]=false,["maxCarDisplacement"]=displacement,["capsuleRadius"]=player.Motor.Capsule.radius,["capsuleHeight"]=player.Motor.Capsule.height}.ToString());
            Destroy(gameObject);
        }
    }
}
