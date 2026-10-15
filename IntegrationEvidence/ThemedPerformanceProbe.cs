using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Profiling;
using Unity.Profiling;
namespace TinyRiders {
 public sealed class ThemedPerformanceProbe : MonoBehaviour {
  private readonly List<float> frames = new List<float>();
  private float begin, sampleBegin; private long peakBytes; private int maxDynamic, maxWorld; private bool reported;
  private ProfilerRecorder draws;
  private bool heldLook, lookWasEnabled; private string sampleMode="idle";
#if UNITY_WEBGL && !UNITY_EDITOR
  [DllImport("__Internal")] private static extern void TinyRidersReport(string json);
#endif
  private System.Collections.IEnumerator Start(){if(!Debug.isDebugBuild){enabled=false;yield break;}begin=Time.realtimeSinceStartup;sampleBegin=begin+3f;draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count");if(Application.absoluteURL.Contains("physics=1")){yield return new WaitForSecondsRealtime(3);SamplePilePhysics();}}
  [UnityEngine.Scripting.Preserve]
  public void SamplePilePhysics(){
   if(!Debug.isDebugBuild)return;
   frames.Clear();sampleBegin=Time.realtimeSinceStartup;peakBytes=0;maxWorld=0;maxDynamic=0;reported=false;
   sampleMode="24-body activation";
   if(Player.Instance!=null&&Player.Instance.Look!=null){lookWasEnabled=Player.Instance.Look.enabled;Player.Instance.Look.enabled=false;heldLook=true;}
   int count=0;foreach(var item in WorldItemBase.All){if(item==null||item.Body==null)continue;item.Body.isKinematic=false;item.Body.linearVelocity=Vector3.up*.6f;ItemPhysicsCulling.Track(item);if(++count==24)break;}
  }
  private void Update(){if(reported||Time.realtimeSinceStartup<sampleBegin)return;frames.Add(Time.unscaledDeltaTime);peakBytes=Math.Max(peakBytes,Profiler.GetTotalAllocatedMemoryLong());maxWorld=Math.Max(maxWorld,WorldItemBase.All.Count);int active=0;foreach(var w in WorldItemBase.All)if(w!=null&&w.Body!=null&&!w.Body.isKinematic)active++;maxDynamic=Math.Max(maxDynamic,active);if(Time.realtimeSinceStartup-sampleBegin<10)return;reported=true;var sorted=frames.OrderBy(f=>f).ToArray();var report=new Report{mode=sampleMode,platform=Application.platform.ToString(),unity=Application.unityVersion,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,samples=frames.Count,seconds=Time.realtimeSinceStartup-sampleBegin,fps=frames.Count/(Time.realtimeSinceStartup-sampleBegin),averageFrameMs=frames.Average()*1000,p95FrameMs=sorted[Mathf.Min(sorted.Length-1,(int)(sorted.Length*.95f))]*1000,peakUnityAllocatedBytes=peakBytes,maxWorldObjects=maxWorld,maxDynamicBodies=maxDynamic,totalRecords=ThemedCollectionSession.Instance.Total,drawCalls=draws.Valid?draws.LastValue:-1,loadedRendererComponents=FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length,enabledRendererComponents=FindObjectsByType<Renderer>(FindObjectsSortMode.None).Count(r=>r.enabled&&r.gameObject.activeInHierarchy),startupRealtime=begin};string json=JsonUtility.ToJson(report,true);Debug.Log("TINYRIDERS_METRICS "+json);RestoreLook();
#if UNITY_EDITOR
 System.IO.File.WriteAllText("IntegrationEvidence/editor_performance.json",json);
#elif UNITY_WEBGL
 TinyRidersReport(json);
#endif
 }
 private void RestoreLook(){if(heldLook&&Player.Instance!=null&&Player.Instance.Look!=null)Player.Instance.Look.enabled=lookWasEnabled;heldLook=false;}
 private void OnDestroy(){RestoreLook();draws.Dispose();}
 [Serializable]private class Report {public string mode,platform,unity,scene;public int samples,maxWorldObjects,maxDynamicBodies,totalRecords,loadedRendererComponents,enabledRendererComponents;public float seconds,fps,averageFrameMs,p95FrameMs,startupRealtime;public long peakUnityAllocatedBytes,drawCalls;}
 }
}
