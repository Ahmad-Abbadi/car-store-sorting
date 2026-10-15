using System;
using System.IO;
using System.Linq;
using TinyRiders;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CompactCollectionBuilder
{
    public const string ScenePath = "Assets/00_Project/00_Scenes/Level.unity";
    public const string Root = "Assets/TinyRiders/Gameplay/Compact";
    public const string CatalogPath = Root + "/CompactCarCatalog.asset";
    public const string SaveKey = "TinyRiders.Compact.Collection.v1";
    const float ShelfScale = .48f;

    static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
    { var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    static void Text(UnityEngine.Object target, string field, string value)
    { var so = new SerializedObject(target); so.FindProperty(field).stringValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    static T Asset<T>(string path) where T : ScriptableObject
    { var value = AssetDatabase.LoadAssetAtPath<T>(path); if (value == null) { value = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(value, path); } return value; }
    static Bounds Bounds(GameObject root, bool bodyOnly = false)
    {
        var accessory = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "ThemeAccessories");
        var renderers = root.GetComponentsInChildren<Renderer>().Where(r => r.enabled && (!bodyOnly || accessory == null || !r.transform.IsChildOf(accessory))).ToArray();
        bool found=false; Bounds bounds=default;
        foreach(var renderer in renderers)
        {
            var mesh=renderer.GetComponent<MeshFilter>()?.sharedMesh;
            if(mesh==null)continue;
            var b=mesh.bounds;
            foreach(float x in new[]{b.min.x,b.max.x})foreach(float y in new[]{b.min.y,b.max.y})foreach(float z in new[]{b.min.z,b.max.z})
            {var p=renderer.transform.TransformPoint(new Vector3(x,y,z));if(!found){bounds=new Bounds(p,Vector3.zero);found=true;}else bounds.Encapsulate(p);}
        }
        if(!found)throw new InvalidOperationException("No mesh geometry in "+root.name);
        return bounds;
    }
    static GameObject Wrap(ThemedCarCatalog.Entry entry, SortableItem_SO item, string kind, out Vector3 size)
    {
        string path = Root + "/Prefabs/" + kind + "_" + entry.collectibleId + ".prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
        {
            size = Bounds(existing).size;
            return existing;
        }
        var root = new GameObject(kind + " " + entry.collectibleId); root.layer = 8;
        try
        {
            var art = (GameObject)PrefabUtility.InstantiatePrefab(entry.prefab); art.transform.SetParent(root.transform, false);
            foreach (var body in art.GetComponentsInChildren<Rigidbody>(true)) UnityEngine.Object.DestroyImmediate(body);
            foreach (var collider in art.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
            foreach (var transform in art.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer = 2;
            var bounds = Bounds(art);
            float scale = kind == "Shelf" ? ShelfScale / 9f * .96f : (kind == "Held" ? .35f : .58f) / Mathf.Max(bounds.size.x, bounds.size.z);
            art.transform.localScale *= scale;
            bounds = Bounds(art); art.transform.localPosition += new Vector3(-bounds.center.x, .015f - bounds.min.y, -bounds.center.z);
            size = Bounds(art).size;
            var group = root.AddComponent<ItemVisualGroup>(); Set(group, "visualRoot", art.transform);
            root.AddComponent<ThemedWorldInstance>();
            if (kind == "World")
            {
                var world = root.AddComponent<SortableWorldItem>(); Set(world, "data", item);
                var body = root.AddComponent<Rigidbody>(); body.isKinematic = true; LooseCarPhysics.Configure(body);
                var capsule = root.AddComponent<CapsuleCollider>();
                LooseCarPhysics.Fit(capsule, Bounds(art, true));
                capsule.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(Root + "/LooseCar.physicMaterial");
                ThemedPickingBounds.AddToWrapper(root);
            }
            else if (kind == "Held") Set(root.AddComponent<HandItemBase>(), "data", item);
            else
            {
                var placed = root.AddComponent<PlacedOnShelfItemBase>();
                var so = new SerializedObject(placed); so.FindProperty("punchDuration").floatValue = .15f;
                so.FindProperty("punchScale").vector3Value = Vector3.zero; so.ApplyModifiedPropertiesWithoutUndo();
                var box = root.AddComponent<BoxCollider>(); var b=Bounds(art); box.center=b.center; box.size=b.size;
            }
            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    [MenuItem("Tools/Tiny Riders/Sorting/Install 600 Designs in Current Room")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Collection authoring requires Edit mode.");
        var source = AssetDatabase.LoadAssetAtPath<ThemedCarCatalog>("Assets/TinyRiders/ThemedCollection/Data/Themes/ThemedCarCatalog.asset");
        if (source.entries.Length != 600 || source.themes.Length != 40) throw new InvalidOperationException("Complete reviewed collection required.");
        foreach (string folder in new[] { Root, Root + "/Items", Root + "/Themes", Root + "/Prefabs" }) Directory.CreateDirectory(folder);
        AssetDatabase.Refresh();
        var catalog = Asset<ThemedCarCatalog>(CatalogPath);
        catalog.themes = source.themes;
        var categories = source.themes.ToDictionary(t => t.id, t =>
        {
            var category = Asset<Catagory_SO>(Root + "/Themes/" + t.id + ".asset");
            category.CatagoryID = t.id; category.CatagoryName = t.displayName;
            EditorUtility.SetDirty(category); return category;
        });
        var entries = new System.Collections.Generic.List<ThemedCarCatalog.Entry>();
        try
        {
            int count = 0;
            foreach (var original in source.entries.OrderBy(e => e.collectibleId, StringComparer.Ordinal))
            {
                var item = Asset<SortableItem_SO>(Root + "/Items/" + original.collectibleId + ".asset");
                Text(item, "saveId", original.collectibleId); Text(item, "displayName", original.displayName);
                item.Catagory = categories[original.themeId]; item.ItemImage = original.sortingItem.ItemImage;
                item.CameraFixed = true; item.HeldPosition = new Vector3(.22f, -.25f, .48f); item.HeldRotation = new Vector3(0,155,0);
                var world = Wrap(original, item, "World", out _);
                var held = Wrap(original, item, "Held", out _);
                var shelf = Wrap(original, item, "Shelf", out var size);
                Set(item, "worldItem", world.GetComponent<WorldItemBase>()); Set(item, "handItem", held.GetComponent<HandItemBase>());
                item.PlacedOnShelfPrefab = shelf.GetComponent<PlacedOnShelfItemBase>(); EditorUtility.SetDirty(item);
                entries.Add(new ThemedCarCatalog.Entry { collectibleId = original.collectibleId, baseCarId = original.baseCarId,
                    themeId = original.themeId, colorwayId = original.colorwayId, displayName = original.displayName,
                    prefab = original.prefab, preview = original.preview, sortingItem = item, displaySize = size,
                    releaseId = original.releaseId, enabled = original.enabled, contentRevision = original.contentRevision });
                count++;
                if (count % 50 == 0) { AssetDatabase.SaveAssets(); Debug.Log("COMPACT_COLLECTION_ASSETS " + count + "/600"); }
            }
        }
        finally { AssetDatabase.SaveAssets(); }
        catalog.entries = entries.ToArray(); EditorUtility.SetDirty(catalog);
        foreach (var category in categories.Values) { category.Items = entries.Where(e => e.themeId == category.CatagoryID).Select(e => e.sortingItem).ToList(); EditorUtility.SetDirty(category); }
        var database = Asset<ItemDatabaseSO>(Root + "/CompactItemDatabase.asset");
        var db = new SerializedObject(database); var list = db.FindProperty("items"); list.arraySize = entries.Count;
        for (int i = 0; i < entries.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = entries[i].sortingItem;
        db.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssets();
        RepairShelfInteraction();
        InstallScene(catalog, database, categories);
    }

    static void InstallScene(ThemedCarCatalog catalog, ItemDatabaseSO database, System.Collections.Generic.Dictionary<string, Catagory_SO> categories)
    {
        var current = SceneManager.GetActiveScene(); if (current.isDirty) EditorSceneManager.SaveScene(current);
        var scene = current.path == ScenePath ? current : EditorSceneManager.OpenScene(ScenePath);
        Directory.CreateDirectory("IntegrationEvidence");
        string backup = "IntegrationEvidence/Level_before_compact_collection.unity";
        if (!File.Exists(backup)) File.Copy(ScenePath, backup);
        var root = scene.GetRootGameObjects().First(g => g.name == "Car Store Generated");
        foreach (string name in new[] { "15 Model Sections", "Mixed Car Floor Pile", "Directory and Aisle Signs", "Compact Theme Collection" })
        { var old = root.transform.Find(name); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject); }
        foreach (var car in UnityEngine.Object.FindObjectsByType<WorldItemBase>(FindObjectsInactive.Include, FindObjectsSortMode.None)) UnityEngine.Object.DestroyImmediate(car.gameObject);
        foreach (var shelf in UnityEngine.Object.FindObjectsByType<SortingShelf>(FindObjectsInactive.Include, FindObjectsSortMode.None)) UnityEngine.Object.DestroyImmediate(shelf.gameObject);
        foreach (var section in UnityEngine.Object.FindObjectsByType<Section>(FindObjectsInactive.Include, FindObjectsSortMode.None)) UnityEngine.Object.DestroyImmediate(section.gameObject);
        foreach (var culling in UnityEngine.Object.FindObjectsByType<CarVisualCulling>(FindObjectsInactive.Include, FindObjectsSortMode.None)) UnityEngine.Object.DestroyImmediate(culling);
        foreach (var chunks in UnityEngine.Object.FindObjectsByType<ItemChunkRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) chunks.enabled = false;
        var orphan = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "TR_Shelf_Base_01"); if (orphan != null) UnityEngine.Object.DestroyImmediate(orphan);
        var collection = new GameObject("Compact Theme Collection"); collection.transform.SetParent(root.transform, false);
        var session = collection.AddComponent<ThemedCollectionSession>(); session.catalog = catalog; session.copiesPerDesign = 2; session.visiblePileLimit = 1200;
        session.spawnEntireCollectionAtStart = true; session.pileCentralAisle = 2;
        session.seed = 73481; session.worldScale = 1; session.boundedPile = true; session.pileAreaSize = new Vector2(31.5f,27);
        session.pileCellSpacing = .75f; session.pileSpacing = new Vector3(.75f,.3f,.75f); session.completeStoreWhenSorted = true;
        var pile = new GameObject("Mixed 600 Design Pile").transform; pile.SetParent(collection.transform, false); pile.position = new Vector3(500,0,0); session.pileOrigin = pile;
        var shelfArt = ExactThemeShelfBuilder.EnsurePrefab();
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/00_Project/04_ِArt/UI/Fonts/Fredoka-SemiBold SDF.asset");
        int index = 0;
        foreach (var theme in catalog.themes.Where(t => t.enabled).OrderBy(t => t.id, StringComparer.Ordinal))
        for (int unit = 1; unit <= 2; unit++, index++)
        {
            Vector3 position; float yaw;
            ShelfPose(index, out position, out yaw);
            var shelfObject = new GameObject(theme.displayName + " - " + unit); shelfObject.layer = 8; shelfObject.transform.SetParent(collection.transform, false);
            shelfObject.transform.SetPositionAndRotation(position, Quaternion.Euler(0,yaw,0));
            var art = (GameObject)PrefabUtility.InstantiatePrefab(shelfArt, scene); art.name = "TR_Shelf_Base_01"; art.transform.SetParent(shelfObject.transform, false); art.transform.localScale = Vector3.one * ShelfScale;
            foreach (var t in art.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 2;
            foreach (var collider in art.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
            var markers = ExactThemeShelfBuilder.Markers(art);
            if (markers.Length != ExactThemeShelfBuilder.SlotsPerShelf) throw new InvalidOperationException("Expected 15 authored shelf markers.");
            var shelf = shelfObject.AddComponent<SortingShelf>(); var so = new SerializedObject(shelf);
            so.FindProperty("_catagory").objectReferenceValue = categories[theme.id]; so.FindProperty("acceptedItem").objectReferenceValue = null;
            so.FindProperty("saveId").stringValue = "COMPACT-" + theme.id + "-S" + unit.ToString("D3");
            so.FindProperty("jumpDuration").floatValue = .2f; so.FindProperty("jumpPower").floatValue = .08f;
            so.FindProperty("fullPunchScale").vector3Value = Vector3.one * .003f;
            var slots = so.FindProperty("placingSpots"); slots.arraySize = markers.Length; for (int slot = 0; slot < markers.Length; slot++) slots.GetArrayElementAtIndex(slot).objectReferenceValue = markers[slot]; so.ApplyModifiedPropertiesWithoutUndo();
            var binding = shelfObject.AddComponent<ThemeShelfBinding>(); binding.themeId = theme.id; binding.clearance = ExactThemeShelfBuilder.Clearance * ShelfScale;
            var body = shelfObject.AddComponent<BoxCollider>(); body.center = new Vector3(0,.29f,0)*ShelfScale; body.size = new Vector3(3.55f,.58f,.86f)*ShelfScale;
            var surface = new GameObject("Theme Placement Surface"); surface.layer = 8; surface.transform.SetParent(shelfObject.transform, false);
            surface.transform.localPosition = new Vector3(0,1.82f,.49f)*ShelfScale;
            var box = surface.AddComponent<BoxCollider>(); box.size = new Vector3(3.48f,3.24f,.08f)*ShelfScale; surface.AddComponent<SortingShelfPlacementSurface>();
            var header = new GameObject("Theme Header"); header.transform.SetParent(shelfObject.transform, false);
            header.transform.localPosition = new Vector3(0,1.8f,.29f); header.transform.localRotation = Quaternion.Euler(0,180,0);
            var label = header.AddComponent<TextMeshPro>(); label.font = font; label.text = theme.displayName; label.fontSize = 1.5f;
            label.color = new Color(.04f,.08f,.12f); label.alignment = TextAlignmentOptions.Center; label.rectTransform.sizeDelta = new Vector2(1.65f,.3f);
        }
        var save = UnityEngine.Object.FindFirstObjectByType<LevelSaveManager>(); Set(save,"database",database); Set(save,"worldItemsContainer",pile); Text(save,"saveKey",SaveKey);
        var saveSo = new SerializedObject(save); saveSo.FindProperty("autosaveInterval").floatValue = 8; saveSo.ApplyModifiedPropertiesWithoutUndo();
        Text(UnityEngine.Object.FindFirstObjectByType<PlayerSavePosition>(),"saveKey",SaveKey+".position");
        foreach (var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        { camera.farClipPlane=10000; camera.useOcclusionCulling=false; camera.layerCullDistances=new float[32]; EditorUtility.SetDirty(camera);PrefabUtility.RecordPrefabInstancePropertyModifications(camera); }
        foreach (var camera in UnityEngine.Object.FindObjectsByType<Unity.Cinemachine.CinemachineCamera>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        { var lens=camera.Lens; lens.FarClipPlane=10000; camera.Lens=lens; EditorUtility.SetDirty(camera);PrefabUtility.RecordPrefabInstancePropertyModifications(camera); }
        foreach(var interaction in UnityEngine.Object.FindObjectsByType<InteractionHandler>(FindObjectsSortMode.None))
        { var so=new SerializedObject(interaction); so.FindProperty("triggerInteraction").enumValueIndex=(int)QueryTriggerInteraction.Collide; so.ApplyModifiedPropertiesWithoutUndo(); }
        ExactThemeShelfBuilder.ValidateScene();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        var menu=EditorSceneManager.OpenScene("Assets/00_Project/00_Scenes/MainMenu Scene.unity",OpenSceneMode.Additive);
        foreach(var m in menu.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MainMenu>(true))) Text(m,"levelScene","Level");
        EditorSceneManager.MarkSceneDirty(menu); EditorSceneManager.SaveScene(menu); EditorSceneManager.CloseScene(menu,true); SceneManager.SetActiveScene(scene);
        foreach(string guid in AssetDatabase.FindAssets("t:WorldData"))
        {var data=AssetDatabase.LoadAssetAtPath<WorldData>(AssetDatabase.GUIDToAssetPath(guid)); data.SceneNames=data.SceneNames.Select(n=>n=="ThemedSorting"?"Level":n).ToList(); EditorUtility.SetDirty(data);}
        AssetDatabase.SaveAssets();
        ArrangeShelves();
        Debug.Log("COMPACT_COLLECTION_READY 600 designs / 1200 copies / 80 shelves / 1200 slots; original room and player preserved");
    }

    static void ShelfPose(int index, out Vector3 position, out float yaw)
    {
        if (index < 24) { position = new Vector3(481,0,(index-11.5f)*1.8f); yaw = 90; }
        else if (index < 48) { position = new Vector3(519,0,(index-35.5f)*1.8f); yaw = -90; }
        else if (index < 68) { position = new Vector3(500+(index-57.5f)*1.8f,0,24); yaw = 180; }
        else { int n=index-68; position=new Vector3(500+(n<6?-12:12)+(n%6-2.5f)*1.8f,0,-24); yaw=0; }
    }

    public static void ArrangeShelves()
    {
        if (SceneManager.GetActiveScene().path == ScenePath && AssetDatabase.LoadAssetAtPath<CollectorRoomLayout>(CollectorRoomBuilder.LayoutPath) != null)
        { CollectorRoomBuilder.Build(); return; }
        var shelves=UnityEngine.Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None)
            .Where(s=>s.GetComponent<ThemeShelfBinding>()!=null).OrderBy(s=>s.SaveId,StringComparer.Ordinal).ToArray();
        if(SceneManager.GetActiveScene().path!=ScenePath||shelves.Length!=80)throw new InvalidOperationException("Open the compact room first.");
        var session=UnityEngine.Object.FindFirstObjectByType<ThemedCollectionSession>();
        if(session.GetComponent<ThemedPerformanceProbe>()==null)session.gameObject.AddComponent<ThemedPerformanceProbe>();
        for(int i=0;i<shelves.Length;i++){ShelfPose(i,out var p,out var yaw);shelves[i].transform.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));}
        ExactThemeShelfBuilder.ValidateScene();
        ThemedShelfBatcher.Apply(shelves, ExactThemeShelfBuilder.PrefabPath);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }

    public static void RepairShelfInteraction()
    {
        var catalog=AssetDatabase.LoadAssetAtPath<ThemedCarCatalog>(CatalogPath);int changed=0;
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach(var entry in catalog.entries)
            {
                if(entry.sortingItem.PlacedOnShelfPrefab.GetComponent<Collider>()!=null)continue;
                string path=AssetDatabase.GetAssetPath(entry.sortingItem.PlacedOnShelfPrefab);
                var root=PrefabUtility.LoadPrefabContents(path);
                try{var b=Bounds(root);var box=root.AddComponent<BoxCollider>();box.center=b.center;box.size=b.size;PrefabUtility.SaveAsPrefabAsset(root,path);changed++;}
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
        }
        finally{AssetDatabase.StopAssetEditing();}
        AssetDatabase.SaveAssets();Debug.Log("COMPACT_SHELF_PICKING_READY "+changed+" colliders added");
    }

    [MenuItem("Tools/Tiny Riders/Sorting/Show Entire Starting Collection")]
    public static void ConfigureFullStartingPile()
    {
        if(EditorApplication.isPlaying||SceneManager.GetActiveScene().path!=ScenePath)
            throw new InvalidOperationException("Open Level in Edit mode first.");
        var session=UnityEngine.Object.FindFirstObjectByType<ThemedCollectionSession>();
        session.spawnEntireCollectionAtStart=true;
        session.visiblePileLimit=session.catalog.entries.Count(e=>e.enabled)*session.copiesPerDesign;
        if(session.roomLayout==null)
        {
            session.boundedPile=true;session.pileAreaSize=new Vector2(31.5f,27);session.pileCellSpacing=.75f;
            session.pileCentralAisle=2;session.pileSpacing=new Vector3(.75f,.3f,.75f);session.pileLayout=null;
        }
        foreach(var hiding in UnityEngine.Object.FindObjectsByType<CarVisualCulling>(FindObjectsInactive.Include,FindObjectsSortMode.None))UnityEngine.Object.DestroyImmediate(hiding);
        foreach(var hiding in UnityEngine.Object.FindObjectsByType<ThemedVisibility>(FindObjectsInactive.Include,FindObjectsSortMode.None))UnityEngine.Object.DestroyImmediate(hiding);
        foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {camera.farClipPlane=10000;camera.useOcclusionCulling=false;camera.layerCullDistances=new float[32];EditorUtility.SetDirty(camera);PrefabUtility.RecordPrefabInstancePropertyModifications(camera);}
        foreach(var camera in UnityEngine.Object.FindObjectsByType<Unity.Cinemachine.CinemachineCamera>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {var lens=camera.Lens;lens.FarClipPlane=10000;camera.Lens=lens;EditorUtility.SetDirty(camera);PrefabUtility.RecordPrefabInstancePropertyModifications(camera);}
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("FULL_STARTING_COLLECTION_READY 600 varieties / 1200 physical cars / no staged refill or distance hiding");
    }
}
