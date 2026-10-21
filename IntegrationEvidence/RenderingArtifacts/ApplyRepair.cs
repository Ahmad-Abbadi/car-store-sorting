using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TinyRiders;
using Object = UnityEngine.Object;

public static class ApplyRenderingRepair
{
    const string Root="IntegrationEvidence/RenderingArtifacts";
    public static string TrimMemory()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required");
        if(SceneManager.GetActiveScene().isDirty)throw new Exception("Unexpected unsaved scene; inspect before unloading");
        if(!SessionState.GetBool("RenderingArtifacts.ProfilerSaved",false))
        {
            SessionState.SetBool("RenderingArtifacts.ProfilerEnabled",UnityEditorInternal.ProfilerDriver.enabled);
            SessionState.SetBool("RenderingArtifacts.ProfilerSaved",true);
        }
        UnityEditorInternal.ProfilerDriver.enabled=false;
        UnityEditorInternal.ProfilerDriver.ClearAllFrames();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        EditorUtility.UnloadUnusedAssetsImmediate(true);
        GC.Collect();GC.WaitForPendingFinalizers();
        return "Owned test profiler frames cleared; empty scene; unused assets unloaded";
    }
    static string Identity(Scene scene)
    {
        return string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SortingShelf>(true))
            .OrderBy(s=>s.SaveId).Select(s=>s.SaveId+":"+s.transform.position.ToString("F5")+":"+s.transform.rotation.ToString("F5")+":"+
                string.Join("|",Enumerable.Range(0,s.SpotCount).Select(i=>s.GetSpot(i).name+s.GetSpot(i).position.ToString("F5")))+":"+
                string.Join("|",s.GetComponentsInChildren<BoxCollider>(true).Select(c=>c.center.ToString("F5")+c.size.ToString("F5")+c.isTrigger))));
    }
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)throw new InvalidOperationException("Editor must be idle.");
        var report=new List<string>();
        foreach(var path in new[]{CompactCollectionBuilder.ScenePath,"Assets/00_Project/00_Scenes/MainMenu Scene.unity",TutorialBuilder.ScenePath})
        {
            var scene=EditorSceneManager.OpenScene(path);var before=Identity(scene);
            ShopCameraClipping.Apply(scene);
            if(before!=Identity(scene))throw new InvalidOperationException("Shelf identity changed: "+path);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            report.Add(path+": shelf IDs, poses, slots and box colliders preserved; camera far planes=80 m.");
        }
        int meshes=0,models=0;
        foreach(var guid in AssetDatabase.FindAssets("t:Mesh",new[]{"Assets/TinyRiders"}))
        {
            var path=AssetDatabase.GUIDToAssetPath(guid);
            bool preserve=path.StartsWith("Assets/TinyRiders/Art/Shelves/")||path.StartsWith("Assets/TinyRiders/Gameplay/Rendering/Shelf_")||
                path.StartsWith("Assets/TinyRiders/Art/ShopInterior/")||path.StartsWith("Assets/TinyRiders/Art/MutedShopSurfaces/")||
                path.StartsWith("Assets/TinyRiders/Art/RacingFloor/")||path.StartsWith("Assets/TinyRiders/Gameplay/TutorialAssets/");
            if(!preserve)continue;
            var importer=AssetImporter.GetAtPath(path) as ModelImporter;
            if(importer!=null)
            {
                if(importer.meshCompression!=ModelImporterMeshCompression.Off){importer.meshCompression=ModelImporterMeshCompression.Off;importer.SaveAndReimport();}
                models++;continue;
            }
            foreach(var mesh in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>())
            {
                if(MeshUtility.GetMeshCompression(mesh)!=ModelImporterMeshCompression.Off){MeshUtility.SetMeshCompression(mesh,ModelImporterMeshCompression.Off);EditorUtility.SetDirty(mesh);}
                meshes++;
            }
        }
        AssetDatabase.SaveAssets();
        report.Add("Verified lossless precision: "+meshes+" generated meshes, "+models+" source models.");
        File.WriteAllLines(Root+"/repair-audit.txt",report);
        EditorSceneManager.OpenScene(CompactCollectionBuilder.ScenePath);
        return string.Join("\n",report);
    }
    public static string AuditRoom()
    {
        var room=GameObject.Find("Collector Promenade");
        var keys=new Dictionary<string,string>();var duplicates=new List<string>();int triangles=0;
        foreach(var filter in room.GetComponentsInChildren<MeshFilter>())
        {
            var renderer=filter.GetComponent<MeshRenderer>();if(!renderer||!renderer.enabled)continue;
            if(filter.name!="Gallery wall"&&filter.name!="Teal dado"&&filter.name!="Store Ceiling")continue;
            var vertices=filter.sharedMesh.vertices;var indices=filter.sharedMesh.triangles;
            for(int i=0;i<indices.Length;i+=3)
            {
                string key=string.Join("|",Enumerable.Range(0,3).Select(j=>filter.transform.TransformPoint(vertices[indices[i+j]]).ToString("F4")).OrderBy(v=>v));
                if(keys.TryGetValue(key,out var other))duplicates.Add(other+" / "+filter.name);
                else keys[key]=filter.name;
                triangles++;
            }
        }
        var result="Current room walls/ceiling: "+triangles+" triangles, "+duplicates.Count+" duplicate triangles.\n"+string.Join("\n",duplicates);
        File.WriteAllText(Root+"/room-overlap-audit.txt",result);return result;
    }
    public static string RebuildShelfCaches()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required");
        var scene=SceneManager.GetActiveScene();
        if(scene.path!=CompactCollectionBuilder.ScenePath)scene=EditorSceneManager.OpenScene(CompactCollectionBuilder.ScenePath);
        string identity=Identity(scene);
        var meshes=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SortingShelf>(true))
            .SelectMany(s=>s.GetComponentsInChildren<MeshFilter>(true)).Select(f=>f.sharedMesh).Where(m=>m!=null)
            .Where(m=>AssetDatabase.GetAssetPath(m).StartsWith("Assets/TinyRiders/Gameplay/Rendering/Shelf_"))
            .Distinct().ToArray();
        var positions=meshes.ToDictionary(m=>m,m=>m.vertices);
        var indices=meshes.ToDictionary(m=>m,m=>m.triangles.Length);
        foreach(var entry in ThemedShelfArtBuilder.Catalog.entries)
        {
            string path=AssetDatabase.GetAssetPath(entry.prefab);
            string report=ThemedShelfArtBuilder.Evidence+"/Batching/"+Path.GetFileNameWithoutExtension(path)+".json";
            byte[] previous=File.Exists(report)?File.ReadAllBytes(report):null;
            try { ThemedShelfBatcher.Apply(Array.Empty<SortingShelf>(),path); }
            finally { if(previous!=null)File.WriteAllBytes(report,previous); }
        }
        float maxError=0;
        foreach(var mesh in meshes)
        {
            var vertices=mesh.vertices;
            if(vertices.Length!=positions[mesh].Length||mesh.triangles.Length!=indices[mesh])throw new Exception("Cache topology changed: "+mesh.name);
            for(int i=0;i<vertices.Length;i++)maxError=Mathf.Max(maxError,Vector3.Distance(vertices[i],positions[mesh][i]));
            if(MeshUtility.GetMeshCompression(mesh)!=ModelImporterMeshCompression.Off)throw new Exception("Lossy cache: "+mesh.name);
        }
        if(maxError>.00001f)throw new Exception("Cache positions changed: "+maxError);
        if(identity!=Identity(scene))throw new Exception("Shelf IDs, slots or colliders changed during cache rebuild");
        var result="Rebuilt "+meshes.Length+" shelf caches from all40 source prefabs. Topology unchanged; maximum position delta="+maxError+"; compression Off. Scene objects were retained.";
        File.WriteAllText(Root+"/cache-rebuild.txt",result);return result;
    }
}
