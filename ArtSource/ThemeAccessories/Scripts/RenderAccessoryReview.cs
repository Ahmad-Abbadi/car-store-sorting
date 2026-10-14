using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TinyRiders.Editor;
public static class RenderAccessoryReview
{
    const string D=ThemeAccessoryBuilder.Prod;
    static float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
    static void Save(PreviewRenderUtility p,string path,int w,int h)
    {
        p.ambientColor=new Color(.48f,.48f,.48f);p.lights[0].intensity=1.25f;p.lights[0].transform.rotation=Quaternion.Euler(35,-35,0);p.lights[1].intensity=.8f;p.lights[1].transform.rotation=Quaternion.Euler(40,150,0);
        p.camera.clearFlags=CameraClearFlags.SolidColor;p.camera.backgroundColor=new Color(.91f,.93f,.95f,1);p.camera.nearClipPlane=.01f;p.camera.farClipPlane=100;
        p.BeginStaticPreview(new Rect(0,0,w,h));p.camera.Render();var tex=p.EndStaticPreview();Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
    }
    public static void Run()
    {
        var cat=JObject.Parse(File.ReadAllText(ThemeAccessoryBuilder.Cars+"/Data/Themes/variant_catalog.json"));
        var reps=cat["entries"].GroupBy(e=>(string)e["themeId"]).Select(g=>g.First()).ToArray();
        var poses=new List<object>();
        for(int group=0;group<2;group++)foreach(bool after in new[]{false,true})
        {
            var p=new PreviewRenderUtility();var root=new GameObject("TemporaryStaticPileReview");
            try{
                for(int i=0;i<10;i++){
                    var e=reps[group*10+i];var car=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>((string)e["prefabPath"]));car.transform.SetParent(root.transform,false);
                    // Loose, overlapping two-tier toy pile. Fixed authored poses, no physics or scene asset.
                    bool upper=i>=6;int k=upper?i-6:i;
                    car.transform.position=upper?new Vector3((k%2)*3.1f-1.55f,1.3f,(k/2)*3.2f-1.6f):new Vector3((k%3)*3.0f-3,0,(k/3)*3.5f-1.75f);
                    car.transform.rotation=Quaternion.Euler(upper?(k%2==0?13:-11):0,(i*71)%140-70,upper?(k%2==0?-16:14):0);
                    car.transform.Find("ThemeAccessories").gameObject.SetActive(after);
                    if(after)poses.Add(new{group,id=(string)e["collectibleId"],position=V(car.transform.position),rotation=V(car.transform.eulerAngles),scale=V(car.transform.localScale)});
                }
                p.AddSingleGO(root);p.camera.orthographic=true;p.camera.orthographicSize=7.8f;p.camera.transform.position=new Vector3(11,14,18);p.camera.transform.LookAt(new Vector3(0,.7f,0));
                Save(p,D+"/before_after/pile_"+group+"_"+(after?"after":"before")+".png",1200,900);
            }finally{p.Cleanup();if(root!=null)UnityEngine.Object.DestroyImmediate(root);}
        }
        File.WriteAllText(D+"/pile_poses.json",JsonConvert.SerializeObject(poses,Formatting.Indented));
        var sourceCamera=Resources.FindObjectsOfTypeAll<Camera>().First(c=>c.gameObject.scene.IsValid()&&c.gameObject.scene.isLoaded&&c.name=="Main Camera");
        var candidates=Resources.FindObjectsOfTypeAll<Transform>().Where(t=>t.gameObject.scene.IsValid()&&t.gameObject.scene.isLoaded&&t.gameObject.activeInHierarchy&&(t.name.StartsWith("Shelf_TR-")||t.name.StartsWith("TR-C"))).ToArray();
        var source=candidates.OrderBy(t=>Vector3.Distance(t.position,new Vector3(500,1,-8))).FirstOrDefault();
        // If no matching displayed product exists, retain the observed display zone instead of inventing integration.
        var pos=source!=null?source.position:new Vector3(500,0,-8.5f);var rotation=source!=null?source.rotation:Quaternion.Euler(0,35,0);
        foreach(var e in reps)foreach(bool after in new[]{false,true})
        {
            var p=new PreviewRenderUtility();GameObject car=null;
            try{
                car=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>((string)e["prefabPath"]));car.transform.position=pos;car.transform.rotation=rotation;car.transform.Find("ThemeAccessories").gameObject.SetActive(after);p.AddSingleGO(car);
                p.camera.CopyFrom(sourceCamera);p.camera.scene=car.scene;p.camera.transform.SetPositionAndRotation(sourceCamera.transform.position,sourceCamera.transform.rotation);
                Save(p,D+"/views/ActualCamera/"+(after?"after/":"before/")+(string)e["collectibleId"]+".png",1280,720);
            }finally{p.Cleanup();if(car!=null)UnityEngine.Object.DestroyImmediate(car);}
        }
        File.WriteAllText(D+"/actual_camera_test.json",JsonConvert.SerializeObject(new{camera=sourceCamera.name,scene=sourceCamera.gameObject.scene.path,cameraPosition=V(sourceCamera.transform.position),cameraRotation=V(sourceCamera.transform.eulerAngles),sourceCamera.fieldOfView,sourceCamera.aspect,placementSource=source!=null?source.name:"Observed display zone",position=V(pos),rotation=V(rotation.eulerAngles),displayedSourceScale=source!=null?V(source.lossyScale):null,testScale=new[]{1,1,1},limitation="Exact existing camera pose/projection and observed display position; unscaled themed prefab substituted only in isolated preview. Current scene uses separate smaller shelf cars. This is a projection/clearance test, not completed shelf integration. Studio lights are held equal in before/after."},Formatting.Indented));
        ThemeAccessoryBuilder.PreserveFoundationMaterialFlags();
        Debug.Log("ACCESSORY_GROUP_AND_ACTUAL_CAMERA_REVIEW_RENDERED");
    }
}
