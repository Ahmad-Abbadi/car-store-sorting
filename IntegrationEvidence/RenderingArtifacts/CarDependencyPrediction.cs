using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TinyRiders;
using Object = UnityEngine.Object;

// Read-only prediction: follows serialized runtime references, never prefab-template metadata.
public static class CarDependencyPrediction
{
    static bool menuNative;
    public static string RunWithMenuNative()
    {
        menuNative=true;
        try{return Run();}finally{menuNative=false;}
    }
    public static string Run()
    {
        if (BuildPipeline.isBuildingPlayer || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Idle Editor required.");
        var set = AssetDatabase.LoadAssetAtPath<ItemLodSet>(ItemDetailBuilder.SetPath);
        var entries = ((List<ItemLodSet.Entry>)typeof(ItemLodSet).GetField("entries", BindingFlags.NonPublic|BindingFlags.Instance).GetValue(set)).ToArray();
        var native = entries.Where(e => e?.mesh != null && e.bakedMesh != null && e.mesh != e.bakedMesh).ToDictionary(e=>e.mesh,e=>e.bakedMesh);
        var originals = new HashSet<Mesh>(native.Keys);
        var baked = new HashSet<Mesh>(native.Values);
        var generated = new Dictionary<GameObject,GameObject>();
        var wrapperVisual = new Dictionary<string,GameObject>(StringComparer.Ordinal);
        foreach (string path in new[]{ItemDetailBuilder.CatalogPath,"Assets/TinyRiders/ThemedCollection/Data/Themes/ThemedCarCatalog.asset"})
        foreach(var e in AssetDatabase.LoadAssetAtPath<ThemedCarCatalog>(path).entries.Where(e=>e!=null && e.enabled && e.prefab!=null && e.sortingItem!=null))
        {
            string folder=CarVisualBatchBuilder.Root+"/"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(e.prefab));
            var visual=AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/Visual.prefab");
            if(visual==null)throw new Exception("Missing generated visual "+folder);
            generated[e.prefab]=visual;
            foreach(var wrapper in new[]{e.sortingItem.WorldItem?.gameObject,e.sortingItem.HandItem?.gameObject,e.sortingItem.PlacedOnShelfPrefab?.gameObject}.Where(p=>p!=null))
                wrapperVisual[AssetDatabase.GetAssetPath(wrapper)]=visual;
        }
        var aliases = new Dictionary<GameObject,GameObject>();
        var seen = new HashSet<Object>();
        var required = new HashSet<Mesh>();
        var queue = new Queue<(Object value,bool map)>();
        var details = new List<string>();
        int substitutions=0, sourceAliases=0, wrapperAliases=0;
        void Enqueue(Object value,bool map)
        {
            if(value==null || value==set || value is MonoScript)return;
            if(value is GameObject go && generated.TryGetValue(go,out var visual)){value=visual;map=true;sourceAliases++;}
            if(value is Transform tf && aliases.TryGetValue(tf.gameObject,out var aliased)){value=aliased.transform;map=true;}
            if(value is GameObject aliasedGo && aliases.TryGetValue(aliasedGo,out var alias)){value=alias;map=true;}
            queue.Enqueue((value,map));
        }
        void Drain()
        {
            while(queue.Count>0)
            {
                var item=queue.Dequeue();var value=item.value;
                if(!seen.Add(value))continue;
                if(value is Mesh mesh){required.Add(mesh);continue;}
                if(value is GameObject go)
                {
                    var group=go.GetComponent<ItemVisualGroup>();
                    bool map=item.map || group!=null || go.GetComponent<ItemDetailDisplay>()!=null || (menuNative && go.name=="MenuStagedCollectionCars");
                    if(group!=null)
                    {
                        string path=AssetDatabase.GetAssetPath(go);
                        if(string.IsNullOrEmpty(path))path=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                        if(wrapperVisual.TryGetValue(path,out var visual))
                        {
                            var root=(Transform)new SerializedObject(group).FindProperty("visualRoot").objectReferenceValue;
                            if(root!=null){aliases[root.gameObject]=visual;wrapperAliases++;}
                        }
                    }
                    foreach(var component in go.GetComponents<Component>())if(component!=null && !(component is Transform))Enqueue(component,map);
                    foreach(Transform child in go.transform)Enqueue(child.gameObject,map);
                    continue;
                }
                if(value is Transform transform){Enqueue(transform.gameObject,item.map);continue;}
                if(!(value is Component) && !(value is ScriptableObject))continue;
                // Instantiating a referenced component instantiates its complete GameObject.
                if(value is Component componentOwner)Enqueue(componentOwner.gameObject,false);
                var iterator=new SerializedObject(value).GetIterator();
                while(iterator.Next(true))
                {
                    if(iterator.propertyType!=SerializedPropertyType.ObjectReference || iterator.name=="m_CorrespondingSourceObject" || iterator.name=="m_PrefabInstance" || iterator.name=="m_PrefabAsset" || iterator.name=="m_GameObject" || iterator.name=="m_Script")continue;
                    Object reference=iterator.objectReferenceValue;
                    if(value is MeshFilter && iterator.name=="m_Mesh" && item.map && reference is Mesh input && native.TryGetValue(input,out var replacement))
                    {reference=replacement;substitutions++;}
                    // Reference fields can point outside this visual hierarchy. Only
                    // parent/child traversal carries its virtual native-mesh context.
                    Enqueue(reference,false);
                }
            }
        }
        foreach(var scenePath in EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path))
        {
            var scene=EditorSceneManager.OpenPreviewScene(scenePath);
            try
            {
                int before=required.Count;
                foreach(var root in scene.GetRootGameObjects())Enqueue(root,false);
                Drain();details.Add(scenePath+": addedMeshes="+(required.Count-before));
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        foreach(string path in AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/",StringComparison.Ordinal) && p.Contains("/Resources/") && !AssetDatabase.IsValidFolder(p)))
            foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path))Enqueue(asset,false);
        Drain();
        int retained=entries.Count(e=>e?.mesh!=null && (required.Contains(e.mesh) || (e.bakedMesh!=null && required.Contains(e.bakedMesh))));
        int selfAliases=entries.Count(e=>e?.mesh!=null && e.bakedMesh!=null && e.mesh!=e.bakedMesh && !required.Contains(e.mesh) && required.Contains(e.bakedMesh));
        string report="READ-ONLY virtual build dependency prediction\n"+string.Join("\n",details)+"\nrequiredMeshes="+required.Count+"\noriginalNativeMeshes="+required.Count(originals.Contains)+"\nbakedNativeMeshes="+required.Count(baked.Contains)+"\noriginalLodEntries="+entries.Length+"\nretainedLodEntries="+retained+"\nselfAliases="+selfAliases+"\nnativeSubstitutions="+substitutions+"\ncatalogAliases="+sourceAliases+"\nwrapperAliases="+wrapperAliases+"\n"+string.Join("\n",required.Where(originals.Contains).OrderBy(m=>m.name).Select(m=>"ORIGINAL "+m.name+" "+AssetDatabase.GetAssetPath(m)));
        File.WriteAllText("IntegrationEvidence/RenderingArtifacts/car-dependency-prediction"+(menuNative?"-menu-native":"")+".txt",report);
        return report;
    }
}
