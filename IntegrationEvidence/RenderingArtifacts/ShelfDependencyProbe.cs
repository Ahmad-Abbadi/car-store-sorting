using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
public static class ShelfDependencyProbe
{
    [Serializable]class Report {public Case[] scenes;}
    [Serializable]class Case {public string scene;public int structuralShelfRoots,prunedFilters,unpackedSourceInstances;public Stage[] stages;}
    [Serializable]class Stage {public string stage;public int sourceMeshObjects;public string[] sourceAssetPaths;}
    public static string Run()
    {
        var report=new List<Case>();
        foreach(string path in new[]{"Assets/00_Project/00_Scenes/Level.unity","Assets/00_Project/00_Scenes/MainMenu Scene.unity"})
        {
            var scene=EditorSceneManager.OpenPreviewScene(path);
            try
            {
                var roots=scene.GetRootGameObjects();
                var sources=roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true))
                    .Where(t=>t.Find("TR_Shelf_Base_01")!=null && t.Find("Batched Shelf Art")!=null)
                    .Select(t=>t.Find("TR_Shelf_Base_01")).ToArray();
                Stage Dependencies(string stage)
                {
                    var meshes=EditorUtility.CollectDependencies(roots.Cast<Object>().ToArray()).OfType<Mesh>()
                        .Where(m=>AssetDatabase.GetAssetPath(m).StartsWith("Assets/TinyRiders/Art/Shelves/",StringComparison.Ordinal)).ToArray();
                    return new Stage{stage=stage,sourceMeshObjects=meshes.Length,
                        sourceAssetPaths=meshes.Select(AssetDatabase.GetAssetPath).Distinct().OrderBy(p=>p).ToArray()};
                }
                var stages=new List<Stage>{Dependencies("original")};
                int pruned=(int)typeof(ShopShelfBuildGeometry).GetMethod("StripSourceBuffers",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{scene});
                stages.Add(Dependencies("pruned"));
                int unpacked=0;
                foreach(var source in sources)
                    if(PrefabUtility.IsAnyPrefabInstanceRoot(source.gameObject))
                    {PrefabUtility.UnpackPrefabInstance(source.gameObject,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);unpacked++;}
                stages.Add(Dependencies("sourceInstancesUnpacked"));
                report.Add(new Case{scene=path,structuralShelfRoots=sources.Length,prunedFilters=pruned,unpackedSourceInstances=unpacked,stages=stages.ToArray()});
            }
            finally {EditorSceneManager.ClosePreviewScene(scene);}
        }
        string json=string.Join("\n",report.SelectMany(c=>new[]{"scene="+c.scene+" roots="+c.structuralShelfRoots+" removed="+c.prunedFilters+" unpacked="+c.unpackedSourceInstances}.Concat(c.stages.Select(s=>s.stage+": meshes="+s.sourceMeshObjects+" assets="+s.sourceAssetPaths.Length+" paths="+string.Join(";",s.sourceAssetPaths)))));
        File.WriteAllText("IntegrationEvidence/RenderingArtifacts/shelf-dependency-probe.txt",json);
        return json;
    }
}

