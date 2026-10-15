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
using Object = UnityEngine.Object;

public static class CollectorRoomBuilder
{
    public const string Root = "Assets/TinyRiders/Gameplay/Compact/CollectorRoom";
    public const string LayoutPath = Root + "/CollectorRoomLayout.asset";
    public const string Evidence = "IntegrationEvidence/CollectorRoom";
    static readonly Color Ink = new Color(.055f, .14f, .17f);
    static TMP_FontAsset font;
    [Serializable] public class Pile { public int revision; public ThemedInstanceRecord[] placements; }

    [MenuItem("Tools/Tiny Riders/Collector Shop/Apply Room Redesign")]
    public static void Build()
    {
        CrowdedCollectorRoomBuilder.Build();
    }

    public static void BuildLegacy()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before editing the room.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != CompactCollectionBuilder.ScenePath) throw new InvalidOperationException("Open the compact Level scene first.");
        Directory.CreateDirectory(Root); Directory.CreateDirectory(Evidence); AssetDatabase.Refresh();
        // Save a copy of the live scene, including unsaved edits, without replacing earlier snapshots.
        string backup = Evidence + "/Level_before_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity";
        EditorSceneManager.SaveScene(scene, backup, true);
        var session = Object.FindFirstObjectByType<ThemedCollectionSession>();
        var shelves = Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None);
        if (session == null || session.catalog.entries.Count(e => e.enabled) != 600 || shelves.Length != 80)
            throw new InvalidOperationException("Expected the current 600-design / 80-shelf compact collection.");
        var layout = AssetDatabase.LoadAssetAtPath<CollectorRoomLayout>(LayoutPath);
        if (layout == null)
        {
            layout = ScriptableObject.CreateInstance<CollectorRoomLayout>();
            Populate(layout); AssetDatabase.CreateAsset(layout, LayoutPath);
        }
        var pile = GeneratePile(layout, session.catalog, session.seed);
        ValidateDefinition(layout, pile, session.catalog);
        File.WriteAllText(Root + "/CollectorPile.json", JsonUtility.ToJson(pile, true));
        AssetDatabase.ImportAsset(Root + "/CollectorPile.json");
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/00_Project/04_ِArt/UI/Fonts/Fredoka-SemiBold SDF.asset");
        var generated = scene.GetRootGameObjects().Single(g => g.name == "Car Store Generated");
        foreach (var name in new[] { "Store Architecture", "Collector Shop Interior", "Directory and Aisle Signs" })
        { var old = generated.transform.Find(name); if (old != null) Object.DestroyImmediate(old.gameObject); }
        var interior = new GameObject("Collector Shop Interior").transform; interior.SetParent(generated.transform, false);
        Architecture(interior, layout);
        foreach (var pose in layout.shelves)
        {
            var shelf = shelves.Single(s => s.SaveId == pose.saveId);
            shelf.transform.SetPositionAndRotation(layout.origin + pose.position, Quaternion.Euler(0, pose.yaw, 0));
            var section = layout.sections[pose.section];
            var label = shelf.transform.Find("Theme Header")?.GetComponent<TextMeshPro>();
            if (label != null)
            {
                label.text = session.ThemeName(shelf.GetComponent<ThemeShelfBinding>().themeId);
                label.color = Ink; label.fontSize = 1.05f; label.enableAutoSizing = true;
                label.fontSizeMin = .65f; label.fontSizeMax = 1.05f;
                label.transform.localPosition = new Vector3(0, 1.59f, .145f);
                label.rectTransform.sizeDelta = new Vector2(1.08f, .17f);
            }
            var blocker = shelf.transform.Find("Player Collision");
            if (blocker == null) { blocker = new GameObject("Player Collision").transform; blocker.SetParent(shelf.transform,false); }
            blocker.gameObject.layer = 0;
            var solid = blocker.GetComponent<BoxCollider>();
            if (solid == null) solid = blocker.gameObject.AddComponent<BoxCollider>();
            solid.center = new Vector3(0,.8f,0); solid.size = new Vector3(1.704f,1.6f,.38f);
            var batch = shelf.transform.Find("Batched Shelf Art");
            if (batch != null)
                foreach (var renderer in batch.GetComponentsInChildren<MeshRenderer>())
                    if (renderer.sharedMaterial != null && (renderer.sharedMaterial.name.Contains("SidePanels") || renderer.sharedMaterial.name.StartsWith("Section Paint")))
                    {
                        string path = Root + "/Section Paint " + pose.section + ".mat";
                        var paint = AssetDatabase.LoadAssetAtPath<Material>(path);
                        if (paint == null) { paint = new Material(renderer.sharedMaterial); AssetDatabase.CreateAsset(paint, path); }
                        paint.name = "Section Paint " + pose.section; paint.SetColor("_BaseColor", section.color);
                        renderer.sharedMaterial = paint; EditorUtility.SetDirty(paint);
                    }
            EditorUtility.SetDirty(shelf);
        }
        session.roomLayout = layout; session.pileLayout = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/CollectorPile.json");
        session.pileOrigin.position = layout.origin; session.boundedPile = false;
        session.spawnEntireCollectionAtStart = true; session.visiblePileLimit = 1200;
        EditorUtility.SetDirty(session);
        var player = Object.FindFirstObjectByType<Player>();
        player.transform.SetPositionAndRotation(layout.spawn, Quaternion.identity);
        PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);
        var upgrade = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == "Upgrade Box");
        if (upgrade != null)
        {
            upgrade.position = layout.origin + new Vector3(-17.35f, 0, -19.6f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(upgrade);
        }
        EditorUtility.SetDirty(layout); AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Audit();
        Debug.Log("COLLECTOR_ROOM_READY: 36x44 / 8 sections / 80 shelves / 1200 mixed cars. " + backup);
    }

    static void Populate(CollectorRoomLayout l)
    {
        string[] titles = { "Wild Wonders", "Coast & Climate", "Cosmic Workshop", "Legends & Magic", "Busy Town", "Creative Corner", "Party & Treats", "Grand Adventures" };
        string[][] groups = {
            new[]{"JUNGLE","FARM","HONEY_BEE","BUTTERFLY_GARDEN","MUSHROOM_GROVE"},
            new[]{"BEACH","OCEAN","ICE","FIRE","THUNDERSTORM"},
            new[]{"SPACE","ALIEN","ROBOT","SCIENCE_LAB","STEAMPUNK"},
            new[]{"PIRATE","ROYAL","WIZARD","VIKING_LEGENDS","ANCIENT_EGYPT"},
            new[]{"CONSTRUCTION","RESCUE","POST_OFFICE","RAILWAY","AVIATION"},
            new[]{"ART_STUDIO","MUSIC","CINEMA","PHOTOGRAPHY","BARBERSHOP"},
            new[]{"FOOD","CANDY","PARTY","WINTER_HOLIDAYS","SPORT"},
            new[]{"PREHISTORIC","WILD_WEST","NINJA","DETECTIVE","SPOOKY"}
        };
        Color[] colors = { new Color(.23f,.52f,.35f), new Color(.12f,.53f,.68f), new Color(.34f,.36f,.62f), new Color(.58f,.32f,.55f), new Color(.77f,.44f,.18f), new Color(.2f,.52f,.5f), new Color(.76f,.35f,.36f), new Color(.48f,.46f,.28f) };
        var poses = new List<CollectorRoomLayout.ShelfPose>();
        l.sections = new CollectorRoomLayout.Section[8];
        for (int i = 0; i < 8; i++)
        {
            float x = i % 2 == 0 ? -9 : 9, z = -15.75f + i / 2 * 10.5f;
            var s = new CollectorRoomLayout.Section { title = titles[i], themes = groups[i].Select(t => "TH_" + t).ToArray(), color = colors[i], center = new Vector3(x,0,z) };
            s.sortingAreas = new[] { new Bounds(new Vector3(x-4.3f,.7f,z),new Vector3(3.1f,1.4f,8.65f)),new Bounds(new Vector3(x+4.3f,.7f,z),new Vector3(3.1f,1.4f,8.65f)) };
            l.sections[i] = s;
            for (int t = 0; t < 5; t++) for (int side = 0; side < 2; side++)
                poses.Add(new CollectorRoomLayout.ShelfPose { section = i, saveId = "COMPACT-" + s.themes[t] + "-S" + (side+1).ToString("D3"), position = new Vector3(x + (side == 0 ? -.25f : .25f),0,z + (t-2)*1.72f), yaw = side == 0 ? -90 : 90 });
        }
        l.shelves = poses.ToArray();
    }

    public static Bounds ArtBounds(GameObject prefab)
    {
        var capsule = prefab.GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            var size = Vector3.one * capsule.radius * 2f;
            size[capsule.direction] = capsule.height;
            return new Bounds(capsule.center, size);
        }
        bool found = false; Bounds bounds = default;
        foreach (var renderer in prefab.GetComponentsInChildren<MeshRenderer>(true))
        {
            var mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh; if (mesh == null) continue;
            var b = mesh.bounds;
            foreach (float x in new[] { b.min.x, b.max.x }) foreach (float y in new[] { b.min.y, b.max.y }) foreach (float z in new[] { b.min.z, b.max.z })
            {
                var p = prefab.transform.InverseTransformPoint(renderer.transform.TransformPoint(new Vector3(x,y,z)));
                if (!found) { bounds = new Bounds(p, Vector3.zero); found = true; } else bounds.Encapsulate(p);
            }
        }
        if (!found) throw new InvalidOperationException("Missing car geometry: " + prefab.name);
        return bounds;
    }

    public static Pile GeneratePile(CollectorRoomLayout layout, ThemedCarCatalog catalog, int seed)
    {
        var entries = catalog.entries.Where(e => e.enabled).ToDictionary(e => e.collectibleId);
        var ids = ThemedPileSequence.InstanceIds(entries.Values, 2, seed)
            .OrderBy(id => ThemedPileSequence.Rank(id + ":collector-room", seed)).ThenBy(id => id, StringComparer.Ordinal).ToArray();
        var bounds = entries.ToDictionary(p => p.Key, p => ArtBounds(p.Value.sortingItem.WorldItem.gameObject));
        var poses = new List<ThemedInstanceRecord>();
        // Shuffle physical IDs before distributing them: a 40-theme cycle sampled every eighth
        // item would otherwise give each department only five themes.
        for (int n = 0; n < ids.Length; n++)
        {
            int section = n % 8, cell = n / 8, side = cell / 75, local = cell % 75;
            bool upper = local >= 48; int grid = upper ? (local-48) / 3 * 4 + (local-48) % 3 : local;
            int col = grid % 4, row = grid / 4;
            string collectible = ids[n].Split(':')[1]; var b = bounds[collectible];
            var center = layout.sections[section].sortingAreas[side].center;
            float yaw = (n * 37 % 25 - 12) + (n % 2 == 0 ? 0 : 180);
            float y = .025f - b.min.y;
            if (upper)
            {
                var support = poses[(side*75+grid)*8+section];
                y = support.position.y + bounds[support.collectibleId].max.y + .025f - b.min.y;
            }
            var offset = Quaternion.Euler(0,yaw,0)*b.center;
            poses.Add(new ThemedInstanceRecord { id = ids[n], collectibleId = collectible, state = "world", position = new Vector3(center.x + (col-1.5f)*.78f + (n%3-1)*.008f-offset.x,y,center.z+(row-5.5f)*.72f-offset.z), euler = new Vector3(0,yaw,0) });
        }
        return new Pile { revision = layout.revision, placements = poses.ToArray() };
    }

    public static void ValidateDefinition(CollectorRoomLayout l, Pile p, ThemedCarCatalog c)
    {
        if (l.sections.Length != 8 || l.shelves.Length != 80 || p.placements.Length != 1200) throw new InvalidOperationException("Room capacity mismatch");
        if (l.sections.SelectMany(s => s.themes).Distinct().Count() != 40 || l.sections.Any(s => s.themes.Length != 5)) throw new InvalidOperationException("Theme assignment mismatch");
        if (l.sections.SelectMany(s => s.themes).Except(c.themes.Select(t => t.id)).Any()) throw new InvalidOperationException("Unknown section theme");
        if (p.placements.Select(r => r.id).Distinct().Count() != 1200 || p.placements.GroupBy(r => r.collectibleId).Any(g => g.Count() != 2)) throw new InvalidOperationException("Physical ID mismatch");
        var bounds = c.entries.Where(e => e.enabled).ToDictionary(e => e.collectibleId, e => ArtBounds(e.sortingItem.WorldItem.gameObject));
        var occupied = new List<Bounds>();
        foreach (var r in p.placements)
        {
            var b = bounds[r.collectibleId]; var q = Quaternion.Euler(r.euler); Bounds box = new Bounds(r.position + q*b.center, Vector3.zero);
            foreach (float x in new[] { b.min.x,b.max.x }) foreach (float y in new[] { b.min.y,b.max.y }) foreach (float z in new[] { b.min.z,b.max.z }) box.Encapsulate(r.position+q*new Vector3(x,y,z));
            if (!l.Contains(l.origin+box.min,.5f) || !l.Contains(l.origin+box.max,.5f)) throw new InvalidOperationException("Car outside room: " + r.id);
            if (box.min.x < l.centralAisle*.5f && box.max.x > -l.centralAisle*.5f) throw new InvalidOperationException("Car blocks main aisle");
            if (!l.sections.SelectMany(s=>s.sortingAreas).Any(a=>box.min.x>=a.min.x&&box.max.x<=a.max.x&&box.min.z>=a.min.z&&box.max.z<=a.max.z))
                throw new InvalidOperationException("Car outside its sorting area: " + r.id);
            if (occupied.Any(other => other.Intersects(box))) throw new InvalidOperationException("Starting cars overlap: " + r.id);
            occupied.Add(box);
        }
    }

    static Material Mat(string name, Color color)
    {
        string path = Root + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat,path); }
        mat.color = color; mat.SetFloat("_Smoothness",.16f); EditorUtility.SetDirty(mat); return mat;
    }
    static GameObject Box(Transform parent, string name, Vector3 p, Vector3 size, Material mat, bool solid = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent,false);
        go.transform.position = new Vector3(500,0,0)+p; go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat; go.isStatic = true;
        if (!solid) { Object.DestroyImmediate(go.GetComponent<Collider>()); go.layer = 2; }
        return go;
    }
    static void Label(Transform parent, string name, string text, Vector3 p, float yaw, Vector2 size, float fontSize, Color color)
    {
        var go = new GameObject(name); go.layer = 2; go.transform.SetParent(parent,false);
        go.transform.position = new Vector3(500,0,0)+p; go.transform.rotation = Quaternion.Euler(0,yaw,0);
        var label = go.AddComponent<TextMeshPro>(); label.font = font; label.text = text; label.fontSize = fontSize;
        label.color = color; label.alignment = TextAlignmentOptions.Center; label.rectTransform.sizeDelta = size;
    }
    static void Architecture(Transform root, CollectorRoomLayout l)
    {
        var floor = Mat("Warm Gray Floor",new Color(.57f,.59f,.57f)); var cream = Mat("Warm Ivory",new Color(.91f,.88f,.79f));
        var wood = Mat("Pale Oak",new Color(.69f,.51f,.32f)); var teal = Mat("Shop Teal",new Color(.08f,.34f,.36f));
        var white = Mat("Soft White",new Color(.97f,.96f,.91f)); var ink = Mat("Ink",Ink);
        Box(root,"Store Floor",new Vector3(0,-.2f,0),new Vector3(l.roomSize.x+.6f,.4f,l.roomSize.y+.6f),floor,true);
        Box(root,"West Wall",new Vector3(-18.15f,2.3f,0),new Vector3(.3f,4.6f,44.6f),cream,true);
        Box(root,"East Wall",new Vector3(18.15f,2.3f,0),new Vector3(.3f,4.6f,44.6f),cream,true);
        Box(root,"Back Wall",new Vector3(0,2.3f,22.15f),new Vector3(36,.0f+4.6f,.3f),cream,true);
        Box(root,"Front Wall",new Vector3(0,2.3f,-22.15f),new Vector3(36,4.6f,.3f),cream,true);
        var ceiling = Mat("Ceiling Ivory",new Color(.85f,.85f,.80f)); ceiling.shader=Shader.Find("Universal Render Pipeline/Unlit");EditorUtility.SetDirty(ceiling);
        Box(root,"Store Ceiling",new Vector3(0,4.7f,0),new Vector3(36.6f,.2f,44.6f),ceiling,true);
        foreach (float x in new[]{-17.96f,17.96f})
        {
            Box(root,"Oak Wainscot",new Vector3(x,.48f,0),new Vector3(.07f,.96f,44),wood);
            Box(root,"Teal Picture Rail",new Vector3(x,3.55f,0),new Vector3(.08f,.16f,44),teal);
        }
        foreach (float z in new[]{-21.96f,21.96f}) Box(root,"End Wall Rail",new Vector3(0,3.55f,z),new Vector3(36,.16f,.08f),teal);
        foreach (float x in new[]{-2.05f,2.05f}) Box(root,"Main Aisle Stripe",new Vector3(x,.006f,0),new Vector3(.065f,.008f,40.5f),teal);
        // Numbered signs sit above the displays; no posts obstruct the cross aisles.
        for(int i=0;i<8;i++)
        {
            var s=l.sections[i]; var accent=Mat("Section " + (i+1),s.color); var c=s.center;
            foreach(var area in s.sortingAreas)
            {
                Box(root,"Sorting Mat " + (i+1),new Vector3(area.center.x,.008f,area.center.z),new Vector3(3.2f,.012f,8.65f),Mat("Mat " + (i+1),Color.Lerp(s.color,new Color(.69f,.69f,.63f),.8f)));
                foreach(float end in new[]{-1f,1f}) Box(root,"Mat Edge",new Vector3(area.center.x,.018f,area.center.z+end*4.32f),new Vector3(3.2f,.008f,.045f),accent);
            }
            Box(root,"Island Oak Spine " + (i+1),c+new Vector3(0,.8f,0),new Vector3(.1f,1.6f,8.55f),wood);
            float signZ=c.z+3.5f,signX=Mathf.Sign(c.x)*5;
            Box(root,"Department Sign " + (i+1),new Vector3(signX,2.4f,signZ),new Vector3(4.6f,.85f,.12f),accent);
            Label(root,"Department Name " + (i+1),(i+1).ToString("00")+"  /  "+s.title,new Vector3(signX,2.41f,signZ-.067f),0,new Vector2(4.3f,.6f),3.1f,Color.white);
            Label(root,"Department Reverse " + (i+1),(i+1).ToString("00")+"  /  "+s.title,new Vector3(signX,2.41f,signZ+.067f),180,new Vector2(4.3f,.6f),3.1f,Color.white);
            foreach(float x in new[]{signX-1.85f,signX+1.85f}) Box(root,"Sign Suspension",new Vector3(x,3.7125f,signZ),new Vector3(.025f,1.775f,.025f),ink);
            Box(root,"Ceiling Diffuser",new Vector3(c.x,4.53f,c.z),new Vector3(5,.06f,1.4f),white);
            var lamp=new GameObject("Soft Shop Light");lamp.transform.SetParent(root,false);lamp.transform.position=l.origin+new Vector3(c.x,4.2f,c.z);
            var light=lamp.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.95f,.85f);light.intensity=1.35f;light.range=15;light.shadows=LightShadows.None;
        }
        // Arrival area: wall-mounted directory and a compact counter outside the continuous side route.
        Box(root,"Collector Counter",new Vector3(-17.3f,.48f,-16),new Vector3(.8f,.96f,2.4f),teal,true);
        Box(root,"Counter Oak Top",new Vector3(-17.3f,1,-16),new Vector3(.95f,.1f,2.5f),wood);
        for(int i=0;i<3;i++)
        {
            Box(root,"Boxed Stock",new Vector3(-17.3f,1.28f,-16.75f+i*.72f),new Vector3(.6f,.45f,.62f),cream);
            Box(root,"Stock Window",new Vector3(-16.992f,1.28f,-16.75f+i*.72f),new Vector3(.018f,.3f,.44f),teal);
        }
        Box(root,"Directory Board",new Vector3(-6,2.35f,-21.87f),new Vector3(7,2,.12f),white);
        string directory="FIND YOUR COLLECTION\n"+string.Join("\n",Enumerable.Range(0,4).Select(i=>(i*2+1).ToString("00")+" "+l.sections[i*2].title+"     |     "+(i*2+2).ToString("00")+" "+l.sections[i*2+1].title));
        Label(root,"Store Directory",directory,new Vector3(-6,2.35f,-21.79f),180,new Vector2(6.6f,1.8f),2.8f,Ink);
        Label(root,"Shop Brand","TINY RIDERS",new Vector3(0,3.95f,21.87f),0,new Vector2(12,.8f),7,Ink);
        Label(root,"Shop Tagline","SMALL CARS. BIG COLLECTIONS.",new Vector3(0,3.3f,21.86f),0,new Vector2(10,.45f),2.8f,Ink);
        Box(root,"Entry Door",new Vector3(0,1.4f,-21.88f),new Vector3(3.5f,2.8f,.08f),teal);
        foreach(float x in new[]{-1f,1f}) Box(root,"Door Glass",new Vector3(x,1.6f,-21.82f),new Vector3(1.3f,1.65f,.03f),Mat("Frosted Blue",new Color(.45f,.66f,.69f)));
        Label(root,"Entry Welcome","WELCOME, COLLECTOR",new Vector3(0,3.1f,-21.82f),180,new Vector2(6,.5f),3.2f,Ink);
        for(int i=0;i<4;i++)
        {
            float z=-15.75f+i*10.5f;
            foreach(float side in new[]{-1f,1f})
            {
                float x=side*17.9f;
                Box(root,"Car Poster Frame",new Vector3(x,2.4f,z),new Vector3(.09f,1.55f,2.5f),wood);
                var panel=Box(root,"Car Poster",new Vector3(x-side*.055f,2.4f,z),new Vector3(.015f,1.35f,2.3f),white);
                Label(root,"Collector Poster",new[]{"DRIVE. SORT. COLLECT.","PARK YOUR IMAGINATION","EVERY CAR HAS A HOME","BUILD YOUR DREAM GARAGE"}[i],new Vector3(x-side*.07f,2.12f,z),side<0?-90:90,new Vector2(2.15f,.5f),2.5f,Ink);
                Box(root,"Poster Car Body",new Vector3(x-side*.075f,2.6f,z),new Vector3(.02f,.22f,1.45f),teal);
                Box(root,"Poster Car Roof",new Vector3(x-side*.075f,2.78f,z-.08f),new Vector3(.02f,.2f,.76f),teal);
                foreach(float wheel in new[]{-.47f,.47f})Box(root,"Poster Wheel",new Vector3(x-side*.09f,2.48f,z+wheel),new Vector3(.025f,.22f,.22f),ink);
            }
        }
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.85f,.88f,.9f);
        RenderSettings.ambientEquatorColor=new Color(.65f,.68f,.66f);RenderSettings.ambientGroundColor=new Color(.4f,.39f,.35f);RenderSettings.ambientIntensity=1;
        RenderSettings.fog=false;
        foreach(var sun in Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(v=>v.type==LightType.Directional))
        {sun.intensity=.8f;sun.color=new Color(1,.96f,.89f);EditorUtility.SetDirty(sun);}
    }

    [MenuItem("Tools/Tiny Riders/Collector Shop/Audit Room")]
    public static void Audit()
    {
        var s=Object.FindFirstObjectByType<ThemedCollectionSession>();
        if(s==null||s.roomLayout==null)throw new InvalidOperationException("Collector room missing");
        if(s.roomLayout.revision>=3) { CrowdedCollectorRoomBuilder.Audit(); return; }
        var p=JsonUtility.FromJson<Pile>(s.pileLayout.text);ValidateDefinition(s.roomLayout,p,s.catalog);
        var shelves=Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None);
        if(shelves.Length!=80||shelves.Sum(v=>v.SpotCount)!=1440)throw new InvalidOperationException("Shelf capacity changed");
        foreach(var pose in s.roomLayout.shelves)
        {
            var shelf=shelves.Single(v=>v.SaveId==pose.saveId);
            if(Vector3.Distance(shelf.transform.position,s.roomLayout.origin+pose.position)>.01f)throw new InvalidOperationException("Shelf pose mismatch");
        }
        Directory.CreateDirectory(Evidence);
        File.WriteAllText(Evidence+"/layout_audit.json",JsonUtility.ToJson(new AuditReport{passed=true,sections=8,designs=600,copies=1200,shelves=80,slots=1440,roomSize=s.roomLayout.roomSize},true));
        Debug.Log("COLLECTOR_ROOM_AUDIT_PASSED");
    }
    [Serializable] class AuditReport { public bool passed;public int sections,designs,copies,shelves,slots;public Vector2 roomSize; }
}
