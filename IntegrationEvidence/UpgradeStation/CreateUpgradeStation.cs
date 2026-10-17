// Run with Unity Pipeline eval_file in Edit mode. Rebuilds the shared upgrade prop.
var assetRoot = "Assets/TinyRiders/Art/UpgradeStation";
System.IO.Directory.CreateDirectory(assetRoot);
UnityEditor.AssetDatabase.Refresh();
var prefabPath = "Assets/00_Project/02_prefabs/Systems/Upgrade Box.prefab";
var root = UnityEditor.PrefabUtility.LoadPrefabContents(prefabPath);
try
{
    var old = root.transform.Find("Visual");
    if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
    var visual = new GameObject("Visual");
    visual.transform.SetParent(root.transform, false);
    var groups = new System.Collections.Generic.Dictionary<Material, System.Collections.Generic.List<CombineInstance>>();
    var temporaryMeshes = new System.Collections.Generic.List<Mesh>();
    Material Mat(string name, Color color, bool glow = false)
    {
        var path = assetRoot + "/" + name + ".mat";
        var m = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); UnityEditor.AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_BaseColor", color); m.SetFloat("_Smoothness", .32f);
        if (glow) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", color * 1.4f); }
        groups[m] = new System.Collections.Generic.List<CombineInstance>();
        UnityEditor.EditorUtility.SetDirty(m);
        return m;
    }
    var teal = Mat("Enamel_Turquoise", new Color(.055f,.43f,.42f));
    var dark = Mat("Midnight_Metal", new Color(.035f,.065f,.085f));
    var cream = Mat("Warm_Ivory", new Color(.89f,.85f,.69f));
    var steel = Mat("Brushed_Steel", new Color(.43f,.55f,.57f));
    var mint = Mat("Mint_Indicator", new Color(.27f,1f,.74f), true);
    var orange = Mat("Coral_Control", new Color(1f,.29f,.14f));
    void Add(Mesh mesh, Material material, Vector3 position, Vector3 scale, Quaternion rotation)
    { groups[material].Add(new CombineInstance { mesh=mesh, transform=Matrix4x4.TRS(position,rotation,scale) }); }
    Mesh Rounded(Vector3 size, float radius)
    {
        var verts = new System.Collections.Generic.List<Vector3>(); var tris = new System.Collections.Generic.List<int>();
        var half = size*.5f; var inner=half-Vector3.one*radius;
        for (int face=0;face<6;face++)
        {
            var axis=face/2; float sign=face%2==0 ? 1f : -1f;
            int a=(axis+1)%3, b=(axis+2)%3, start=verts.Count;
            for(int y=0;y<4;y++) for(int x=0;x<4;x++)
            {
                Vector3 p=Vector3.zero; p[axis]=half[axis]*sign;
                p[a]=x==0?-half[a]:x==1?-inner[a]:x==2?inner[a]:half[a];
                p[b]=y==0?-half[b]:y==1?-inner[b]:y==2?inner[b]:half[b];
                var c=new Vector3(Mathf.Clamp(p.x,-inner.x,inner.x),Mathf.Clamp(p.y,-inner.y,inner.y),Mathf.Clamp(p.z,-inner.z,inner.z));
                verts.Add(c+(p-c).normalized*radius);
            }
            for(int y=0;y<3;y++) for(int x=0;x<3;x++)
            { int i=start+y*4+x; if(sign>0) tris.AddRange(new[]{i,i+1,i+5,i,i+5,i+4}); else tris.AddRange(new[]{i,i+5,i+1,i,i+4,i+5}); }
        }
        var mesh=new Mesh();mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();temporaryMeshes.Add(mesh);return mesh;
    }
    void Box(string name, Vector3 p, Vector3 size, Material mat, float bevel=.012f)
    { Add(Rounded(size,Mathf.Min(bevel,Mathf.Min(size.x,Mathf.Min(size.y,size.z))*.45f)),mat,p,Vector3.one,Quaternion.identity); }
    var cylinderObject=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
    var cylinder=cylinderObject.GetComponent<MeshFilter>().sharedMesh;
    UnityEngine.Object.DestroyImmediate(cylinderObject);
    void Disc(Vector3 p,float radius,float depth,Material mat)
    { Add(cylinder,mat,p,new Vector3(radius*2,depth*.5f,radius*2),Quaternion.Euler(90,0,0)); }
    void Bar(Vector3 a,Vector3 b,float width,float depth,Material mat)
    { Add(Rounded(new Vector3(width,Vector3.Distance(a,b),depth),Mathf.Min(width,depth)*.23f),mat,(a+b)*.5f,Vector3.one,Quaternion.FromToRotation(Vector3.up,b-a)); }
    void Text(string name,string text,Vector3 p,float size,Color color)
    {
        var go=new GameObject(name);go.transform.SetParent(visual.transform,false);go.transform.localPosition=p;go.transform.localRotation=Quaternion.Euler(0,180,0);
        var tm=go.AddComponent<TMPro.TextMeshPro>();tm.text=text;tm.fontSize=size;tm.alignment=TMPro.TextAlignmentOptions.Center;tm.color=color;tm.fontStyle=TMPro.FontStyles.Bold;tm.rectTransform.sizeDelta=new Vector2(.69f,.18f);
        tm.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
    }
    // Rounded rolling cabinet, inset drawers, recessed metal handles.
    Box("Plinth",new Vector3(0,.08f,0),new Vector3(.79f,.12f,.65f),dark,.035f);
    foreach(float x in new[]{-.28f,.28f}) foreach(float z in new[]{-.22f,.22f})
        Add(cylinder,dark,new Vector3(x,.065f,z),new Vector3(.11f,.035f,.11f),Quaternion.Euler(90,0,90));
    Box("Cabinet",new Vector3(0,.53f,0),new Vector3(.74f,.83f,.58f),teal,.045f);
    Box("Worktop rim",new Vector3(0,.97f,0),new Vector3(.82f,.08f,.67f),cream,.026f);
    Box("Worktop inset",new Vector3(0,1.016f,0),new Vector3(.71f,.025f,.55f),dark);
    for(int i=0;i<3;i++)
    {
        float y=.29f+i*.235f;
        Box("Drawer reveal",new Vector3(0,y,.294f),new Vector3(.65f,.204f,.018f),dark);
        Box("Drawer face",new Vector3(0,y,.308f),new Vector3(.615f,.18f,.025f),teal);
        Box("Handle inset",new Vector3(0,y+.032f,.326f),new Vector3(.28f,.048f,.012f),dark);
        Box("Handle",new Vector3(0,y+.048f,.344f),new Vector3(.29f,.023f,.034f),steel);
    }
    for(int i=0;i<4;i++) Box("Side vent",new Vector3(.373f,.59f+i*.055f,0),new Vector3(.012f,.018f,.27f),dark);
    // Retro diagnostic terminal.
    Box("Screen stem",new Vector3(0,1.20f,-.16f),new Vector3(.13f,.36f,.13f),steel);
    Box("Screen surround",new Vector3(0,1.43f,-.10f),new Vector3(.74f,.62f,.18f),dark,.048f);
    Box("Screen trim",new Vector3(0,1.46f,.003f),new Vector3(.65f,.44f,.035f),steel,.023f);
    Box("Screen glass",new Vector3(0,1.46f,.025f),new Vector3(.60f,.39f,.018f),dark,.02f);
    Text("Screen caption","UPGRADE LAB",new Vector3(0,1.59f,.045f),.46f,new Color(.65f,.94f,.84f));
    for(int i=0;i<3;i++)
    {
        float x=-.17f+i*.17f;
        Box("Arrow shaft",new Vector3(x,1.43f,.044f),new Vector3(.036f,.10f,.012f),mint);
        Bar(new Vector3(x-.055f,1.46f,.044f),new Vector3(x,1.515f,.044f),.031f,.012f,mint);
        Bar(new Vector3(x+.055f,1.46f,.044f),new Vector3(x,1.515f,.044f),.031f,.012f,mint);
        Box("Progress track",new Vector3(x,1.32f,.044f),new Vector3(.13f,.018f,.012f),teal);
        Box("Progress fill",new Vector3(x-.025f+i*.012f,1.32f,.053f),new Vector3(.06f+i*.022f,.018f,.012f),mint);
    }
    Disc(new Vector3(-.27f,1.17f,.013f),.022f,.022f,mint);
    Disc(new Vector3(.26f,1.17f,.013f),.038f,.026f,orange);
    // Illuminated marquee and a recognizable oversized open-end wrench.
    Box("Marquee backing",new Vector3(0,1.86f,-.10f),new Vector3(.79f,.20f,.15f),teal,.035f);
    Box("Marquee insert",new Vector3(.07f,1.86f,-.015f),new Vector3(.57f,.145f,.022f),cream);
    Text("Tune Up sign","TUNE UP",new Vector3(.07f,1.86f,.003f),.75f,new Color(.035f,.105f,.12f));
    Disc(new Vector3(-.29f,1.86f,.005f),.091f,.026f,dark);
    Bar(new Vector3(-.32f,1.81f,.028f),new Vector3(-.273f,1.88f,.028f),.029f,.018f,cream);
    Bar(new Vector3(-.273f,1.88f,.028f),new Vector3(-.297f,1.91f,.028f),.027f,.018f,cream);
    Bar(new Vector3(-.273f,1.88f,.028f),new Vector3(-.243f,1.883f,.028f),.027f,.018f,cream);
    Bar(new Vector3(-.297f,1.91f,.028f),new Vector3(-.288f,1.936f,.028f),.022f,.018f,cream);
    Bar(new Vector3(-.243f,1.883f,.028f),new Vector3(-.221f,1.901f,.028f),.022f,.018f,cream);
    // Worktop control pad and shop-tool details.
    Box("Control pedestal",new Vector3(.18f,1.045f,.17f),new Vector3(.25f,.055f,.19f),teal);
    Disc(new Vector3(.18f,1.082f,.19f),.046f,.019f,orange);
    foreach(float x in new[]{-.27f,-.20f,-.13f}) Box("Control key",new Vector3(x,1.045f,.19f),new Vector3(.045f,.022f,.065f),cream);
    // Cable hangs on the side within the original interaction footprint.
    Vector3 prev=new Vector3(.37f,.98f,-.12f);
    for(int i=1;i<=18;i++)
    {
        float t=i/18f; var next=new Vector3(.385f+Mathf.Sin(t*Mathf.PI)*.025f,.98f-.49f*Mathf.Sin(t*Mathf.PI),-.12f+.29f*t);
        Bar(prev,next,.025f,.025f,dark);prev=next;
    }
    Box("Cable socket",new Vector3(.38f,.97f,.17f),new Vector3(.06f,.11f,.08f),orange);
    foreach(var group in groups)
    {
        var mesh=new Mesh {name=group.Key.name+"_Geometry"};mesh.CombineMeshes(group.Value.ToArray(),true,true);mesh.RecalculateBounds();
        var path=assetRoot+"/"+group.Key.name+".asset";
        var existing=UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing==null) UnityEditor.AssetDatabase.CreateAsset(mesh,path); else {UnityEditor.EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}
        var go=new GameObject(group.Key.name);go.transform.SetParent(visual.transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=group.Key;
    }
    // Children inherit the interactable layer; the existing collider and UpgradeBox stay on the root.
    foreach(var t in visual.GetComponentsInChildren<Transform>(true)) t.gameObject.layer=root.layer;
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
    UnityEditor.AssetDatabase.SaveAssets();
    foreach(var mesh in temporaryMeshes) UnityEngine.Object.DestroyImmediate(mesh);
}
finally { UnityEditor.PrefabUtility.UnloadPrefabContents(root); }
return "Tune-Up station saved to existing Upgrade Box prefab.";
