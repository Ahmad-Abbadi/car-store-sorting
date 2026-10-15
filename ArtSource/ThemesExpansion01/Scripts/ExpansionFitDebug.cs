using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
public class ExpansionFitDebug {
 const string D="TinyRidersProduction/ThemesExpansion01";
 struct Tri {public Vector3 a,b,c; public Tri(Vector3 a,Vector3 b,Vector3 c){this.a=a;this.b=b;this.c=c;}}
 static bool Ray(Vector3 o,Vector3 d,Tri t,out float hit){hit=0;var e1=t.b-t.a;var e2=t.c-t.a;var h=Vector3.Cross(d,e2);float det=Vector3.Dot(e1,h);if(Mathf.Abs(det)<1e-7)return false;float inv=1/det;var s=o-t.a;float u=inv*Vector3.Dot(s,h);if(u<0||u>1)return false;var q=Vector3.Cross(s,e1);float v=inv*Vector3.Dot(d,q);if(v<0||u+v>1)return false;hit=inv*Vector3.Dot(e2,q);return hit>=0;}
 static bool Acc(Transform t){for(;t!=null;t=t.parent)if(t.name=="ThemeAccessories")return true;return false;}
 public static void Run(){var records=new List<object>();var errors=new List<string>();var products=JArray.Parse(File.ReadAllText(D+"/products.json"));var profiles=JArray.Parse(File.ReadAllText(D+"/fitting_profiles.json"));
 foreach(var p in products.Where(p=>new[]{"TR-C004-TH_WILD_WEST-V01","TR-C005-TH_WILD_WEST-V01","TR-C011-TH_DETECTIVE-V01"}.Contains((string)p["variantId"]))){var g=AssetDatabase.LoadAssetAtPath<GameObject>((string)p["prefabPath"]);var profile=profiles.First(x=>(string)x["baseCarId"]==(string)p["baseCarId"]);var anchor=(string)profile["anchorRenderer"];var roof=new List<Tri>();var glass=new List<Tri>();var accessory=new List<Tri>();var glassOwners=new Dictionary<int,string>();
 foreach(var mf in g.GetComponentsInChildren<MeshFilter>()){var rr=mf.GetComponent<Renderer>();if(rr==null||!rr.enabled)continue;var m=mf.sharedMesh;if(m==null)continue;var vs=m.vertices.Select(mf.transform.TransformPoint).ToArray();bool acc=Acc(mf.transform);
 for(int sub=0;sub<m.subMeshCount;sub++){var mat=rr.sharedMaterials[Math.Min(sub,rr.sharedMaterials.Length-1)];string name=mat==null?"":mat.name;bool glazing=name.IndexOf("Glass",StringComparison.OrdinalIgnoreCase)>=0;bool anchorSurface=mf.name==anchor&&!glazing&&!name.Contains("Decals");if(!acc&&!glazing&&!anchorSurface)continue;var ix=m.GetTriangles(sub);for(int i=0;i<ix.Length;i+=3){var tri=new Tri(vs[ix[i]],vs[ix[i+1]],vs[ix[i+2]]);if(acc)accessory.Add(tri);else if(glazing){glassOwners[glass.Count]=AnimationUtility.CalculateTransformPath(mf.transform,g.transform)+" material:"+name;glass.Add(tri);}else if(Vector3.Cross(tri.b-tri.a,tri.c-tri.a).normalized.y>.25f)roof.Add(tri);}}}
 var points=accessory.SelectMany(t=>new[]{t.a,t.b,t.c}).Distinct().ToArray();float lowest=points.Min(v=>v.y);var low=points.Where(v=>v.y<lowest+.08f).ToArray();var gaps=new List<float>();foreach(var v in low){var origin=new Vector3(v.x,10,v.z);float best=100;foreach(var t in roof)if(Ray(origin,Vector3.down,t,out var hit)&&hit<best)best=hit;if(best<100)gaps.Add(v.y-(10-best));}
 var hits=new List<object>();int windowCrossings=0;foreach(var a in accessory){foreach(var pair in new[]{new[]{a.a,a.b},new[]{a.b,a.c},new[]{a.c,a.a}}){var delta=pair[1]-pair[0];float len=delta.magnitude;if(len<1e-5)continue;foreach(var t in glass)if(Ray(pair[0],delta/len,t,out var hit)&&hit>1e-4&&hit<len-1e-4){windowCrossings++;var hp=pair[0]+delta/len*hit;var origin=new Vector3(hp.x,10,hp.z);float top=-99;foreach(var rt in roof)if(Ray(origin,Vector3.down,rt,out var rh))top=Mathf.Max(top,10-rh);hits.Add(new{owner=glassOwners[glass.IndexOf(t)],point=new[]{hp.x,hp.y,hp.z},glassNormal=new[]{Vector3.Cross(t.b-t.a,t.c-t.a).normalized.x,Vector3.Cross(t.b-t.a,t.c-t.a).normalized.y,Vector3.Cross(t.b-t.a,t.c-t.a).normalized.z},paintedTop=top,embed=top-hp.y});break;}}}
 float closest=gaps.Count==0?999:gaps.OrderBy(Mathf.Abs).First();bool support=gaps.Any(v=>v>=-.10f&&v<=.10f);if(!support)errors.Add((string)p["variantId"]+" no sampled roof contact");if(windowCrossings>0)errors.Add((string)p["variantId"]+" intersects glazing");records.Add(new{hits,variantId=(string)p["variantId"],anchorRenderer=anchor,roofTriangles=roof.Count,glazingTriangles=glass.Count,accessoryTriangles=accessory.Count,lowVerticesSampled=low.Length,surfaceHits=gaps.Count,closestSurfaceGap=closest,supportedWithinTenCentimeters=support,glazingEdgeCrossings=windowCrossings});
 }
 File.WriteAllText(D+"/Reports/fit_debug.json",JsonConvert.SerializeObject(new{records,errors,method="Exact imported mesh triangles; vertical probes from lowest accessory vertices against upward-facing painted anchor surfaces. Glazing checked by accessory-edge/triangle intersections. Small concealed mounting embed allowed; visual views are also required.",passed=errors.Count==0},Formatting.Indented));Debug.Log("FIT_AUDIT "+products.Count+" issues "+errors.Count);
 }
}
