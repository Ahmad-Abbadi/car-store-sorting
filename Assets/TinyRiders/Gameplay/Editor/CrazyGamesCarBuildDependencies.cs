using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using TinyRiders;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Transient, lossless car dependency substitutions. Authoring assets are byte-restored.</summary>
public sealed class CrazyGamesCarBuildDependencies : IDisposable
{
    const string LegacyCatalog="Assets/TinyRiders/ThemedCollection/Data/Themes/ThemedCarCatalog.asset";
    const string Evidence=CrazyGamesReleaseBuild.Evidence;
    const string Status=Evidence+"/car-dependency-restoration.json";
    const string Pointer=Evidence+"/last-car-dependency-backup.txt";
    static CrazyGamesCarBuildDependencies current;
    readonly Dictionary<string,string> native=new Dictionary<string,string>(StringComparer.Ordinal);
    readonly HashSet<Mesh> known=new HashSet<Mesh>();
    readonly HashSet<string> precision=new HashSet<string>(StringComparer.Ordinal);
    readonly JArray records=new JArray();
    readonly string folder;
    readonly bool previousSkip;
    int nativeSwaps,wrapperSwaps;
    bool disposed;

    public static bool RequiresRecovery => File.Exists(Status) && !(bool)(JObject.Parse(File.ReadAllText(Status))["restored"] ?? false);
    public static string[] ActivePrecisionMeshPaths() => current==null ? Array.Empty<string>() : current.precision.OrderBy(p=>p,StringComparer.Ordinal).ToArray();

