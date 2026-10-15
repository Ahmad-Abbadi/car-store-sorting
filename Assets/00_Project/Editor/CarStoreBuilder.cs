using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using TinyRiders;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Idempotent authoring of the full TinyRiders sorting store.</summary>
public static class CarStoreBuilder
{
    private const string Root = "Assets/00_Project/GeneratedCarStore";
    private const string ScenePath = "Assets/00_Project/00_Scenes/Level.unity";
    private const string Stamp = "CarStoreBuilder.v1";
    private static readonly Vector3 StoreOffset = new Vector3(500, 0, 0);
    private static Material floorMaterial, shelfMaterial, binMaterial, signMaterial;
    private static Material wallMaterial, ceilingMaterial, tealMaterial, creamMaterial, pathMaterial;

    [InitializeOnLoadMethod]
    private static void QueueFirstBuild()
    {
        if (System.IO.File.Exists("Assets/TinyRiders/Scenes/ThemedSorting.unity")) return;
        if (EditorPrefs.GetBool(Stamp, false)) return;
        EditorApplication.update += TryFirstBuild;
    }

    private static void TryFirstBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;
        EditorApplication.update -= TryFirstBuild;
        try
        {
            Generate();
            EditorPrefs.SetBool(Stamp, true);
        }
        catch (Exception ex) { Debug.LogError("CAR_STORE_BUILD_FAILED: " + ex); }
    }

    [MenuItem("Tools/Car Store/Generate Complete Store")]
    public static void Generate()
    {
        if (System.IO.File.Exists(CompactCollectionBuilder.CatalogPath))
        {
            CompactCollectionBuilder.Build();
            return;
        }
        EnsureFolder(Root);
        EnsureFolder(Root + "/Items");
        EnsureFolder(Root + "/Prefabs");
        EnsureFolder(Root + "/Models");
        EnsureFolder(Root + "/Materials");
        CarVariantDefinition[] variants = AssetDatabase.FindAssets("t:CarVariantDefinition", new[] { "Assets/TinyRiders/Data/Variants" })
            .Select(g => AssetDatabase.LoadAssetAtPath<CarVariantDefinition>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(v => v != null && v.buildCode == "STD").OrderBy(v => v.variantId, StringComparer.Ordinal).ToArray();
        if (variants.Length != 75 || variants.Any(v => v.prefab == null || v.thumbnail == null))
            throw new InvalidOperationException("TinyRiders gameplay catalog must contain 75 Standard variants with a prefab and thumbnail.");
        var ids = new HashSet<string>(variants.Select(v => v.variantId));
        if (ids.Count != variants.Length) throw new InvalidOperationException("Duplicate TinyRiders variant ID.");

        var categories = new Dictionary<string, Catagory_SO>();
        foreach (CarVariantDefinition variant in variants)
        {
            if (categories.ContainsKey(variant.baseCarId)) continue;
            string path = Root + "/Models/Model_" + variant.baseCarId + ".asset";
            Catagory_SO category = LoadOrCreate<Catagory_SO>(path);
            category.CatagoryID = variant.baseCarId;
            category.CatagoryName = CarModelCatalog.EnglishName(variant.baseCarId);
            category.CatagoryIcon = variant.thumbnail;
            categories.Add(variant.baseCarId, category);
            EditorUtility.SetDirty(category);
        }
        if (categories.Count != 15) throw new InvalidOperationException("Expected 15 car models.");

        var items = new Dictionary<string, SortableItem_SO>();
        foreach (CarVariantDefinition variant in variants)
        {
            SortableItem_SO item = LoadOrCreate<SortableItem_SO>(Root + "/Items/" + variant.variantId + ".asset");
            SerializedObject serialized = new SerializedObject(item);
            serialized.FindProperty("saveId").stringValue = variant.variantId;
            serialized.FindProperty("displayName").stringValue = CarModelCatalog.EnglishName(variant.baseCarId)
                + " " + variant.buildDisplayName + " " + variant.colorwayName;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            item.Catagory = categories[variant.baseCarId];
            item.ItemImage = variant.thumbnail;
            item.HeldPosition = new Vector3(0.22f, -0.25f, 0.48f);
            item.HeldRotation = new Vector3(0, 155, 0);
            items.Add(variant.variantId, item);
            EditorUtility.SetDirty(item);
        }

        foreach (CarVariantDefinition variant in variants)
        {
            SortableItem_SO item = items[variant.variantId];
            GameObject world = CreateCarPrefab(variant, item, "World", true);
            GameObject hand = CreateCarPrefab(variant, item, "Held", false);
            GameObject placed = CreateCarPrefab(variant, item, "Shelf", false);
            SerializedObject serialized = new SerializedObject(item);
            serialized.FindProperty("worldItem").objectReferenceValue = world.GetComponent<WorldItemBase>();
            serialized.FindProperty("handItem").objectReferenceValue = hand.GetComponent<HandItemBase>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            item.PlacedOnShelfPrefab = placed.GetComponent<PlacedOnShelfItemBase>();
            EditorUtility.SetDirty(item);
        }

        ItemDatabaseSO database = AssetDatabase.LoadAssetAtPath<ItemDatabaseSO>("Assets/00_Project/03_scriptableObjects/ItemDatabase.asset");
        if (database == null) throw new InvalidOperationException("Template ItemDatabase.asset is missing.");
        SerializedObject db = new SerializedObject(database);
        SerializedProperty list = db.FindProperty("items");
        list.arraySize = variants.Length;
        for (int i = 0; i < variants.Length; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = items[variants[i].variantId];
        db.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(database);

        CreateMaterials();
        CarStoreLocalization.Merge(variants);
        BuildScene(variants, items, categories, database);
        RegisterScenes();
        // Legacy Custom item assets stay on disk, but aren't part of gameplay category lists.
        foreach (Catagory_SO category in categories.Values)
        {
            category.Items = items.Values.Where(item => item.Catagory == category).OrderBy(item => item.SaveId, StringComparer.Ordinal).ToList();
            EditorUtility.SetDirty(category);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("CAR_STORE_BUILD_OK: 75 Standard variants, 525 cars in one floor pile, 75 seven-spot shelves, 15 sections, 40 x 50 store.");
    }

    private static GameObject CreateCarPrefab(CarVariantDefinition variant, SortableItem_SO item,
        string kind, bool physics)
    {
        string path = Root + "/Prefabs/" + kind + "_" + variant.variantId + ".prefab";
        GameObject root = new GameObject(kind + " " + variant.variantId);
        root.layer = 8;
        GameObject art = PrefabUtility.InstantiatePrefab(variant.prefab) as GameObject;
        if (art == null) throw new InvalidOperationException("Cannot instantiate car " + variant.variantId);
        art.name = "TinyRiders Visual";
        art.transform.SetParent(root.transform, false);
        art.transform.localPosition = Vector3.zero;
        art.transform.localRotation = Quaternion.identity;
        foreach (Collider collider in art.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
        foreach (Rigidbody body in art.GetComponentsInChildren<Rigidbody>(true)) UnityEngine.Object.DestroyImmediate(body);
        Renderer[] renderers = art.GetComponentsInChildren<Renderer>(true);
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        bool found = false;
        foreach (Renderer renderer in renderers)
        {
            if (!found) { bounds = renderer.bounds; found = true; }
            else bounds.Encapsulate(renderer.bounds);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
        if (!found) throw new InvalidOperationException("No visible mesh in " + variant.variantId);
        float scale = 0.58f / Mathf.Max(bounds.size.x, bounds.size.z, 0.01f);
        art.transform.localScale *= scale;
        if (kind == "Held") art.transform.localScale *= 0.6f;
        bounds = art.GetComponentInChildren<Renderer>().bounds;
        foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
        art.transform.localPosition = new Vector3(-bounds.center.x, 0.03f - bounds.min.y, -bounds.center.z);

        root.AddComponent<ItemVisualGroup>();
        if (kind == "World")
        {
            SortableWorldItem world = root.AddComponent<SortableWorldItem>();
            SerializedObject serialized = new SerializedObject(world);
            serialized.FindProperty("data").objectReferenceValue = item;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            TinyRiders.LooseCarPhysics.Fit(root.AddComponent<CapsuleCollider>(),
                new Bounds(new Vector3(0, .16f, 0), new Vector3(.42f, .28f, .58f)));
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.isKinematic = physics;
            TinyRiders.LooseCarPhysics.Configure(body);
        }
        else if (kind == "Held")
        {
            HandItemBase held = root.AddComponent<HandItemBase>();
            SerializedObject serialized = new SerializedObject(held);
            serialized.FindProperty("data").objectReferenceValue = item;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            root.AddComponent<PlacedOnShelfItemBase>();
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0, 0.16f, 0);
            collider.size = new Vector3(0.42f, 0.28f, 0.42f);
        }
        if (kind == "Shelf") root.transform.localScale = Vector3.one * TRShelfSceneIntegration.CompactShelfScale;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab;
    }

    private static void CreateMaterials()
    {
        floorMaterial = CreateCheckerboardMaterial();
        shelfMaterial = Material("Shelves", new Color(0.43f, 0.27f, 0.17f));
        binMaterial = Material("Stock Bins", new Color(0.49f, 0.31f, 0.19f));
        signMaterial = Material("Signs", new Color(0.06f, 0.28f, 0.30f));
        wallMaterial = Material("Warm Walls", new Color(0.90f, 0.87f, 0.79f));
        ceilingMaterial = Material("Ceiling", new Color(0.97f, 0.95f, 0.88f));
        Shader ceilingShader = Shader.Find("Universal Render Pipeline/Lit");
        if (ceilingShader != null) ceilingMaterial.shader = ceilingShader;
        ceilingMaterial.color = new Color(0.65f, 0.63f, 0.58f);
        tealMaterial = Material("Shelf Backing", new Color(0.12f, 0.34f, 0.34f));
        creamMaterial = Material("Cream Trim", new Color(0.98f, 0.87f, 0.61f));
        pathMaterial = Material("Walkway", new Color(0.82f, 0.78f, 0.69f));
    }

    private static Material Material(string name, Color color)
    {
        string path = Root + "/Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CreateCheckerboardMaterial()
    {
        string path = Root + "/Materials/Checkerboard.png";
        var texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
        Color dark = new Color(0.08f, 0.085f, 0.09f);
        Color light = new Color(0.72f, 0.70f, 0.65f);
        texture.SetPixels(new[] { dark, light, light, dark });
        texture.Apply();
        System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        Material material = Material("Floor", Color.white);
        material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        // Cube's top UVs cover the entire 40 x 50 floor. Each repeat contains two squares.
        material.mainTextureScale = new Vector2(10f, 12.5f);
        material.SetFloat("_Smoothness", 0f);
        material.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void BuildScene(CarVariantDefinition[] variants, Dictionary<string, SortableItem_SO> items,
        Dictionary<string, Catagory_SO> categories, ItemDatabaseSO database)
    {
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            foreach (GameObject old in scene.GetRootGameObjects())
                if (old.name == "Car Store Generated") UnityEngine.Object.DestroyImmediate(old);
            foreach (Section old in UnityEngine.Object.FindObjectsByType<Section>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (old != null && old.gameObject.scene == scene) UnityEngine.Object.DestroyImmediate(old.gameObject);
            foreach (SortingShelf old in UnityEngine.Object.FindObjectsByType<SortingShelf>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (old != null && old.gameObject.scene == scene) UnityEngine.Object.DestroyImmediate(old.gameObject);
            foreach (WorldItemBase old in UnityEngine.Object.FindObjectsByType<WorldItemBase>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (old != null && old.gameObject.scene == scene) UnityEngine.Object.DestroyImmediate(old.gameObject);

            GameObject root = new GameObject("Car Store Generated");
            SceneManager.MoveGameObjectToScene(root, scene);
            GameObject architecture = Child(root.transform, "Store Architecture");
            GameObject sectionRoot = Child(root.transform, "15 Model Sections");
            GameObject stockRoot = Child(root.transform, "Mixed Car Floor Pile");
            GameObject signs = Child(root.transform, "Directory and Aisle Signs");
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.57f, 0.60f);
            RenderSettings.ambientEquatorColor = new Color(0.40f, 0.42f, 0.45f);
            RenderSettings.ambientGroundColor = new Color(0.23f, 0.24f, 0.26f);
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.fog = false;
            foreach (Light sunlight in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (sunlight.gameObject.scene == scene && sunlight.type == LightType.Directional)
                {
                    sunlight.intensity = 0.65f;
                    EditorUtility.SetDirty(sunlight);
                }
            Cube(architecture.transform, "Store Floor", StoreOffset + new Vector3(0, -0.35f, 0), new Vector3(40, 0.7f, 50), floorMaterial);
            Cube(architecture.transform, "West Wall", StoreOffset + new Vector3(-20, 2.1f, 0), new Vector3(0.45f, 4.2f, 50), wallMaterial);
            Cube(architecture.transform, "East Wall", StoreOffset + new Vector3(20, 2.1f, 0), new Vector3(0.45f, 4.2f, 50), wallMaterial);
            Cube(architecture.transform, "Back Wall", StoreOffset + new Vector3(0, 2.1f, 25), new Vector3(40, 4.2f, 0.45f), wallMaterial);
            Cube(architecture.transform, "Front Wall Left", StoreOffset + new Vector3(-12, 2.1f, -25), new Vector3(16, 4.2f, 0.45f), wallMaterial);
            Cube(architecture.transform, "Front Wall Right", StoreOffset + new Vector3(12, 2.1f, -25), new Vector3(16, 4.2f, 0.45f), wallMaterial);
            Cube(architecture.transform, "Store Ceiling", StoreOffset + new Vector3(0, 4.35f, 0), new Vector3(40, 0.3f, 50), ceilingMaterial);
            foreach (int x in new[] { -12, 0, 12 })
                foreach (int z in new[] { -16, 0, 16 })
            {
                GameObject fixture = Child(architecture.transform, "Warm Ceiling Light");
                fixture.transform.position = StoreOffset + new Vector3(x, 3.9f, z);
                Light light = fixture.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.91f, 0.75f);
                light.intensity = 0.65f;
                light.range = 13f;
                Cube(fixture.transform, "Light Diffuser", fixture.transform.position + Vector3.up * 0.35f,
                    new Vector3(2.5f, 0.08f, 1.2f), creamMaterial).layer = 2;
            }

            GameObject shelfPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/00_Project/02_prefabs/Sorting/Sorting_Shelf.prefab");
            if (shelfPrefab == null) throw new InvalidOperationException("Sorting_Shelf prefab missing.");
            var modelIds = categories.Keys.OrderBy(s => s, StringComparer.Ordinal).ToArray();
            for (int modelIndex = 0; modelIndex < 15; modelIndex++)
            {
                string modelId = modelIds[modelIndex];
                Vector3 localOrigin;
                float yaw;
                string zone;
                if (modelIndex < 5) { localOrigin = new Vector3(-19f, 0, -18 + modelIndex * 9f); yaw = -90f; zone = "West"; }
                else if (modelIndex < 10) { localOrigin = new Vector3(19f, 0, -18 + (modelIndex - 5) * 9f); yaw = 90f; zone = "East"; }
                else if (modelIndex < 13) { localOrigin = new Vector3((modelIndex - 11) * 12f, 0, 24f); yaw = 0f; zone = "Back"; }
                else { localOrigin = new Vector3(modelIndex == 13 ? -12f : 12f, 0, -24f); yaw = 180f; zone = "Front"; }
                Vector3 origin = StoreOffset + localOrigin;
                GameObject sectionObject = Child(sectionRoot.transform, "Model " + (modelIndex + 1).ToString("00") + " - " + zone);
                sectionObject.transform.position = origin;
                sectionObject.transform.rotation = Quaternion.Euler(0, yaw, 0);
                Section section = sectionObject.AddComponent<Section>();
                Catagory_SO category = categories[modelId];
                SerializedObject sectionData = new SerializedObject(section);
                sectionData.FindProperty("_catagory").objectReferenceValue = category;
                var shelfList = sectionData.FindProperty("_shelves");
                shelfList.arraySize = 5;

                CarVariantDefinition[] modelVariants = variants.Where(v => v.baseCarId == modelId)
                    .OrderBy(v => v.variantId, StringComparer.Ordinal).ToArray();
                if (modelVariants.Length != 5) throw new InvalidOperationException(modelId + " must have five Standard variants.");
                for (int colorIndex = 0; colorIndex < modelVariants.Length; colorIndex++)
                {
                    SortableItem_SO item = items[modelVariants[colorIndex].variantId];
                    GameObject shelfObject = PrefabUtility.InstantiatePrefab(shelfPrefab) as GameObject;
                    shelfObject.transform.SetParent(sectionObject.transform, false);
                    shelfObject.name = modelId + " - " + modelVariants[colorIndex].colorwayName;
                    shelfObject.transform.localPosition = new Vector3((colorIndex - 2f) * 1.8f, 0, 0);
                    SortingShelf shelf = shelfObject.GetComponent<SortingShelf>();
                    if (shelf == null) throw new InvalidOperationException("Sorting_Shelf has no SortingShelf component.");
                    foreach (AutoGridding grid in shelfObject.GetComponentsInChildren<AutoGridding>(true))
                        UnityEngine.Object.DestroyImmediate(grid);
                    SerializedObject shelfData = new SerializedObject(shelf);
                    shelfData.FindProperty("_catagory").objectReferenceValue = category;
                    shelfData.FindProperty("acceptedItem").objectReferenceValue = item;
                    shelfData.FindProperty("saveId").stringValue = "car-shelf-" + item.SaveId;
                    SerializedProperty spots = shelfData.FindProperty("placingSpots");
                    spots.arraySize = 7;
                    for (int spot = 0; spot < 7; spot++)
                    {
                        GameObject point = Child(shelfObject.transform, "Car Spot " + (spot + 1));
                        point.transform.localPosition = new Vector3(((spot % 2) - 0.5f) * 0.53f,
                            0.22f + (spot / 2) * 0.39f, -0.1f);
                        spots.GetArrayElementAtIndex(spot).objectReferenceValue = point.transform;
                    }
                    AddShelfInteractionSurface(shelfObject.transform);
                    shelfData.ApplyModifiedPropertiesWithoutUndo();
                    shelfList.GetArrayElementAtIndex(colorIndex).objectReferenceValue = shelf;
                    GameObject labelBoard = CubeLocal(shelfObject.transform, "Color Label Board",
                        new Vector3(0, 1.96f, -0.31f), new Vector3(1.48f, 0.42f, 0.08f), creamMaterial, true);
                    labelBoard.layer = 2;
                    GameObject swatch = CubeLocal(shelfObject.transform, "Color Swatch",
                        new Vector3(-0.56f, 1.96f, -0.365f), new Vector3(0.25f, 0.25f, 0.025f),
                        Material("Swatch " + modelVariants[colorIndex].colorwayName,
                            SwatchColor(modelVariants[colorIndex].colorwayName)), true);
                    swatch.layer = 2;
                    TMP_Text label = WorldText(shelfObject.transform, "Target " + item.SaveId,
                        new Vector3(0.15f, 1.96f, -0.37f), new Vector2(1.15f, 0.37f), 1.55f);
                    label.color = new Color(0.08f, 0.18f, 0.18f);
                    label.gameObject.AddComponent<CarShelfLabel>().Configure(item, label);
                    MakeRack(shelfObject.transform, shelfMaterial);
                }
                sectionData.ApplyModifiedPropertiesWithoutUndo();
                GameObject signBoard = CubeLocal(sectionObject.transform, "Model Sign Board",
                    new Vector3(0, 2.7f, -0.42f), new Vector3(8.5f, 0.7f, 0.12f), signMaterial, true);
                signBoard.layer = 2;
                CubeLocal(sectionObject.transform, "Sign Cream Border",
                    new Vector3(0, 2.7f, -0.34f), new Vector3(8.7f, 0.8f, 0.06f), creamMaterial, true).layer = 2;
                signBoard.transform.localPosition += new Vector3(0, 0, -0.11f);
                TMP_Text sectionSign = WorldText(sectionObject.transform, modelId + " Sign",
                    new Vector3(0.4f, 2.7f, -0.64f), new Vector2(7.1f, 0.65f), 3.3f);
                sectionSign.gameObject.AddComponent<CarModelSign>().Configure(category, sectionSign, modelIndex + 1);
                GameObject preview = Child(sectionObject.transform, "Model Preview");
                preview.transform.localPosition = new Vector3(-3.6f, 2.7f, -0.66f);
                SpriteRenderer icon = preview.AddComponent<SpriteRenderer>();
                icon.sprite = modelVariants[0].thumbnail;
                icon.sortingOrder = 1;
                if (icon.sprite != null)
                {
                    Vector2 size = icon.sprite.bounds.size;
                    preview.transform.localScale = new Vector3(0.7f / size.x, 0.5f / size.y, 1f);
                }
            }

            var copies = new List<SortableItem_SO>(variants.Length * 7);
            foreach (CarVariantDefinition variant in variants)
                for (int copy = 0; copy < 7; copy++) copies.Add(items[variant.variantId]);
            System.Random random = new System.Random(1501050);
            for (int i = copies.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (copies[i], copies[j]) = (copies[j], copies[i]);
            }
            BakePilePoses(copies.Count, out Vector3[] pilePositions, out Quaternion[] pileRotations);
            int nextCopyId = 1;
            for (int i = 0; i < copies.Count; i++)
            {
                SortableItem_SO item = copies[i];
                GameObject car = PrefabUtility.InstantiatePrefab(item.WorldItem.gameObject) as GameObject;
                car.transform.SetParent(stockRoot.transform, false);
                car.name = item.SaveId + " copy " + nextCopyId;
                car.transform.SetPositionAndRotation(pilePositions[i], pileRotations[i]);
                SerializedObject carData = new SerializedObject(car.GetComponent<WorldItemBase>());
                carData.FindProperty("authoredSaveId").intValue = nextCopyId++;
                carData.ApplyModifiedPropertiesWithoutUndo();
            }
            if (nextCopyId != variants.Length * 7 + 1) throw new InvalidOperationException("Stock copy count is incorrect.");

            Cube(signs.transform, "Stock Area Header Back", StoreOffset + new Vector3(0, 3.1f, -13.3f),
                new Vector3(12, 0.7f, 0.12f), signMaterial).layer = 2;
            TMP_Text stockSign = WorldText(signs.transform, "Mixed Cars Sign",
                StoreOffset + new Vector3(0, 3.1f, -13.4f), new Vector2(11.8f, 0.65f), 4.5f, true);
            stockSign.gameObject.AddComponent<CarStockSign>().Configure(stockSign,
                "store.mixed_cars", "MIXED CARS  •  PICK UP HERE");
            Cube(signs.transform, "Entrance Header Back", StoreOffset + new Vector3(0, 3.1f, -24.55f),
                new Vector3(8, 0.8f, 0.1f), signMaterial).layer = 2;
            TMP_Text directory = WorldText(signs.transform, "Store Directory",
                StoreOffset + new Vector3(0, 3.1f, -24.64f), new Vector2(7.8f, 0.7f), 4.2f, true);
            directory.gameObject.AddComponent<CarStockSign>().Configure(directory,
                "store.title", "TOY CAR STORE");
            GameObject ui = Child(root.transform, "Store UI Controller");
            ui.AddComponent<CarStoreUI>();

            foreach (LevelSaveManager manager in UnityEngine.Object.FindObjectsByType<LevelSaveManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (manager.gameObject.scene != scene) continue;
                SerializedObject data = new SerializedObject(manager);
                data.FindProperty("database").objectReferenceValue = database;
                data.FindProperty("worldItemsContainer").objectReferenceValue = stockRoot.transform;
                data.FindProperty("saveKey").stringValue = "CarStore.Save.v6";
                data.FindProperty("autosaveInterval").floatValue = 30f;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach (Player player in UnityEngine.Object.FindObjectsByType<Player>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (player.gameObject.scene == scene)
                {
                    player.transform.position = StoreOffset + new Vector3(0, 1.15f, -21);
                    player.transform.rotation = Quaternion.identity;
                    if (player.Motor != null)
                    {
                        SerializedObject motor = new SerializedObject(player.Motor);
                        motor.FindProperty("InteractiveRigidbodyHandling").boolValue = true;
                        motor.FindProperty("SimulatedCharacterMass").floatValue = 60f;
                        motor.FindProperty("RigidbodyInteractionType").enumValueIndex =
                            (int)KinematicCharacterController.RigidbodyInteractionType.SimulatedDynamic;
                        motor.FindProperty("AllowSteppingOnDynamicRigidbodies").boolValue = false;
                        motor.FindProperty("MaxStepHeight").floatValue = 0.2f;
                        motor.FindProperty("StableGroundLayers").FindPropertyRelative("m_Bits").intValue = 243;
                        motor.ApplyModifiedPropertiesWithoutUndo();
                        PrefabUtility.RecordPrefabInstancePropertyModifications(player.Motor);
                    }
                }
            foreach (ItemPhysicsCulling culling in UnityEngine.Object.FindObjectsByType<ItemPhysicsCulling>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (culling.gameObject.scene != scene) continue;
                SerializedObject settings = new SerializedObject(culling);
                settings.FindProperty("activeRadius").floatValue = 3.5f;
                settings.FindProperty("maxDynamic").intValue = 160;
                settings.FindProperty("wakeRadius").floatValue = 1.1f;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
            ConfigurePlayerSave(scene);
            TRShelfSceneIntegration.Apply(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }
    }

    /// <summary>Settles lightweight collider stand-ins in an isolated physics scene before authoring the pile.</summary>
    private static void BakePilePoses(int count, out Vector3[] positions, out Quaternion[] rotations)
    {
        Scene bakeScene = EditorSceneManager.NewPreviewScene();
        positions = new Vector3[count];
        rotations = new Quaternion[count];
        try
        {
            PhysicsScene physicsScene = bakeScene.GetPhysicsScene();
            var random = new System.Random(1050150);
            var bodies = new Rigidbody[count];
            GameObject floor = new GameObject("Bake Floor");
            SceneManager.MoveGameObjectToScene(floor, bakeScene);
            BoxCollider floorCollider = floor.AddComponent<BoxCollider>();
            floorCollider.center = new Vector3(0, -0.25f, 0);
            floorCollider.size = new Vector3(60, 0.5f, 60);
            var grip = new PhysicsMaterial("Car Pile Bake Grip")
            {
                staticFriction = 0.9f,
                dynamicFriction = 0.75f,
                frictionCombine = PhysicsMaterialCombine.Maximum
            };
            floorCollider.sharedMaterial = grip;

            int index = 0;
            // Drop small batches into one shared area. Continuous positions and three
            // overlapping density lobes avoid the visible rows left by the old grid.
            for (int layer = 0; layer < 7 && index < count; layer++)
            {
                int batchEnd = Mathf.Min(count, index + (count + 6) / 7);
                while (index < batchEnd)
                {
                        float angle = (float)(random.NextDouble() * Mathf.PI * 2);
                        float radius = Mathf.Sqrt((float)random.NextDouble());
                        int lobe = random.Next(3);
                        Vector2 lobeCenter = lobe == 0 ? new Vector2(-2.1f, -0.7f) :
                            lobe == 1 ? new Vector2(2.0f, -0.8f) : new Vector2(0.1f, 1.9f);
                        float x = lobeCenter.x + Mathf.Cos(angle) * radius * 4.8f;
                        float z = lobeCenter.y + Mathf.Sin(angle) * radius * 4.2f;
                        GameObject proxy = new GameObject("Car Pile Proxy " + index);
                        SceneManager.MoveGameObjectToScene(proxy, bakeScene);
                        proxy.transform.position = new Vector3(
                            x,
                            0.5f + layer * 0.48f + (float)random.NextDouble() * 0.35f,
                            z);
                        proxy.transform.rotation = Quaternion.Euler(
                            (float)(random.NextDouble() * 50 - 25),
                            (float)(random.NextDouble() * 360),
                            (float)(random.NextDouble() * 50 - 25));
                        CapsuleCollider box = proxy.AddComponent<CapsuleCollider>();
                        TinyRiders.LooseCarPhysics.Fit(box, new Bounds(new Vector3(0, .16f, 0), new Vector3(.42f, .28f, .58f)));
                        box.sharedMaterial = grip;
                        Rigidbody body = proxy.AddComponent<Rigidbody>();
                        TinyRiders.LooseCarPhysics.Configure(body);
                        bodies[index++] = body;
                }
                for (int step = 0; step < 75; step++) physicsScene.Simulate(0.02f);
            }
            if (index != count) throw new InvalidOperationException("Car pile capacity is smaller than the stock count.");

            for (int step = 0; step < 450; step++) physicsScene.Simulate(0.02f);
            Vector3 pileOrigin = StoreOffset + new Vector3(0, 0, -5f);
            for (int i = 0; i < count; i++)
            {
                positions[i] = pileOrigin + bodies[i].position;
                rotations[i] = bodies[i].rotation;
            }
            UnityEngine.Object.DestroyImmediate(grip);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(bakeScene);
        }
    }

    private static GameObject Child(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject Cube(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.position = position;
        cube.transform.localScale = scale;
        cube.GetComponent<Renderer>().sharedMaterial = material;
        return cube;
    }

    private static GameObject CubeLocal(Transform parent, string name, Vector3 position, Vector3 scale,
        Material material, bool decoration = false)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = position;
        cube.transform.localRotation = Quaternion.identity;
        cube.transform.localScale = scale;
        cube.GetComponent<Renderer>().sharedMaterial = material;
        if (decoration) UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
        return cube;
    }

    private static Color SwatchColor(string name)
    {
        switch (name)
        {
            case "Scarlet": return new Color(0.83f, 0.12f, 0.16f);
            case "Tangerine": return new Color(0.95f, 0.36f, 0.08f);
            case "Amber": return new Color(0.98f, 0.67f, 0.08f);
            case "Lime": return new Color(0.65f, 0.83f, 0.10f);
            case "Emerald": return new Color(0.08f, 0.65f, 0.32f);
            case "Teal": return new Color(0.06f, 0.54f, 0.52f);
            case "Cyan": return new Color(0.08f, 0.73f, 0.80f);
            case "Cobalt": return new Color(0.10f, 0.30f, 0.77f);
            case "Violet": return new Color(0.43f, 0.25f, 0.73f);
            case "Fuchsia": return new Color(0.83f, 0.18f, 0.62f);
            default: throw new ArgumentException("Unknown shelf color: " + name);
        }
    }

    private static TMP_Text WorldText(Transform parent, string name, Vector3 position, Vector2 size,
        float fontSize, bool worldPosition = false)
    {
        GameObject go = new GameObject(name, typeof(TextMeshPro));
        go.transform.SetParent(parent, false);
        if (worldPosition) go.transform.position = position;
        else go.transform.localPosition = position;
        go.transform.localRotation = Quaternion.identity;
        TextMeshPro text = go.GetComponent<TextMeshPro>();
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.rectTransform.sizeDelta = size;
        text.color = Color.white;
        text.enableWordWrapping = true;
        return text;
    }

    private static void RegisterScenes()
    {
        string[] paths = {
            "Assets/00_Project/00_Scenes/Initializer.unity",
            "Assets/00_Project/00_Scenes/MainMenu Scene.unity",
            "Assets/00_Project/00_Scenes/Loading Scene.unity",
            ScenePath,
        };
        EditorBuildSettings.scenes = paths.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
        WorldData world = AssetDatabase.LoadAssetAtPath<WorldData>(
            "Assets/00_Project/03_scriptableObjects/Game Data/Level Porgression Data/World 1.asset");
        if (world != null)
        {
            world.SceneNames.Clear();
            world.SceneNames.Add("Level");
            EditorUtility.SetDirty(world);
        }
    }

    /// <summary>Updates the existing generated car prefabs and shelf scene without rebuilding the store.</summary>
    [MenuItem("Tools/Car Store/Update Shelf Placement Assets")]
    public static void UpdateShelfPlacementAssets()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { Root + "/Prefabs" });
        int updatedPrefabs = 0;
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!System.IO.Path.GetFileName(path).StartsWith("Shelf_", StringComparison.Ordinal)) continue;
            if (!System.IO.Path.GetFileName(path).Contains("-STD-")) continue;
            GameObject prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Transform visual = prefab.transform.Find("TinyRiders Visual");
                if (visual == null) throw new InvalidOperationException("Missing TinyRiders Visual in " + path);
                if (Quaternion.Angle(visual.localRotation, Quaternion.Euler(0f, 180f, 0f)) < 1f)
                {
                    Vector3 centeredPosition = visual.localPosition;
                    centeredPosition.x = -centeredPosition.x;
                    centeredPosition.z = -centeredPosition.z;
                    visual.localPosition = centeredPosition;
                }
                visual.localRotation = Quaternion.identity;
                prefab.transform.localScale = Vector3.one * TRShelfSceneIntegration.CompactShelfScale;
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
                updatedPrefabs++;
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }
        if (updatedPrefabs != 75) throw new InvalidOperationException("Expected 75 Standard shelf car prefabs; found " + updatedPrefabs);

        Scene previous = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool wasLoaded = scene.IsValid() && scene.isLoaded;
        if (!wasLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            int updatedShelves = TRShelfSceneIntegration.Apply(scene);
            if (updatedShelves != 75) throw new InvalidOperationException("Expected 75 car shelves; found " + updatedShelves);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        finally
        {
            if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("CAR_STORE_SHELF_PLACEMENT_UPDATED: " + updatedPrefabs + " car prefabs and 75 shelf interaction surfaces.");
    }

    private static void AddShelfInteractionSurface(Transform shelf)
    {
        Transform surface = shelf.Find("Car Placement Surface");
        if (surface == null) surface = Child(shelf, "Car Placement Surface").transform;
        surface.gameObject.layer = 8;
        surface.localPosition = new Vector3(0f, 0.91f, 0.34f);
        surface.localRotation = Quaternion.identity;
        BoxCollider collider = surface.GetComponent<BoxCollider>();
        if (collider == null) collider = surface.gameObject.AddComponent<BoxCollider>();
        collider.center = Vector3.zero;
        collider.size = new Vector3(1.45f, 1.85f, 0.08f);
        if (surface.GetComponent<SortingShelfPlacementSurface>() == null)
            surface.gameObject.AddComponent<SortingShelfPlacementSurface>();
    }

    private static void MakeRack(Transform shelf, Material material)
    {
        if (shelf.Find("Display Rack") != null) return;
        GameObject rack = Child(shelf, "Display Rack");
        CubeLocal(rack.transform, "Cabinet", new Vector3(0, -0.52f, 0.35f),
            new Vector3(1.5f, 1.05f, 0.9f), material);
        CubeLocal(rack.transform, "Back Board", new Vector3(0, 0.78f, 0.43f),
            new Vector3(1.5f, 2.15f, 0.12f), tealMaterial);
        for (int level = 0; level < 4; level++)
        {
            GameObject ledge = CubeLocal(rack.transform, "Shelf Tier " + (level + 1),
                new Vector3(0, 0.16f + level * 0.39f, -0.08f),
                new Vector3(1.42f, 0.055f, 0.72f), material);
            ledge.layer = 2; // interaction ray ignores display geometry
            CubeLocal(rack.transform, "Shelf Edge " + (level + 1),
                new Vector3(0, 0.18f + level * 0.39f, -0.43f),
                new Vector3(1.42f, 0.025f, 0.025f), creamMaterial, true).layer = 2;
        }
    }

    private static void ConfigurePlayerSave(Scene scene)
    {
        foreach (PlayerSavePosition saver in UnityEngine.Object.FindObjectsByType<PlayerSavePosition>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (saver.gameObject.scene != scene) continue;
            SerializedObject data = new SerializedObject(saver);
            data.FindProperty("saveKey").stringValue = "CarStore.PlayerPosition.v4";
            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(saver);
        }
    }

    [MenuItem("Tools/Car Store/Improve Store Presentation")]
    public static void ImproveStorePresentation() => Generate();


    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        string parent = path.Substring(0, slash);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
    }
}
