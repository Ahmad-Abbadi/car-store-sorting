using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TinyRiders;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public class ThemedShelfArtTests
{
    [Test]
    public void SignatureMigration_PreservesShelfIdentitySlotsAndPile()
    {
        EditorSceneManager.OpenScene(CompactCollectionBuilder.ScenePath);
        var current=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var backup=EditorSceneManager.OpenPreviewScene(Path.GetFullPath(ThemedShelfArtBuilder.Evidence+"/Level_before_signature_art.unity"));
        try
        {
            var oldShelves=backup.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SortingShelf>(true)).OrderBy(s=>s.SaveId).ToArray();
            var newShelves=current.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SortingShelf>(true)).OrderBy(s=>s.SaveId).ToArray();
            Assert.AreEqual(80,oldShelves.Length);Assert.AreEqual(oldShelves.Length,newShelves.Length);
            for(int i=0;i<oldShelves.Length;i++)
            {
                var old=oldShelves[i];var now=newShelves[i];Assert.AreEqual(old.SaveId,now.SaveId);
                Assert.AreEqual(old.transform.position,now.transform.position);Assert.Less(Quaternion.Angle(old.transform.rotation,now.transform.rotation),.001f);
                Assert.AreEqual(old.GetComponent<ThemeShelfBinding>().themeId,now.GetComponent<ThemeShelfBinding>().themeId);
                for(int slot=0;slot<15;slot++)
                {Assert.AreEqual(old.GetSpot(slot).name,now.GetSpot(slot).name);Assert.Less(Vector3.Distance(old.GetSpot(slot).position,now.GetSpot(slot).position),.0001f);}
                var a=old.GetComponentsInChildren<BoxCollider>();var b=now.GetComponentsInChildren<BoxCollider>();Assert.AreEqual(a.Length,b.Length);
                for(int c=0;c<a.Length;c++){Assert.AreEqual(a[c].center,b[c].center);Assert.AreEqual(a[c].size,b[c].size);Assert.AreEqual(a[c].isTrigger,b[c].isTrigger);}
            }
            var before=backup.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ThemedCollectionSession>()).Single();
            var after=current.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ThemedCollectionSession>()).Single();
            Assert.AreEqual(before.catalog,after.catalog);Assert.AreEqual(before.roomLayout,after.roomLayout);Assert.AreEqual(before.pileLayout,after.pileLayout);
            Assert.AreEqual(before.seed,after.seed);Assert.AreEqual(before.copiesPerDesign,after.copiesPerDesign);Assert.AreEqual(before.pileOrigin.position,after.pileOrigin.position);
            File.WriteAllText(ThemedShelfArtBuilder.Evidence+"/scene_identity_audit.txt","PASS: 80 shelf IDs and poses, 1200 named slots, box colliders, catalog, copies, seed, room and pile references preserved.\n");
        }
        finally{EditorSceneManager.ClosePreviewScene(backup);}
    }

    [Test]
    public void AllThemes_HaveBudgetedArtAndUnchangedMarkers()
    {
        var catalog = ThemedShelfArtBuilder.Catalog;
        var cars = AssetDatabase.LoadAssetAtPath<ThemedCarCatalog>(CompactCollectionBuilder.CatalogPath);
        CollectionAssert.AreEquivalent(cars.themes.Where(t=>t.enabled).Select(t=>t.id), catalog.entries.Select(e=>e.themeId));
        Assert.AreEqual(40,catalog.entries.Select(e=>e.prefab).Distinct().Count());
        var baseline = ExactThemeShelfBuilder.Markers(ExactThemeShelfBuilder.EnsurePrefab());
        foreach(var entry in catalog.entries)
        {
            Assert.LessOrEqual(entry.decorationTriangles,8000,entry.themeId);
            Assert.Greater(entry.decorationTriangles,0,entry.themeId);
            Assert.AreEqual(1,entry.signatureRevision,entry.themeId);
            Assert.AreEqual(4,entry.materials.Length);
            Assert.Greater(entry.topperTriangles,0);
            Assert.LessOrEqual(entry.topperSizeMeters.x,.601f);
            Assert.LessOrEqual(entry.topperSizeMeters.z,.351f);
            Assert.That(entry.postHeightMeters,Is.InRange(.1f,.12f));
            Assert.AreEqual(1,entry.prefab.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="ThemeTopper"));
            var signature=entry.materials.Single(m=>m.name=="Signature_"+entry.themeId);
            foreach(var property in new[]{"_BaseMap","_BumpMap","_MetallicGlossMap"})
            {Assert.AreEqual(2048,signature.GetTexture(property).width);Assert.AreEqual(2048,signature.GetTexture(property).height);}
            var atlas=entry.materials.Single(m=>m.name=="Shelf_"+entry.themeId).GetTexture("_BaseMap");
            Assert.AreEqual(1024,atlas.width);Assert.AreEqual(1024,atlas.height);
            var markers=ExactThemeShelfBuilder.Markers(entry.prefab);
            Assert.AreEqual(15,markers.Length);
            foreach(var marker in markers)
            {
                var old=baseline.Single(m=>m.name==marker.name);
                Assert.Less(Vector3.Distance(old.position,marker.position),.0001f,entry.themeId+"/"+marker.name);
                Assert.Less(Quaternion.Angle(old.rotation,marker.rotation),.01f);
            }
            Assert.AreEqual(0,entry.prefab.GetComponentsInChildren<Collider>(true).Length);
            Assert.AreEqual(0,entry.prefab.GetComponentsInChildren<Light>(true).Length);
            Assert.True(entry.materials.All(m=>m.GetFloat("_Surface")==0));
        }
    }

    [Test]
    public void CurrentRoom_SharesThemeMeshesAndPreservesCapacity()
    {
        EditorSceneManager.OpenScene(CompactCollectionBuilder.ScenePath);
        ThemedShelfArtBuilder.Audit();
        foreach(var shelf in Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None))
        {
            var label=shelf.transform.Find("Theme Header").GetComponent<TMPro.TextMeshPro>();
            label.ForceMeshUpdate();
            Assert.False(label.isTextOverflowing,"Theme label does not fit: "+label.text);
            var art=shelf.transform.Find("TR_Shelf_Base_01");
            var topper=art.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="ThemeTopper");
            float top=topper.GetComponentsInChildren<MeshRenderer>(true).Max(r=>r.bounds.max.y);
            Assert.LessOrEqual(top,2.36f,"Topper exceeds the reserved display band: "+shelf.SaveId);
            foreach(var sign in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name.StartsWith("Category ")||r.name=="Directory Board"))
                Assert.GreaterOrEqual(sign.bounds.min.y-top,.1f,"Overhead signage clearance: "+shelf.SaveId+" / "+sign.name);
        }
        CaptureCheckpoints("empty");
    }

    public static void CaptureCheckpoints(string state)
    {
        foreach(string id in new[]{"TH_WILD_WEST","TH_PIRATE","TH_CANDY","TH_SCIENCE_LAB"})
        {
            var shelf=Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None)
                .Where(s=>s.GetComponent<ThemeShelfBinding>().themeId==id).OrderBy(s=>s.SaveId,StringComparer.Ordinal).First();
            Capture(state+"_"+id,shelf,false);
            Capture(state+"_"+id+"_aisle",shelf,true);
        }
    }

    static void Capture(string name,SortingShelf shelf,bool aisle)
    {
        Directory.CreateDirectory(ThemedShelfArtBuilder.Evidence);
        var go=new GameObject("Themed Shelf Review Camera");var camera=go.AddComponent<Camera>();
        if(Camera.main!=null)camera.CopyFrom(Camera.main);
        camera.enabled=false;camera.fieldOfView=aisle?58:45;camera.nearClipPlane=.02f;camera.farClipPlane=40;
        var target=shelf.transform.position+Vector3.up*1.18f;
        camera.transform.position=shelf.transform.position+Vector3.up*1.6f+shelf.transform.forward*(aisle?3.5f:3.3f)+shelf.transform.right*(aisle?1.2f:0);
        camera.transform.LookAt(target);
        var rt=new RenderTexture(1440,1080,24);var previous=RenderTexture.active;Texture2D texture=null;
        try
        {
            camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            texture=new Texture2D(1440,1080,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1440,1080),0,0);texture.Apply();
            File.WriteAllBytes(ThemedShelfArtBuilder.Evidence+"/Unity_"+name+".png",texture.EncodeToPNG());
        }
        finally{camera.targetTexture=null;RenderTexture.active=previous;Object.DestroyImmediate(go);Object.DestroyImmediate(rt);if(texture!=null)Object.DestroyImmediate(texture);}
    }
}
