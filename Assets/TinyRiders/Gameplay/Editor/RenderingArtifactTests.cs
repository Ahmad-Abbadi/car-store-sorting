using NUnit.Framework;
using UnityEditor;
using System.Linq;
using UnityEngine;
using UnityEditor.SceneManagement;
using Unity.Cinemachine;
using System.Collections.Generic;

public class RenderingArtifactTests
{
    [TestCase("Assets/TinyRiders/Art/Shelves/Themed/Models/TH_AVIATION.fbx")]
    [TestCase("Assets/TinyRiders/Gameplay/Rendering/Shelf_shared.asset")]
    [TestCase("Assets/TinyRiders/Gameplay/Rendering/CarBatches/Car_LOD0.asset")]
    [TestCase("Assets/TinyRiders/Art/MutedShopSurfaces/Meshes/Store Ceiling.asset")]
    [TestCase("Assets/TinyRiders/Art/MutedShopSurfaces/Meshes/Gallery wall.asset")]
    [TestCase("Assets/TinyRiders/Art/ShopInterior/Meshes/Skyline Mural.asset")]
    [TestCase("Assets/TinyRiders/Art/RacingFloor/Lane Markings.asset")]
    [TestCase("Assets/TinyRiders/Gameplay/TutorialAssets/Gallery wall.asset")]
    [TestCase("Assets\\TinyRiders\\Gameplay\\Rendering\\Shelf_shared.asset")]
    public void LayeredEnvironment_PreservesVertexAndUVPrecision(string path)
    {
        Assert.AreEqual(ModelImporterMeshCompression.Off, CrazyGamesMeshCompression.CompressionFor(path));
    }

    [TestCase("Assets/TinyRiders/ThemedCollection/Models/TH_AVIATION/C001.fbx")]
    [TestCase("Assets/TinyRiders/Art/ThemeAccessories/CarAccessory.asset")]
    [TestCase("Assets/ithappy/Modular_Racing_Cars/Models/Car.fbx")]
    public void UnrelatedMeshes_StillUseReleaseCompression(string path)
    {
        Assert.AreEqual(ModelImporterMeshCompression.High, CrazyGamesMeshCompression.CompressionFor(path));
    }

    [Test]
    public void ExactUnmergedCarParts_PreservePrecisionWithoutChangingOtherImports()
    {
        const string part = "Assets/TinyRiders/ThemedCollection/Models/Accessory.fbx";
        var precise = new HashSet<string> { part };
        Assert.AreEqual(ModelImporterMeshCompression.Off, CrazyGamesMeshCompression.CompressionFor(part, precise));
        Assert.AreEqual(ModelImporterMeshCompression.High, CrazyGamesMeshCompression.CompressionFor(part + ".other.fbx", precise));
    }

    [Test]
    public void AllShelfSymbols_AreInFrontOfTheirBackingPlaques()
    {
        foreach (var entry in ThemedShelfArtBuilder.Catalog.entries)
        {
            var filters = entry.prefab.GetComponentsInChildren<MeshFilter>(true);
            var icons = filters.Where(f => f.name.StartsWith("SignatureIcon")).ToArray();
            Assert.AreEqual(2, icons.Length, entry.themeId);
            foreach (var icon in icons)
            {
                string suffix = icon.name.Substring("SignatureIcon".Length);
                var plaque = filters.Single(f => f.name == "SignaturePlaque" + suffix);
                var normal = icon.transform.localToWorldMatrix.inverse.transpose.MultiplyVector(icon.sharedMesh.normals[0]).normalized;
                float iconPlane = icon.sharedMesh.vertices.Min(v => Vector3.Dot(icon.transform.TransformPoint(v), normal));
                float plaqueFront = plaque.sharedMesh.vertices.Max(v => Vector3.Dot(plaque.transform.TransformPoint(v), normal));
                Assert.Greater(iconPlane - plaqueFront, .0005f, entry.themeId + "/" + icon.name);
            }
        }
    }

