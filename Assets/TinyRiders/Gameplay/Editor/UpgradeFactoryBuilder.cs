using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TinyRiders.Editor
{
    /// <summary> Blender assembly entry point and shared display-mesh utilities. </summary>
    public static class UpgradeFactoryBuilder
    {
        public const string PrefabPath = "Assets/00_Project/02_prefabs/Systems/Upgrade Box.prefab";
        public const string Art = "Assets/TinyRiders/Art/UpgradeStation/Factory";



        [MenuItem("Tiny Riders/Art/Rebuild Mini Tuning Factory")]
        public static void Build() => BlenderFactoryAssembler.Build();

        internal static void CreateDisplayCar(Transform table)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ithappy/Modular_Racing_Cars/Prefabs/Car_01/Car_01.prefab");
            var car=Group("Miniature rally car",table);
            car.localRotation=Quaternion.Euler(0,55,0); car.localPosition=new Vector3(0,.024f,0); car.localScale=Vector3.one*.108f;
            var selected=new HashSet<string>{"Car_01","Car_01_Decals","Car_01_Engine","Car_01_Headlight_Simple","Car_01_FrontBumper_Simple","Car_01_RearBumper_Simple","Car_01_Spoiler","Car_01_Wheel_FL","Car_01_Wheel_FR","Car_01_Wheel_RL","Car_01_Wheel_RR"};
            foreach(var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if(!selected.Contains(filter.name))continue;
                var go=new GameObject(filter.name); go.transform.SetParent(car,false);
                var matrix=prefab.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                go.transform.localPosition=matrix.GetColumn(3); go.transform.localRotation=matrix.rotation; go.transform.localScale=matrix.lossyScale;
                go.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;
                go.AddComponent<MeshRenderer>().sharedMaterials=filter.GetComponent<MeshRenderer>().sharedMaterials;
            }
        }

        private static Transform Group(string name,Transform parent)
        {var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;}
        private static Transform MeshObject(Transform parent,string name,Mesh mesh,Material mat,Vector3 position)
        {
            var t=Group(name,parent);t.localPosition=position;t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=t.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=mat;return t;
        }
        internal static void Combine(Transform parent,string key)
        {
            var groups=new Dictionary<Material,List<CombineInstance>>();
            foreach(var f in parent.GetComponentsInChildren<MeshFilter>(true))
            {
                var renderer=f.GetComponent<MeshRenderer>();if(!renderer||!f.sharedMesh)continue;
                for(int sub=0;sub<f.sharedMesh.subMeshCount;sub++)
                {
                    var material=renderer.sharedMaterials[Mathf.Min(sub,renderer.sharedMaterials.Length-1)];if(!material)continue;
                    if(!groups.ContainsKey(material))groups[material]=new List<CombineInstance>();
                    groups[material].Add(new CombineInstance{mesh=f.sharedMesh,subMeshIndex=sub,transform=parent.worldToLocalMatrix*f.transform.localToWorldMatrix});
                }
            }
            foreach(var pair in groups)
            {
                string meshName=key+"_"+pair.Key.name.Replace(" ","_");
                var mesh=new Mesh{name=meshName,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(pair.Value.ToArray(),true,true);mesh.RecalculateBounds();
                string path=Art+"/"+meshName+".asset";
                var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(existing){existing.Clear();EditorUtility.CopySerialized(mesh,existing);existing.UploadMeshData(false);EditorUtility.SetDirty(existing);Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,path);
                // Output is created after gathering input, then old geometry is removed below.
                MeshObject(parent,"Combined "+pair.Key.name,mesh,pair.Key,Vector3.zero);
            }
            foreach(Transform child in parent.Cast<Transform>().ToArray())if(!child.name.StartsWith("Combined "))Object.DestroyImmediate(child.gameObject);
        }
    }
}


