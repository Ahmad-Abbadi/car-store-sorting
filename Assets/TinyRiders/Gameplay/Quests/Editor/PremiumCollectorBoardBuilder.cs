using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using TinyRiders.Quests;
using Object = UnityEngine.Object;

namespace TinyRiders.Editor
{
    /// <summary>One source of truth for the physical board. Does not rebuild quest content or UI.</summary>
    public static class PremiumCollectorBoardBuilder
    {
        public const string Folder = CollectorQuestBuilder.Folder + "/PremiumBoard";
        static readonly Color Ink = new Color(.075f, .16f, .19f);
        static readonly Color Cream = new Color(.96f, .91f, .79f);
        static readonly Color[] Accents = {new Color(.20f,.39f,.57f),new Color(.20f,.48f,.40f),new Color(.70f,.43f,.14f)};
        static readonly List<Mesh> TemporaryMeshes = new List<Mesh>();
        static readonly Dictionary<Material, List<CombineInstance>> Parts = new Dictionary<Material, List<CombineInstance>>();
        static TMP_FontAsset font;
        static Transform geometry;
        static Material wood, darkWood, cork, paper, brass, navy, rubber, enamel;
        [Serializable] sealed class BadgeExport { public BadgePart[] parts; }
        [Serializable] sealed class BadgePart { public string name, material; public float[] vertices, normals; public int[] triangles; }

        [MenuItem("Tiny Riders/Quests/Rebuild Premium Noticeboard Only")]
        public static void Rebuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before rebuilding the noticeboard.");
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(CollectorQuestBuilder.BoardPath);
            var root = existing ? PrefabUtility.LoadPrefabContents(CollectorQuestBuilder.BoardPath) : new GameObject("CollectorQuestBoard");
            Parts.Clear(); TemporaryMeshes.Clear();
            try
            {
                var board = root.GetComponent<CollectorQuestBoard>() ?? root.AddComponent<CollectorQuestBoard>();
                // Reuse the three existing text objects to preserve prefab fileIDs and scene overrides.
                var preserved = new[] {board.title ? board.title.transform : null, board.subtitle ? board.subtitle.transform : null, board.status ? board.status.transform : null};
                foreach (var t in preserved) if(t)t.SetParent(root.transform,false);
                foreach (var t in root.transform.Cast<Transform>().ToArray()) if(!preserved.Contains(t)) Object.DestroyImmediate(t.gameObject);
                geometry = new GameObject("Crafted Noticeboard").transform; geometry.SetParent(root.transform,false);
                font = Resources.Load<RuntimeLocalizationData>("RuntimeLocalizationData").latinFont;
                Materials();
                Structure();
                var boardTitle = Label(root.transform, board.title, "Board Title", "COLLECTOR\nSCRAPBOOK", new Vector3(.105f,2.265f,-.211f),new Vector2(.80f,.27f),1.64f,Ink);
                boardTitle.characterSpacing=0;
                board.title=boardTitle;
                board.subtitle=Label(root.transform,board.subtitle,"Board Subtitle","SORT • DISCOVER • GROW",new Vector3(0,2.015f,-.205f),new Vector2(1.12f,.09f),.53f,Cream);
                board.status=Label(root.transform,board.status,"Board Status","Your next goal starts here",new Vector3(0,.715f,-.191f),new Vector2(1.08f,.105f),.66f,Ink);
                var labels=root.GetComponent<CollectorNoticeboardLabels>() ?? root.AddComponent<CollectorNoticeboardLabels>();
                labels.captions = Cards(root.transform);
                board.rewardIndicator = Reward(root.transform);
                Combine(geometry,"Structure");
                if (!existing)
                {
                    var station=Object.FindFirstObjectByType<UpgradeBox>();root.layer=station ? station.gameObject.layer : 8;
                    var collider=root.AddComponent<BoxCollider>();collider.center=new Vector3(0,1.25f,0);collider.size=new Vector3(1.48f,2.5f,.38f);
                }
                foreach(var r in root.GetComponentsInChildren<Renderer>(true))
                {
                    r.gameObject.layer=2; // Decorative children never intercept interaction rays.
                    r.receiveShadows=true;
                    if(r.GetComponent<TMP_Text>())r.shadowCastingMode=ShadowCastingMode.Off;
                }
                EditorUtility.SetDirty(board); EditorUtility.SetDirty(labels);
                PrefabUtility.SaveAsPrefabAsset(root,CollectorQuestBuilder.BoardPath);
                foreach(var guid in AssetDatabase.FindAssets("",new[]{Folder}))AssetDatabase.SaveAssetIfDirty(new GUID(guid));
            }
            finally
            {
                if(existing)PrefabUtility.UnloadPrefabContents(root);else Object.DestroyImmediate(root);
                foreach(var mesh in TemporaryMeshes)if(mesh)Object.DestroyImmediate(mesh);
                TemporaryMeshes.Clear(); Parts.Clear();
            }
            Debug.Log("PREMIUM_NOTICEBOARD_READY: beveled wood, cork, three decorative collector cards; quest data and scene placement preserved.");
        }

