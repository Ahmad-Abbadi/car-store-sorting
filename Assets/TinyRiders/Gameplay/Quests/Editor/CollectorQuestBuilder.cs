using System;
using System.IO;
using System.Linq;
using TMPro;
using ToyShop.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TinyRiders.Quests;
using Object = UnityEngine.Object;

namespace TinyRiders.Editor
{
    public static partial class CollectorQuestBuilder
    {
        public const string Folder="Assets/TinyRiders/Gameplay/Quests/Content";
        public const string BoardPath=Folder+"/CollectorQuestBoard.prefab";
        public const string UIPath=Folder+"/CollectorQuestUI.prefab";
        public const string CardPath=Folder+"/CollectorQuestCard.prefab";
        static ToyShopTheme T => ToyShopTheme.Current;

        [MenuItem("Tiny Riders/Quests/Build Collector Quest System")]
        public static void Build()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before building quests.");
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!="Assets/00_Project/00_Scenes/Level.unity")
                throw new InvalidOperationException("Open Level before building quests.");
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            var defs=Definitions();
            BuildCard(); BuildUI(); BuildBoard();
            Polish();
            Scrapbook();
            var manager=Object.FindFirstObjectByType<CollectorQuestManager>();
            if(!manager) manager=new GameObject("Collector Quest Manager").AddComponent<CollectorQuestManager>();
            manager.definitions=defs; EditorUtility.SetDirty(manager);
            var ui=Object.FindFirstObjectByType<CollectorQuestUI>();
            if(!ui) ui=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(UIPath))).GetComponent<CollectorQuestUI>();
            var board=Object.FindFirstObjectByType<CollectorQuestBoard>();
            if(!board)
            {
                board=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(BoardPath))).GetComponent<CollectorQuestBoard>();
                // Initial placement only; rebuilding must respect an authored board location.
                board.transform.position=new Vector3(501.7f,0,-10.25f); board.transform.rotation=Quaternion.Euler(0,50,0);
            }
            board.interfaceUI=ui; PrefabUtility.RecordPrefabInstancePropertyModifications(board);
            PrefabUtility.RecordPrefabInstancePropertyModifications(board.transform);
            BindAudio();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
        }

        public static CollectorQuestDefinition[] Definitions()
        {
            string[] ids={"first_display","getting_organized","full_house","better_tools","growing_collection","theme_collector","showroom_specialist","store_master"};
            string[] titles={"First Display","Getting Organized","Full House","Better Tools","Growing Collection","Theme Collector","Showroom Specialist","Store Master"};
            var goals=new[]{CollectorQuestGoal.SortedCars,CollectorQuestGoal.SortedCars,CollectorQuestGoal.FullShelves,CollectorQuestGoal.OwnedUpgrades,
                CollectorQuestGoal.SortedCars,CollectorQuestGoal.CompletedThemes,CollectorQuestGoal.CompletedThemes,CollectorQuestGoal.EntireStore};
            int[] targets={5,25,1,1,100,1,3,int.MaxValue}, coins={3,5,5,5,10,10,15,25}, xp={20,30,30,30,50,50,75,100};
            string[] descriptions={"Correctly shelve {0} cars","Correctly shelve {0} cars","Fill {0} shelf correctly","Own any upgrade",
                "Correctly shelve {0} cars","Complete {0} themes","Complete {0} themes","Correctly shelve the entire collection"};
            var legacy=ids.Select((id,i)=>{
                string path=Folder+"/"+id+".asset";
                var d=AssetDatabase.LoadAssetAtPath<CollectorQuestDefinition>(path);
                if(!d){d=ScriptableObject.CreateInstance<CollectorQuestDefinition>(); AssetDatabase.CreateAsset(d,path);}
                d.id=id; d.title=titles[i]; d.description=descriptions[i]; d.goal=goals[i]; d.target=targets[i]; d.coins=coins[i]; d.xp=xp[i];
                d.seriesId="legacy"; d.authoredOrder=i; d.category=CategoryFor(d.goal);
                EditorUtility.SetDirty(d); return d;
            }).ToArray();
            return ExpandDefinitions(legacy);
        }

        static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=new Vector2(0,1); r.pivot=new Vector2(0,1); r.anchoredPosition=new Vector2(x,-y); r.sizeDelta=new Vector2(w,h); return r;
        }
        static UnityEngine.UI.Image Surface(RectTransform rect,Color color)
        {var i=rect.gameObject.AddComponent<UnityEngine.UI.Image>(); ToyShopStyle.Surface(i,color,false); return i;}
        static TMP_Text Text(Transform parent,string name,string value,float x,float y,float w,float h,float size)
        {
            var r=Rect(parent,name,x,y,w,h); var t=r.gameObject.AddComponent<TextMeshProUGUI>(); t.text=value;
            if(T.font) t.font=T.font; t.fontSize=size; t.fontSizeMax=size; t.fontSizeMin=size*.7f;
            t.enableAutoSizing=true; t.color=T.navy; t.raycastTarget=false; t.alignment=TextAlignmentOptions.MidlineLeft;
            return t;
        }
        static UnityEngine.UI.Button Button(Transform parent,string name,string value,float x,float y,float w,out TMP_Text label)
        {
            var r=Rect(parent,name,x,y,w,44); var i=Surface(r,T.muted); var b=r.gameObject.AddComponent<UnityEngine.UI.Button>(); b.targetGraphic=i;
            label=Text(r,"Label",value,8,0,w-16,44,21); label.alignment=TextAlignmentOptions.Center;
            ToyShopStyle.Button(b); return b;
        }
        static UnityEngine.UI.Image Bar(Transform parent,float x,float y,float width,float height)
        {
            var bg=Rect(parent,"Progress Background",x,y,width,height); Surface(bg,T.muted);
            var f=Surface(Rect(bg,"Fill",0,0,width,height),T.mint); f.type=UnityEngine.UI.Image.Type.Filled;
            f.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal; f.fillOrigin=0; f.fillAmount=0; f.raycastTarget=false; return f;
        }
        static void Stretch(RectTransform r)
        {r.anchorMin=Vector2.zero; r.anchorMax=Vector2.one; r.offsetMin=r.offsetMax=Vector2.zero;}

        static void BuildCard()
        {
            var root=Rect(null,"Collector Quest Card",0,0,1040,158); Surface(root,Color.white);
            var layout=root.gameObject.AddComponent<UnityEngine.UI.LayoutElement>(); layout.preferredHeight=158;
            var c=root.gameObject.AddComponent<CollectorQuestCard>();
            c.stripe=Surface(Rect(root,"Stripe",0,10,6,138),T.blue);
            c.title=Text(root,"Title","First Display",24,10,620,34,27);
            c.description=Text(root,"Description","Correctly shelve 5 cars",24,46,640,40,21);
            c.rewards=Text(root,"Rewards","3 coins  +  20 XP",24,87,450,30,20); c.rewards.color=T.blue;
            c.progress=Text(root,"Progress","0 / 5",568,112,100,30,19); c.progress.alignment=TextAlignmentOptions.Right;
            c.fill=Bar(root,24,125,520,9);
            c.state=Text(root,"State","IN PROGRESS",700,12,310,28,17); c.state.alignment=TextAlignmentOptions.Right;
            c.claim=Button(root,"Claim Reward","Claim",735,90,130,out c.claimLabel); c.claim.GetComponent<UnityEngine.UI.Image>().color=T.gold;
            c.pin=Button(root,"Pin Quest","Pin",880,90,130,out c.pinLabel);
            c.replace=Button(root,"Replace Quest","Replace",720,90,145,out c.replaceLabel);
            PrefabUtility.SaveAsPrefabAsset(root.gameObject,CardPath); Object.DestroyImmediate(root.gameObject);
        }

        static void BuildUI()
        {
            var root=Rect(null,"Collector Quest Interface",0,0,1600,900);
            var canvas=root.gameObject.AddComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=90;
            var scaler=root.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1600,900); scaler.matchWidthOrHeight=.5f;
            root.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var ui=root.gameObject.AddComponent<CollectorQuestUI>();
            var tracker=Rect(root,"Pinned Quest",0,0,390,142); tracker.anchorMin=tracker.anchorMax=new Vector2(0,1); tracker.pivot=new Vector2(0,1); tracker.anchoredPosition=new Vector2(24,-230);
            Surface(tracker,T.cream); ui.tracker=tracker.gameObject;
            ui.trackerTitle=Text(tracker,"Title","First Display",18,10,354,32,24);
            ui.trackerProgress=Text(tracker,"Progress","Correctly shelve 5 cars  0 / 5",18,45,354,40,18);
            ui.trackerFill=Bar(tracker,18,96,354,8);
            ui.trackerHint=Text(tracker,"Hint","Claim rewards at the quest board",18,109,354,24,15);
            var toast=Rect(root,"Quest Celebration",0,0,480,100); toast.anchorMin=toast.anchorMax=new Vector2(.5f,1); toast.pivot=new Vector2(.5f,1); toast.anchoredPosition=new Vector2(0,-95);
            Surface(toast,T.gold); ui.toast=toast.gameObject;
            ui.toastTitle=Text(toast,"Heading","QUEST COMPLETE",20,12,440,30,24); ui.toastTitle.alignment=TextAlignmentOptions.Center;
            ui.toastDetail=Text(toast,"Detail","First Display",20,48,440,34,24); ui.toastDetail.alignment=TextAlignmentOptions.Center;
            var dim=Rect(root,"Quest Board Window",0,0,1600,900); Stretch(dim); Surface(dim,new Color(T.navy.r,T.navy.g,T.navy.b,.78f));
            dim.gameObject.AddComponent<CollectorQuestPanel>().Configure(); ui.window=dim.gameObject;
            var frame=Rect(dim,"Frame",0,0,1100,760); frame.anchorMin=frame.anchorMax=new Vector2(.5f,.5f); frame.pivot=new Vector2(.5f,.5f); frame.anchoredPosition=Vector2.zero;
            Surface(frame,T.cream);
            var accent=Rect(frame,"Accent",0,0,1100,9); Surface(accent,T.gold);
            ui.heading=Text(frame,"Heading","COLLECTOR QUESTS",32,26,690,58,42);
            ui.subtitle=Text(frame,"Subtitle","Small goals. A spectacular collection.",34,88,680,34,23);
            ui.claimedLabel=Text(frame,"Claimed Count","0 / 240 quests claimed",34,136,680,28,20);
            var rank=Rect(frame,"Collector Rank",744,30,324,150); Surface(rank,T.navy);
            ui.rank=Text(rank,"Rank","Rookie",20,12,284,44,31); ui.rank.color=T.gold;
            ui.xp=Text(rank,"XP","0 / 100 XP",20,65,284,30,21); ui.xp.color=T.cream;
            ui.rankFill=Bar(rank,20,110,284,10);
            ui.journey=Button(frame,"Journey Tab","Journey",32,194,184,out ui.journeyLabel);
            ui.challenges=Button(frame,"Challenges Tab","Challenges",230,194,184,out ui.challengesLabel);
            ui.allQuests=Button(frame,"All Quests Tab","All Quests",428,194,184,out ui.allQuestsLabel);
            ui.completed=Button(frame,"Completed Tab","Completed",626,194,184,out ui.completedLabel);
            ui.close=Button(frame,"Close Quests","Close",918,194,150,out ui.closeLabel);
            ui.categoryButtons=new UnityEngine.UI.Button[7]; ui.categoryLabels=new TMP_Text[7];
            string[] categories={"All","Sorting","Discoveries","Shelves","Themes","Car Families","Upgrades"};
            for(int i=0;i<7;i++)
            {
                ui.categoryButtons[i]=Button(frame,"Category "+categories[i],categories[i],32+i*150,248,136,out ui.categoryLabels[i]);
                ((RectTransform)ui.categoryButtons[i].transform).sizeDelta=new Vector2(136,34);
                ui.categoryLabels[i].rectTransform.sizeDelta=new Vector2(120,34); ui.categoryLabels[i].fontSizeMax=18; ui.categoryLabels[i].fontSizeMin=12;
            }
            var scroll=Rect(frame,"Quest Scroll",30,300,1040,350); ui.scroll=scroll.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            var viewport=Rect(scroll,"Viewport",0,0,1040,350); Surface(viewport,Color.white); viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic=false;
            var content=Rect(viewport,"Content",0,0,1040,0); content.anchorMax=new Vector2(1,1); content.sizeDelta=Vector2.zero;
            var v=content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>(); v.spacing=12; v.childControlWidth=true; v.childControlHeight=true; v.childForceExpandWidth=true; v.childForceExpandHeight=false;
            var fitter=content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>(); fitter.verticalFit=UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            ui.content=content; ui.scroll.content=content; ui.scroll.viewport=viewport; ui.scroll.horizontal=false;
            ui.scroll.movementType=UnityEngine.UI.ScrollRect.MovementType.Clamped; ui.scroll.scrollSensitivity=32;
            ui.cardPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(CardPath).GetComponent<CollectorQuestCard>();
            ui.empty=Text(frame,"Empty State","Your claimed quests will appear here",80,382,940,120,30); ui.empty.alignment=TextAlignmentOptions.Center;
            ui.previousPage=Button(frame,"Previous Page","Previous",244,670,172,out ui.previousLabel);
            ui.nextPage=Button(frame,"Next Page","Next",684,670,172,out ui.nextLabel);
            ui.pageLabel=Text(frame,"Page Number","Page 1 / 20",426,670,248,44,20); ui.pageLabel.alignment=TextAlignmentOptions.Center;
            ui.footer=Text(frame,"Footer","No rush. No timers. Make the showroom yours.",32,720,1036,28,17);
            PrefabUtility.SaveAsPrefabAsset(root.gameObject,UIPath); Object.DestroyImmediate(root.gameObject);
        }

        static Material Material(string name,Color color)
        {
            string path=Folder+"/"+name+".mat"; var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m) {m=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m,path);}
            m.color=color; m.SetFloat("_Smoothness",.25f); EditorUtility.SetDirty(m); return m;
        }
        static Transform Cube(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name; go.transform.SetParent(parent,false);
            go.transform.localPosition=position; go.transform.localScale=scale;
            Object.DestroyImmediate(go.GetComponent<Collider>()); go.GetComponent<MeshRenderer>().sharedMaterial=material; return go.transform;
        }
        static TMP_Text Sign(Transform parent,string name,string value,Vector3 position,Vector2 size,float fontSize,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshPro)); go.transform.SetParent(parent,false);
            go.transform.localPosition=position; var t=go.GetComponent<TextMeshPro>(); t.rectTransform.sizeDelta=size;
            t.text=value; t.font=T.font; t.fontSize=fontSize; t.color=color; t.alignment=TextAlignmentOptions.Center;
            t.enableAutoSizing=true; t.fontSizeMax=fontSize; t.fontSizeMin=fontSize*.6f; return t;
        }
        static void BuildBoard()
        {
            PremiumCollectorBoardBuilder.Rebuild();
        }

        static void BindAudio()
        {
            foreach(string guid in AssetDatabase.FindAssets("t:GameplaySounds_SO"))
            {
                var s=AssetDatabase.LoadAssetAtPath<GameplaySounds_SO>(AssetDatabase.GUIDToAssetPath(guid));
                if(!s.QuestCompleteSFX.IsValid) s.QuestCompleteSFX=s.CompleteShelfSFX;
                if(!s.QuestClaimSFX.IsValid) s.QuestClaimSFX=s.GetRandomCollectSFX();
                EditorUtility.SetDirty(s);
            }
        }
    }
}
