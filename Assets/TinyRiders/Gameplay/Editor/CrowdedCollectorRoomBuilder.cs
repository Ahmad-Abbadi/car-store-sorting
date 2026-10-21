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

/// <summary>Revision three: compact perimeter displays and a physics-baked, floor-wide collection.</summary>
public static class CrowdedCollectorRoomBuilder
{
    public const string Root = "Assets/TinyRiders/Gameplay/Compact/CrowdedRoom";
    public const string Evidence = "IntegrationEvidence/CrowdedRoom";
    public const string LayoutPath = Root + "/CrowdedRoomLayout.asset";
    static readonly Color Ink = new Color(.055f,.14f,.17f);
    static TMP_FontAsset font;
    static CollectorRoomLayout activeLayout;

    [MenuItem("Tools/Tiny Riders/Collector Shop/Apply Crowded Room")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != CompactCollectionBuilder.ScenePath) throw new InvalidOperationException("Open compact Level first.");
        var session = Object.FindFirstObjectByType<ThemedCollectionSession>();
        var shelves = Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None);
        if(session == null || shelves.Length != 80) throw new InvalidOperationException("Expected 80 compact shelves.");
        Directory.CreateDirectory(Evidence); Directory.CreateDirectory(Root);
        EditorSceneManager.SaveScene(scene,Evidence+"/Level_before_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity",true);
        var candidate = Object.Instantiate(session.roomLayout);
        Configure(candidate);
        CollectorRoomBuilder.Pile pile;
        try { pile = Bake(candidate,session.catalog,session.seed); Validate(candidate,pile,session.catalog); }
        catch { Object.DestroyImmediate(candidate); throw; }
        var layout = AssetDatabase.LoadAssetAtPath<CollectorRoomLayout>(LayoutPath);
        if(layout == null) { layout=candidate; AssetDatabase.CreateAsset(layout,LayoutPath); }
        else { EditorUtility.CopySerialized(candidate,layout); Object.DestroyImmediate(candidate); }
        layout.name="CrowdedRoomLayout"; activeLayout=layout;
        File.WriteAllText(Root+"/CrowdedPile.json",JsonUtility.ToJson(pile,true));
        AssetDatabase.ImportAsset(Root+"/CrowdedPile.json");
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/00_Project/04_ِArt/UI/Fonts/Fredoka-SemiBold SDF.asset");
        var generated=scene.GetRootGameObjects().Single(g=>g.name=="Car Store Generated");
        foreach(var name in new[]{"Store Architecture","Collector Shop Interior","Directory and Aisle Signs","Crowded Shop Interior"})
        {var old=generated.transform.Find(name);if(old!=null)Object.DestroyImmediate(old.gameObject);}
        var interior=new GameObject("Crowded Shop Interior").transform;interior.SetParent(generated.transform,false);
        Architecture(interior,layout,session);
        foreach(var pose in layout.shelves)
        {
            var shelf=shelves.Single(s=>s.SaveId==pose.saveId);
            shelf.transform.SetPositionAndRotation(layout.origin+pose.position,Quaternion.Euler(0,pose.yaw,0));
            var section=layout.sections[pose.section];
            var header=shelf.transform.Find("Theme Header")?.GetComponent<TextMeshPro>();
            if(header!=null)
            {
                header.text=session.ThemeName(shelf.GetComponent<ThemeShelfBinding>().themeId);
                header.color=Ink;header.fontSize=1.05f;header.enableAutoSizing=true;header.fontSizeMin=.65f;header.fontSizeMax=1.05f;
                header.transform.localPosition=new Vector3(0,1.59f,.145f);header.rectTransform.sizeDelta=new Vector2(ThemedShelfArtBuilder.HasThemedArt(shelf) ? .88f : 1.08f,.17f);
            }
            var blocker=shelf.transform.Find("Player Collision");
            if(blocker==null){blocker=new GameObject("Player Collision").transform;blocker.SetParent(shelf.transform,false);}
            blocker.gameObject.layer=0;
            var solid=blocker.GetComponent<BoxCollider>()??blocker.gameObject.AddComponent<BoxCollider>();
            solid.center=new Vector3(0,.8f,0);solid.size=new Vector3(1.704f,1.6f,.38f);
            var batch=shelf.transform.Find("Batched Shelf Art");
            if(batch!=null&&!ThemedShelfArtBuilder.HasThemedArt(shelf))foreach(var r in batch.GetComponentsInChildren<MeshRenderer>())
                if(r.sharedMaterial!=null&&(r.sharedMaterial.name.Contains("SidePanels")||r.sharedMaterial.name.StartsWith("Section Paint")))
                {var paint=Mat("Section Paint "+pose.section,section.color);r.sharedMaterial=paint;}
            EditorUtility.SetDirty(shelf);
        }
        session.roomLayout=layout;session.pileLayout=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/CrowdedPile.json");
        session.pileOrigin.position=layout.origin;session.boundedPile=false;session.spawnEntireCollectionAtStart=true;session.visiblePileLimit=1200;
        var player=Object.FindFirstObjectByType<Player>();player.transform.SetPositionAndRotation(layout.spawn,Quaternion.identity);
        PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);
        var upgrade=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(t=>t.name=="Upgrade Box");
        upgrade.position=layout.origin+new Vector3(-.9f,0,-11.9f);upgrade.rotation=Quaternion.identity;
        PrefabUtility.RecordPrefabInstancePropertyModifications(upgrade);
        EditorUtility.SetDirty(session);EditorUtility.SetDirty(layout);AssetDatabase.SaveAssets();
        ExactThemeShelfBuilder.ValidateScene();
        ThemedShelfSignClearance.Apply();
        ShopInteriorPolishBuilder.Apply(interior,layout);
        ShowroomLightingBuilder.Apply(scene);
        ShowroomLightingBuilder.BakeReflection(scene);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Audit();
    }

    public static void Configure(CollectorRoomLayout l)
    {
        l.revision=4;l.roomSize=new Vector2(24,26);l.ceilingHeight=3.2f;l.centralAisle=0;l.minimumAisle=1.2f;
        l.walkThroughLooseCars=false;l.spawn=l.origin+new Vector3(0,.05f,-10.9f);l.spawnPitch=12;
        foreach(var section in l.sections)section.sortingAreas=Array.Empty<Bounds>();
        var themes=l.sections.SelectMany((s,i)=>s.themes.Select(t=>new {theme=t,section=i})).ToArray();
        var positions=new List<(Vector3 p,float yaw)>();
        // Clockwise around the walls, beginning beside the entrance.
        for(int i=0;i<6;i++)positions.Add((new Vector3(-1.9f-i*1.72f,0,-12.7f),0));
        for(int i=0;i<12;i++)positions.Add((new Vector3(-11.7f,0,-9.46f+i*1.72f),90));
        for(int i=0;i<12;i++)positions.Add((new Vector3(-9.46f+i*1.72f,0,12.7f),180));
        for(int i=0;i<12;i++)positions.Add((new Vector3(11.7f,0,9.46f-i*1.72f),-90));
        for(int i=0;i<6;i++)positions.Add((new Vector3(10.5f-i*1.72f,0,-12.7f),0));
        foreach(float x in new[]{-4f,4f})foreach(int side in new[]{-1,1})
            for(int i=0;i<8;i++)positions.Add((new Vector3(x+side*.25f,0,-6.02f+i*1.72f),side<0?-90:90));
        l.shelves=positions.Select((p,i)=>new CollectorRoomLayout.ShelfPose{saveId="COMPACT-"+themes[i/2].theme+"-S"+(i%2+1).ToString("D3"),section=themes[i/2].section,position=p.p,yaw=p.yaw}).ToArray();
        l.pileRegions=new[]{new Bounds(new Vector3(0,.55f,0),new Vector3(21.2f,1.1f,23.2f))};
        l.pileExclusions=new[]{
            new Bounds(new Vector3(-4,1,0),new Vector3(2.24f,2,13.9f)),
            new Bounds(new Vector3(4,1,0),new Vector3(2.24f,2,13.9f)),
            new Bounds(new Vector3(0,1,-11.3f),new Vector3(3.3f,2,3.4f))};
        for(int i=0;i<8;i++)l.sections[i].center=l.shelves.First(s=>s.section==i).position;
    }

    public static Bounds TransformedBounds(Bounds b,Vector3 p,Quaternion q)
    {
        return LooseCarPhysics.WorldBounds(b,p,q);
    }
    static bool Inside(CollectorRoomLayout l,Bounds b)
    {
        return l.pileRegions.Any(a=>b.min.x>=a.min.x&&b.max.x<=a.max.x&&b.min.z>=a.min.z&&b.max.z<=a.max.z)&&
            !l.pileExclusions.Any(a=>a.min.x<b.max.x&&a.max.x>b.min.x&&a.min.z<b.max.z&&a.max.z>b.min.z);
    }

    public static CollectorRoomBuilder.Pile Bake(CollectorRoomLayout l,ThemedCarCatalog catalog,int seed,string evidence=Evidence,float fixtureClearance=0)
    {
        // A visual body can extend slightly beyond its rounded physics capsule at fixture corners.
        var bakeExclusions=l.pileExclusions.Select(b=>{b.Expand(new Vector3(fixtureClearance*2,0,fixtureClearance*2));return b;}).ToArray();
        var definitions=catalog.entries.Where(e=>e.enabled).ToDictionary(e=>e.collectibleId);
        var ids=ThemedPileSequence.InstanceIds(definitions.Values,2,seed).OrderBy(id=>ThemedPileSequence.Rank(id+":crowded",seed)).ThenBy(id=>id,StringComparer.Ordinal).ToArray();
        var art=definitions.ToDictionary(p=>p.Key,p=>CollectorRoomBuilder.ArtBounds(p.Value.sortingItem.WorldItem.gameObject));
        var random=new System.Random(seed);var placements=new List<ThemedInstanceRecord>();var occupied=new List<Bounds>();
        float Random(float min,float max)=>min+(float)random.NextDouble()*(max-min);
        // Geometry-aware seed poses are deliberately irregular; no rows or theme-local distribution.
        foreach(string id in ids.OrderByDescending(id=>art[id.Split(':')[1]].size.y * art[id.Split(':')[1]].size.x * art[id.Split(':')[1]].size.z))
        {
            string collectible=id.Split(':')[1];var b=art[collectible];float best=float.MaxValue;Vector3 bestP=default;Quaternion bestQ=default;Bounds bestBox=default;
            for(int attempt=0;attempt<700;attempt++)
            {
                float roll=random.NextDouble()<.08?90:Random(-23,23);
                var q=Quaternion.Euler(Random(-15,15),Random(0,360),roll);
                var box=TransformedBounds(b,Vector3.zero,q);var a=l.pileRegions[placements.Count % l.pileRegions.Length];
                var p=new Vector3(Random(a.min.x+.35f,a.max.x-.35f),.03f-box.min.y,Random(a.min.z+.4f,a.max.z-.4f));
                box=TransformedBounds(b,p,q);if(!Inside(l,box)||bakeExclusions.Any(a=>a.min.x<box.max.x&&a.max.x>box.min.x&&a.min.z<box.max.z&&a.max.z>box.min.z))continue;
                float support=0;
                foreach(var other in occupied)if(box.min.x<other.max.x+.015f&&box.max.x>other.min.x-.015f&&box.min.z<other.max.z+.015f&&box.max.z>other.min.z-.015f) support=Mathf.Max(support,other.max.y);
                p.y+=support;box.center+=Vector3.up*support;if(box.max.y>1.07f)continue;
                // Prefer floor placements, but permit shallow local stacks when floor packing fills.
                float score=support+Random(0,.12f);
                if(score<best){best=score;bestP=p;bestQ=q;bestBox=box;if(best<.025f)break;}
            }
            if(best==float.MaxValue)throw new InvalidOperationException("Cannot safely pack car "+placements.Count+" / "+id);
            placements.Add(new ThemedInstanceRecord{id=id,collectibleId=collectible,state="world",position=bestP,euler=bestQ.eulerAngles});occupied.Add(bestBox);
        }
        var original=SceneManager.GetActiveScene();var bakeScene=EditorSceneManager.NewPreviewScene();
        var physics=bakeScene.GetPhysicsScene();var bodies=new List<Rigidbody>();var colliders=new List<CapsuleCollider>();
        var material=new PhysicsMaterial("Car grip"){dynamicFriction=.35f,staticFriction=.4f,bounciness=0};
        try
        {
            BoxCollider Proxy(string name,Bounds b)
            {
                var go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,bakeScene);var col=go.AddComponent<BoxCollider>();col.center=b.center;col.size=b.size;col.sharedMaterial=material;col.contactOffset=.002f;return col;
            }
            Proxy("Floor",new Bounds(new Vector3(0,-.2f,0),new Vector3(l.roomSize.x,.4f,l.roomSize.y)));
            foreach(var region in l.pileRegions)
            foreach(float side in new[]{-1f,1f})
            {
                Proxy("Bake X boundary",new Bounds(new Vector3(region.center.x+side*(region.extents.x+.1f),1,region.center.z),new Vector3(.2f,2,region.size.z+.4f)));
                Proxy("Bake Z boundary",new Bounds(new Vector3(region.center.x,1,region.center.z+side*(region.extents.z+.1f)),new Vector3(region.size.x+.4f,2,.2f)));
            }
            foreach(var exclusion in bakeExclusions)Proxy("Protected fixture approach",exclusion);
            for(int i=0;i<placements.Count;i++)
            {
                var r=placements[i];var go=new GameObject(r.id);SceneManager.MoveGameObjectToScene(go,bakeScene);
                var col=go.AddComponent<CapsuleCollider>();LooseCarPhysics.Fit(col,art[r.collectibleId]);col.sharedMaterial=material;col.transform.SetPositionAndRotation(r.position,Quaternion.Euler(r.euler));
                var body=col.gameObject.AddComponent<Rigidbody>();LooseCarPhysics.Configure(body);
                bodies.Add(body);colliders.Add(col);
            }
            int steps=0;for(;steps<1200;steps++){physics.Simulate(.02f);if(steps>180&&bodies.All(b=>b.IsSleeping()))break;}
            int moving=bodies.Count(b=>!b.IsSleeping()&&(b.linearVelocity.sqrMagnitude>.0025f||b.angularVelocity.sqrMagnitude>.01f));
            if(moving>0)throw new InvalidOperationException("Physics bake has unsettled cars: "+moving);
            float penetration=0;
            for(int i=0;i<colliders.Count;i++)
            {
                var col=colliders[i];var body=bodies[i];placements[i].position=body.position;placements[i].euler=body.rotation.eulerAngles;
                for(int j=0;j<i;j++)if(col.bounds.Intersects(colliders[j].bounds)&&Physics.ComputePenetration(col,body.position,body.rotation,colliders[j],bodies[j].position,bodies[j].rotation,out _,out float depth))penetration=Mathf.Max(penetration,depth);
            }
            if(penetration>.012f)throw new InvalidOperationException("Bake penetration exceeds contact tolerance: "+penetration);
            Directory.CreateDirectory(evidence);
            File.WriteAllText(evidence+"/physics_bake.json",JsonUtility.ToJson(new BakeReport{cars=placements.Count,steps=steps,unsettled=moving,maxContactPenetration=penetration},true));
        }
        finally{SceneManager.SetActiveScene(original);EditorSceneManager.ClosePreviewScene(bakeScene);Object.DestroyImmediate(material);}
        return new CollectorRoomBuilder.Pile{revision=l.revision,placements=placements.ToArray()};
    }
    [Serializable] class BakeReport {public int cars,steps,unsettled;public float maxContactPenetration;}

    public static void Validate(CollectorRoomLayout l,CollectorRoomBuilder.Pile pile,ThemedCarCatalog catalog,string evidence=Evidence)
    {
        if(l.shelves.Length!=80||l.sections.SelectMany(s=>s.themes).Distinct().Count()!=40||pile.placements.Length!=1200||pile.placements.Select(r=>r.id).Distinct().Count()!=1200||pile.placements.GroupBy(r=>r.collectibleId).Count()!=600||pile.placements.GroupBy(r=>r.collectibleId).Any(g=>g.Count()!=2))throw new InvalidOperationException("Collection identity/capacity mismatch");
        var art=catalog.entries.Where(e=>e.enabled).ToDictionary(e=>e.collectibleId,e=>CollectorRoomBuilder.ArtBounds(e.sortingItem.WorldItem.gameObject));
        foreach(var r in pile.placements)
        {
            var b=TransformedBounds(art[r.collectibleId],r.position,Quaternion.Euler(r.euler));
            // Contact slop is at most millimetres; use a tiny inset for boundary comparisons.
            var inset=b;inset.Expand(-.016f);
            if(!Inside(l,inset)||b.min.y<-.012f||b.max.y>1.1f)throw new InvalidOperationException("Invalid settled pile pose "+r.id+" "+b);
        }
        foreach(var pair in l.shelves.GroupBy(p=>p.saveId.Substring(0,p.saveId.Length-5)))
        {var poses=pair.ToArray();if(poses.Length!=2||Mathf.Abs(Mathf.DeltaAngle(poses[0].yaw,poses[1].yaw))>.1f||Vector3.Distance(poses[0].position,poses[1].position)>1.73f)throw new InvalidOperationException("Theme pair must share an adjacent display face");}
        ValidateContacts(pile,art,evidence);
    }

    // Validate persisted poses too: a manually edited pile must not bypass the bake checks.
    static void ValidateContacts(CollectorRoomBuilder.Pile pile,Dictionary<string,Bounds> art,string evidence)
    {
        var preview=EditorSceneManager.NewPreviewScene();var colliders=new List<CapsuleCollider>();
        try
        {
            var supported=new HashSet<int>();var contacts=new List<int>[pile.placements.Length];
            for(int i=0;i<pile.placements.Length;i++)
            {
                var r=pile.placements[i];var go=new GameObject("Contact validation");SceneManager.MoveGameObjectToScene(go,preview);
                go.transform.SetPositionAndRotation(r.position,Quaternion.Euler(r.euler));
                var c=go.AddComponent<CapsuleCollider>();LooseCarPhysics.Fit(c,art[r.collectibleId]);colliders.Add(c);contacts[i]=new List<int>();
                if(TransformedBounds(art[r.collectibleId],r.position,go.transform.rotation).min.y<=.02f)supported.Add(i);
            }
            float maxPenetration=0;
            for(int i=0;i<colliders.Count;i++)for(int j=0;j<i;j++)
            {
                var a=colliders[i];var b=colliders[j];var broad=a.bounds;broad.Expand(.04f);
                if(!broad.Intersects(b.bounds))continue;
                if(Physics.ComputePenetration(a,a.transform.position,a.transform.rotation,b,b.transform.position,b.transform.rotation,out _,out float depth))maxPenetration=Mathf.Max(maxPenetration,depth);
                // A 2 cm envelope accommodates PhysX resting contact separation, not arbitrary floating cars.
                var radius=a.radius;var height=a.height;a.radius=radius+.02f;a.height=height+.04f;
                bool touching=Physics.ComputePenetration(a,a.transform.position,a.transform.rotation,b,b.transform.position,b.transform.rotation,out _,out _);a.radius=radius;a.height=height;
                if(touching){contacts[i].Add(j);contacts[j].Add(i);}
            }
            var queue=new Queue<int>(supported);while(queue.Count>0)foreach(int neighbour in contacts[queue.Dequeue()])if(supported.Add(neighbour))queue.Enqueue(neighbour);
            if(maxPenetration>.012f||supported.Count!=pile.placements.Length)throw new InvalidOperationException("Invalid pile contacts: penetration="+maxPenetration+", unsupported="+(pile.placements.Length-supported.Count));
            Directory.CreateDirectory(evidence);
            File.WriteAllText(evidence+"/contact_validation.json",JsonUtility.ToJson(new ContactReport{supported=supported.Count,maxPenetration=maxPenetration},true));
        }
        finally{EditorSceneManager.ClosePreviewScene(preview);}
    }
    [Serializable] class ContactReport {public int supported;public float maxPenetration;}

    static Material Mat(string name,Color color)
    {
        string path=Root+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
        mat.color=color;mat.SetFloat("_Smoothness",.12f);EditorUtility.SetDirty(mat);return mat;
    }
    static GameObject Box(Transform root,string name,Vector3 p,Vector3 size,Material mat,bool solid=false)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root,false);go.transform.position=activeLayout.origin+p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;go.isStatic=true;
        if(!solid){Object.DestroyImmediate(go.GetComponent<Collider>());go.layer=2;}return go;
    }
    static void Label(Transform root,string name,string text,Vector3 p,float yaw,Vector2 size,float fontSize,Color color)
    {
        var go=new GameObject(name);go.layer=2;go.transform.SetParent(root,false);go.transform.SetPositionAndRotation(activeLayout.origin+p,Quaternion.Euler(0,yaw,0));
        var label=go.AddComponent<TextMeshPro>();label.font=font;label.text=text;label.color=color;label.fontSize=fontSize;label.alignment=TextAlignmentOptions.Center;label.rectTransform.sizeDelta=size;
    }
    static void Architecture(Transform root,CollectorRoomLayout l,ThemedCollectionSession session)
    {
        float w=l.roomSize.x,d=l.roomSize.y,h=l.ceilingHeight;
        var floor=Mat("Warm Gray",new Color(.57f,.59f,.57f));var cream=Mat("Ivory",new Color(.91f,.88f,.79f));var wood=Mat("Pale Oak",new Color(.69f,.51f,.32f));var teal=Mat("Teal",new Color(.08f,.34f,.36f));var white=Mat("Soft White",new Color(.97f,.96f,.91f));
        Box(root,"Store Floor",new Vector3(0,-.2f,0),new Vector3(w+.6f,.4f,d+.6f),floor,true);
        RacingFloorBuilder.Apply(root, l);
        foreach(float side in new[]{-1f,1f})
        {
            Box(root,"Side Wall",new Vector3(side*(w*.5f+.15f),h*.5f,0),new Vector3(.3f,h,d+.6f),cream,true);
            Box(root,"End Wall",new Vector3(0,h*.5f,side*(d*.5f+.15f)),new Vector3(w,h,.3f),cream,true);
            Box(root,"Side Teal Rail",new Vector3(side*(w*.5f-.025f),h-.35f,0),new Vector3(.05f,.1f,d),teal);
            Box(root,"End Teal Rail",new Vector3(0,h-.35f,side*(d*.5f-.025f)),new Vector3(w,.1f,.05f),teal);
        }
        var ceiling=Mat("Ceiling",new Color(.85f,.85f,.8f));ceiling.shader=Shader.Find("Universal Render Pipeline/Unlit");
        Box(root,"Store Ceiling",new Vector3(0,h+.1f,0),new Vector3(w+.6f,.2f,d+.6f),ceiling,true);
        foreach(float x in new[]{-4f,4f})Box(root,"Island Oak Spine",new Vector3(x,.8f,0),new Vector3(.1f,1.6f,13.75f),wood);
        // Small category markers are attached to the rack ends, without hanging aisle banners.
        for(int i=0;i<8;i++)
        {
            var first=l.shelves.First(s=>s.section==i);var q=Quaternion.Euler(0,first.yaw,0);var p=first.position+q*new Vector3(.86f,2.05f,.15f);
            var sign=Box(root,"Category "+(i+1),p,new Vector3(2.9f,.42f,.055f),Mat("Category "+i,l.sections[i].color));sign.transform.rotation=q;
            Label(root,"Category Name "+i,(i+1).ToString("00")+" / "+l.sections[i].title,p+q*new Vector3(0,0,.04f),first.yaw+180,new Vector2(2.75f,.34f),1.9f,Color.white);
        }
        foreach(float x in new[]{-7f,0f,7f})foreach(float z in new[]{-7f,7f})
        {
            Box(root,"Ceiling Diffuser",new Vector3(x,h-.08f,z),new Vector3(3,.05f,.8f),white);
            var lamp=new GameObject("Soft Shop Light");lamp.transform.SetParent(root,false);lamp.transform.position=l.origin+new Vector3(x,h-.25f,z);
            var light=lamp.AddComponent<Light>();light.type=LightType.Point;light.intensity=1;light.range=12;light.color=new Color(1,.95f,.85f);light.shadows=LightShadows.None;
        }
        Box(root,"Entry Door",new Vector3(0,1.2f,-d*.5f+.05f),new Vector3(2,2.4f,.06f),teal);
        Box(root,"Collector Counter",new Vector3(.55f,.45f,-d*.5f+.5f),new Vector3(.95f,.9f,.7f),teal,true);
        Box(root,"Counter Oak Top",new Vector3(.55f,.94f,-d*.5f+.5f),new Vector3(1.05f,.08f,.8f),wood);
        Box(root,"Boxed Stock",new Vector3(.55f,1.14f,-d*.5f+.5f),new Vector3(.5f,.32f,.4f),cream);
        Box(root,"Directory Board",new Vector3(-4.7f,2.3f,-d*.5f+.06f),new Vector3(6.4f,1.05f,.06f),white);
        string directory="COLLECTOR DIRECTORY\n"+string.Join("\n",Enumerable.Range(0,4).Select(i=>(i*2+1).ToString("00")+" "+l.sections[i*2].title+"   |   "+(i*2+2).ToString("00")+" "+l.sections[i*2+1].title));
        Label(root,"Store Directory",directory,new Vector3(-4.7f,2.3f,-d*.5f+.105f),180,new Vector2(6.2f,1),1.9f,Ink);
        Label(root,"Shop Brand","TOY CAR SORT / COLLECTOR SHOP",new Vector3(0,2.65f,d*.5f-.07f),0,new Vector2(10,.55f),3.3f,Ink);
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.85f,.88f,.9f);RenderSettings.ambientEquatorColor=new Color(.65f,.68f,.66f);RenderSettings.ambientGroundColor=new Color(.4f,.39f,.35f);RenderSettings.ambientIntensity=1;RenderSettings.fog=false;
    }

    public static void Audit()
    {
        var s=Object.FindFirstObjectByType<ThemedCollectionSession>();var l=s.roomLayout;var pile=JsonUtility.FromJson<CollectorRoomBuilder.Pile>(s.pileLayout.text);Validate(l,pile,s.catalog);
        var shelves=Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None);
        ThemedShelfCapacity.Validate(s.catalog,s.copiesPerDesign,shelves);
        if(shelves.Length!=80||shelves.Any(v=>v.SpotCount!=ExactThemeShelfBuilder.SlotsPerShelf))throw new InvalidOperationException("Shelf capacity mismatch");
        foreach(var pose in l.shelves)if(Vector3.Distance(shelves.Single(v=>v.SaveId==pose.saveId).transform.position,l.origin+pose.position)>.01f)throw new InvalidOperationException("Shelf pose mismatch");
        Directory.CreateDirectory(Evidence);
        File.WriteAllText(Evidence+"/layout_audit.json",JsonUtility.ToJson(new AuditReport{passed=true,designs=600,cars=1200,shelves=80,slots=shelves.Sum(v=>v.SpotCount),perimeterShelves=48,islandShelves=32,roomSize=l.roomSize,ceilingHeight=l.ceilingHeight,revision=l.revision},true));
    }
    [Serializable] class AuditReport {public bool passed;public int designs,cars,shelves,slots,perimeterShelves,islandShelves,revision;public Vector2 roomSize;public float ceilingHeight;}
}