        static void Materials()
        {
            wood=Surface("Honey oak",new Color(.62f,.36f,.17f),.30f,0,Grain("Oak",0));
            darkWood=Surface("Walnut joinery",new Color(.28f,.14f,.075f),.24f,0,Grain("Walnut",0));
            cork=Surface("Natural cork",new Color(.57f,.36f,.19f),.05f,0,Grain("Cork",1));
            paper=Surface("Cotton paper",Cream,.12f,0,Grain("Paper",2));
            brass=Surface("Brushed brass",new Color(.73f,.51f,.23f),.45f,.42f);
            navy=Surface("Ink enamel",Ink,.34f,0);
            rubber=Surface("Rubber and recesses",new Color(.055f,.064f,.061f),.12f,0);
            enamel=Surface("Ivory enamel",Cream,.45f,0);
        }

        static void Structure()
        {
            // Recessed panel and individually finished frame rails; front faces point toward local -Z.
            Box("Back panel",new Vector3(0,1.57f,.045f),new Vector3(1.43f,1.83f,.12f),.05f,.013f,darkWood);
            Box("Inner shadow",new Vector3(0,1.57f,-.044f),new Vector3(1.34f,1.74f,.07f),.04f,.01f,navy);
            Box("Brass inner lip",new Vector3(0,1.57f,-.086f),new Vector3(1.29f,1.69f,.025f),.037f,.006f,brass);
            Box("Cork inset",new Vector3(0,1.57f,-.104f),new Vector3(1.25f,1.65f,.03f),.03f,.005f,cork);
            foreach(float x in new[]{-.685f,.685f})
                Box("Oak upright",new Vector3(x,1.57f,-.015f),new Vector3(.11f,1.88f,.22f),.025f,.014f,wood);
            foreach(float y in new[]{.685f,2.455f})
                Box("Oak rail",new Vector3(0,y,-.015f),new Vector3(1.48f,.11f,.22f),.025f,.014f,wood);
            foreach(float x in new[]{-.685f,.685f})foreach(float y in new[]{.70f,2.44f})
            {
                Box("Recessed corner washer",new Vector3(x,y,-.128f),new Vector3(.036f,.036f,.005f),.018f,.001f,brass);
                Screw(new Vector3(x,y,-.133f),.022f);
            }
            foreach(float x in new[]{-.51f,.51f})
            {
                Box("Leg",new Vector3(x,.40f,.045f),new Vector3(.115f,.76f,.16f),.025f,.013f,wood);
                Box("Foot",new Vector3(x,.055f,0),new Vector3(.25f,.09f,.36f),.04f,.015f,darkWood);
                Box("Rubber sole",new Vector3(x,.016f,0),new Vector3(.23f,.032f,.34f),.035f,.006f,rubber);
                Box("Leg collar",new Vector3(x,.18f,.045f),new Vector3(.121f,.055f,.166f),.019f,.004f,brass);
                Screw(new Vector3(x,.53f,-.047f),.024f);
            }
            Box("Crossbar",new Vector3(0,.34f,.045f),new Vector3(1.06f,.095f,.12f),.02f,.012f,wood);
            // Rear joinery and maker plate also read correctly when the player walks behind it.
            foreach(float y in new[]{.94f,2.19f})
            {
                Box("Back batten",new Vector3(0,y,.12f),new Vector3(1.28f,.065f,.045f),.015f,.007f,wood);
                foreach(float x in new[]{-.54f,.54f})Box("Rear dowel",new Vector3(x,y,.147f),new Vector3(.023f,.023f,.008f),.011f,.002f,brass);
            }
            Box("Maker plate",new Vector3(0,1.58f,.114f),new Vector3(.36f,.19f,.02f),.028f,.005f,brass);
            // Plaques have a dark gasket, a raised metal rim, then the enamel face.
            Plaque(new Vector3(0,2.265f,-.17f),new Vector2(1.17f,.35f));
            Plaque(new Vector3(0,.715f,-.15f),new Vector2(1.18f,.17f));
            foreach(float x in new[]{-.535f,.535f})Screw(new Vector3(x,2.265f,-.206f),.014f);
            Emblem(new Vector3(-.434f,2.27f,-.213f),.75f);
        }

