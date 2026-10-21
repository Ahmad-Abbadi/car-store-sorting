using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DependencyScopeCheck
{
    [Serializable]class RestoreStatus{public bool restored;public int files;public string backup;}
    [Serializable]class Report{public bool passed;public int sceneMeshAssignmentsVerified,sourceFilesUnchanged,registryEntriesRestored;public RestoreStatus restoration;}
    static string Hash(string path)
    {using(var sha=SHA256.Create())using(var s=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(s));}
    static string Key(UnityEngine.Object value)
    {if(value==null)return "";AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value,out string guid,out long id);return guid+":"+id;}
    static ItemLodSet.Entry[] Entries(ItemLodSet set)=>( (List<ItemLodSet.Entry>)typeof(ItemLodSet).GetField("entries",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(set)).ToArray();
    public static string Run()
    {
        string output="IntegrationEvidence/CarBatching/dependency-scope-check.json";
        var set=AssetDatabase.LoadAssetAtPath<ItemLodSet>(ItemDetailBuilder.SetPath);
        var original=Entries(set);
        var originalKeys=original.Select(e=>Key(e.mesh)+"|"+Key(e.bakedMesh)).ToArray();
        var maps=original.Where(e=>e.mesh!=null && e.bakedMesh!=null).ToDictionary(e=>Key(e.mesh),e=>e.bakedMesh);
        var paths=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path)
            .Concat(original.SelectMany(e=>new[]{AssetDatabase.GetAssetPath(e.mesh),AssetDatabase.GetAssetPath(e.bakedMesh)}))
            .Concat(Directory.GetFiles("Assets/TinyRiders/ThemedCollection/Prefabs","*.prefab",SearchOption.AllDirectories))
            .Where(File.Exists).Distinct().ToArray();
        var hashes=paths.ToDictionary(p=>p,Hash);
        int verified=0;
        using(CrazyGamesCarBuildDependencies.Apply())
        {
            foreach(string path in EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path))
            {
                var scene=EditorSceneManager.OpenPreviewScene(path);
                try
                {
                    var filters=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MeshFilter>(true))
                        .Where(f=>f.sharedMesh!=null && f.GetComponent<MeshRenderer>()!=null &&
                            (f.GetComponentInParent<ItemVisualGroup>(true)!=null || f.GetComponentInParent<ItemDetailDisplay>(true)!=null)).ToArray();
                    var expected=filters.Select(f=>maps.TryGetValue(Key(f.sharedMesh),out var mesh)?mesh:f.sharedMesh).ToArray();
                    typeof(CrazyGamesCarBuildDependencies).GetMethod("ProcessScene",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{scene});
                    for(int i=0;i<filters.Length;i++)
                    {
                        if(Key(filters[i].sharedMesh)!=Key(expected[i]))throw new Exception("Build-scene LOD substitution changed: "+path+"/"+filters[i].name);
                        if(maps.ContainsKey(Key(expected[i])) || original.Any(e=>e.bakedMesh==expected[i]))
                        {
                            if(!set.TryGet(expected[i],out var entry) || entry.bakedMesh!=expected[i])throw new Exception("Required native LOD entry missing: "+filters[i].name);
                            if(expected[i].lodCount!=3)throw new Exception("Three existing detail levels required: "+filters[i].name);
                        }
                        verified++;
                    }
                }
                finally {EditorSceneManager.ClosePreviewScene(scene);}
            }
        }
        foreach(var pair in hashes)if(Hash(pair.Key)!=pair.Value)throw new Exception("Source art, mesh buffers or saved scene bytes changed: "+pair.Key);
        set=AssetDatabase.LoadAssetAtPath<ItemLodSet>(ItemDetailBuilder.SetPath);
        var restored=Entries(set);
        if(!originalKeys.SequenceEqual(restored.Select(e=>Key(e.mesh)+"|"+Key(e.bakedMesh))))throw new Exception("LOD registry order/references were not restored.");
        foreach(var e in restored)if(!set.TryGet(e.mesh,out var found) || found.bakedMesh!=e.bakedMesh)throw new Exception("Restored registry lookup stale.");
        var report=new Report{passed=true,sceneMeshAssignmentsVerified=verified,sourceFilesUnchanged=hashes.Count,
            registryEntriesRestored=restored.Length,restoration=JsonUtility.FromJson<RestoreStatus>(File.ReadAllText(CrazyGamesReleaseBuild.Evidence+"/car-dependency-restoration.json"))};
        string json=JsonUtility.ToJson(report,true);File.WriteAllText(output,json);return json;
    }
}
