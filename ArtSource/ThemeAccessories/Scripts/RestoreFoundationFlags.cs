using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;
using TinyRiders.Editor;
public static class RestoreFoundationFlags
{
    public static void Run()
    {
        var report=JObject.Parse(File.ReadAllText(ThemeAccessoryBuilder.Prod+"/source_protection.json"));int count=0;
        foreach(var token in report["body_materials_textures"]["changed"])
        {
            string path=(string)token;
            if(!path.EndsWith(".mat"))throw new Exception("Unexpected texture change: "+path);
            string baseline=ThemeAccessoryBuilder.Prod+"/baseline/Collection/"+path.Substring((ThemeAccessoryBuilder.Cars+"/").Length);
            string original=File.ReadAllText(baseline);
            if(!original.Contains("m_ValidKeywords: []"))throw new Exception("Unexpected baseline keywords: "+path);
            int flags=int.Parse(Regex.Match(original,@"m_LightmapFlags: (\d+)").Groups[1].Value);
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);mat.DisableKeyword("_EMISSION");mat.globalIlluminationFlags=(MaterialGlobalIlluminationFlags)flags;EditorUtility.SetDirty(mat);AssetDatabase.SaveAssetIfDirty(mat);count++;
        }
        Debug.Log("RESTORED_ORIGINAL_MATERIAL_FLAGS "+count);
    }
}