        static void Plaque(Vector3 p,Vector2 size)
        {
            Box("Gasket",p,new Vector3(size.x+.006f,size.y+.006f,.024f),.035f,.005f,navy);
            Box("Plaque rim",p+Vector3.back*.015f,new Vector3(size.x,size.y,.018f),.032f,.004f,brass);
            Box("Enamel inset",p+Vector3.back*.025f,new Vector3(size.x-.016f,size.y-.016f,.012f),.025f,.003f,enamel);
        }

        static TMP_Text[] Cards(Transform parent)
        {
            var captions=new TMP_Text[3];
            string[] themes={"TH_PIRATE","TH_SPACE","TH_BUTTERFLY_GARDEN"};
            var catalog=AssetDatabase.LoadAssetAtPath<ThemedCarCatalog>("Assets/TinyRiders/ThemedCollection/Data/Themes/ThemedCarCatalog.asset");
            var available=catalog.entries.Where(e=>e.enabled&&e.preview).ToArray();
            if(available.Length==0)throw new InvalidOperationException("The noticeboard needs car preview artwork.");
            for(int i=0;i<3;i++)
            {
                var card=new GameObject("Collector Card "+(i+1)).transform;card.SetParent(parent,false);
                card.localPosition=new Vector3(0,1.79f-i*.385f,-.16f);
                card.localRotation=Quaternion.Euler(0,0,new[]{-1.5f,1.1f,-.8f}[i]);
                var accent=Surface("Card accent "+i,Accents[i],.2f,0);
                // Accumulate the whole assembly in board coordinates, including its slight rotation.
                var matrix=card.localToWorldMatrix; var rootMatrix=parent.worldToLocalMatrix*matrix;
                CardBox(card,rootMatrix,new Vector3(.008f,-.009f,.008f),new Vector3(1.15f,.34f,.012f),.02f,.003f,darkWood);
                CardBox(card,rootMatrix,Vector3.zero,new Vector3(1.15f,.34f,.012f),.018f,.003f,paper);
                CardBox(card,rootMatrix,new Vector3(-.543f,0,-.009f),new Vector3(.034f,.308f,.004f),.009f,.001f,accent);
                CardBox(card,rootMatrix,new Vector3(-.346f,0,-.01f),new Vector3(.321f,.309f,.005f),.015f,.001f,brass);
                var entry=available.FirstOrDefault(e=>e.themeId==themes[i]&&e.baseCarId=="C001") ?? available.FirstOrDefault(e=>e.themeId==themes[i]) ?? available[i%available.Length];
                Picture(card,entry.preview,i);
                captions[i]=Label(card,null,"Card Caption",new[]{"SORT","DISCOVER","GROW"}[i],new Vector3(.123f,.049f,-.023f),new Vector2(.53f,.105f),1.13f,Accents[i]);
                var collection=Label(card,null,"Collection Name",entry.DisplayName,new Vector3(.123f,-.037f,-.023f),new Vector2(.52f,.062f),.48f,Ink);
                collection.gameObject.AddComponent<LocalizedText>().SetKey("theme."+entry.themeId+".name");
                Label(card,null,"Edition Number","0"+(i+1),new Vector3(.453f,.09f,-.023f),new Vector2(.10f,.055f),.47f,Accents[i]);
                // Printed collector stamp: concentric seals, a tiny car silhouette, and perforation-like dots.
                CardBox(card,rootMatrix,new Vector3(.447f,-.062f,-.014f),new Vector3(.113f,.113f,.003f),.0565f,.001f,accent);
                CardBox(card,rootMatrix,new Vector3(.447f,-.062f,-.017f),new Vector3(.098f,.098f,.003f),.049f,.001f,paper);
                CardBox(card,rootMatrix,new Vector3(.447f,-.062f,-.020f),new Vector3(.080f,.080f,.002f),.040f,.0005f,accent);
                CardBox(card,rootMatrix,new Vector3(.447f,-.062f,-.022f),new Vector3(.068f,.068f,.002f),.034f,.0005f,paper);
                for(int d=0;d<8;d++)CardBox(card,rootMatrix,new Vector3(-.084f+d*.051f,-.105f,-.014f),new Vector3(.010f,.010f,.002f),.005f,.0005f,accent);
                CardBox(card,rootMatrix,new Vector3(.446f,-.061f,-.026f),new Vector3(.043f,.018f,.003f),.006f,.001f,accent);
                CardBox(card,rootMatrix,new Vector3(.441f,-.048f,-.026f),new Vector3(.024f,.015f,.003f),.006f,.001f,accent);
                foreach(float x in new[]{.433f,.459f})CardBox(card,rootMatrix,new Vector3(x,-.072f,-.028f),new Vector3(.009f,.009f,.002f),.0045f,.0005f,accent);
                // Folded brass clip, recess, rivet and rolled top lip.
                CardBox(card,rootMatrix,new Vector3(.02f,.152f,-.025f),new Vector3(.12f,.068f,.014f),.012f,.004f,brass);
                CardBox(card,rootMatrix,new Vector3(.02f,.158f,-.034f),new Vector3(.066f,.024f,.004f),.01f,.001f,navy);
                CardBox(card,rootMatrix,new Vector3(.02f,.180f,-.022f),new Vector3(.129f,.015f,.02f),.007f,.005f,brass);
                CardBox(card,rootMatrix,new Vector3(.02f,.131f,-.035f),new Vector3(.013f,.013f,.004f),.0065f,.001f,navy);
            }
            return captions;
        }

