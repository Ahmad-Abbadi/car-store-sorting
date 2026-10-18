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
        static void Motion(GameObject go,bool entrance=false,bool hover=false,bool badge=false)
        {var m=go.GetComponent<CollectorQuestMotion>() ?? go.AddComponent<CollectorQuestMotion>(); m.entrance=entrance;m.hover=hover;m.floatBadge=badge;}
        static void Shadow(GameObject go)
        {var s=go.GetComponent<UnityEngine.UI.Shadow>() ?? go.AddComponent<UnityEngine.UI.Shadow>(); s.effectColor=new Color(.04f,.1f,.2f,.16f); s.effectDistance=new Vector2(0,-4);}
        static void Accent(Transform parent,string name,float x,float y,float w,float h,Color c)
        {var child=parent.Find(name); if(!child) Surface(Rect(parent,name,x,y,w,h),c);}
        [MenuItem("Tiny Riders/Quests/Polish Board and Celebrations")]
        public static void Polish()
        {
            if(EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play mode first.");
            var root=PrefabUtility.LoadPrefabContents(CardPath);
            var card=root.GetComponent<CollectorQuestCard>();
            Shadow(root); Motion(root,true);
            card.rewards.color=new Color(.08f,.39f,.48f);
            Accent(root.transform,"Reward Highlight",16,86,480,32,new Color(.9f,.97f,.96f));
            root.transform.Find("Reward Highlight").SetAsFirstSibling();
            foreach(var b in root.GetComponentsInChildren<UnityEngine.UI.Button>(true)){Motion(b.gameObject,false,true);Shadow(b.gameObject);}
            PrefabUtility.SaveAsPrefabAsset(root,CardPath); PrefabUtility.UnloadPrefabContents(root);
            root=PrefabUtility.LoadPrefabContents(UIPath);
            var ui=root.GetComponent<CollectorQuestUI>(); var frame=ui.window.transform.Find("Frame");
            Motion(frame.gameObject,true); Shadow(frame.gameObject);
            frame.GetComponent<UnityEngine.UI.Image>().color=new Color(.96f,.97f,.99f);
            Accent(frame,"Header Backdrop",0,9,1100,174,T.navy); frame.Find("Header Backdrop").SetSiblingIndex(1);
            ui.heading.color=Color.white; ui.subtitle.color=new Color(.7f,.86f,.96f); ui.claimedLabel.color=T.gold;
            ui.rank.transform.parent.GetComponent<UnityEngine.UI.Image>().color=new Color(.12f,.24f,.36f);
            Accent(frame,"Header Divider",32,180,1036,3,T.gold);
            foreach(var b in root.GetComponentsInChildren<UnityEngine.UI.Button>(true)){Motion(b.gameObject,false,true);Shadow(b.gameObject);}
            Motion(ui.toast,true); Shadow(ui.toast); Motion(ui.tracker,true); Shadow(ui.tracker);
            Accent(ui.toast.transform,"Celebration Ribbon",0,0,480,5,T.mint);
            if(!ui.toast.transform.Find("Confetti"))
            {
                var confetti=Rect(ui.toast.transform,"Confetti",240,50,0,0);
                var burst=confetti.gameObject.AddComponent<CollectorQuestRewardFX>();burst.celebrateOnEnable=true;
                burst.pieces=new UnityEngine.UI.Image[24];
                for(int i=0;i<24;i++) {
                    var p=Rect(confetti,"Confetti "+i,0,0,7,12);p.anchorMin=p.anchorMax=p.pivot=new Vector2(.5f,.5f);
                    burst.pieces[i]=Surface(p,i%3==0 ? T.mint : i%3==1 ? T.gold : T.blue);burst.pieces[i].raycastTarget=false;
                }
            }
            var fxRoot=frame.Find("Reward Effects");
            if(!fxRoot)
            {
                var r=Rect(frame,"Reward Effects",0,0,1100,760); r.pivot=new Vector2(.5f,.5f); r.anchoredPosition=new Vector2(550,-380);
                var fx=r.gameObject.AddComponent<CollectorQuestRewardFX>(); ui.rewardFX=fx; fx.destination=(RectTransform)ui.rank.transform;
                var banner=Rect(r,"Reward Banner",240,605,620,58); Surface(banner,T.gold); Shadow(banner.gameObject);
                fx.banner=banner.gameObject.AddComponent<CanvasGroup>();fx.banner.alpha=0;fx.banner.blocksRaycasts=false;fx.banner.interactable=false;
                fx.label=Text(banner,"Amount","",16,0,588,58,26);fx.label.alignment=TextAlignmentOptions.Center;
                fx.pieces=new UnityEngine.UI.Image[28];
                var sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                for(int i=0;i<fx.pieces.Length;i++)
                {
                    var p=Rect(r,"Reward Spark "+i,0,0,14,14);p.anchorMin=p.anchorMax=p.pivot=new Vector2(.5f,.5f);
                    var image=Surface(p,i%3==0 ? T.mint : i%3==1 ? T.gold : T.blue); image.sprite=sprite;image.raycastTarget=false;
                    fx.pieces[i]=image;p.gameObject.SetActive(false);
                }
            }
            // Decorative overlay must never intercept card/button input.
            foreach(var g in frame.Find("Reward Effects").GetComponentsInChildren<UnityEngine.UI.Graphic>(true)) g.raycastTarget=false;
            PrefabUtility.SaveAsPrefabAsset(root,UIPath); PrefabUtility.UnloadPrefabContents(root);
            PremiumCollectorBoardBuilder.Rebuild();
            AssetDatabase.SaveAssets();
        }
    }
}