    [TestCase("Assets/00_Project/00_Scenes/Level.unity")]
    [TestCase("Assets/00_Project/00_Scenes/MainMenu Scene.unity")]
    [TestCase("Assets/00_Project/00_Scenes/Tutorial.unity")]
    public void ShopAndCinematicCameras_UseRoomSizedFarPlanes(string path)
    {
        var scene = EditorSceneManager.OpenPreviewScene(path);
        try
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var camera in root.GetComponentsInChildren<Camera>(true))
                    Assert.AreEqual(ShopCameraClipping.FarDistance, camera.farClipPlane, path + "/" + camera.name);
                foreach (var camera in root.GetComponentsInChildren<CinemachineCamera>(true))
                    Assert.AreEqual(ShopCameraClipping.FarDistance, camera.Lens.FarClipPlane, path + "/" + camera.name);
            }
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    [TestCase("Assets/00_Project/00_Scenes/Level.unity")]
    [TestCase("Assets/00_Project/00_Scenes/MainMenu Scene.unity")]
    [TestCase("Assets/00_Project/00_Scenes/Tutorial.unity")]
    public void BuildShelfPruning_PreservesVisibleGeometrySlotsAndCollisions(string path)
    {
        var originalBytes = System.IO.File.ReadAllBytes(path);
        var scene = EditorSceneManager.OpenPreviewScene(path);
        try
        {
            var roots = scene.GetRootGameObjects();
            var inactiveShelf = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(s => s.Find("TR_Shelf_Base_01") != null && s.Find("Batched Shelf Art") != null);
            var inactiveSource = inactiveShelf?.Find("TR_Shelf_Base_01").GetComponentsInChildren<MeshFilter>(true);
            if (inactiveShelf != null) inactiveShelf.gameObject.SetActive(false);
            string Snapshot()
            {
                var visible = roots.SelectMany(r => r.GetComponentsInChildren<MeshRenderer>(true))
                    .Where(r => r.enabled).Select(r => r.GetInstanceID() + ":" +
                        r.GetComponent<MeshFilter>()?.sharedMesh?.GetInstanceID() + ":" + r.localToWorldMatrix.ToString("R"));
                var gameplay = roots.SelectMany(r => r.GetComponentsInChildren<Collider>(true)).Cast<Object>()
                    .Concat(roots.SelectMany(r => r.GetComponentsInChildren<SortingShelf>(true)))
                    .Select(o => EditorJsonUtility.ToJson(o));
                return string.Join("\n", visible.Concat(gameplay));
            }
            string before = Snapshot();
            int removed = ShopShelfBuildGeometry.StripSourceBuffers(scene);
            if (path.EndsWith("/Level.unity") || path.EndsWith("/MainMenu Scene.unity")) Assert.Greater(removed, 0);
            if (inactiveSource != null) Assert.IsTrue(inactiveSource.All(f => f.sharedMesh == null), "Inactive stages must also omit duplicated shelf buffers.");
            Assert.AreEqual(before, Snapshot(), "Visible meshes, transforms, shelf slots and colliders must stay identical.");
            Assert.AreEqual(0, ShopShelfBuildGeometry.StripSourceBuffers(scene), "Pruning must be idempotent.");
            if (path.EndsWith("/Level.unity") || path.EndsWith("/MainMenu Scene.unity"))
                Assert.IsFalse(EditorUtility.CollectDependencies(roots.Cast<Object>().ToArray()).OfType<Mesh>()
                    .Any(m => AssetDatabase.GetAssetPath(m).StartsWith("Assets/TinyRiders/Art/Shelves/")),
                    "Source prefab connections must not retain FBX buffers already represented by the shelf caches.");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
        CollectionAssert.AreEqual(originalBytes, System.IO.File.ReadAllBytes(path), "Saved scene must stay untouched.");
    }
}