        static void Picture(Transform parent,Texture2D texture,int index)
        {
            // Deliberately retain the preview's pale photographic backdrop and original aspect ratio.
            var material=Surface("Car photograph "+index,Color.white,0,0,null,true);
            material.mainTexture=texture;
            var quad=new GameObject("Car Photograph",typeof(MeshFilter),typeof(MeshRenderer));quad.transform.SetParent(parent,false);
            quad.transform.localPosition=new Vector3(-.346f,0,-.017f);
            float aspect=(float)texture.width/texture.height;
            float w=Mathf.Min(.297f,.287f*aspect),h=w/aspect;
            var mesh=new Mesh {name="Car photograph quad"};
            mesh.vertices=new[]{new Vector3(-w/2,-h/2,0),new Vector3(-w/2,h/2,0),new Vector3(w/2,h/2,0),new Vector3(w/2,-h/2,0)};
            mesh.uv=new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)};
            mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateNormals();mesh.RecalculateBounds();
            quad.GetComponent<MeshFilter>().sharedMesh=SaveMesh(mesh,"Photo "+index);
            quad.GetComponent<MeshRenderer>().sharedMaterial=material;
            quad.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
        }

        static GameObject Reward(Transform parent)
        {
            // A separate batch is necessary because this is the only animated mesh on the board.
            var saved=Parts.ToDictionary(p=>p.Key,p=>p.Value);Parts.Clear();
            var root=new GameObject("Rewards Ready Medallion");root.transform.SetParent(parent,false);
            root.transform.localPosition=new Vector3(.59f,2.39f,-.19f);
            Box("Reward rim",Vector3.zero,new Vector3(.23f,.23f,.028f),.115f,.005f,brass);
            Box("Mint enamel",Vector3.back*.02f,new Vector3(.192f,.192f,.018f),.096f,.004f,Surface("Mint reward enamel",new Color(.32f,.68f,.52f),.4f,0));
            Box("Gift body",new Vector3(0,-.012f,-.036f),new Vector3(.095f,.068f,.012f),.008f,.002f,enamel);
            Box("Gift lid",new Vector3(0,.027f,-.04f),new Vector3(.109f,.018f,.014f),.004f,.002f,enamel);
            Box("Gift ribbon",new Vector3(0,-.007f,-.048f),new Vector3(.016f,.095f,.004f),.002f,.001f,brass);
            Box("Bow left",new Vector3(-.024f,.052f,-.038f),new Vector3(.045f,.025f,.012f),.012f,.003f,brass);
            Box("Bow right",new Vector3(.024f,.052f,-.038f),new Vector3(.045f,.025f,.012f),.012f,.003f,brass);
            Combine(root.transform,"Reward");Parts.Clear();foreach(var p in saved)Parts.Add(p.Key,p.Value);
            var motion=root.AddComponent<CollectorQuestMotion>();motion.floatBadge=true;
            root.SetActive(false);return root;
        }

        static void Emblem(Vector3 p,float scale)
        {
            const string path="ArtSource/CollectorBoard/Blender/CarBadge.mesh.json";
            if(!File.Exists(path))throw new FileNotFoundException("Re-export the Blender car badge using ArtSource/CollectorBoard/build_car_badge.py",path);
            var data=JsonUtility.FromJson<BadgeExport>(File.ReadAllText(path));
            float s=.19f*scale/1.72f;
            foreach(var part in data.parts)
            {
                var vertices=new Vector3[part.vertices.Length/3];var normals=new Vector3[vertices.Length];
                for(int i=0;i<vertices.Length;i++)
                {
                    vertices[i]=new Vector3(part.vertices[i*3],part.vertices[i*3+1],part.vertices[i*3+2]);
                    normals[i]=new Vector3(part.normals[i*3],part.normals[i*3+1],part.normals[i*3+2]);
                }
                var mesh=new Mesh{name=part.name};mesh.vertices=vertices;mesh.normals=normals;mesh.triangles=part.triangles;mesh.RecalculateBounds();
                Add(mesh,Matrix4x4.TRS(p,Quaternion.identity,Vector3.one*s),part.material=="Brass"?brass:part.material=="Enamel"?enamel:navy);
            }
        }

        static void Screw(Vector3 p,float size)
        {
            Box("Fastener",p,new Vector3(size,size,.008f),size*.5f,.002f,brass);
            Box("Screw slot",p+Vector3.back*.0045f,new Vector3(size*.63f,size*.12f,.001f),.001f,.0002f,navy);
        }
        static void CardBox(Transform card,Matrix4x4 matrix,Vector3 p,Vector3 size,float r,float b,Material material) => Add(Rounded(size,r,b),matrix*Matrix4x4.Translate(p),material);
        static void Box(string name,Vector3 p,Vector3 size,float radius,float bevel,Material material)
        {
            var mesh=Rounded(size,radius,bevel);
            if((material==wood||material==darkWood)&&size.x>size.y)
                mesh.uv=mesh.uv.Select(uv=>new Vector2(uv.y,uv.x)).ToArray();
            Add(mesh,Matrix4x4.Translate(p),material);
        }
        static void Add(Mesh mesh,Matrix4x4 matrix,Material material)
        {
            TemporaryMeshes.Add(mesh);
            if(!Parts.TryGetValue(material,out var list))Parts[material]=list=new List<CombineInstance>();
            list.Add(new CombineInstance{mesh=mesh,transform=matrix});
        }
        static void Combine(Transform parent,string prefix)
        {
            foreach(var pair in Parts)
            {
                var mesh=new Mesh{name=prefix+" "+pair.Key.name,indexFormat=IndexFormat.UInt32};
                mesh.CombineMeshes(pair.Value.ToArray(),true,true);mesh.RecalculateBounds();
                var go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
                go.GetComponent<MeshFilter>().sharedMesh=SaveMesh(mesh,mesh.name);go.GetComponent<MeshRenderer>().sharedMaterial=pair.Key;
                // Small stamped details are below the showroom shadow-map resolution.
                // Keep the stand's floor shadow, without jagged self-shadows over the print.
                go.GetComponent<MeshRenderer>().shadowCastingMode=pair.Key==wood||pair.Key==darkWood ? ShadowCastingMode.On : ShadowCastingMode.Off;
            }
        }
        static Mesh SaveMesh(Mesh mesh,string name)
        {
            string path=Folder+"/"+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing)
            {
                // Update through the mesh API so existing scene renderers invalidate GPU buffers too.
                existing.Clear();existing.indexFormat=mesh.indexFormat;
                existing.vertices=mesh.vertices;existing.normals=mesh.normals;existing.uv=mesh.uv;
                existing.tangents=mesh.tangents;existing.triangles=mesh.triangles;existing.bounds=mesh.bounds;
                existing.UploadMeshData(false);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);return existing;
            }
            AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        static TMP_Text Label(Transform parent,TMP_Text label,string name,string value,Vector3 p,Vector2 size,float fontSize,Color color)
        {
            if(!label)label=new GameObject(name,typeof(RectTransform),typeof(TextMeshPro)).GetComponent<TextMeshPro>();
            label.transform.SetParent(parent,false);label.transform.localPosition=p;label.transform.localRotation=Quaternion.identity;label.transform.localScale=Vector3.one;
            label.rectTransform.sizeDelta=size;label.rectTransform.pivot=new Vector2(.5f,.5f);
            label.font=font;label.text=value;label.fontSize=label.fontSizeMax=fontSize;label.fontSizeMin=fontSize*.50f;
            label.enableAutoSizing=true;label.alignment=TextAlignmentOptions.Center;label.color=color;
            label.margin=Vector4.zero;label.overflowMode=TextOverflowModes.Ellipsis;label.raycastTarget=false;
            label.ForceMeshUpdate();return label;
        }
        static Material Surface(string name,Color color,float smooth,float metal,Texture2D texture=null,bool unlit=false)
        {
            string path=Folder+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat){mat=new Material(Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            mat.color=color;if(texture)mat.mainTexture=texture;
            if(!unlit)
            {
                mat.SetFloat("_Smoothness",smooth);mat.SetFloat("_Metallic",metal);
                bool receive=name=="Honey oak"||name=="Walnut joinery"||name=="Natural cork";
                mat.SetFloat("_ReceiveShadows",receive?1:0);
                if(receive)mat.DisableKeyword("_RECEIVE_SHADOWS_OFF");else mat.EnableKeyword("_RECEIVE_SHADOWS_OFF");
            }
            if(texture)
            {
                var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/"+texture.name+" Normal.png");
                if(normal){mat.SetTexture("_BumpMap",normal);mat.SetFloat("_BumpScale",.32f);mat.EnableKeyword("_NORMALMAP");}
            }
            EditorUtility.SetDirty(mat);return mat;
        }

        static Texture2D Grain(string name,int kind)
        {
            string path=Folder+"/"+name+".png";
            const string revision="PremiumBoardGrain.v2";
            if(File.Exists(path)&&AssetImporter.GetAtPath(path)?.userData==revision)return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            const int n=512;var heights=new float[n*n];var colors=new Color[n*n];
            var random=new System.Random(412+kind);
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                float u=x/(float)n,v=y/(float)n,noise=(float)random.NextDouble();float h;
                if(kind==0){float wave=Mathf.PerlinNoise(u*3,v*2)*1.4f;h=.83f+.027f*Mathf.Sin((u*22+wave)*Mathf.PI)+.035f*Mathf.PerlinNoise(u*110,v*4)+noise*.012f;}
                else if(kind==1){h=.52f+.28f*Mathf.PerlinNoise(u*105,v*105)+.13f*Mathf.PerlinNoise(u*240+15,v*240)+noise*.10f;}
                else h=.93f+noise*.055f+.015f*Mathf.PerlinNoise(u*75,v*75);
                heights[y*n+x]=h;colors[y*n+x]=new Color(h,h,h,1);
            }
            var tex=new Texture2D(n,n,TextureFormat.RGB24,false);tex.SetPixels(colors);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());
            var normals=new Color[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                float dx=heights[y*n+(x+1)%n]-heights[y*n+(x+n-1)%n];float dy=heights[((y+1)%n)*n+x]-heights[((y+n-1)%n)*n+x];
                var normal=new Vector3(-dx,-dy,1).normalized;normals[y*n+x]=new Color(normal.x*.5f+.5f,normal.y*.5f+.5f,normal.z*.5f+.5f,1);
            }
            tex.SetPixels(normals);tex.Apply();File.WriteAllBytes(Folder+"/"+name+" Normal.png",tex.EncodeToPNG());Object.DestroyImmediate(tex);
            foreach(var file in new[]{path,Folder+"/"+name+" Normal.png"})
            {
                AssetDatabase.ImportAsset(file);var importer=(TextureImporter)AssetImporter.GetAtPath(file);
                importer.textureType=file.Contains(" Normal")?TextureImporterType.NormalMap:TextureImporterType.Default;
                importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;importer.wrapMode=TextureWrapMode.Repeat;
                importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.maxTextureSize=512;importer.userData=revision;importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // Four rounded contours form a solid with a true bevel on front and back, rather than scaled cubes.
        static Mesh Rounded(Vector3 size,float radius,float bevel)
        {
            radius=Mathf.Min(radius,Mathf.Min(size.x,size.y)*.5f);bevel=Mathf.Min(bevel,Mathf.Min(size.z*.45f,radius*.8f));
            const int segments=6,count=4*(segments+1);
            var v=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            for(int ring=0;ring<4;ring++)
            {
                bool inset=ring==0||ring==3;float ix=inset?bevel:0,hx=size.x*.5f-ix,hy=size.y*.5f-ix,r=radius-ix;
                float z=ring==0?-size.z*.5f:ring==1?-size.z*.5f+bevel:ring==2?size.z*.5f-bevel:size.z*.5f;
                for(int corner=0;corner<4;corner++)for(int j=0;j<=segments;j++)
                {
                    float angle=(corner*90+j*90f/segments)*Mathf.Deg2Rad;
                    float cx=(corner==0||corner==3?1:-1)*(hx-r),cy=(corner<2?1:-1)*(hy-r);
                    var p=new Vector3(cx+Mathf.Cos(angle)*r,cy+Mathf.Sin(angle)*r,z);v.Add(p);uv.Add(new Vector2(p.x/size.x+.5f,p.y/size.y+.5f));
                }
            }
            for(int ring=0;ring<3;ring++)for(int i=0;i<count;i++)
            {int a=ring*count+i,b=ring*count+(i+1)%count,c=b+count,d=a+count;triangles.AddRange(new[]{a,b,c,a,c,d});}
            // Duplicate the caps' vertices so their broad surfaces stay flat under lighting.
            for(int side=0;side<2;side++)
            {
                int start=v.Count,source=side==0?0:3*count;v.Add(new Vector3(0,0,(side==0?-1:1)*size.z*.5f));uv.Add(new Vector2(.5f,.5f));
                for(int i=0;i<count;i++){v.Add(v[source+i]);uv.Add(uv[source+i]);}
                for(int i=0;i<count;i++){int a=start+1+i,b=start+1+(i+1)%count;triangles.AddRange(side==0?new[]{start,b,a}:new[]{start,a,b});}
            }
            var mesh=new Mesh();mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
        }
    }
}
