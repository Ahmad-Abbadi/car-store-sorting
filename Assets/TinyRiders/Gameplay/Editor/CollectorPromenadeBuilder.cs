using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TinyRiders;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>A compact, floor-wide sorting shop with small service and reward alcoves.</summary>
public static class CollectorPromenadeBuilder
{
    public const string Root = "Assets/TinyRiders/Gameplay/Compact/Promenade";
    public const string Evidence = "IntegrationEvidence/CompactScatter";
    const string InteriorName = "Collector Promenade";
    static readonly Vector3 Origin = new Vector3(500, 0, 0);
    static readonly Color Ink = new Color(.055f, .15f, .19f);
    static readonly Color Teal = new Color(.12f, .58f, .59f);
    static readonly Color Coral = new Color(.94f, .37f, .26f);
    static readonly Color Gold = new Color(.96f, .68f, .22f);
    static TMP_FontAsset font;
    static Transform interior;

    [MenuItem("Tools/Tiny Riders/Collector Shop/Apply Collector Promenade")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != CompactCollectionBuilder.ScenePath) throw new InvalidOperationException("Open Level first.");
        var session = Object.FindFirstObjectByType<ThemedCollectionSession>();
        var shelves = Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None);
        var golden = Object.FindFirstObjectByType<GoldenCarReveal>();
        var quest = GameObject.Find("CollectorQuestBoard");
        var upgrade = GameObject.Find("Upgrade Box");
        if (session == null || shelves.Length != 80 || golden == null || quest == null || upgrade == null)
            throw new InvalidOperationException("Expected the collection and all three feature stations.");
        Directory.CreateDirectory(Root); Directory.CreateDirectory(Evidence); AssetDatabase.Refresh();
        EditorSceneManager.SaveScene(scene, Evidence + "/Level_before_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity", true);

        var candidate = Object.Instantiate(session.roomLayout);
        Configure(candidate);
        CollectorRoomBuilder.Pile pile;
        try
        {
            pile = CrowdedCollectorRoomBuilder.Bake(candidate, session.catalog, session.seed, Evidence, .12f);
            CrowdedCollectorRoomBuilder.Validate(candidate, pile, session.catalog, Evidence);
        }
        catch { Object.DestroyImmediate(candidate); throw; }
        var layout = AssetDatabase.LoadAssetAtPath<CollectorRoomLayout>(Root + "/PromenadeLayout.asset");
        if (layout == null) { layout = candidate; AssetDatabase.CreateAsset(layout, Root + "/PromenadeLayout.asset"); }
        else { EditorUtility.CopySerialized(candidate, layout); Object.DestroyImmediate(candidate); }
        layout.name = "PromenadeLayout";
        File.WriteAllText(Root + "/PromenadePile.json", JsonUtility.ToJson(pile, true));
        AssetDatabase.ImportAsset(Root + "/PromenadePile.json");
        var generated = GameObject.Find("Car Store Generated").transform;
        // Move the complete reward hierarchy, including its camera path and effects, before replacing the room.
        golden.transform.SetParent(generated, true);
        foreach (var name in new[] { "Crowded Shop Interior", InteriorName })
        {
            var old = generated.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
        }
        interior = new GameObject(InteriorName).transform; interior.SetParent(generated, false); interior.position = Origin;
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/00_Project/04_ِArt/UI/Fonts/Fredoka-SemiBold SDF.asset");
        Architecture(layout);
        foreach (var pose in layout.shelves)
        {
            var shelf = shelves.Single(s => s.SaveId == pose.saveId);
            shelf.transform.SetPositionAndRotation(Origin + pose.position, Quaternion.Euler(0, pose.yaw, 0));
            PrefabUtility.RecordPrefabInstancePropertyModifications(shelf.transform);
        }
        Place(quest.transform, new Vector3(-4.4f, 0, -14.1f), -68);
        Place(upgrade.transform, new Vector3(4.4f, 0, -14.1f), 248);
        Place(golden.transform, new Vector3(0, 0, 16), 180);
        var ceremony = golden.GetComponent<GoldenRewardCeremony>();
        ceremony.protectedLooseBounds = new Bounds(Origin + new Vector3(0, 1, 16), new Vector3(5.5f, 2, 5.5f));
        EditorUtility.SetDirty(ceremony);
        golden.transform.SetParent(interior, true);
        session.roomLayout = layout;
        session.pileLayout = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/PromenadePile.json");
        session.pileOrigin.position = Origin;
        session.spawnEntireCollectionAtStart = true; session.visiblePileLimit = 1200; session.boundedPile = false;
        Place(Object.FindFirstObjectByType<Player>().transform, layout.spawn - Origin, 0);
        EditorUtility.SetDirty(session); EditorUtility.SetDirty(layout);
        ThemedShelfSignClearance.Apply();
        FinishLighting();
        Validate();
        EditorSceneManager.SaveScene(scene);
        RefreshLightingReflections();
    }

    public static void RefreshLightingReflections()
    {
        var scene = SceneManager.GetActiveScene();
        var controller = Object.FindFirstObjectByType<ShowroomLightingController>();
        int step = 0;
        // Let the renderer finish each theme before starting the next probe bake.
        EditorApplication.CallbackFunction refresh = null;
        refresh = () =>
        {
            if (!scene.isLoaded || EditorApplication.isPlayingOrWillChangePlaymode)
            { EditorApplication.update -= refresh; return; }
            try
            {
                if (step < 2)
                {
                    CaptureLightingReflection(controller, step++ == 0 ? LightingTheme.Dark : LightingTheme.Bright);
                    return;
                }
                EditorApplication.update -= refresh;
                controller.ApplyTheme(LightingThemeSettings.Current);
                EditorUtility.SetDirty(controller);
                EditorSceneManager.SaveScene(scene);
                Directory.CreateDirectory(Evidence + "/Lighting");
                File.WriteAllText(Evidence + "/Lighting/reflections.txt", "Original Bright and Dark materials and presets; both compact room reflections refreshed.\n");
            }
            catch { EditorApplication.update -= refresh; throw; }
        };
        EditorApplication.update += refresh;
    }

    static void CaptureLightingReflection(ShowroomLightingController controller, LightingTheme theme)
    {
        controller.ApplyTheme(theme);
        var probe = controller.reflectionProbe;
        var go = new GameObject("Temporary showroom reflection camera");
        var camera = go.AddComponent<Camera>(); camera.enabled = false;
        camera.transform.position = probe.transform.position + probe.center;
        camera.nearClipPlane = probe.nearClipPlane; camera.farClipPlane = probe.farClipPlane;
        camera.cullingMask = probe.cullingMask; camera.allowHDR = true;
        camera.clearFlags = CameraClearFlags.Skybox; camera.backgroundColor = probe.backgroundColor;
        int size = probe.resolution;
        var cube = new RenderTexture(size, size, 24, RenderTextureFormat.ARGBHalf) { dimension = TextureDimension.Cube };
        var panorama = new RenderTexture(size * 4, size * 2, 0, RenderTextureFormat.ARGBHalf);
        var pixels = new Texture2D(size * 4, size * 2, TextureFormat.RGBAFloat, false, true);
        var previous = RenderTexture.active;
        try
        {
            if (!camera.RenderToCubemap(cube)) throw new InvalidOperationException("Could not render " + theme + " reflections.");
            cube.ConvertToEquirect(panorama, Camera.MonoOrStereoscopicEye.Mono);
            RenderTexture.active = panorama;
            pixels.ReadPixels(new Rect(0, 0, panorama.width, panorama.height), 0, 0); pixels.Apply();
            string path = ShowroomThemeBuilder.ReflectionPath(SceneManager.GetActiveScene(), theme);
            string temporary = Path.GetFullPath("Temp/CompactReflection-" + theme + ".exr");
            Directory.CreateDirectory(Path.GetDirectoryName(temporary));
            File.WriteAllBytes(temporary, pixels.EncodeToEXR(Texture2D.EXRFlags.CompressZIP));
            // Replace the source atomically; preserve the existing texture GUID and importer settings.
            if (File.Exists(path)) File.Replace(temporary, Path.GetFullPath(path), null);
            else File.Move(temporary, Path.GetFullPath(path));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.maxTextureSize != size) { importer.maxTextureSize = size; importer.SaveAndReimport(); }
            var texture = AssetDatabase.LoadAssetAtPath<Texture>(path);
            if (texture == null) throw new InvalidOperationException("Reflection texture missing: " + path);
            if (theme == LightingTheme.Bright) controller.brightReflection = texture;
            else controller.darkReflection = texture;
        }
        finally
        {
            RenderTexture.active = previous;
            Object.DestroyImmediate(pixels); Object.DestroyImmediate(panorama); Object.DestroyImmediate(cube); Object.DestroyImmediate(go);
            controller.ApplyTheme(LightingThemeSettings.Current);
        }
    }

    public static void FinishLighting()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != CompactCollectionBuilder.ScenePath)
            throw new InvalidOperationException("Open Level in Edit mode first.");
        var room = GameObject.Find(InteriorName).transform;
        var controller = GameObject.Find("Showroom Lighting").GetComponent<ShowroomLightingController>();
        Material Original(string name, bool dark)
        {
            string path = dark ? ShowroomLightingBuilder.Root + "/Showroom " + name + ".mat"
                : ShopInteriorPolishBuilder.ArtRoot + "/Materials/" + name + ".mat";
            return AssetDatabase.LoadAssetAtPath<Material>(path) ?? throw new InvalidOperationException("Original lighting material missing: " + path);
        }
        var surfaces = controller.surfaces.Where(s => s.renderer != null && !s.renderer.transform.IsChildOf(room)).ToList();
        foreach (var renderer in room.GetComponentsInChildren<MeshRenderer>())
        {
            string material = renderer.name == "Store Ceiling" ? "Warm White Acoustic Panels"
                : renderer.name == "Opal light" || renderer.name == "Lobby and exhibit light" ? "Even Opal Diffusers" : null;
            if (material == null) continue;
            surfaces.Add(new ShowroomLightingController.Surface {
                renderer = renderer,
                bright = renderer.name == "Store Ceiling" && MutedShopSurfaceBuilder.CeilingMaterial(false) != null ? MutedShopSurfaceBuilder.CeilingMaterial(false) : Original(material, false),
                dark = renderer.name == "Store Ceiling" && MutedShopSurfaceBuilder.CeilingMaterial(true) != null ? MutedShopSurfaceBuilder.CeilingMaterial(true) : Original(material, true) });
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            EditorUtility.SetDirty(renderer);
        }
        controller.surfaces = surfaces.ToArray();
        controller.ceilingLights = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Light>(true))
            .Where(l => l.name == "Soft Shop Light").ToArray();
        // Keep the established presets and the menu's material references intact.
        foreach (var surface in surfaces)
        {
            if (!controller.brightPreset.ceilingMaterials.Contains(surface.bright)) controller.brightPreset.ceilingMaterials.Add(surface.bright);
            if (!controller.darkPreset.ceilingMaterials.Contains(surface.dark)) controller.darkPreset.ceilingMaterials.Add(surface.dark);
        }
        controller.ApplyTheme(LightingThemeSettings.Current);
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(controller.brightPreset); EditorUtility.SetDirty(controller.darkPreset);
        var probe = controller.reflectionProbe;
        probe.transform.position = Origin + new Vector3(0, 1.7f, 1); probe.size = new Vector3(25, 5, 37);
        EditorUtility.SetDirty(probe);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
    }

    static void Place(Transform target, Vector3 position, float yaw)
    {
        target.SetPositionAndRotation(Origin + position, Quaternion.Euler(0, yaw, 0));
        PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }

    static void Configure(CollectorRoomLayout l)
    {
        l.revision = 6; l.origin = Origin; l.roomSize = new Vector2(24, 38);
        l.ceilingHeight = 3.4f; l.centralAisle = 0; l.minimumAisle = 1.2f;
        l.spawn = Origin + new Vector3(0, .05f, -15.8f); l.spawnPitch = 6;
        l.walkThroughLooseCars = false;
        l.floorRegions = new[] {
            new Bounds(Vector3.zero, new Vector3(24, 1, 26)),
            new Bounds(new Vector3(0, 0, -15), new Vector3(14, 1, 4)),
            new Bounds(new Vector3(0, 0, 16), new Vector3(6, 1, 6)) };
        var poses = new List<(Vector3 p, float yaw)>();
        foreach (int side in new[] { -1, 1 })
            for (int i = 0; i < 12; i++) poses.Add((new Vector3(side * 11.7f, 0, -9.46f + i * 1.72f), side < 0 ? 90 : -90));
        foreach (int side in new[] { -1, 1 })
            for (int i = 0; i < 4; i++) poses.Add((new Vector3(side * (5.16f + i * 1.72f), 0, 12.7f), 180));
        foreach (float x in new[] { -4f, 4f })
            foreach (int side in new[] { -1, 1 })
                for (int i = 0; i < 12; i++) poses.Add((new Vector3(x + side * .25f, 0, -9.46f + i * 1.72f), side < 0 ? -90 : 90));
        var themes = l.sections.SelectMany((s, i) => s.themes.Select(t => new { theme = t, section = i })).ToArray();
        l.shelves = poses.Select((p, i) => new CollectorRoomLayout.ShelfPose {
            saveId = "COMPACT-" + themes[i / 2].theme + "-S" + (i % 2 + 1).ToString("D3"),
            section = themes[i / 2].section, position = p.p, yaw = p.yaw }).ToArray();
        l.pileRegions = new[] { new Bounds(new Vector3(0, .55f, 0), new Vector3(21.2f, 1.1f, 23.2f)) };
        // Only reserve the island fixtures. The centre and side passages are part of the scatter.
        l.pileExclusions = new[] {
            new Bounds(new Vector3(-4, 1, 0), new Vector3(2.24f, 2, 20.78f)),
            new Bounds(new Vector3(4, 1, 0), new Vector3(2.24f, 2, 20.78f)) };
        for (int i = 0; i < l.sections.Length; i++)
        {
            l.sections[i].center = l.shelves.First(p => p.section == i).position;
            l.sections[i].sortingAreas = Array.Empty<Bounds>();
        }
    }

    static Material Mat(string name, Color color)
    {
        string path = Root + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.color = color; m.SetFloat("_Smoothness", .22f); EditorUtility.SetDirty(m); return m;
    }
    static GameObject Box(string name, Vector3 p, Vector3 size, Material mat, bool solid = false, Transform parent = null)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = name;
        g.transform.SetParent(parent != null ? parent : interior, false);
        g.transform.localPosition = p; g.transform.localScale = size;
        g.GetComponent<Renderer>().sharedMaterial = mat;
        if (!solid) Object.DestroyImmediate(g.GetComponent<Collider>());
        return g;
    }
    static void Text(string name, string value, Vector3 p, Vector2 size, float fontSize, Color color, Vector3 euler)
    {
        var g = new GameObject(name); g.transform.SetParent(interior, false);
        g.transform.localPosition = p; g.transform.localEulerAngles = euler;
        var t = g.AddComponent<TextMeshPro>(); t.font = font; t.text = value; t.fontSize = fontSize;
        t.color = color; t.alignment = TextAlignmentOptions.Center; t.rectTransform.sizeDelta = size;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.enableAutoSizing = true; t.fontSizeMin = fontSize * .5f; t.fontSizeMax = fontSize;
    }
    static void Sign(string text, Vector3 p, float width, Color accent, float yaw = 0)
    {
        var g = Box(text + " sign", p, new Vector3(width, .38f, .07f), Mat("Ink", Ink));
        g.transform.localEulerAngles = new Vector3(0, yaw, 0);
        var q = Quaternion.Euler(0, yaw, 0);
        Text(text + " title", text, p + q * new Vector3(0, .02f, -.045f), new Vector2(width - .16f, .28f), 1.7f, Color.white, new Vector3(0, yaw, 0));
        Text(text + " reverse title", text, p + q * new Vector3(0, .02f, .045f), new Vector2(width - .16f, .28f), 1.7f, Color.white, new Vector3(0, yaw + 180, 0));
        var stripe = Box(text + " accent", p + q * new Vector3(0, -.16f, -.05f), new Vector3(width, .035f, .02f), Mat("Accent " + ColorUtility.ToHtmlStringRGB(accent), accent));
        stripe.transform.localEulerAngles = new Vector3(0, yaw, 0);
    }
    static void Architecture(CollectorRoomLayout l)
    {
        var floor = AssetDatabase.LoadAssetAtPath<Material>(RacingFloorBuilder.ArtRoot + "/Warm Cream Satin.mat");
        var plaster = Mat("Ivory plaster", new Color(.88f, .87f, .77f));
        var ink = Mat("Ink", Ink); var teal = Mat("Teal", Teal);
        var white = Mat("Warm white", new Color(.94f, .95f, .86f)); var gold = Mat("Gold", Gold);
        foreach (var r in l.floorRegions)
        {
            Box("Store Floor", new Vector3(r.center.x, -.2f, r.center.z), new Vector3(r.size.x, .4f, r.size.z), floor, true);
            var roof = Box("Store Ceiling", new Vector3(r.center.x, l.ceilingHeight + .1f, r.center.z), new Vector3(r.size.x, .2f, r.size.z), plaster);
            roof.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }
        void Wall(float x, float z, float w, float d)
        {
            Box("Gallery wall", new Vector3(x, l.ceilingHeight * .5f, z), new Vector3(w, l.ceilingHeight, d), plaster, true);
            Box("Teal dado", new Vector3(x, .55f, z), new Vector3(w + .025f, 1.1f, d + .025f), teal);
            Box("Crown rail", new Vector3(x, 3.12f, z), new Vector3(w + .04f, .07f, d + .04f), ink);
        }
        Wall(-12.15f, 0, .3f, 26.6f); Wall(12.15f, 0, .3f, 26.6f);
        Wall(-9.5f, -13.15f, 5, .3f); Wall(9.5f, -13.15f, 5, .3f);
        Wall(-7.15f, -15, .3f, 4); Wall(7.15f, -15, .3f, 4); Wall(0, -17.15f, 14.6f, .3f);
        Wall(-7.5f, 13.15f, 9, .3f); Wall(7.5f, 13.15f, 9, .3f);
        Wall(-3.15f, 16, .3f, 6); Wall(3.15f, 16, .3f, 6); Wall(0, 19.15f, 6.6f, .3f);
        foreach (float x in new[] { -4f, 4f })
            Box("Island oak spine", new Vector3(x, .8f, 0), new Vector3(.1f, 1.6f, 20.65f), Mat("Oak", new Color(.59f, .4f, .23f)), true);
        // Keep the floor continuous: the loose collection itself fills all three passages.
        foreach (float z in new[] { -7f, 7f })
        {
            foreach (float x in new[] { -8f, 0f, 8f })
            {
                Box("Slim fixture housing", new Vector3(x, 3.28f, z), new Vector3(3.05f, .08f, .75f), ink);
                Box("Opal light", new Vector3(x, 3.23f, z), new Vector3(2.9f, .035f, .6f), white);
                var g = new GameObject("Soft Shop Light"); g.transform.SetParent(interior, false); g.transform.localPosition = new Vector3(x, 3.07f, z);
                var light = g.AddComponent<Light>(); light.type = LightType.Point; light.shadows = LightShadows.None; light.range = 12; light.intensity = 2;
            }
        }
        foreach (float z in new[] { -15f, 16f })
        {
            Box("Lobby and exhibit light", new Vector3(0, 3.29f, z), new Vector3(2.8f, .06f, .7f), white);
            var g = new GameObject("Soft Shop Light"); g.transform.SetParent(interior, false); g.transform.localPosition = new Vector3(0, 3.1f, z);
            g.AddComponent<Light>().range = 10;
        }
        Sign("QUESTS", new Vector3(-4.4f, 2.85f, -14.1f), 1.8f, Teal, -68);
        Sign("UPGRADES", new Vector3(4.4f, 2.85f, -14.1f), 1.8f, Coral, 68);
        Sign("THE GOLDEN CAR", new Vector3(0, 2.95f, 13), 3.6f, Gold);
        Text("Reward explanation", "COMPLETE YOUR COLLECTION", new Vector3(0, 2.7f, 18.95f), new Vector2(5.5f, .3f), 1.7f, Ink, Vector3.zero);
        Box("Reward carpet", new Vector3(0, .018f, 16), new Vector3(5.2f, .02f, 5.5f), gold);
        Box("Quest welcome mat", new Vector3(-4.5f, .017f, -14.8f), new Vector3(3, .02f, 2.5f), Mat("Quest mat", new Color(.55f, .76f, .72f)));
        Box("Workshop welcome mat", new Vector3(4.5f, .017f, -14.8f), new Vector3(3, .02f, 2.5f), Mat("Workshop mat", new Color(.85f, .62f, .48f)));
        RestoreOriginalFloor();
        MutedShopSurfaceBuilder.ApplySurfaces();
    }

    public static void RestoreOriginalFloor()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().path != CompactCollectionBuilder.ScenePath)
            throw new InvalidOperationException("Open Level in Edit mode first.");
        var room = GameObject.Find(InteriorName).transform;
        Material Original(string name) => AssetDatabase.LoadAssetAtPath<Material>(RacingFloorBuilder.ArtRoot + "/" + name + ".mat")
            ?? throw new InvalidOperationException("Original floor material missing: " + name);
        foreach (var renderer in room.GetComponentsInChildren<MeshRenderer>().Where(r => r.name == "Store Floor"))
        {
            renderer.sharedMaterial = Original("Warm Cream Satin");
            EditorUtility.SetDirty(renderer);
        }
        var root = room.Find(RacingFloorBuilder.DecorationName);
        if (root == null) { root = new GameObject(RacingFloorBuilder.DecorationName).transform; root.SetParent(room, false); }
        root.localPosition = Vector3.zero; root.localRotation = Quaternion.identity; root.localScale = Vector3.one;
        var names = new[] { "Toy Town Background", "Track", "Lane Markings", "Coral Curbs", "Perimeter Trim" };
        var materials = new[] { "Toy Town Satin", "Teal Track Satin", "Ivory Lane Paint", "Coral Corner Paint", "Deep Teal Trim" };
        for (int i = 0; i < names.Length; i++)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(RacingFloorBuilder.ArtRoot + "/" + names[i] + ".asset")
                ?? throw new InvalidOperationException("Original floor graphics missing: " + names[i]);
            var child = root.Find(names[i]);
            var go = child != null ? child.gameObject : new GameObject(names[i]);
            go.transform.SetParent(root, false); go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity; go.transform.localScale = Vector3.one; go.layer = 2;
            var filter = go.GetComponent<MeshFilter>(); if (filter == null) filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>(); if (renderer == null) renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = Original(materials[i]); renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = true;
            EditorUtility.SetDirty(go); EditorUtility.SetDirty(renderer);
        }
    }

    public static void Validate()
    {
        var s = Object.FindFirstObjectByType<ThemedCollectionSession>();
        var pile = JsonUtility.FromJson<CollectorRoomBuilder.Pile>(s.pileLayout.text);
        CrowdedCollectorRoomBuilder.Validate(s.roomLayout, pile, s.catalog, Evidence);
        ExactThemeShelfBuilder.ValidateScene();
        var shelves = Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None);
        foreach (var p in s.roomLayout.shelves)
            if (Vector3.Distance(shelves.Single(x => x.SaveId == p.saveId).transform.position, Origin + p.position) > .01f)
                throw new InvalidOperationException("Shelf placement mismatch: " + p.saveId);
        var counts = new[] {
            pile.placements.Count(p => p.position.x < -5.12f),
            pile.placements.Count(p => Mathf.Abs(p.position.x) < 2.88f),
            pile.placements.Count(p => p.position.x > 5.12f) };
        if (counts.Any(n => n < 200)) throw new InvalidOperationException("Cars must fill the centre and both side passages.");
        foreach (var p in pile.placements)
            if (!s.roomLayout.Contains(Origin + p.position)) throw new InvalidOperationException("Car outside shop floor: " + p.id);
        var geometry = GameObject.Find(InteriorName).GetComponentsInChildren<Transform>();
        if (geometry.Any(t => t.name.StartsWith("Sorting bay") || t.name == "Low stock rim" || t.name == "Collector walk"))
            throw new InvalidOperationException("The compact shop must not contain bays or a central road.");
        File.WriteAllText(Evidence + "/layout-validation.json", JsonUtility.ToJson(new Report {
            passed = true, revision = s.roomLayout.revision, shelves = shelves.Length, slots = shelves.Sum(x => x.SpotCount),
            cars = pile.placements.Length, carsPerPassage = counts,
            floorArea = s.roomLayout.floorRegions.Sum(r => r.size.x * r.size.z), ceilingHeight = s.roomLayout.ceilingHeight }, true));
    }
    [Serializable] class Report { public bool passed; public int revision, shelves, slots, cars; public int[] carsPerPassage; public float floorArea, ceilingHeight; }
}
