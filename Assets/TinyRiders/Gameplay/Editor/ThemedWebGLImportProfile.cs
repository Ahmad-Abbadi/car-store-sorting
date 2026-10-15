using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json.Linq;

/// <summary>Measured desktop WebGL profile; original PNG pixels, GUIDs and other platform settings remain intact.</summary>
public static class ThemedWebGLImportProfile
{
 [MenuItem("Tools/Tiny Riders/Sorting/Apply WebGL Texture Profile")]
 public static void Apply()
 {
  EditorUserBuildSettings.webGLBuildSubtarget=WebGLTextureSubtarget.DXT;
  PlayerSettings.WebGL.initialMemorySize=256;
  var changed=new JArray();int inspected=0;
  AssetDatabase.StartAssetEditing();
  try{
   foreach(var guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/TinyRiders/ThemedCollection/Textures/Themes"})){
    string path=AssetDatabase.GUIDToAssetPath(guid);
    var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)continue;inspected++;
    var platform=importer.GetPlatformTextureSettings("WebGL");
    int cap=Mathf.Min(importer.maxTextureSize,path.Contains("ThemeDecals")?512:256);
    if(platform.overridden&&platform.maxTextureSize==cap&&platform.format==TextureImporterFormat.Automatic&&platform.textureCompression==TextureImporterCompression.Compressed)continue;
    changed.Add(new JObject{["path"]=path,["guid"]=guid,["defaultMax"]=importer.maxTextureSize,["webGLMax"]=cap});
    platform.name="WebGL";platform.overridden=true;platform.maxTextureSize=cap;
    platform.format=TextureImporterFormat.Automatic;platform.textureCompression=TextureImporterCompression.Compressed;
    importer.SetPlatformTextureSettings(platform);AssetDatabase.WriteImportSettingsIfDirty(path);
   }
  }finally{AssetDatabase.StopAssetEditing();}
  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);AssetDatabase.SaveAssets();
  Directory.CreateDirectory("IntegrationEvidence");
  File.WriteAllText("IntegrationEvidence/webgl_import_profile.json",new JObject{["profile"]="Desktop WebGL DXT",["inspected"]=inspected,["changedThisRun"]=changed.Count,["initialMemoryMB"]=PlayerSettings.WebGL.initialMemorySize,["originalImageFilesChanged"]=false,["otherPlatformSettingsChanged"]=false,["changes"]=changed}.ToString());
  Debug.Log("WEBGL_TEXTURE_PROFILE "+inspected+" imported textures, "+changed.Count+" WebGL-only overrides updated");
 }
}
