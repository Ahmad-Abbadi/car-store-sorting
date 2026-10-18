using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using TinyRiders.Quests;
using Object=UnityEngine.Object;
namespace TinyRiders.Editor
{
    public static partial class CollectorQuestBuilder
    {
        static readonly Color Paper=new Color(.97f,.93f,.81f), Ink=new Color(.22f,.27f,.25f), Coral=new Color(.78f,.32f,.23f), Teal=new Color(.19f,.43f,.4f);
        static void Place(Component c,float x,float y,float w,float h)
        {
            var r=(RectTransform)c.transform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);
        }
        static void Type(TMP_Text text,float size,Color color)
        {text.fontSize=text.fontSizeMax=size;text.fontSizeMin=size*.55f;text.color=color;text.enableAutoSizing=true;}
        static RectTransform PaperPart(Transform parent,string name,float x,float y,float w,float h,Color color)
        {
            var r=parent.Find(name) as RectTransform;
            if(!r)r=Rect(parent,name,x,y,w,h);else Place(r,x,y,w,h);
            var image=r.GetComponent<UnityEngine.UI.Image>();if(!image)image=Surface(r,color);
            image.color=color;image.raycastTarget=false;return r;
        }
        static UnityEngine.UI.RawImage Picture(Transform parent,string name,float x,float y,float w,float h)
        {
            var r=parent.Find(name) as RectTransform;if(!r)r=Rect(parent,name,x,y,w,h);else Place(r,x,y,w,h);
            var image=r.GetComponent<UnityEngine.UI.RawImage>() ?? r.gameObject.AddComponent<UnityEngine.UI.RawImage>();image.raycastTarget=false;return image;
        }
        static Sprite PaperTexture()
        {
            string path=Folder+"/Scrapbook Paper.asset";
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(!texture){
                texture=new Texture2D(128,128,TextureFormat.RGBA32,false);texture.name="Scrapbook paper grain";texture.wrapMode=TextureWrapMode.Repeat;
                AssetDatabase.CreateAsset(texture,path);
            }
            var colors=new Color[128*128];uint noise=9371;
            for(int i=0;i<colors.Length;i++){noise^=noise<<13;noise^=noise>>17;noise^=noise<<5;float shade=.99f+(noise%100)/10000f;colors[i]=new Color(shade,shade,shade,1);}
            texture.SetPixels(colors);texture.Apply();EditorUtility.SetDirty(texture);
            var sprite=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
            if(!sprite){sprite=Sprite.Create(texture,new Rect(0,0,128,128),Vector2.one*.5f,100);sprite.name="Paper";AssetDatabase.AddObjectToAsset(sprite,texture);}
            return sprite;
        }
        [MenuItem("Tiny Riders/Quests/Build Collector Scrapbook")]
        public static void Scrapbook()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode first");
            var paper=PaperTexture();
            var root=PrefabUtility.LoadPrefabContents(CardPath);var c=root.GetComponent<CollectorQuestCard>();
            root.GetComponent<RectTransform>().sizeDelta=new Vector2(620,150);
            root.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight=150;
            root.GetComponent<UnityEngine.UI.Image>().sprite=paper;
            root.GetComponent<UnityEngine.UI.Image>().type=UnityEngine.UI.Image.Type.Simple;
            Motion(root,false,true);Shadow(root);
            Place(c.stripe,8,12,4,126);Place(c.title,120,8,390,31);Type(c.title,23,Ink);
            Place(c.description,120,39,474,36);Type(c.description,17,Ink);
            Place(c.rewards,120,78,290,25);Type(c.rewards,18,Teal);
            Place(c.progress,15,118,90,23);Type(c.progress,16,Ink);c.progress.alignment=TextAlignmentOptions.Center;
            Place(c.fill.transform.parent,120,112,275,7);Place(c.fill,0,0,275,7);
            Place(c.state,120,126,275,19);Type(c.state,13,Teal);c.state.alignment=TextAlignmentOptions.MidlineLeft;
            Place(c.claim,416,108,90,32);Place(c.pin,514,108,90,32);Place(c.replace,407,108,99,32);
            foreach(var label in new[]{c.claimLabel,c.pinLabel,c.replaceLabel}){Place(label,3,0,84,32);Type(label,17,Ink);}
            c.claim.GetComponent<UnityEngine.UI.Image>().color=new Color(.93f,.68f,.27f);
            var highlight=root.transform.Find("Reward Highlight");if(highlight)Place(highlight,115,77,295,27);
            var backing=PaperPart(root.transform,"Sticker Backing",20,16,86,91,Color.white);backing.localRotation=Quaternion.Euler(0,0,-4);Shadow(backing.gameObject);
            c.illustration=Picture(root.transform,"Car Illustration",23,24,80,72);
            var tape=PaperPart(root.transform,"Tape",42,8,50,14,new Color(.88f,.72f,.36f,.6f));tape.localRotation=Quaternion.Euler(0,0,-9);
            var pin=PaperPart(root.transform,"Pushpin",595,3,12,12,Coral);pin.GetComponent<UnityEngine.UI.Image>().sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");c.pushpin=pin.gameObject;
            var seal=PaperPart(root.transform,"Achievement Seal",548,7,44,28,new Color(.94f,.68f,.24f));seal.localRotation=Quaternion.Euler(0,0,-8);c.seal=seal.gameObject;
            var oldCheck=seal.Find("Check");if(oldCheck)oldCheck.gameObject.SetActive(false);
            Picture(seal,"Medal",6,-6,32,40).texture=ScrapbookIcon(6);
            var stamp=PaperPart(root.transform,"Collected Stamp",210,45,270,48,new Color(.95f,.9f,.71f,.97f));stamp.localRotation=Quaternion.Euler(0,0,-6);Motion(stamp.gameObject,true);
            var outline=stamp.GetComponent<UnityEngine.UI.Outline>() ?? stamp.gameObject.AddComponent<UnityEngine.UI.Outline>();outline.effectColor=Teal;outline.effectDistance=new Vector2(2,-2);
            var stampText=stamp.Find("Text")?.GetComponent<TMP_Text>();if(!stampText)stampText=Text(stamp,"Text","COLLECTED",5,0,260,48,28);stampText.color=Teal;stampText.alignment=TextAlignmentOptions.Center;c.collectedStamp=stampText;stamp.gameObject.SetActive(false);
            foreach(var button in root.GetComponentsInChildren<UnityEngine.UI.Button>(true)){button.GetComponent<UnityEngine.UI.Image>().sprite=paper;button.GetComponent<UnityEngine.UI.Image>().type=UnityEngine.UI.Image.Type.Simple;}
            c.pin.GetComponent<UnityEngine.UI.Image>().color=new Color(.88f,.84f,.72f);
            if(highlight)highlight.GetComponent<UnityEngine.UI.Image>().color=new Color(.94f,.88f,.69f);
            PrefabUtility.SaveAsPrefabAsset(root,CardPath);PrefabUtility.UnloadPrefabContents(root);
            root=PrefabUtility.LoadPrefabContents(UIPath);var ui=root.GetComponent<CollectorQuestUI>();
            root.GetComponent<UnityEngine.UI.CanvasScaler>().screenMatchMode=UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
            var p=root.GetComponent<CollectorScrapbookPresenter>() ?? root.AddComponent<CollectorScrapbookPresenter>();ui.scrapbook=p;p.ui=ui;
            p.artwork=AssetDatabase.LoadAssetAtPath<ThemedCarCatalog>("Assets/TinyRiders/ThemedCollection/Data/Themes/ThemedCarCatalog.asset");
            var frame=(RectTransform)ui.window.transform.Find("Frame");frame.sizeDelta=new Vector2(1360,820);
            frame.GetComponent<UnityEngine.UI.Image>().color=new Color(.33f,.45f,.4f);
            foreach(string old in new[]{"Header Backdrop","Header Divider","Accent"}){var t=frame.Find(old);if(t)t.gameObject.SetActive(false);}
            for(int side=0;side<2;side++){
                var sheet=PaperPart(frame,"Album Page "+side,18+side*672,16,652,788,Paper);sheet.GetComponent<UnityEngine.UI.Image>().sprite=paper;sheet.SetSiblingIndex(side);
                var edge=PaperPart(frame,"Paper Edge "+side,23+side*672,797,642,3,new Color(.73f,.68f,.53f));edge.SetSiblingIndex(2);edge.gameObject.SetActive(false);
            }
            var binding=PaperPart(frame,"Stitched Binding",666,16,28,788,new Color(.61f,.53f,.38f));binding.SetSiblingIndex(3);
            for(int i=0;i<18;i++)PaperPart(binding,"Stitch "+i,5,12+i*43,18,3,new Color(.94f,.87f,.68f));
            Place(ui.heading,45,30,850,48);Type(ui.heading,36,Ink);
            Place(ui.subtitle,46,78,820,29);Type(ui.subtitle,20,Teal);
            Place(ui.claimedLabel,47,108,800,22);Type(ui.claimedLabel,17,Ink);
            var rank=ui.rank.transform.parent;Place(rank,1000,30,310,142);rank.GetComponent<UnityEngine.UI.Image>().color=new Color(.91f,.83f,.64f);
            Place(ui.rank,18,8,274,34);Type(ui.rank,26,Teal);Place(ui.xp,18,44,274,24);Type(ui.xp,17,Ink);
            Place(ui.rankFill.transform.parent,18,75,274,5);Place(ui.rankFill,0,0,274,5);
            p.xpStamps=new UnityEngine.UI.Image[10];for(int i=0;i<10;i++)p.xpStamps[i]=PaperPart(rank,"XP Stamp "+i,18+i*28,88,22,19,Teal).GetComponent<UnityEngine.UI.Image>();
            var wallet=rank.Find("Wallet")?.GetComponent<TMP_Text>();if(!wallet)wallet=Text(rank,"Wallet","0 coins",18,111,274,25,18);p.wallet=wallet;wallet.color=Ink;
            var tabs=new[]{ui.journey,ui.challenges,ui.allQuests,ui.completed};
            foreach(var b in tabs){Place(b,40+Array.IndexOf(tabs,b)*205,133,190,44);Place(b.transform.GetChild(0),5,0,180,44);}
            Place(ui.close,1188,180,122,40);Place(ui.closeLabel,4,0,114,40);Type(ui.closeLabel,18,Ink);
            // Move Close into the top gutter, keeping divider tabs a continuous row.
            Place(ui.close,864,133,118,40);
            p.categoryArt=new UnityEngine.UI.RawImage[6];
            for(int i=0;i<7;i++){
                Place(ui.categoryButtons[i],40+i*184,190,176,40);Place(ui.categoryLabels[i],i==0 ? 6 : 40,0,i==0 ? 164 : 130,40);Type(ui.categoryLabels[i],16,Ink);
                if(i>0){p.categoryArt[i-1]=Picture(ui.categoryButtons[i].transform,"Divider Car",5,6,31,28);p.categoryArt[i-1].texture=ScrapbookIcon(i-1);}
            }
            p.rankBadge=ScrapbookIcon(6);
            foreach(var button in frame.GetComponentsInChildren<UnityEngine.UI.Button>(true)){
                var image=button.GetComponent<UnityEngine.UI.Image>();image.sprite=paper;image.type=UnityEngine.UI.Image.Type.Simple;
                if(button==ui.previousPage || button==ui.nextPage) image.color=new Color(.86f,.81f,.66f);
            }
            Place(ui.scroll,40,246,1280,474);Place(ui.scroll.viewport,0,0,1280,474);
            ui.scroll.enabled=false;
            // Keep the viewport mask, replace only its list layout with a fixed reusable spread.
            var v=ui.content.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();if(v)Object.DestroyImmediate(v);
            var fit=ui.content.GetComponent<UnityEngine.UI.ContentSizeFitter>();if(fit)Object.DestroyImmediate(fit);
            Place(ui.content,0,0,1280,474);
            var grid=ui.content.GetComponent<UnityEngine.UI.GridLayoutGroup>() ?? ui.content.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();
            grid.cellSize=new Vector2(620,150);grid.spacing=new Vector2(40,12);grid.startAxis=UnityEngine.UI.GridLayoutGroup.Axis.Vertical;
            grid.constraint=UnityEngine.UI.GridLayoutGroup.Constraint.FixedRowCount;grid.constraintCount=3;p.grid=grid;p.spread=ui.content;
            ui.cardPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(CardPath).GetComponent<CollectorQuestCard>();
            Place(ui.empty,100,350,1160,110);Type(ui.empty,28,Ink);
            Place(ui.previousPage,180,746,170,40);Place(ui.previousLabel,4,0,162,40);
            Place(ui.nextPage,1010,746,170,40);Place(ui.nextLabel,4,0,162,40);
            Place(ui.pageLabel,510,746,340,40);Type(ui.pageLabel,19,Ink);
            Place(ui.footer,40,785,1280,21);Type(ui.footer,14,Teal);ui.footer.alignment=TextAlignmentOptions.Center;
            var leaf=PaperPart(frame,"Turning Page",680,240,620,480,new Color(.98f,.95f,.84f,.65f));leaf.pivot=new Vector2(0, .5f);leaf.anchoredPosition=new Vector2(680,-480);p.pageLeaf=leaf;leaf.gameObject.SetActive(false);
            var fx=(RectTransform)ui.rewardFX.transform;fx.sizeDelta=new Vector2(1360,820);fx.anchoredPosition=new Vector2(680,-410);
            ui.rewardFX.destination=p.wallet.rectTransform;
            Place(ui.rewardFX.banner,370,697,620,48);Place(ui.rewardFX.label,12,0,596,48);Type(ui.rewardFX.label,23,Ink);
            fx.SetAsLastSibling();
            // Corner notification uses the same earned-sticker language, outside the sorting crosshair.
            var toast=(RectTransform)ui.toast.transform;toast.anchorMin=toast.anchorMax=Vector2.zero;toast.pivot=Vector2.zero;toast.anchoredPosition=new Vector2(24,24);toast.sizeDelta=new Vector2(510,120);p.notification=toast;
            toast.GetComponent<UnityEngine.UI.Image>().color=Paper;toast.GetComponent<UnityEngine.UI.Image>().sprite=paper;
            var motion=toast.GetComponent<CollectorQuestMotion>();if(motion)motion.enabled=false;
            foreach(string old in new[]{"Confetti","Celebration Ribbon"}){var t=toast.Find(old);if(t)t.gameObject.SetActive(false);}
            Place(ui.toastTitle,104,12,390,24);Type(ui.toastTitle,17,Teal);ui.toastTitle.alignment=TextAlignmentOptions.MidlineLeft;
            Place(ui.toastDetail,104,38,390,44);Type(ui.toastDetail,22,Ink);ui.toastDetail.alignment=TextAlignmentOptions.MidlineLeft;
            p.notificationArt=Picture(toast,"Achievement Artwork",12,20,80,74);
            var hint=toast.Find("Board Reminder")?.GetComponent<TMP_Text>();if(!hint)hint=Text(toast,"Board Reminder","Reward ready at the board",104,85,390,24,16);p.notificationHint=hint;hint.color=Teal;
            p.stamp=PaperPart(toast,"Earned Stamp",12,8,34,8,Coral);
            foreach(var graphic in toast.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))graphic.raycastTarget=false;
            var group=toast.GetComponent<CanvasGroup>();if(!group)group=toast.gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
            // Pinned objective becomes a small paper bookmark.
            ui.tracker.GetComponent<UnityEngine.UI.Image>().color=Paper;
            PrefabUtility.SaveAsPrefabAsset(root,UIPath);PrefabUtility.UnloadPrefabContents(root);
            ScrapbookBoard(paper);
            AssetDatabase.SaveAssets();
        }
        static void ScrapbookBoard(Sprite paper)
        {
            PremiumCollectorBoardBuilder.Rebuild();
        }
    }
}
