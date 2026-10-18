using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
namespace TinyRiders.Quests
{
    /// <summary>Scrapbook visuals only; progress and reward delivery remain owned by the manager.</summary>
    public sealed class CollectorScrapbookPresenter : MonoBehaviour
    {
        public CollectorQuestUI ui;
        public ThemedCarCatalog artwork;
        public RectTransform spread, pageLeaf, notification;
        public UnityEngine.UI.GridLayoutGroup grid;
        public TMP_Text wallet, notificationHint;
        public UnityEngine.UI.RawImage notificationArt;
        public UnityEngine.UI.Image[] xpStamps;
        public UnityEngine.UI.RawImage[] categoryArt;
        public Texture rankBadge;
        public Transform stamp;
        readonly Dictionary<string,Texture> pictures=new Dictionary<string,Texture>();
        Texture[] fallback;
        float turnAge=1, rankTarget, displayedRank;
        int turnDirection=1;
        bool rankInitialized;
        public Texture Picture(CollectorQuestView q)
        {
            EnsureArtwork();
            if(q!=null) {
                string key=q.Category==CollectorQuestCategory.CarFamilies ? "family:"+q.SubjectId : "theme:"+(string.IsNullOrEmpty(q.SubjectId) ? q.ThemeId : q.SubjectId);
                if(pictures.TryGetValue(key,out var texture)) return texture;
            }
            return fallback[(int)(q?.Category ?? CollectorQuestCategory.Sorting)];
        }
        void EnsureArtwork()
        {
            if(fallback!=null) return;
            fallback=new Texture[6];
            if(!artwork) return;
            var entries=artwork.entries.Where(e=>e.enabled && e.preview).ToArray();
            for(int i=0;i<6 && entries.Length>0;i++) fallback[i]=entries[(i*entries.Length/6)%entries.Length].preview;
            foreach(var e in artwork.entries.Where(e=>e.enabled && e.preview)) {
                pictures.TryAdd("theme:"+e.themeId,e.preview); pictures.TryAdd("family:"+e.baseCarId,e.preview);
            }
        }
        public void RefreshLanguage()
        {
            EnsureArtwork();
            bool rtl=Localizer.IsRtl;
            grid.startCorner=rtl ? UnityEngine.UI.GridLayoutGroup.Corner.UpperRight : UnityEngine.UI.GridLayoutGroup.Corner.UpperLeft;
            var prev=(RectTransform)ui.previousPage.transform; var next=(RectTransform)ui.nextPage.transform;
            prev.anchoredPosition=new Vector2(rtl ? 1010 : 180,-746);next.anchoredPosition=new Vector2(rtl ? 180 : 1010,-746);
            for(int i=0;i<ui.categoryButtons.Length;i++) ((RectTransform)ui.categoryButtons[i].transform).anchoredPosition=new Vector2(40+(rtl ? 6-i : i)*184,-190);
            var tabs=new[]{ui.journey,ui.challenges,ui.allQuests,ui.completed};
            for(int i=0;i<4;i++) ((RectTransform)tabs[i].transform).anchoredPosition=new Vector2(40+(rtl ? 3-i : i)*205,-133);
            notificationHint.SetLocalized(Localizer.GetOr("quest.scrapbook.notification","Reward ready at the board"));
            wallet.SetLocalized(Localizer.FormatText(Localizer.GetOr("quest.scrapbook.wallet","{0} coins"),CoinsManager.Instance ? CoinsManager.Instance.Coins : 0));
        }
        public void OpenAlbum() {CancelTransient(); TurnPage(1);}
        public void TurnPage(int direction)
        {
            CancelTransient(); turnDirection=(Localizer.IsRtl ? -1 : 1)*direction; turnAge=0;
            pageLeaf.gameObject.SetActive(true);
        }
        public void ConfirmClaim(Vector3 origin,CollectorQuestView quest)
        {
            ui.rewardFX.Play(origin,Localizer.FormatText(Localizer.GetOr("quest.rewards","{0} coins  +  {1} XP"),quest.Coins,quest.XP),true);
            wallet.SetLocalized(Localizer.FormatText(Localizer.GetOr("quest.scrapbook.wallet","{0} coins"),CoinsManager.Instance.Coins));
        }
        public void SetRankProgress(float value)
        {rankTarget=value; if(!rankInitialized){displayedRank=value;rankInitialized=true;}}
        public void ShowAchievement(CollectorQuestView quest,bool rankUp)
        {
            notificationArt.texture=rankUp ? rankBadge : quest!=null ? categoryArt[(int)quest.Category].texture : rankBadge;
            notificationHint.SetLocalized(Localizer.GetOr(rankUp ? "quest.scrapbook.rank_hint" : "quest.scrapbook.notification",rankUp ? "Your collector card has a new badge!" : "Reward ready at the board"));
        }
        public void AnimateNotification(float remaining)
        {
            float elapsed=4-remaining;
            float slide=1-Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.32f));
            slide=Mathf.Max(slide,1-Mathf.Clamp01(remaining/.3f));
            notification.anchoredPosition=new Vector2(24-550*slide,24);
            float t=Mathf.Clamp01(elapsed/.45f);
            stamp.localScale=Vector3.one*(1+.22f*Mathf.Sin(t*Mathf.PI));
        }
        public void CancelTransient()
        {
            if(pageLeaf) {pageLeaf.gameObject.SetActive(false);pageLeaf.localScale=Vector3.one;}
            turnAge=1;
            if(ui && ui.rewardFX) ui.rewardFX.Cancel();
        }
        void OnDisable()=>CancelTransient();
        void Update()
        {
            if(turnAge<.3f) {
                turnAge+=Time.unscaledDeltaTime;
                float t=Mathf.Clamp01(turnAge/.3f);
                pageLeaf.localScale=new Vector3(1-Mathf.SmoothStep(0,1,t),1,1);
                pageLeaf.localRotation=Quaternion.Euler(0,0,turnDirection*(1-t)*2);
                if(t>=1)pageLeaf.gameObject.SetActive(false);
            }
            displayedRank=Mathf.MoveTowards(displayedRank,rankTarget,Time.unscaledDeltaTime*.75f);
            ui.rankFill.fillAmount=displayedRank;
            for(int i=0;i<xpStamps.Length;i++) xpStamps[i].color=(i+.5f)/xpStamps.Length<=displayedRank ? new Color(.21f,.52f,.43f) : new Color(.62f,.58f,.43f,.25f);
        }
    }
}