    public static CrazyGamesCarBuildDependencies Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || current!=null)throw new InvalidOperationException("Car dependency scope requires an idle Editor.");
        if(RequiresRecovery)throw new InvalidOperationException("Recover temporary car dependencies before building again.");
        var scope=new CrazyGamesCarBuildDependencies();current=scope;
        try {scope.Prepare();return scope;}
        catch(Exception error)
        {
            try {scope.Dispose();}
            catch(Exception restoreError){throw new AggregateException(error,restoreError);}
            throw;
        }
    }

    CrazyGamesCarBuildDependencies()
    {
        previousSkip=CarVisualBatchBuilder.SkipForBenchmark;
        folder=Path.GetFullPath(Evidence+"/car-dependency-backups/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        CarVisualBatchBuilder.SkipForBenchmark=true;
    }

    void Prepare()
    {
        var catalogs=new[]{ItemDetailBuilder.CatalogPath,LegacyCatalog}.Select(AssetDatabase.LoadAssetAtPath<ThemedCarCatalog>).ToArray();
        if(catalogs.Any(c=>c==null))throw new InvalidOperationException("Missing car catalog.");
        var entries=catalogs.SelectMany(c=>c.entries).Where(e=>e!=null && e.enabled && e.prefab!=null && e.sortingItem!=null).ToArray();
        var visuals=new Dictionary<GameObject,string>();
        foreach(var e in entries)
        {
            string source=AssetDatabase.GetAssetPath(e.prefab);
            var visual=AssetDatabase.LoadAssetAtPath<GameObject>(CarVisualBatchBuilder.Root+"/"+AssetDatabase.AssetPathToGUID(source)+"/Visual.prefab");
            if(visual==null)throw new InvalidOperationException("Bake exact car visuals first: "+e.collectibleId);
            visuals[e.prefab]=AssetDatabase.GetAssetPath(visual);
        }
        var wrappers=entries.SelectMany(e=>new[]{e.sortingItem.WorldItem?.gameObject,e.sortingItem.HandItem?.gameObject,e.sortingItem.PlacedOnShelfPrefab?.gameObject})
            .Where(p=>p!=null).Distinct().ToArray();
        var generated=visuals.Values.Distinct().Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();
        var invariants=wrappers.ToDictionary(AssetDatabase.GetAssetPath,WrapperInvariant,StringComparer.Ordinal);
        var catalogInvariants=catalogs.Select(CatalogInvariant).ToArray();
        var artworkInvariants=catalogs.Select(ArtworkInvariant).ToArray();
        var sortingItems=entries.Select(e=>e.sortingItem).Distinct().ToArray();
        var set=AssetDatabase.LoadAssetAtPath<ItemLodSet>(ItemDetailBuilder.SetPath);
        var original=((List<ItemLodSet.Entry>)typeof(ItemLodSet).GetField("entries",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(set)).ToArray();
        foreach(var e in original.Where(e=>e!=null))
        {
            if(e.mesh!=null)known.Add(e.mesh);if(e.bakedMesh!=null)known.Add(e.bakedMesh);
            if(e.mesh!=null && e.bakedMesh!=null && e.mesh!=e.bakedMesh)native[MeshKey(e.mesh)]=AssetDatabase.GetAssetPath(e.bakedMesh);
        }

        // Snapshot every possible mutation before changing a single reference.
        var paths=catalogs.Select(AssetDatabase.GetAssetPath).Concat(sortingItems.Select(AssetDatabase.GetAssetPath))
            .Concat(wrappers.Concat(generated).Select(AssetDatabase.GetAssetPath))
            .Append(ItemDetailBuilder.SetPath).Distinct().OrderBy(p=>p,StringComparer.Ordinal).ToArray();
        foreach(string path in paths){Backup(path);if(File.Exists(path+".meta"))Backup(path+".meta");}
        File.WriteAllText(Path.Combine(folder,"manifest.json"),records.ToString());
        File.WriteAllText(Pointer,folder);
        File.WriteAllText(Status,new JObject{["restored"]=false,["backup"]=folder,["files"]=records.Count}.ToString());

        // These are the same existing native LOD buffers selected by ItemDetail.ApplyTo at runtime.
        foreach(var visual in generated)RewritePrefabMeshes(visual,true);
        var replace=typeof(CarVisualBatchBuilder).GetMethod("ReplaceVisual",BindingFlags.Static|BindingFlags.NonPublic);
        foreach(var e in entries)
        {
            var visual=AssetDatabase.LoadAssetAtPath<GameObject>(visuals[e.prefab]);
            foreach(var wrapper in new[]{e.sortingItem.WorldItem?.gameObject,e.sortingItem.HandItem?.gameObject,e.sortingItem.PlacedOnShelfPrefab?.gameObject}.Where(p=>p!=null))
            {
                var group=wrapper.GetComponent<ItemVisualGroup>();
                var root=(Transform)new SerializedObject(group).FindProperty("visualRoot").objectReferenceValue;
                if(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root.gameObject)==AssetDatabase.GetAssetPath(visual))continue;
                replace.Invoke(null,new object[]{wrapper,visual});wrapperSwaps++;
            }
        }
        foreach(var wrapper in wrappers)RewritePrefabMeshes(wrapper,false);
        var retainedArtworkItems=new HashSet<SortableItem_SO>();
        int thumbnailAliases=0;
        foreach(var catalog in catalogs)
        {
            foreach(var e in catalog.entries.Where(e=>e!=null && e.prefab!=null && visuals.ContainsKey(e.prefab)))e.prefab=AssetDatabase.LoadAssetAtPath<GameObject>(visuals[e.prefab]);
            thumbnailAliases+=AliasUnusedPreviews(catalog,retainedArtworkItems);
            EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssetIfDirty(catalog);
        }
        var fallbackIcon=retainedArtworkItems.Select(i=>i.ItemImage).FirstOrDefault(i=>i!=null);
        foreach(var item in sortingItems.Where(i=>i!=null && i.ItemImage!=null && !retainedArtworkItems.Contains(i)))
        {
            item.ItemImage=fallbackIcon;
            EditorUtility.SetDirty(item);AssetDatabase.SaveAssetIfDirty(item);
        }
        foreach(var pair in invariants)
            if(WrapperInvariant(AssetDatabase.LoadAssetAtPath<GameObject>(pair.Key))!=pair.Value)
                throw new InvalidOperationException("Car wrapper identity, physics, or visual pose changed: "+pair.Key);
        for(int i=0;i<catalogs.Length;i++)
            if(CatalogInvariant(catalogs[i])!=catalogInvariants[i])throw new InvalidOperationException("Car catalog gameplay fields changed.");
        for(int i=0;i<catalogs.Length;i++)
            if(ArtworkInvariant(catalogs[i])!=artworkInvariants[i])throw new InvalidOperationException("Scrapbook artwork selection changed.");

        var direct=CollectDirectMeshes();
        var retained=new List<ItemLodSet.Entry>();
        int aliases=0;
        foreach(var e in original.Where(e=>e!=null && e.mesh!=null))
        {
            if(direct.Contains(e.mesh))retained.Add(e);
            else if(e.bakedMesh!=null && direct.Contains(e.bakedMesh))
            {retained.Add(new ItemLodSet.Entry{mesh=e.bakedMesh,bakedMesh=e.bakedMesh,lod1=e.lod1,lod2=e.lod2});aliases++;}
        }
        foreach(var mesh in direct.Where(known.Contains))
            if(!retained.Any(e=>e.mesh==mesh || e.bakedMesh==mesh))
                throw new InvalidOperationException("A required runtime mesh lost its detail registry entry: "+mesh.name);
        // Aliasing changes lookup keys only. Preserve every selectable detail buffer exactly.
        foreach(var before in original.Where(e=>e?.mesh!=null && e.bakedMesh!=null && direct.Contains(e.bakedMesh)))
        {
            var after=retained.Single(e=>e.bakedMesh==before.bakedMesh);
            for(int sub=0;sub<before.bakedMesh.subMeshCount;sub++)
            for(int lod=0;lod<3;lod++)
                if(!before.GetIndices(lod,sub).SequenceEqual(after.GetIndices(lod,sub)))
                    throw new InvalidOperationException("A selectable detail buffer changed: "+before.mesh.name);
        }
        set.SetEntries(retained);EditorUtility.SetDirty(set);AssetDatabase.SaveAssetIfDirty(set);
        foreach(var mesh in direct.Where(m=>known.Contains(m)))precision.Add(AssetDatabase.GetAssetPath(mesh));
        foreach(JObject record in records)record["appliedHash"]=Hash((string)record["source"]);
        File.WriteAllText(Path.Combine(folder,"manifest.json"),records.ToString());
        File.WriteAllText(Evidence+"/car-dependency-audit.json",new JObject{
            ["catalogs"]=catalogs.Length,["designs"]=generated.Length,["wrapperVisualReplacements"]=wrapperSwaps,
            ["nativeMeshSubstitutions"]=nativeSwaps,["originalLodEntries"]=original.Length,["retainedLodEntries"]=retained.Count,
            ["nativeSelfAliases"]=aliases,["directMeshObjects"]=direct.Count,["precisionPaths"]=new JArray(precision.OrderBy(p=>p,StringComparer.Ordinal)),
            ["wrapperInvariantsVerified"]=invariants.Count,["catalogGameplayFieldsUnchanged"]=true,
            ["thumbnailAliases"]=thumbnailAliases,["scrapbookArtworkUnchanged"]=true,
            ["backup"]=folder,["sourceArtChanged"]=false,["sceneFilesChanged"]=false}.ToString());
    }

    void RewritePrefabMeshes(GameObject prefab,bool generated)
    {
        string path=AssetDatabase.GetAssetPath(prefab);
        if(!generated && !prefab.GetComponentsInChildren<MeshFilter>(true).Any(f=>f.sharedMesh!=null && native.ContainsKey(MeshKey(f.sharedMesh))))return;
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            Substitute(root,generated);
            // Remove authoring-only source-template dependencies while retaining the entire hierarchy.
            if(generated)
            {
                foreach(var t in root.GetComponentsInChildren<Transform>(true))
                    if(t!=null && PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))
                        PrefabUtility.UnpackPrefabInstance(t.gameObject,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            }
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally {PrefabUtility.UnloadPrefabContents(root);}
    }

    void Substitute(GameObject root,bool all)
    {
        foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if(filter.sharedMesh==null || !native.TryGetValue(MeshKey(filter.sharedMesh),out var replacementPath))continue;
            if(!all && filter.GetComponentInParent<ItemVisualGroup>(true)==null && filter.GetComponentInParent<ItemDetailDisplay>(true)==null)continue;
            if(filter.GetComponent<MeshRenderer>()==null)continue;
            var replacement=AssetDatabase.LoadAssetAtPath<Mesh>(replacementPath);
            if(replacement==null)throw new InvalidOperationException("Missing native LOD mesh: "+replacementPath);
            filter.sharedMesh=replacement;precision.Add(replacementPath);nativeSwaps++;
        }
    }

    internal static void ProcessScene(Scene scene)
    {
        if(current==null)return;
        foreach(var root in scene.GetRootGameObjects())current.Substitute(root,false);
    }

    HashSet<Mesh> CollectDirectMeshes()
    {
        var result=new HashSet<Mesh>();
        var visited=new HashSet<Object>();
        var pending=new Queue<Object>();
        var registry=AssetDatabase.LoadAssetAtPath<ItemLodSet>(ItemDetailBuilder.SetPath);
        void Enqueue(Object value)
        {
            // The registry is what this traversal will prune, not an independent runtime root.
            if(value!=null && value!=registry && !(value is MonoScript))pending.Enqueue(value);
        }
        void Drain()
        {
            while(pending.Count>0)
            {
                var value=pending.Dequeue();
                if(!visited.Add(value))continue;
                if(value is Mesh mesh){result.Add(mesh);continue;}
                if(value is GameObject root)
                {
                    foreach(var component in root.GetComponents<Component>())
                        if(component!=null && !(component is Transform))Enqueue(component);
                    foreach(Transform child in root.transform)Enqueue(child.gameObject);
                    continue;
                }
                if(value is Transform transform){Enqueue(transform.gameObject);continue;}
                if(!(value is Component) && !(value is ScriptableObject))continue;
                // Runtime component references can be instantiated as complete prefabs.
                if(value is Component owner)Enqueue(owner.gameObject);
                var property=new SerializedObject(value).GetIterator();
                while(property.Next(true))
                {
                    if(property.propertyType!=SerializedPropertyType.ObjectReference)continue;
                    // Template links are Editor authoring metadata, not player object fields.
                    if(property.name=="m_CorrespondingSourceObject" || property.name=="m_PrefabInstance" ||
                        property.name=="m_PrefabAsset" || property.name=="m_GameObject" || property.name=="m_Script")continue;
                    Enqueue(property.objectReferenceValue);
                }
            }
        }
        var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
        foreach(string path in scenes)
        {
            var scene=EditorSceneManager.OpenPreviewScene(path);
            try
            {
                foreach(var root in scene.GetRootGameObjects()){Substitute(root,false);Enqueue(root);}
                Drain();
            }
            finally {EditorSceneManager.ClosePreviewScene(scene);}
        }
        var resources=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/",StringComparison.Ordinal) && p.Contains("/Resources/") && !AssetDatabase.IsValidFolder(p)).ToArray();
        foreach(string path in resources)
            foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path))Enqueue(asset);
        Drain();
        return result;
    }

    static bool Owned(string path)
    {
        path=path.Replace('\\','/');if(path.EndsWith(".meta",StringComparison.Ordinal))path=path.Substring(0,path.Length-5);
        if(path.Contains("/../"))return false;
        return path==LegacyCatalog || path==ItemDetailBuilder.CatalogPath || path==ItemDetailBuilder.SetPath ||
            path.StartsWith(CarVisualBatchBuilder.Root+"/",StringComparison.Ordinal) ||
            path.StartsWith("Assets/TinyRiders/Gameplay/Prefabs/",StringComparison.Ordinal) ||
            path.StartsWith("Assets/TinyRiders/Gameplay/Items/",StringComparison.Ordinal) ||
            path.StartsWith("Assets/TinyRiders/Gameplay/Compact/Items/",StringComparison.Ordinal) ||
            path.StartsWith("Assets/TinyRiders/Gameplay/Compact/Prefabs/",StringComparison.Ordinal);
    }
    static JArray Vector(Vector3 v)=>new JArray(Math.Round(v.x,5),Math.Round(v.y,5),Math.Round(v.z,5));
    static JArray Rotation(Quaternion q)=>new JArray(Math.Round(q.x,5),Math.Round(q.y,5),Math.Round(q.z,5),Math.Round(q.w,5));
    static string Reference(Object value)
    {
        if(value==null)return "";
        return AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value,out string guid,out long id)?guid+":"+id:value.name;
    }
    static string CatalogInvariant(ThemedCarCatalog catalog)
    {
        var json=JObject.Parse(JsonUtility.ToJson(catalog));
        foreach(var entry in (JArray)json["entries"])
        {
            ((JObject)entry).Remove("prefab");
            ((JObject)entry).Remove("preview");
        }
        return json.ToString(Newtonsoft.Json.Formatting.None);
    }
    static string ArtworkInvariant(ThemedCarCatalog catalog)
    {
        var entries=catalog.entries.Where(e=>e!=null && e.enabled && e.preview!=null).ToArray();
        var selected=new List<string>();
        for(int i=0;i<6 && entries.Length>0;i++)selected.Add("fallback:"+i+":"+Reference(entries[(i*entries.Length/6)%entries.Length].preview));
        var themes=new HashSet<string>(StringComparer.Ordinal);
        var families=new HashSet<string>(StringComparer.Ordinal);
        foreach(var entry in entries)
        {
            if(themes.Add(entry.themeId))selected.Add("theme:"+entry.themeId+":"+Reference(entry.preview));
            if(families.Add(entry.baseCarId))selected.Add("family:"+entry.baseCarId+":"+Reference(entry.preview));
        }
        return string.Join("|",selected);
    }
    static int AliasUnusedPreviews(ThemedCarCatalog catalog,HashSet<SortableItem_SO> retainedItems)
    {
        var entries=catalog.entries.Where(e=>e!=null && e.enabled && e.preview!=null).ToArray();
        if(entries.Length==0)return 0;
        var keep=new HashSet<ThemedCarCatalog.Entry>();
        for(int i=0;i<6;i++)keep.Add(entries[(i*entries.Length/6)%entries.Length]);
        var themes=new HashSet<string>(StringComparer.Ordinal);
        var families=new HashSet<string>(StringComparer.Ordinal);
        foreach(var entry in entries)
            if(themes.Add(entry.themeId) | families.Add(entry.baseCarId))keep.Add(entry);
        foreach(var entry in keep)
            if(entry.sortingItem!=null)retainedItems.Add(entry.sortingItem);
        var fallback=entries[0].preview;
        int aliases=0;
        foreach(var entry in entries)
        {
            if(keep.Contains(entry))continue;
            entry.preview=fallback;
            aliases++;
        }
        return aliases;
    }
    static string WrapperInvariant(GameObject root)
    {
        var visual=(Transform)new SerializedObject(root.GetComponent<ItemVisualGroup>()).FindProperty("visualRoot").objectReferenceValue;
        var identity=visual.GetComponentInChildren<ThemedCarIdentity>(true);
        var colliders=new JArray();
        foreach(var collider in root.GetComponentsInChildren<Collider>(true).Where(c=>!c.transform.IsChildOf(visual)).OrderBy(c=>AnimationUtility.CalculateTransformPath(c.transform,root.transform),StringComparer.Ordinal))
        {
            var c=new JObject{["path"]=AnimationUtility.CalculateTransformPath(collider.transform,root.transform),["type"]=collider.GetType().FullName,
                ["position"]=Vector(collider.transform.localPosition),["rotation"]=Rotation(collider.transform.localRotation),["scale"]=Vector(collider.transform.localScale),
                ["enabled"]=collider.enabled,["trigger"]=collider.isTrigger,["material"]=Reference(collider.sharedMaterial),["contactOffset"]=collider.contactOffset};
            if(collider is BoxCollider box){c["center"]=Vector(box.center);c["size"]=Vector(box.size);}
            if(collider is SphereCollider sphere){c["center"]=Vector(sphere.center);c["radius"]=sphere.radius;}
            if(collider is CapsuleCollider capsule){c["center"]=Vector(capsule.center);c["radius"]=capsule.radius;c["height"]=capsule.height;c["direction"]=capsule.direction;}
            if(collider is MeshCollider mesh){c["mesh"]=Reference(mesh.sharedMesh);c["convex"]=mesh.convex;c["cooking"]=mesh.cookingOptions.ToString();}
            colliders.Add(c);
        }
        var result=new JObject{["name"]=root.name,["layer"]=root.layer,["active"]=root.activeSelf,
            ["position"]=Vector(root.transform.localPosition),["rotation"]=Rotation(root.transform.localRotation),["scale"]=Vector(root.transform.localScale),
            ["visualName"]=visual.name,["visualPosition"]=Vector(visual.localPosition),["visualRotation"]=Rotation(visual.localRotation),["visualScale"]=Vector(visual.localScale),
            ["visualActive"]=visual.gameObject.activeSelf,["visualLayer"]=visual.gameObject.layer,["identity"]=identity!=null?JsonUtility.ToJson(identity):"",
            ["components"]=new JArray(root.GetComponents<Component>().Select(c=>c.GetType().FullName).OrderBy(n=>n,StringComparer.Ordinal)),["colliders"]=colliders,
            ["worldData"]=Reference(root.GetComponent<WorldItemBase>()?.Data),["heldData"]=Reference(root.GetComponent<HandItemBase>()?.Data)};
        var body=root.GetComponent<Rigidbody>();
        if(body!=null)result["body"]=new JObject{["mass"]=body.mass,["linearDamping"]=body.linearDamping,["angularDamping"]=body.angularDamping,
            ["kinematic"]=body.isKinematic,["gravity"]=body.useGravity,["constraints"]=body.constraints.ToString(),["interpolation"]=body.interpolation.ToString(),["detection"]=body.collisionDetectionMode.ToString()};
        return result.ToString(Newtonsoft.Json.Formatting.None);
    }
    static string Hash(string path)
    {using(var sha=SHA256.Create())using(var stream=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();}
    static string MeshKey(Mesh mesh)
    {
        if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh,out string guid,out long id))return "instance:"+mesh.GetInstanceID();
        return guid+":"+id;
    }
    void Backup(string source)
    {
        if(!Owned(source))throw new InvalidOperationException("Unexpected car dependency path: "+source);
        string file=records.Count.ToString("D6")+".bin";File.Copy(source,Path.Combine(folder,file));
        records.Add(new JObject{["source"]=source,["backup"]=file,["originalHash"]=Hash(source)});
    }

    public void Dispose()
    {
        if(disposed)return;disposed=true;
        try {if(records.Count>0 && File.Exists(Path.Combine(folder,"manifest.json")))Restore(folder,records,false);}
        finally {current=null;CarVisualBatchBuilder.SkipForBenchmark=previousSkip;}
    }

    static void Restore(string folder,JArray records,bool recovery)
    {
        string workspace=Path.GetFullPath(".")+Path.DirectorySeparatorChar;
        string backupRoot=Path.GetFullPath(folder)+Path.DirectorySeparatorChar;
        foreach(JObject record in records)
        {
            string source=(string)record["source"],backup=Path.GetFullPath(Path.Combine(folder,(string)record["backup"]));
            if(!Owned(source) || !Path.GetFullPath(source).StartsWith(workspace,StringComparison.OrdinalIgnoreCase) || !backup.StartsWith(backupRoot,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Invalid car recovery path.");
            if(Hash(backup)!=(string)record["originalHash"])throw new InvalidOperationException("Car backup checksum mismatch: "+source);
            string applied=(string)record["appliedHash"];
            if(!recovery && applied!=null && Hash(source)!=applied && Hash(source)!=(string)record["originalHash"])throw new InvalidOperationException("Car asset changed during build: "+source);
        }
        AssetDatabase.ReleaseCachedFileHandles();AssetDatabase.StartAssetEditing();
        try
        {
            foreach(JObject record in records)
            {
                string destination=(string)record["source"],backup=Path.Combine(folder,(string)record["backup"]);
                if(File.Exists(destination) && Hash(destination)==(string)record["originalHash"])continue;
                try {File.Copy(backup,destination,true);}
                catch(IOException)
                {
                    // Windows can retain a mapped view after its cached handle is closed.
                    // Replace the directory entry instead of overwriting that mapped file in place.
                    string temporary=Path.Combine(folder,Guid.NewGuid().ToString("N")+".restore");
                    File.Copy(backup,temporary);
                    try {File.Replace(temporary,destination,null);}
                    finally {if(File.Exists(temporary))File.Delete(temporary);}
                }
            }
        }
        finally {AssetDatabase.StopAssetEditing();}
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var set=AssetDatabase.LoadAssetAtPath<ItemLodSet>(ItemDetailBuilder.SetPath);
        if(set!=null)
        {
            var entries=(List<ItemLodSet.Entry>)typeof(ItemLodSet).GetField("entries",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(set);
            set.SetEntries(new List<ItemLodSet.Entry>(entries)); // invalidate the nonserialized lookup after reimport
        }
        bool restored=records.All(r=>Hash((string)r["source"])==(string)r["originalHash"]);
        File.WriteAllText(Status,new JObject{["restored"]=restored,["files"]=records.Count,["backup"]=folder}.ToString());
        if(!restored)throw new InvalidOperationException("Car dependency restoration did not match; backups retained.");
    }

    [MenuItem("Tools/Toy Car Sort/CrazyGames/Restore Temporary Car Dependencies")]
    public static void Recover()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)throw new InvalidOperationException("Editor must be idle for car recovery.");
        string folder=File.ReadAllText(Pointer);
        if(!Path.GetFullPath(folder).StartsWith(Path.GetFullPath(Evidence)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Invalid car backup location.");
        Restore(folder,JArray.Parse(File.ReadAllText(Path.Combine(folder,"manifest.json"))),true);
        current=null;CarVisualBatchBuilder.SkipForBenchmark=false;
    }
}

// Only the temporary build scene is changed. Saved scene files are never edited.
public sealed class CrazyGamesCarBuildSceneProcessor : IProcessSceneWithReport
{
    public int callbackOrder=>-990;
    public void OnProcessScene(Scene scene,BuildReport report){if(report!=null)CrazyGamesCarBuildDependencies.ProcessScene(scene);}
}
