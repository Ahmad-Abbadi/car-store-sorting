using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TinyRiders;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

// Updates only the staged cars in the existing menu; never regenerates its room or cameras.
public static class MenuCarBatchRefresh
{
    const string Path="Assets/00_Project/00_Scenes/MainMenu Scene.unity";
    public static string Preview()=>Run(false);
    public static string Apply()=>Run(true);
    static string Key(Object o)
    {
        if(o==null)return "";
        return AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o,out string guid,out long id)?guid+":"+id:o.GetType().Name+":"+o.name;
    }
    static void SameMatrix(Matrix4x4 a,Matrix4x4 b,string message)
    {for(int i=0;i<16;i++)if(Mathf.Abs(a[i]-b[i])>.0001f)throw new Exception(message+" matrix component "+i+" actual="+a[i].ToString("R")+" expected="+b[i].ToString("R")+" delta="+Mathf.Abs(a[i]-b[i]).ToString("R"));}
    static Transform Find(Transform root,string path)=>path.Length==0?root:root.Find(path);
    static Matrix4x4 Relative(Transform root,Transform part)
    {
        var matrix=Matrix4x4.identity;
        for(var t=part;t!=root;t=t.parent)
        {
            if(t==null)throw new Exception("Part is outside visual root");
            matrix=Matrix4x4.TRS(t.localPosition,t.localRotation,t.localScale)*matrix;
        }
        return matrix;
    }

    static string Run(bool apply)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)throw new Exception("Idle Editor required");
        if(apply && SceneManager.GetActiveScene().isDirty)throw new Exception("Save current scene before menu refresh");
        var scene=apply?EditorSceneManager.OpenScene(Path):EditorSceneManager.OpenPreviewScene(Path);
        byte[] original=File.ReadAllBytes(Path);
        var lodField=typeof(CarVisualBatchBuilder).GetField("lods",BindingFlags.Static|BindingFlags.NonPublic);
        object previousLods=lodField.GetValue(null);
        try
        {
            var roots=scene.GetRootGameObjects();
            var stage=roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="Staged Collection Cars");
            var beforeCars=stage.Cast<Transform>().ToArray();
            var beforeOther=roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Where(t=>!t.IsChildOf(stage))
                .ToDictionary(t=>t,t=>t.localToWorldMatrix);
            var entries=AssetDatabase.LoadAssetAtPath<ThemedCarCatalog>(ItemDetailBuilder.CatalogPath).entries.Where(e=>e.enabled)
                .ToDictionary(e=>e.prefab.name);
            var carObjects=new HashSet<Object>(beforeCars.SelectMany(c=>c.GetComponentsInChildren<Transform>(true))
                .SelectMany(t=>t.GetComponents<Component>().Cast<Object>().Append(t.gameObject)));
            // Scene hierarchy links update through parenting. Other external bindings must stay valid.
            foreach(var component in roots.SelectMany(r=>r.GetComponentsInChildren<Component>(true)).Where(c=>c!=null && !(c is Transform) && !carObjects.Contains(c)))
            {
                var property=new SerializedObject(component).GetIterator();
                while(property.Next(true))
                    if(property.propertyType==SerializedPropertyType.ObjectReference && property.objectReferenceValue!=null && carObjects.Contains(property.objectReferenceValue))
                        throw new Exception("External car binding requires preservation: "+component.name+"/"+property.propertyPath);
            }
            var lods=AssetDatabase.LoadAssetAtPath<ItemLodSet>(ItemDetailBuilder.SetPath);
            lodField.SetValue(null,lods);
            Mesh Native(Mesh mesh)=>mesh!=null && lods.TryGet(mesh,out var entry) && entry.bakedMesh!=null?entry.bakedMesh:mesh;
            int replaced=0,renderersBefore=0,renderersAfter=0,partsVerified=0;
            var verified=new HashSet<string>();
            foreach(var old in beforeCars)
            {
                var visual=old.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>entries.ContainsKey(t.name));
                if(visual==null)throw new Exception("Unidentified staged car: "+old.name);
                var entry=entries[visual.name];
                var source=entry.prefab.transform;
                // Validate the stored visual still matches its authored design before replacing it.
                foreach(var expected in source.GetComponentsInChildren<MeshRenderer>(true))
                {
                    string partPath=AnimationUtility.CalculateTransformPath(expected.transform,source);
                    var actual=Find(visual,partPath)?.GetComponent<MeshRenderer>();
                    if(actual==null)throw new Exception("Missing staged part: "+entry.collectibleId+"/"+partPath);
                    if(Key(Native(actual.GetComponent<MeshFilter>()?.sharedMesh))!=Key(Native(expected.GetComponent<MeshFilter>()?.sharedMesh)))
                        throw new Exception("Staged mesh override: "+entry.collectibleId+"/"+partPath);
                    if(!actual.sharedMaterials.Select(Key).SequenceEqual(expected.sharedMaterials.Select(Key)) || actual.enabled!=expected.enabled ||
                        actual.shadowCastingMode!=expected.shadowCastingMode || actual.receiveShadows!=expected.receiveShadows ||
                        actual.sortingLayerID!=expected.sortingLayerID || actual.sortingOrder!=expected.sortingOrder)
                        throw new Exception("Staged renderer override: "+entry.collectibleId+"/"+partPath);
                    SameMatrix(Relative(visual,actual.transform),Relative(source,expected.transform),
                        "Staged part pose differs: "+entry.collectibleId+"/"+partPath);
                    partsVerified++;
                }
                var prefab=old.name.StartsWith("Floor display ",StringComparison.Ordinal)?entry.sortingItem.WorldItem.gameObject:entry.sortingItem.PlacedOnShelfPrefab.gameObject;
                var currentVisual=(Transform)new SerializedObject(prefab.GetComponent<ItemVisualGroup>()).FindProperty("visualRoot").objectReferenceValue;
                if(verified.Add(entry.collectibleId))
                    CarVisualBatchBuilder.ValidateCounts(entry.prefab,currentVisual.gameObject);
                var replacement=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
                replacement.name=old.name;replacement.transform.SetParent(stage,false);replacement.transform.SetSiblingIndex(old.GetSiblingIndex());
                replacement.transform.localPosition=old.localPosition;replacement.transform.localRotation=old.localRotation;replacement.transform.localScale=old.localScale;
                replacement.layer=old.gameObject.layer;replacement.SetActive(old.gameObject.activeSelf);
                var newVisual=(Transform)new SerializedObject(replacement.GetComponent<ItemVisualGroup>()).FindProperty("visualRoot").objectReferenceValue;
                newVisual.localPosition=visual.localPosition;newVisual.localRotation=visual.localRotation;newVisual.localScale=visual.localScale;
                SameMatrix(newVisual.localToWorldMatrix,visual.localToWorldMatrix,"Visual world pose changed: "+entry.collectibleId);
                // Detach authoring-only links in this visual-only snapshot.
                if(PrefabUtility.IsAnyPrefabInstanceRoot(replacement))
                    PrefabUtility.UnpackPrefabInstance(replacement,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                typeof(CinematicMenuBuilder).GetMethod("StripGameplay",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{replacement});
                renderersBefore+=old.GetComponentsInChildren<MeshRenderer>(true).Length;
                renderersAfter+=replacement.GetComponentsInChildren<MeshRenderer>(true).Length;
                Object.DestroyImmediate(old.gameObject);replaced++;
            }
            foreach(var pair in beforeOther)SameMatrix(pair.Key.localToWorldMatrix,pair.Value,"Non-car scene transform changed: "+pair.Key.name);
            if(stage.childCount!=beforeCars.Length)throw new Exception("Staged car count changed");
            string report="Staged cars="+replaced+" designs="+verified.Count+" authored parts checked="+partsVerified+
                " renderers before="+renderersBefore+" after="+renderersAfter+"; world poses and external bindings preserved; room/cameras unchanged; apply="+apply;
            if(apply)
            {
                File.WriteAllBytes("IntegrationEvidence/RenderingArtifacts/menu-before-car-refresh.unity",original);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            File.WriteAllText("IntegrationEvidence/RenderingArtifacts/menu-car-refresh-"+(apply?"applied":"preview")+".txt",report);
            return report;
        }
        finally
        {
            lodField.SetValue(null,previousLods);
            if(!apply)EditorSceneManager.ClosePreviewScene(scene);
            if(!apply && !original.SequenceEqual(File.ReadAllBytes(Path)))throw new Exception("Preview changed saved menu bytes");
        }
    }
}
