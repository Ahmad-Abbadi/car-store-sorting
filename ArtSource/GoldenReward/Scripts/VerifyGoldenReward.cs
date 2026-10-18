using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public static class VerifyGoldenReward
{
    public static string Main()
    {
        string a="Assets/TinyRiders/Art/GoldenReward/";
        var log=new List<string>();
        Action<string,bool> check=(label,ok)=>{log.Add((ok?"PASS ":"FAIL ")+label);};
        var car=AssetDatabase.LoadAssetAtPath<GameObject>(a+"Prefabs/TR_GoldenCar_Reward.prefab");
        var display=AssetDatabase.LoadAssetAtPath<GameObject>(a+"Prefabs/TR_GoldenReward_Display.prefab");
        var accessories=AssetDatabase.LoadAssetAtPath<GameObject>(a+"Prefabs/TR_GoldenReward_Accessories.prefab");
        var assembly=AssetDatabase.LoadAssetAtPath<GameObject>(a+"Prefabs/TR_GoldenReward_Filled_ArtPreset.prefab");
        foreach(var g in new[]{car,display,accessories,assembly})
        {
            check(g.name+" identity root",g.transform.localScale==Vector3.one && g.transform.localPosition==Vector3.zero && Quaternion.Angle(g.transform.localRotation,Quaternion.identity)<.01f);
            check(g.name+" no gameplay scripts",g.GetComponentsInChildren<MonoBehaviour>(true).Length==0);
            check(g.name+" persistent valid mesh/material references",g.GetComponentsInChildren<MeshFilter>(true).All(f=>f.sharedMesh!=null && AssetDatabase.Contains(f.sharedMesh)) && g.GetComponentsInChildren<Renderer>(true).All(r=>r.sharedMaterials.All(m=>m!=null && AssetDatabase.Contains(m) && m.shader!=null)));
            check(g.name+" UVs on every mesh",g.GetComponentsInChildren<MeshFilter>(true).All(f=>f.sharedMesh.uv.Length==f.sharedMesh.vertexCount));
        }
        var wheels=car.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Wheel_")).ToArray();check("four independent wheel pivots",wheels.Length==4);
        check("wheel left/right labels follow Unity forward",wheels.All(t=>t.name.EndsWith("_L")?t.localPosition.x<0:t.localPosition.x>0));
        check("empty display contains no reward car",!display.GetComponentsInChildren<Transform>().Any(t=>t.name.StartsWith("TR_GoldenCar")));
        var anchor=display.GetComponentsInChildren<Transform>().Single(t=>t.name=="Anchor_Car");check("anchor contact plane and parent",Mathf.Abs(anchor.localPosition.y-.05409091f)<.00001f && anchor.parent.name=="TurntablePivot");
        check("cover/accents/platter independent",new[]{"GlassCover","AccentEmission","Platter"}.All(n=>display.GetComponentsInChildren<Transform>().Any(t=>t.name==n)));
        check("filled art preset nests reward under anchor",assembly.GetComponentsInChildren<Transform>().Any(t=>t.name=="TR_GoldenCar_Reward" && t.parent.name=="Anchor_Car"));
        var points=car.GetComponentsInChildren<MeshFilter>().SelectMany(f=>f.sharedMesh.vertices.Select(v=>f.transform.TransformPoint(v))).ToArray();
        float radius=points.Max(v=>new Vector2(v.x,v.z).magnitude);float ground=points.Min(v=>v.y);float length=points.Max(v=>v.z)-points.Min(v=>v.z);
        check("ground contact",Mathf.Abs(ground)<.00001f);check("matches compact collectible length",Mathf.Abs(length-.35f)<.003f);
        check("full-turn cover clearance",radius<.21540909f);check("full-turn platter support",radius<.20522727f);
        var trim=car.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name=="Trim");var mesh=trim.GetComponent<MeshFilter>().sharedMesh;
        int grille=Array.FindIndex(trim.sharedMaterials,m=>m.name=="TR_Grille");int rear=Array.FindIndex(trim.sharedMaterials,m=>m.name=="TR_RedLight");
        check("front grille points +Z",mesh.GetIndices(grille).Select(i=>trim.transform.TransformPoint(mesh.vertices[i]).z).Average()>.09f);
        check("rear lamps point -Z",mesh.GetIndices(rear).Select(i=>trim.transform.TransformPoint(mesh.vertices[i]).z).Average()<-.12f);
        check("accessories in front of stand",accessories.GetComponentsInChildren<MeshFilter>().SelectMany(f=>f.sharedMesh.vertices.Select(v=>f.transform.TransformPoint(v).z)).Min()>.1f);
        var glow=AssetDatabase.LoadAssetAtPath<Material>(a+"Materials/TR_WarmLight.mat");check("emission survives saved material validation",glow.IsKeywordEnabled("_EMISSION") && glow.GetColor("_EmissionColor").maxColorComponent>1);
        var cover=AssetDatabase.LoadAssetAtPath<Material>(a+"Materials/TR_Cover.mat");check("separate restrained transparent cover",cover.renderQueue==3000 && cover.GetFloat("_ZWrite")==0 && cover.GetColor("_BaseColor").a<=.03f && cover.GetFloat("_SpecularHighlights")==0);
        foreach(string texture in new[]{"TR_Checker_BaseColor","TR_Grille_BaseColor"}){var t=AssetDatabase.LoadAssetAtPath<Texture2D>(a+"Textures/"+texture+".png");var imp=(TextureImporter)AssetImporter.GetAtPath(a+"Textures/"+texture+".png");check(texture+" 512px sRGB repeat",t.width==512 && t.height==512 && imp.sRGBTexture && imp.wrapMode==TextureWrapMode.Repeat);}
        check("both preview scenes saved",File.Exists(a+"PreviewScenes/TR_GoldenReward_Empty.unity") && File.Exists(a+"PreviewScenes/TR_GoldenReward_Filled.unity"));
        log.Add("Actual Unity radial envelope="+radius.ToString("F6")+" cover margin="+(.21540909f-radius).ToString("F6")+" platter margin="+(.20522727f-radius).ToString("F6")+" length="+length.ToString("F6"));
        log.Add("Checks="+log.Count(x=>x.StartsWith("PASS"))+" passed, "+log.Count(x=>x.StartsWith("FAIL"))+" failed");
        File.WriteAllLines("ArtSource/GoldenReward/unity_verification.txt",log);return string.Join("\n",log);
    }
}
