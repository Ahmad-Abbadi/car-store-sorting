using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TinyRiders.Editor
{
    public class UpgradeFactoryTests
    {
        [Test]
        public void UpgradeFactory_HasCompleteVisualBindingsAndOriginalInteraction()
        {
            var root=PrefabUtility.LoadPrefabContents(UpgradeFactoryBuilder.PrefabPath);
            try
            {
                Assert.NotNull(root.GetComponent<UpgradeBox>());
                Assert.AreEqual(1,root.GetComponentsInChildren<Collider>(true).Length,"Decorative parts must not intercept interaction rays");
                var p=root.GetComponentInChildren<UpgradeFactoryPresentation>();
                Assert.NotNull(p);Assert.NotNull(p.turntable);Assert.NotNull(p.leftArm);Assert.NotNull(p.rightArm);
                Assert.NotNull(p.scanner);Assert.NotNull(p.readyIndicators);Assert.NotNull(p.visibilityBody);
                Assert.NotNull(p.completionClip);Assert.NotNull(p.completionParticles);
                Assert.False(p.scanner.gameObject.activeSelf);
                Assert.Zero(root.GetComponentsInChildren<Light>(true).Length);
                Assert.Zero(root.GetComponentsInChildren<Rigidbody>(true).Length);
                Assert.True(root.GetComponentsInChildren<MeshRenderer>(true).All(r=>r.sharedMaterials.All(m=>m && m.shader && m.shader.isSupported)));
                var shell=root.transform.Find("Visual/Shell");
                foreach(var filter in shell.GetComponentsInChildren<MeshFilter>())
                {
                    var bounds=filter.sharedMesh.bounds;
                    Assert.LessOrEqual(bounds.max.x,.401f);Assert.GreaterOrEqual(bounds.min.x,-.401f);
                    Assert.LessOrEqual(bounds.max.z,.401f);Assert.GreaterOrEqual(bounds.min.z,-.401f);
                }
                Assert.LessOrEqual(root.GetComponentsInChildren<MeshFilter>(true).Sum(f=>f.sharedMesh.triangles.Length/3),33732);
                Assert.LessOrEqual(root.GetComponentsInChildren<Renderer>(true).Length,36);
                Assert.True(root.GetComponentsInChildren<Transform>(true).All(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0));
                var signs=root.GetComponentsInChildren<TMPro.TMP_Text>(true);
                Assert.AreEqual(3,signs.Length);
                Assert.True(signs.All(s=>s.GetComponent<LocalizedText>() && !string.IsNullOrEmpty(s.GetComponent<LocalizedText>().Key)));
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }

        [Test]
        public void BlenderFactory_TexturesAndAnimatedPartsStayWithinBudget()
        {
            var mat=AssetDatabase.LoadAssetAtPath<Material>(BlenderFactoryAssembler.Art+"/FactoryAtlas.mat");
            foreach(var slot in new[]{"_BaseMap","_BumpMap","_MetallicGlossMap","_OcclusionMap"})
            {
                var map=mat.GetTexture(slot);Assert.NotNull(map,slot);Assert.AreEqual(2048,map.width);
            }
            var root=PrefabUtility.LoadPrefabContents(UpgradeFactoryBuilder.PrefabPath);
            try
            {
                var p=root.GetComponentInChildren<UpgradeFactoryPresentation>();
                // Sample the full display revolution and the +/-12 degree purchase stroke.
                var meshes=new[]{p.turntable,p.leftArm,p.rightArm}
                    .SelectMany(t=>t.GetComponentsInChildren<MeshFilter>())
                    .ToDictionary(f=>f,f=>f.sharedMesh.vertices);
                for(int step=0;step<=36;step++)
                {
                    float stroke=12*Mathf.Sin(step*Mathf.PI/18);
                    p.turntable.localRotation=Quaternion.Euler(0,step*10,0);
                    p.leftArm.localRotation=Quaternion.Euler(0,0,stroke);
                    p.rightArm.localRotation=Quaternion.Euler(0,0,-stroke);
                    foreach(var pair in meshes)
                    {
                        var matrix=root.transform.worldToLocalMatrix*pair.Key.transform.localToWorldMatrix;
                        float maxX=0,maxZ=0;
                        foreach(var v in pair.Value)
                        {
                            var local=matrix.MultiplyPoint3x4(v);
                            maxX=Mathf.Max(maxX,Mathf.Abs(local.x));maxZ=Mathf.Max(maxZ,Mathf.Abs(local.z));
                        }
                        Assert.LessOrEqual(maxX,.401f,pair.Key.name+" width");
                        Assert.LessOrEqual(maxZ,.401f,pair.Key.name+" depth");
                    }
                }
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }
}
