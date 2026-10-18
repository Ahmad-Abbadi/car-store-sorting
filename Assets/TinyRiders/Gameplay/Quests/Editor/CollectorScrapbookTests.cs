using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using TinyRiders.Quests;
using Object=UnityEngine.Object;
namespace TinyRiders.Editor
{
    public class CollectorScrapbookTests
    {
        [Test] public void SpreadAndFeedback_AreBoundedAndNeverInterceptGameplay()
        {
            var root=PrefabUtility.LoadPrefabContents(CollectorQuestBuilder.UIPath);
            try {
                var ui=root.GetComponent<CollectorQuestUI>();var p=ui.scrapbook;
                Assert.NotNull(p);Assert.AreEqual(6,CollectorQuestPresentation.PageSize);
                Assert.AreEqual(3,p.grid.constraintCount);Assert.False(ui.scroll.enabled);
                Assert.AreEqual(6,p.categoryArt.Length);Assert.True(p.categoryArt.All(i=>i.texture));
                Assert.AreEqual(10,p.xpStamps.Length);Assert.AreEqual(p.wallet.rectTransform,ui.rewardFX.destination);
                Assert.True(ui.toast.GetComponentsInChildren<UnityEngine.UI.Graphic>(true).All(g=>!g.raycastTarget));
                Assert.False(ui.toast.GetComponent<CanvasGroup>().blocksRaycasts);
                Assert.NotNull(ui.cardPrefab.collectedStamp);Assert.NotNull(ui.cardPrefab.illustration);
                ui.rewardFX.Play(Vector3.zero,"test",true);Assert.AreEqual(28,ui.rewardFX.pieces.Count(i=>i.gameObject.activeSelf));
                p.CancelTransient();Assert.Zero(ui.rewardFX.banner.alpha);Assert.False(ui.rewardFX.pieces.Any(i=>i.gameObject.activeSelf));
            } finally {PrefabUtility.UnloadPrefabContents(root);}
        }
        [Test] public void Artwork_UsesTheQuestSubjectAndAllNewCopyIsLocalized()
        {
            var root=PrefabUtility.LoadPrefabContents(CollectorQuestBuilder.UIPath);
            try {
                var p=root.GetComponent<CollectorScrapbookPresenter>();var entry=p.artwork.entries.Last(e=>e.enabled&&e.preview);
                var q=new CollectorQuestView(new CollectorQuestRecord{category=CollectorQuestCategory.Themes,subjectId=entry.themeId});
                Assert.AreEqual(p.artwork.entries.First(e=>e.enabled&&e.preview&&e.themeId==entry.themeId).preview,p.Picture(q));
                var data=Resources.Load<RuntimeLocalizationData>("RuntimeLocalizationData");
                foreach(var locale in data.locales)foreach(var key in new[]{"title","board","collected","wallet","notification","rank_hint","batch"})
                    Assert.True(data.TryGet(locale.Identifier.Code,"quest.scrapbook."+key,out var value)&&!string.IsNullOrWhiteSpace(value),locale.Identifier.Code+key);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }
    public static class CollectorScrapbookQA
    {
        public static string LastCapture;
        public static void Capture(int width,int height,string language,string name)
        {CinematicMenuTests.SetGameSize(width,height);Localizer.SetLanguage(language);Object.FindFirstObjectByType<CollectorQuestUI>().StartCoroutine(Shot(name));}
        static IEnumerator Shot(string name)
        {
            yield return new WaitForSecondsRealtime(.6f);Canvas.ForceUpdateCanvases();
            var ui=Object.FindFirstObjectByType<CollectorQuestUI>();
            var overflow=ui.window.GetComponentsInChildren<TMPro.TMP_Text>().Where(t=>t.isTextOverflowing).Select(t=>t.name).ToArray();
            var frame=(RectTransform)ui.window.transform.Find("Frame");var corners=new Vector3[4];frame.GetWorldCorners(corners);
            bool fits=corners.All(v=>v.x>=0&&v.y>=0&&v.x<=Screen.width&&v.y<=Screen.height);
            LastCapture=name+": "+Screen.width+"x"+Screen.height+", fits="+fits+", overflow="+string.Join(",",overflow)+", cards="+ui.VisibleCardCount+", RTL="+Localizer.IsRtl;
            System.IO.File.AppendAllText("IntegrationEvidence/Scrapbook/layout-checks.txt",LastCapture+"\n");
            ScreenCapture.CaptureScreenshot("IntegrationEvidence/Scrapbook/"+name+".png");
        }
        public static void RestoreGameSize()
        {
            if(!SessionState.GetBool("CinematicMenu.SizeSaved",false))return;
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic;
            var type=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");type.GetProperty("selectedSizeIndex",flags).SetValue(EditorWindow.GetWindow(type),SessionState.GetInt("CinematicMenu.PreviousSize",0));
            var st=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSizes");var sizes=typeof(ScriptableSingleton<>).MakeGenericType(st).GetProperty("instance").GetValue(null);
            var gt=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSizeGroupType");var group=st.GetMethod("GetGroup").Invoke(sizes,new[]{System.Enum.Parse(gt,"Standalone")});
            int count=(int)group.GetType().GetMethod("GetCustomCount").Invoke(group,null);
            for(int i=count-1;i>=SessionState.GetInt("CinematicMenu.PreviousCustomCount",count);i--)group.GetType().GetMethod("RemoveCustomSize").Invoke(group,new object[]{i});
            SessionState.SetBool("CinematicMenu.SizeSaved",false);
        }
    }
}
