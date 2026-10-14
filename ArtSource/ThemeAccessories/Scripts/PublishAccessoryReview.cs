using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;
using TinyRiders.Editor;
public static class PublishAccessoryReview
{
    public static void Run()
    {
        string d=ThemeAccessoryBuilder.Prod,c=ThemeAccessoryBuilder.Cars;
        var cat=JObject.Parse(File.ReadAllText(c+"/Data/Themes/variant_catalog.json"));
        var profiles=JArray.Parse(File.ReadAllText(d+"/fitting_profiles.json"));
        foreach(var e in cat["entries"])
        {
            string id=(string)e["collectibleId"],theme=(string)e["themeId"];
            File.Copy(d+"/before_after/after/"+id+".png",(string)e["previewPath"],true);
            File.Copy(d+"/views/Gameplay/"+id+".png",(string)e["gameplayPreviewPath"],true);
            var tex=new Texture2D(2,2);tex.LoadImage(File.ReadAllBytes(d+"/thumbnails/64/"+id+".png"));File.WriteAllBytes((string)e["thumbnailPath"],tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
            var profile=profiles.Single(x=>(string)x["baseCarId"]==(string)e["baseCarId"]);var lo=profile["sourceBoundsMin"];var hi=profile["sourceBoundsMax"];var b=new Bounds();b.SetMinMax(new Vector3((float)lo[0],(float)lo[1],(float)lo[2]),new Vector3((float)hi[0],(float)hi[1],(float)hi[2]));
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>((string)e["prefabPath"]);
            ThemeAccessoryBuilder.Render(prefab,c+"/Preview/Reverse/"+theme+"/"+id+".png",b,"Reverse",384);
            // Exact no-accessory control uses the same prefab/camera/light setup as the fresh baseline.
            ThemeAccessoryBuilder.Render(prefab,d+"/views/HiddenAccessories/"+id+".png",b,"Front",512,false);
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);AssetDatabase.SaveAssets();
        ThemeAccessoryBuilder.PreserveFoundationMaterialFlags();
        Debug.Log("R03_PREVIEWS_PUBLISHED_AND_BASELINE_CONTROLS_RENDERED");
    }
}
