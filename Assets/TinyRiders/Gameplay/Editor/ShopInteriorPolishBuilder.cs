using System;
using System.Collections.Generic;
using System.Linq;
using TinyRiders;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Reusable, collision-free toy-town wall and ceiling finishes. Editor only.</summary>
public static class ShopInteriorPolishBuilder
{
    public const string ArtRoot = "Assets/TinyRiders/Art/ShopInterior";
    public const string WallRoot = "Shop Wall Finish";
    public const string CeilingRoot = "Shop Ceiling Finish";

    [MenuItem("Tools/Tiny Riders/Collector Shop/Apply Shop Interior Polish")]
    public static void ApplyToCurrentRoom()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode first.");
        var scene=SceneManager.GetActiveScene();
        if(scene.path!=CompactCollectionBuilder.ScenePath)throw new InvalidOperationException("Open the collector Level scene first.");
        var session=Object.FindFirstObjectByType<ThemedCollectionSession>();
        var interior=scene.GetRootGameObjects().Single(g=>g.name=="Car Store Generated").transform.Find("Crowded Shop Interior");
        if(session==null||interior==null)throw new InvalidOperationException("The crowded collector room is required.");
        Apply(interior,session.roomLayout);
        ShowroomLightingBuilder.Apply(scene);
        ShowroomLightingBuilder.BakeReflection(scene);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }

    public static void Apply(Transform interior,CollectorRoomLayout layout)
    {
        if(layout==null||Vector2.Distance(layout.roomSize,new Vector2(24,26))>.01f||Mathf.Abs(layout.ceilingHeight-3.2f)>.001f)
            throw new InvalidOperationException("These finishes require the 24 x 26 x 3.2 m collector shop.");
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit mode only.");
        var oldDiffusers=interior.Cast<Transform>().Where(t=>t.name=="Ceiling Diffuser").ToArray();
        if(oldDiffusers.Length!=6||interior.Find("Store Ceiling")==null)throw new InvalidOperationException("Expected the original ceiling and six fixture locations.");
        EnsureFolder(ArtRoot+"/Materials");EnsureFolder(ArtRoot+"/Meshes");
        var atlas=Texture("Category Murals",false,false,true);
        var layoutAsset=AssetDatabase.LoadAssetAtPath<TextAsset>(ArtRoot+"/Textures/Rich Mural Layout.json");
        if(layoutAsset==null)throw new InvalidOperationException("Build rich interior artwork and its atlas layout first.");
        var artwork=JsonUtility.FromJson<MuralAtlas>(layoutAsset.text);
        if(artwork?.categories==null||artwork.categories.Length!=8)throw new InvalidOperationException("Expected eight rich category panoramas.");
        var ceilingAtlas=Texture("Racing Ceiling Graphics",false,false,true);
        var wallBase=Texture("Wall Plaster BaseColor",false,false);
        var wallNormal=Texture("Wall Plaster Normal",true,false);
        var wallSmooth=Texture("Wall Plaster Smoothness",false,true);
        var ceilingBase=Texture("Ceiling Acoustic BaseColor",false,false);
        var ceilingNormal=Texture("Ceiling Acoustic Normal",true,false);
        var ceilingSmooth=Texture("Ceiling Acoustic Smoothness",false,true);
        var plaster=Material("Warm Mineral Plaster","C8C8BF",.19f);Surface(plaster,wallBase,wallNormal,wallSmooth);
        Emission(plaster,wallBase,new Color(.12f,.12f,.10f));
        var teal=Material("Teal Vertical Paneling","539597",.2f);Surface(teal,wallBase,wallNormal,wallSmooth);
        var mural=Material("Illustrated Category Murals","CCCCBF",.18f);mural.SetTexture("_BaseMap",atlas);
        Emission(mural,atlas,new Color(.16f,.16f,.14f));
        var panel=Material("Warm White Acoustic Panels","2E2E2E",.16f);Surface(panel,ceilingBase,ceilingNormal,ceilingSmooth);
        Emission(panel,ceilingBase,new Color(.73f,.73f,.68f));
        var racing=Material("Racing Ceiling Print","242424",.16f);racing.SetTexture("_BaseMap",ceilingAtlas);
        Emission(racing,ceilingAtlas,new Color(.90f,.90f,.86f));
        var housing=Material("Ivory Fixture Housings","AAB4AA",.2f);
        var diffuser=Material("Even Opal Diffusers","F3F0DE",.05f);diffuser.shader=Shader.Find("Universal Render Pipeline/Unlit");
        var glow=GlowMaterial();
        var walls=Root(interior,WallRoot,layout.origin);var ceiling=Root(interior,CeilingRoot,layout.origin);
        // Remove only the previous builder's superseded groups, never unrelated room content.
        foreach(var name in new[]{"Sage Wall Panels","Teal Wall Joinery"})RemoveOwnedChild(walls,name);
        RemoveOwnedChild(ceiling,"Teal Ceiling Frame");
        var plasterMesh=new Geometry();var tealMesh=new Geometry();var muralMesh=new Geometry();
        Wall(plasterMesh,tealMesh,muralMesh,new Vector3(0,0,13),Vector3.right,Vector3.back,24,0,artwork);
        Wall(plasterMesh,tealMesh,muralMesh,new Vector3(0,0,-13),Vector3.left,Vector3.forward,24,1,artwork);
        Wall(plasterMesh,tealMesh,muralMesh,new Vector3(-12,0,0),Vector3.forward,Vector3.right,26,2,artwork);
        Wall(plasterMesh,tealMesh,muralMesh,new Vector3(12,0,0),Vector3.back,Vector3.left,26,3,artwork);
        foreach(var sign in interior.Cast<Transform>().Where(t=>t.name.StartsWith("Category ")&&t.GetComponent<MeshRenderer>()!=null))
        {
            var center=sign.position-layout.origin;var right=sign.right;var up=sign.up;var normal=sign.forward;
            WallRing(muralMesh,center+normal*.030f,right,up,normal,3.10f,.59f,.11f,2.90f,.42f,.035f,Palette(2));
            WallRing(muralMesh,center+normal*.031f,right,up,normal,3.16f,.65f,.13f,3.10f,.59f,.11f,Palette(7));
        }
        // Full-room builds create this legacy label; remove it on every application as requested.
        var brand=interior.Find("Shop Brand");
        if(brand!=null)Undo.DestroyObjectImmediate(brand.gameObject);
        Publish(walls,"Mineral Plaster",plasterMesh,plaster);
        Publish(walls,"Teal Vertical Panels",tealMesh,teal);
        Publish(walls,"Skyline Mural",muralMesh,mural);

        var panels=new Geometry();var roads=new Geometry();var cove=new Geometry();var fixtures=new Geometry();var lenses=new Geometry();
        // Large quiet ceiling fields replace the former visually dominant 2 m grid.
        // UVs remain meters/2, retaining the physical scale of the acoustic surface set.
        panels.BevelBox(new Vector3(0,3.188f,0),new Vector3(24,.024f,26),.006f);
        Circuit(roads,cove,10.9f,11.85f,2.05f,1.20f,false);
        Circuit(roads,cove,4.75f,9.9f,2.15f,1.10f,true);
        // Retain the teal inset and warm outside edge, with no car, lettering or badge border.
        CeilingFill(roads,3.10f,2.20f,.65f,3.153f,CeilingPalette(1));
        var badge=Rounded(3.13f,2.23f,.68f);Ribbon(cove,badge,.15f,3.150f,null);
        // Cove line follows the room edge and stays above the directory top (3.09 m).
        Ribbon(cove,Rounded(11.91f,12.91f,.26f),.14f,3.132f,null);
        foreach(var old in oldDiffusers)
        {
            var p=old.position-layout.origin;
            fixtures.BevelBox(new Vector3(p.x,3.151f,p.z),new Vector3(3.12f,.054f,.92f),.016f);
            lenses.Quad(new Vector3(p.x-1.48f,3.121f,p.z-.38f),new Vector3(p.x+1.48f,3.121f,p.z-.38f),
                new Vector3(p.x+1.48f,3.121f,p.z+.38f),new Vector3(p.x-1.48f,3.121f,p.z+.38f),Vector3.down);
            Undo.RecordObject(old.GetComponent<Renderer>(),"Refine ceiling fixture");old.GetComponent<Renderer>().enabled=false;
        }
        Publish(ceiling,"Acoustic Ceiling Panels",panels,panel);
        Publish(ceiling,"Racing Ceiling Ribbon",roads,racing);
        Publish(ceiling,"Warm Cove Edges",cove,glow);
        Publish(ceiling,"Slim Fixture Housings",fixtures,housing);
        Publish(ceiling,"Opal Diffusers",lenses,diffuser);
        foreach(var t in interior.Cast<Transform>())
        {
            if(t.name=="Side Teal Rail"||t.name=="End Teal Rail") {Undo.RecordObject(t.GetComponent<Renderer>(),"Replace wall rails");t.GetComponent<Renderer>().enabled=false;}
            if(t.name=="Store Ceiling") {var r=t.GetComponent<Renderer>();Undo.RecordObject(r,"Retain ceiling daylight");r.shadowCastingMode=ShadowCastingMode.Off;}
        }
        foreach(string guid in AssetDatabase.FindAssets("",new[]{ArtRoot}))
            AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid)));
    }
    static void Emission(Material material,Texture2D map,Color color)
    {
        material.EnableKeyword("_EMISSION");material.SetTexture("_EmissionMap",map);material.SetColor("_EmissionColor",color);
        // Required by URP keyword validation; meshes are not ContributeGI, so this is local fill only.
        material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;
    }
    static void RemoveOwnedChild(Transform root,string name)
    {var t=root.Find(name);if(t!=null)Undo.DestroyObjectImmediate(t.gameObject);}
    static Vector2 Palette(int i)=>new Vector2((104+i*128)/8192f,48/8192f);
    static Vector2 CeilingPalette(int i)=>new Vector2((75+i*120)/1000f,1-462/500f);
    static void SolidQuad(Geometry g,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normal,Vector2 uv)
    {g.Quad(a,b,c,d,normal,uv,uv,uv,uv);}
    static void Wall(Geometry plaster,Geometry teal,Geometry art,Vector3 center,Vector3 right,Vector3 inward,float length,int wall,MuralAtlas artwork)
    {
        Vector3 P(float x,float y,float depth)=>center+right*x+Vector3.up*y+inward*depth;
        plaster.Quad(P(-length/2,0,.002f),P(length/2,0,.002f),P(length/2,3.2f,.002f),P(-length/2,3.2f,.002f),inward,
            new Vector2(-length/4,0),new Vector2(length/4,0),new Vector2(length/4,1.6f),new Vector2(-length/4,1.6f));
        var q=Quaternion.LookRotation(inward,Vector3.up);
        // Dark teal backing makes the fine board joints recessed rather than bright cracks.
        teal.Quad(P(-length/2,0,.004f),P(length/2,0,.004f),P(length/2,2.02f,.004f),P(-length/2,2.02f,.004f),inward);
        for(float x=-length/2+.012f;x<length/2-.02f;x+=.4f)
        {
            float end=Mathf.Min(x+.394f,length/2-.012f);
            teal.BevelBox(P((x+end)/2,1.04f,.010f),new Vector3(end-x,1.96f,.016f),.003f,q);
        }
        foreach(float x in new[]{-length/2+.025f,length/2-.025f})
            teal.BevelBox(P(x,1.57f,.027f),new Vector3(.044f,3.10f,.05f),.006f,q);
        teal.BevelBox(P(0,.065f,.024f),new Vector3(length,.10f,.045f),.006f,q);
        SolidQuad(art,P(-length/2,2.025f,.024f),P(length/2,2.025f,.024f),P(length/2,2.06f,.024f),P(-length/2,2.06f,.024f),inward,Palette(3));
        SolidQuad(art,P(-length/2,3.106f,.008f),P(length/2,3.106f,.008f),P(length/2,3.125f,.008f),P(-length/2,3.125f,.008f),inward,Palette(3));
        // Offset joins Coast around its corner. Cosmic now fills the former rear brand reservation.
        var zones=new List<(float a,float b,int category,float offset)>();
        if(wall==0){zones.Add((-12,-6.88f,1,16.44f));zones.Add((-6.88f,12,2,0));}
        if(wall==1){zones.Add((8.1f,12,5,0));zones.Add((-6.65f,-1.3f,6,0));zones.Add((-12,-6.65f,7,0));}
        if(wall==2){zones.Add((-13,-3.44f,0,0));zones.Add((-3.44f,13,1,0));}
        if(wall==3){zones.Add((-13,6.88f,3,0));zones.Add((6.88f,13,4,0));}
        foreach(var zone in zones)
        {
            var category=artwork.categories.Single(c=>c.id==zone.category);
            float end=zone.offset+zone.b-zone.a;
            if(end>category.lengthMeters+.001f)throw new InvalidOperationException("Mural length mismatch: "+category.name);
            foreach(var tile in category.tiles)
            {
                float from=Mathf.Max(zone.offset,tile.startMeters),to=Mathf.Min(end,tile.endMeters);
                if(to<=from)continue;
                float a=zone.a+from-zone.offset,b=zone.a+to-zone.offset;
                float ua=Mathf.Lerp(tile.u0,tile.u1,(from-tile.startMeters)/(tile.endMeters-tile.startMeters));
                float ub=Mathf.Lerp(tile.u0,tile.u1,(to-tile.startMeters)/(tile.endMeters-tile.startMeters));
                art.Quad(P(a,2.06f,.006f),P(b,2.06f,.006f),P(b,3.105f,.006f),P(a,3.105f,.006f),inward,
                    new Vector2(ua,tile.v0),new Vector2(ub,tile.v0),new Vector2(ub,tile.v1),new Vector2(ua,tile.v1));
            }
            // Small shared-palette checker accents stay above the illustrated landmarks.
            for(int col=0;col<6;col++)for(int row=0;row<2;row++)
            {
                float x=zone.b-.48f+col*.06f,y=3.005f+row*.045f;
                SolidQuad(art,P(x,y,.009f),P(x+.06f,y,.009f),P(x+.06f,y+.045f,.009f),P(x,y+.045f,.009f),inward,Palette((col+row)%2==0?0:2));
            }
        }
    }
    [Serializable] sealed class MuralAtlas { public MuralCategory[] categories; }
    [Serializable] sealed class MuralCategory { public int id;public string name;public float lengthMeters;public MuralTile[] tiles; }
    [Serializable] sealed class MuralTile { public float startMeters,endMeters,u0,u1,v0,v1; }
    static void RacingFlag(Geometry g,Vector3 p,int direction)
    {
        Vector3 P(float x,float y)=>p+new Vector3(direction*x,y,0);
        SolidQuad(g,P(-.018f,0),P(.018f,0),P(.018f,.43f),P(-.018f,.43f),Vector3.back,Palette(0));
        for(int x=0;x<5;x++)for(int y=0;y<3;y++)
        {
            float ax=.035f+x*.085f,ay=.19f+y*.075f;
            SolidQuad(g,P(ax,ay),P(ax+.085f,ay),P(ax+.085f,ay+.075f),P(ax,ay+.075f),Vector3.back,Palette((x+y)%2==0?0:2));
        }
    }
    static List<Vector2> Rounded(float hx,float hy,float r)
    {
        var points=new List<Vector2>();r=Mathf.Min(r,Mathf.Min(hx,hy));
        foreach(var c in new[]{new Vector3(hx-r,hy-r,0),new Vector3(-hx+r,hy-r,90),new Vector3(-hx+r,-hy+r,180),new Vector3(hx-r,-hy+r,270)})
            for(int i=0;i<=24;i++){float a=(c.z+i*90/24)*Mathf.Deg2Rad;points.Add(new Vector2(c.x+Mathf.Cos(a)*r,c.y+Mathf.Sin(a)*r));}
        return points;
    }
    static void WallRing(Geometry g,Vector3 center,Vector3 right,Vector3 up,Vector3 normal,float w,float h,float r,float iw,float ih,float ir,Vector2 uv)
    {
        var outer=Rounded(w/2,h/2,r);var inner=Rounded(iw/2,ih/2,ir);
        for(int i=0;i<outer.Count;i++){int j=(i+1)%outer.Count;Vector3 P(Vector2 v)=>center+right*v.x+up*v.y;SolidQuad(g,P(outer[i]),P(outer[j]),P(inner[j]),P(inner[i]),normal,uv);}
    }
    static void CeilingFill(Geometry g,float x,float z,float r,float y,Vector2 uv)
    {
        var points=Rounded(x,z,r);
        for(int i=0;i<points.Count;i++){int j=(i+1)%points.Count;SolidQuad(g,new Vector3(0,y,0),new Vector3(points[i].x,y,points[i].y),new Vector3(points[j].x,y,points[j].y),new Vector3(0,y,0),Vector3.down,uv);}
    }
    static void CeilingImage(Geometry g,float x0,float z0,float x1,float z1,float y,Rect uv)
    {
        g.Quad(new Vector3(x0,y,z0),new Vector3(x1,y,z0),new Vector3(x1,y,z1),new Vector3(x0,y,z1),Vector3.down,
            new Vector2(uv.xMin,uv.yMax),new Vector2(uv.xMax,uv.yMax),new Vector2(uv.xMax,uv.yMin),new Vector2(uv.xMin,uv.yMin));
    }
    static void Circuit(Geometry g,Geometry glow,float x,float z,float radius,float width,bool coral)
    {
        var path=Rounded(x,z,radius);
        Ribbon(g,path,width+.20f,3.166f,CeilingPalette(1));
        Ribbon(g,path,width,3.161f,CeilingPalette(0));
        // Sample dash positions by distance, so corners maintain the same spacing as straights.
        float distance=0;
        for(int i=0;i<path.Count;i++)
        {
            var a=path[i];var b=path[(i+1)%path.Count];float length=Vector2.Distance(a,b);
            for(float d=Mathf.Ceil(distance/1.4f)*1.4f;d<distance+length;d+=1.4f)
            {
                float t=(d-distance)/length;var p=Vector2.Lerp(a,b,t);var tangent=(b-a).normalized;
                var end=p+tangent*.43f;var n=new Vector2(-tangent.y,tangent.x)*.055f;
                SolidQuad(g,new Vector3(p.x+n.x,3.157f,p.y+n.y),new Vector3(end.x+n.x,3.157f,end.y+n.y),new Vector3(end.x-n.x,3.157f,end.y-n.y),new Vector3(p.x-n.x,3.157f,p.y-n.y),Vector3.down,CeilingPalette(2));
            }
            distance+=length;
        }
        Ribbon(glow,Rounded(x+width*.5f,z+width*.5f,radius+width*.5f),.19f,3.155f,null);
        Ribbon(glow,Rounded(x-width*.5f,z-width*.5f,radius-width*.5f),.19f,3.155f,null);
        if(coral)Ribbon(g,Rounded(x+width*.5f+.15f,z+width*.5f+.15f,radius+width*.5f+.15f),.12f,3.159f,CeilingPalette(4));
    }
    static void Ribbon(Geometry g,List<Vector2> path,float width,float y,Vector2? solid)
    {
        for(int i=0;i<path.Count;i++)
        {
            int j=(i+1)%path.Count;
            Vector2 Normal(int k){var t=(path[(k+1)%path.Count]-path[(k+path.Count-1)%path.Count]).normalized;return new Vector2(-t.y,t.x)*width/2;}
            var a=path[i];var b=path[j];var na=Normal(i);var nb=Normal(j);
            Vector3 P(Vector2 p)=>new Vector3(p.x,y,p.y);
            if(solid.HasValue)SolidQuad(g,P(a+na),P(b+nb),P(b-nb),P(a-na),Vector3.down,solid.Value);
            else g.Quad(P(a+na),P(b+nb),P(b-nb),P(a-na),Vector3.down,new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1));
        }
    }
    static Material GlowMaterial()
    {
        const string path=ArtRoot+"/Textures/Warm Cove Glow.png";
        var t=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(t==null)throw new InvalidOperationException("Generate Warm Cove Glow first.");
        var i=(TextureImporter)AssetImporter.GetAtPath(path);
        if(i.alphaSource!=TextureImporterAlphaSource.FromInput||!i.alphaIsTransparency||!i.sRGBTexture||i.wrapMode!=TextureWrapMode.Clamp||!i.mipmapEnabled||i.anisoLevel!=8)
        {i.alphaSource=TextureImporterAlphaSource.FromInput;i.alphaIsTransparency=true;i.sRGBTexture=true;i.wrapMode=TextureWrapMode.Clamp;i.mipmapEnabled=true;i.anisoLevel=8;i.filterMode=FilterMode.Trilinear;i.textureCompression=TextureImporterCompression.CompressedHQ;i.SaveAndReimport();}
        var m=Material("Warm Cove Edge Light","FFFFFF",0);m.shader=Shader.Find("Universal Render Pipeline/Unlit");m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path));
        m.SetFloat("_Surface",1);m.SetFloat("_Blend",0);m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=(int)RenderQueue.Transparent;m.SetOverrideTag("RenderType","Transparent");return m;
    }
    static Transform Root(Transform parent,string name,Vector3 origin)
    {
        var root=parent.Find(name);if(root==null){root=new GameObject(name).transform;Undo.RegisterCreatedObjectUndo(root.gameObject,"Create interior finish");root.SetParent(parent,false);}
        root.SetPositionAndRotation(origin,Quaternion.identity);root.localScale=Vector3.one;root.gameObject.layer=2;return root;
    }
    static void EnsureFolder(string path)
    {
        if(AssetDatabase.IsValidFolder(path))return;int slash=path.LastIndexOf('/');EnsureFolder(path.Substring(0,slash));AssetDatabase.CreateFolder(path.Substring(0,slash),path.Substring(slash+1));
    }
    static Texture2D Texture(string name,bool normal,bool linear,bool atlas=false)
    {
        string path=ArtRoot+"/Textures/"+name+".png";
        var t=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(t==null)throw new InvalidOperationException("Generate interior artwork first: "+path);
        var i=(TextureImporter)AssetImporter.GetAtPath(path);
        var type=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
        var wrap=atlas?TextureWrapMode.Clamp:TextureWrapMode.Repeat;int size=atlas?4096:2048;
        var alpha=linear?TextureImporterAlphaSource.FromInput:TextureImporterAlphaSource.None;
        var desktop=i.GetPlatformTextureSettings("Standalone");
        var format=normal?TextureImporterFormat.BC5:TextureImporterFormat.BC7;
        if(i.textureType!=type||i.sRGBTexture!=( !normal&&!linear)||!i.mipmapEnabled||i.anisoLevel!=8||i.maxTextureSize!=size||i.wrapMode!=wrap||i.filterMode!=FilterMode.Trilinear||i.textureCompression!=TextureImporterCompression.CompressedHQ||i.alphaSource!=alpha||i.isReadable||i.npotScale!=TextureImporterNPOTScale.None||!desktop.overridden||desktop.format!=format||desktop.maxTextureSize!=size)
        {
            i.textureType=type;i.sRGBTexture=!normal&&!linear;i.mipmapEnabled=true;i.anisoLevel=8;i.maxTextureSize=size;i.wrapMode=wrap;i.filterMode=FilterMode.Trilinear;
            i.textureCompression=TextureImporterCompression.CompressedHQ;i.compressionQuality=100;i.alphaSource=alpha;i.alphaIsTransparency=false;i.isReadable=false;i.npotScale=TextureImporterNPOTScale.None;
            desktop.overridden=true;desktop.format=format;desktop.maxTextureSize=size;desktop.compressionQuality=100;i.SetPlatformTextureSettings(desktop);i.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Material Material(string name,string hex,float smooth)
    {
        string path=ArtRoot+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};AssetDatabase.CreateAsset(m,path);}
        m.shader=Shader.Find("Universal Render Pipeline/Lit");ColorUtility.TryParseHtmlString("#"+hex,out var c);m.SetColor("_BaseColor",c);
        m.SetFloat("_Metallic",0);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Surface",0);m.SetFloat("_SpecularHighlights",0);m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        m.SetFloat("_EnvironmentReflections",0);m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");m.SetTextureScale("_BaseMap",Vector2.one);m.SetTextureOffset("_BaseMap",Vector2.zero);EditorUtility.SetDirty(m);return m;
    }
    static void Surface(Material m,Texture2D color,Texture2D normal,Texture2D smooth)
    {
        m.SetTexture("_BaseMap",color);m.SetTexture("_BumpMap",normal);m.SetFloat("_BumpScale",.25f);m.EnableKeyword("_NORMALMAP");
        m.SetTexture("_MetallicGlossMap",smooth);m.SetFloat("_Smoothness",1);m.SetFloat("_SmoothnessTextureChannel",0);m.EnableKeyword("_METALLICSPECGLOSSMAP");EditorUtility.SetDirty(m);
    }
    static void Publish(Transform root,string name,Geometry g,Material mat)
    {
        string path=ArtRoot+"/Meshes/"+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh==null){mesh=new Mesh{name=name};AssetDatabase.CreateAsset(mesh,path);}
        mesh.Clear();mesh.indexFormat=IndexFormat.UInt32;mesh.SetVertices(g.vertices);mesh.SetUVs(0,g.uv);mesh.SetTriangles(g.triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
        MeshUtility.SetMeshCompression(mesh,ModelImporterMeshCompression.Off);EditorUtility.SetDirty(mesh);
        var t=root.Find(name);if(t==null){t=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)).transform;t.SetParent(root,false);Undo.RegisterCreatedObjectUndo(t.gameObject,"Create interior decoration");}
        t.localPosition=Vector3.zero;t.localRotation=Quaternion.identity;t.localScale=Vector3.one;t.gameObject.layer=2;
        t.GetComponent<MeshFilter>().sharedMesh=mesh;var r=t.GetComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=true;
        GameObjectUtility.SetStaticEditorFlags(t.gameObject,StaticEditorFlags.OccludeeStatic);
    }
    sealed class Geometry
    {
        public readonly List<Vector3> vertices=new List<Vector3>();public readonly List<Vector2> uv=new List<Vector2>();public readonly List<int> triangles=new List<int>();
        public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normal)
        {
            Vector2 UV(Vector3 v)=>Mathf.Abs(normal.y)>.5f?new Vector2(v.x,v.z)/2:Mathf.Abs(normal.x)>.5f?new Vector2(v.z,v.y)/2:new Vector2(v.x,v.y)/2;
            Quad(a,b,c,d,normal,UV(a),UV(b),UV(c),UV(d));
        }
        public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normal,Vector2 ua,Vector2 ub,Vector2 uc,Vector2 ud)
        {
            int i=vertices.Count;vertices.AddRange(new[]{a,b,c,d});uv.AddRange(new[]{ua,ub,uc,ud});
            if(Vector3.Dot(Vector3.Cross(b-a,c-a),normal)>0)triangles.AddRange(new[]{i,i+1,i+2,i,i+2,i+3});
            else triangles.AddRange(new[]{i,i+2,i+1,i,i+3,i+2});
        }
        // Six inset faces joined by twelve chamfers and eight corner triangles.
        public void BevelBox(Vector3 center,Vector3 size,float bevel,Quaternion rotation=default)
        {
            if(rotation==default)rotation=Quaternion.identity;
            var h=size*.5f;float b=Mathf.Min(bevel,Mathf.Min(h.x,Mathf.Min(h.y,h.z))*.8f);
            Vector3 P(Vector3 v)=>center+rotation*v;
            Vector3 V(int axis,float value){var v=Vector3.zero;v[axis]=value;return v;}
            for(int axis=0;axis<3;axis++)foreach(float side in new[]{-1f,1f})
            {
                int u=(axis+1)%3,v=(axis+2)%3;var n=V(axis,side);var c=V(axis,h[axis]*side);var a=V(u,h[u]-b);var d=V(v,h[v]-b);
                Quad(P(c-a-d),P(c+a-d),P(c+a+d),P(c-a+d),rotation*n);
            }
            for(int axis=0;axis<3;axis++)foreach(float su in new[]{-1f,1f})foreach(float sv in new[]{-1f,1f})
            {
                int u=(axis+1)%3,v=(axis+2)%3;var a=V(u,h[u]*su)+V(v,(h[v]-b)*sv);var c=V(u,(h[u]-b)*su)+V(v,h[v]*sv);var along=V(axis,h[axis]-b);
                Quad(P(a-along),P(a+along),P(c+along),P(c-along),rotation*(V(u,su)+V(v,sv)).normalized);
            }
            foreach(float x in new[]{-1f,1f})foreach(float y in new[]{-1f,1f})foreach(float z in new[]{-1f,1f})
            {
                var s=new Vector3(x,y,z);var corner=Vector3.Scale(s,h-Vector3.one*b);var a=P(corner+Vector3.right*x*b);var c=P(corner+Vector3.up*y*b);var d=P(corner+Vector3.forward*z*b);
                Quad(a,c,d,d,rotation*s.normalized);
            }
        }
    }
}
