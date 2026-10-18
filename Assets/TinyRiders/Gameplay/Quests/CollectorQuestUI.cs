using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace TinyRiders.Quests
{
    public sealed class CollectorQuestUI : MonoBehaviour
    {
        public GameObject window, tracker, toast;
        public RectTransform content;
        public CollectorQuestCard cardPrefab;
        public TMP_Text heading, subtitle, rank, xp, footer, empty, trackerTitle, trackerProgress, trackerHint, toastTitle, toastDetail;
        public UnityEngine.UI.Image rankFill, trackerFill;
        public UnityEngine.UI.Button journey, challenges, completed, close, allQuests, previousPage, nextPage;
        public TMP_Text journeyLabel, challengesLabel, completedLabel, closeLabel, allQuestsLabel, previousLabel, nextLabel, pageLabel, claimedLabel;
        public UnityEngine.UI.Button[] categoryButtons;
        public TMP_Text[] categoryLabels;
        public UnityEngine.UI.ScrollRect scroll;
        public CollectorQuestRewardFX rewardFX;
        public CollectorScrapbookPresenter scrapbook;
        bool deliveringClaim;
        float claimHold;
        public bool IsOpen => window && window.activeSelf;
        CollectorQuestManager manager;
        readonly List<CollectorQuestCard> cards=new List<CollectorQuestCard>();
        CollectorQuestTab tab;
        int page, category=-1;
        int pendingCompletions;
        string singleCompletedId;
        int pendingRank=-1;
        public int CurrentPage => page;
        public int PageCount { get; private set; } = 1;
        public int VisibleCardCount => cards.Count(c=>c.gameObject.activeSelf);
        float toastRemaining;
        GoldenCarReveal reveal;

        void Awake()
        {
            window.SetActive(false); toast.SetActive(false); tracker.SetActive(false);
            journey.onClick.AddListener(()=>SelectTab(CollectorQuestTab.Journey)); challenges.onClick.AddListener(()=>SelectTab(CollectorQuestTab.Challenges));
            completed.onClick.AddListener(()=>SelectTab(CollectorQuestTab.Completed)); close.onClick.AddListener(Close);
            allQuests.onClick.AddListener(()=>SelectTab(CollectorQuestTab.AllQuests));
            previousPage.onClick.AddListener(()=>ChangePage(-1)); nextPage.onClick.AddListener(()=>ChangePage(1));
            for(int i=0;i<categoryButtons.Length;i++) {int value=i-1; categoryButtons[i].onClick.AddListener(()=>SelectCategory(value));}
        }
        void Start()
        {
            manager=CollectorQuestManager.Instance;
            reveal=FindFirstObjectByType<GoldenCarReveal>();
            if(manager) {manager.Changed+=Refresh; manager.CompletionBatch+=OnCompleted; manager.RankedUp+=OnRankedUp;}
            Localizer.LanguageChanged+=Refresh; Refresh();
        }
        void OnDisable() { Close(); PauseHandler.RemoveInterceptor(InterceptPause); }
        void OnDestroy()
        {
            if(manager) {manager.Changed-=Refresh; manager.CompletionBatch-=OnCompleted; manager.RankedUp-=OnRankedUp;}
            Localizer.LanguageChanged-=Refresh; PauseHandler.RemoveInterceptor(InterceptPause);
        }
        public void Open()
        {
            if(!manager || !manager.Ready || IsOpen) return;
            var flow=GameFlowManager.Instance;
            if(flow && flow.currentState!=GameFlowManager.GameState.Playing) return;
            window.SetActive(true); PauseHandler.AddInterceptor(InterceptPause); Refresh();
            if(scrapbook) scrapbook.OpenAlbum();
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(journey.gameObject);
        }
        public void Close()
        {
            claimHold=0; if(scrapbook) scrapbook.CancelTransient();
            if(window) window.SetActive(false);
            PauseHandler.RemoveInterceptor(InterceptPause);
            if(manager) Refresh();
        }
        bool InterceptPause() {if(!IsOpen) return false; Close(); return true;}
        public void SelectTab(CollectorQuestTab value) {claimHold=0; tab=value; page=0; Refresh(); if(scrapbook) scrapbook.TurnPage(1);}
        public void SelectCategory(int value) {claimHold=0; category=Mathf.Clamp(value,-1,5); page=0; Refresh(); if(scrapbook) scrapbook.TurnPage(1);}
        public void ChangePage(int delta) {claimHold=0; page=Mathf.Clamp(page+delta,0,PageCount-1); Refresh(); if(scrapbook) scrapbook.TurnPage(delta);}
        public void Claim(string id)
        {
            if(!IsOpen || !manager || claimHold>0 || deliveringClaim) return;
            var q=manager.Quests.FirstOrDefault(q=>q.Id==id);
            var source=cards.FirstOrDefault(c=>c.gameObject.activeSelf && c.QuestId==id);
            Vector3 origin=source ? source.claim.transform.position : rank.transform.position;
            bool success=false;
            deliveringClaim=true;
            try {success=q!=null && manager.Claim(id);} finally {deliveringClaim=false;}
            if(success)
            {
                claimHold=.7f;
                if(source) source.ShowCollected();
                if(scrapbook) scrapbook.ConfirmClaim(origin,q);
                else if(rewardFX) rewardFX.Play(origin,Localizer.FormatText(Localizer.GetOr("quest.rewards","{0} coins  +  {1} XP"),q.Coins,q.XP),true);
                var audio=SimpleAudioManager.instance;
                if(audio && audio.GameplaySounds) audio.PlayUI(audio.GameplaySounds.QuestClaimSFX);
            }
        }
        void Refresh()
        {
            if(!manager || deliveringClaim || claimHold>0) return;
            heading.SetLocalized(Localizer.GetOr("quest.scrapbook.title","MY COLLECTOR SCRAPBOOK"));
            subtitle.SetLocalized(Localizer.GetOr("quest.ui.subtitle","Small goals. A spectacular collection."));
            rank.SetLocalized(CollectorQuestManager.RankName(manager.Rank));
            var levels=CollectorQuestProgress.RankThresholds; int current=levels[manager.Rank];
            int next=manager.Rank<levels.Length-1 ? levels[manager.Rank+1] : current;
            xp.SetLocalized(manager.Rank<levels.Length-1 ? manager.XP+" / "+next+" XP" : manager.XP+" XP");
            float rankProgress=next>current ? (float)(manager.XP-current)/(next-current) : 1;
            if(scrapbook) {scrapbook.SetRankProgress(rankProgress);scrapbook.RefreshLanguage();} else rankFill.fillAmount=rankProgress;
            journeyLabel.SetLocalized(Localizer.GetOr("quest.tab.journey","Journey"));
            challengesLabel.SetLocalized(Localizer.GetOr("quest.tab.challenges","Challenges"));
            completedLabel.SetLocalized(Localizer.GetOr("quest.tab.completed","Completed"));
            closeLabel.SetLocalized(Localizer.GetOr("quest.close","Close"));
            allQuestsLabel.SetLocalized(Localizer.GetOr("quest.tab.all","All Quests"));
            previousLabel.SetLocalized(Localizer.GetOr("quest.page.previous","Previous"));
            nextLabel.SetLocalized(Localizer.GetOr("quest.page.next","Next"));
            string[] categoryNames={"All","Sorting","Discoveries","Shelves","Themes","Car Families","Upgrades"};
            for(int i=0;i<categoryLabels.Length;i++) categoryLabels[i].SetLocalized(Localizer.GetOr("quest.category."+(i-1),categoryNames[i]));
            claimedLabel.SetLocalized(Localizer.FormatText(Localizer.GetOr("quest.claimed_count","{0} / {1} quests claimed"),
                manager.Quests.Count(q=>!q.Challenge && q.Claimed),manager.Quests.Count(q=>!q.Challenge && q.Available)));
            footer.SetLocalized(Localizer.GetOr("quest.ui.footer","No rush. No timers. Make the showroom yours."));
            trackerHint.SetLocalized(Localizer.GetOr("quest.tracker.hint","Claim rewards at the quest board"));
            var pinned=manager.Quests.FirstOrDefault(q=>q.Id==manager.PinnedId && !q.Claimed);
            tracker.SetActive(pinned!=null && !IsOpen);
            if(pinned!=null)
            {
                trackerTitle.SetLocalized(manager.Title(pinned));
                trackerProgress.SetLocalized(pinned.Complete ? Localizer.GetOr("quest.ready","READY TO CLAIM") : manager.Description(pinned)+"  "+pinned.Progress+" / "+pinned.Target);
                trackerFill.fillAmount=pinned.Target>0 ? (float)pinned.Progress/pinned.Target : 0;
            }
            if(!IsOpen) return;
            var theme=ToyShop.UI.ToyShopTheme.Current;
            journey.GetComponent<UnityEngine.UI.Image>().color=tab==CollectorQuestTab.Journey ? new Color(.89f,.68f,.31f) : new Color(.86f,.81f,.66f);
            challenges.GetComponent<UnityEngine.UI.Image>().color=tab==CollectorQuestTab.Challenges ? new Color(.89f,.68f,.31f) : new Color(.86f,.81f,.66f);
            completed.GetComponent<UnityEngine.UI.Image>().color=tab==CollectorQuestTab.Completed ? new Color(.89f,.68f,.31f) : new Color(.86f,.81f,.66f);
            allQuests.GetComponent<UnityEngine.UI.Image>().color=tab==CollectorQuestTab.AllQuests ? new Color(.89f,.68f,.31f) : new Color(.86f,.81f,.66f);
            for(int i=0;i<categoryButtons.Length;i++) categoryButtons[i].GetComponent<UnityEngine.UI.Image>().color=i==category+1 ? new Color(.22f,.46f,.4f) : new Color(.88f,.84f,.72f);
            for(int i=0;i<categoryLabels.Length;i++) categoryLabels[i].color=i==category+1 ? Color.white : theme.navy;
            var selected=CollectorQuestPresentation.Select(manager.Quests,tab,category<0 ? (CollectorQuestCategory?)null : (CollectorQuestCategory)category);
            PageCount=CollectorQuestPresentation.PageCount(selected.Length); page=Mathf.Clamp(page,0,PageCount-1);
            var visible=CollectorQuestPresentation.Page(selected,page);
            pageLabel.SetLocalized(Localizer.FormatText(Localizer.GetOr("quest.page.status","Page {0} / {1}"),page+1,PageCount));
            previousPage.interactable=page>0; nextPage.interactable=page<PageCount-1;
            for(int i=cards.Count;i<visible.Length;i++) cards.Add(Instantiate(cardPrefab,content));
            for(int i=0;i<cards.Count;i++)
            {
                cards[i].gameObject.SetActive(i<visible.Length);
                if(i<visible.Length) cards[i].Bind(this,manager,visible[i]);
            }
            empty.gameObject.SetActive(visible.Length==0);
            empty.SetLocalized(Localizer.GetOr(category>=0 ? "quest.empty.category" : tab==CollectorQuestTab.Challenges ? "quest.empty.challenges" : tab==CollectorQuestTab.Completed ? "quest.empty.completed" : "quest.empty.journey",
                category>=0 ? "No quests in this category yet" : tab==CollectorQuestTab.Challenges ? "All challenges complete for this store" : tab==CollectorQuestTab.Completed ? "Your claimed quests will appear here" : "Collector journey complete!"));
        }
        void OnCompleted(IReadOnlyList<CollectorQuestView> quests)
        {
            pendingCompletions+=quests.Count;
            singleCompletedId=pendingCompletions==1 ? quests[0].Id : null;
        }
        void OnRankedUp(int r) => pendingRank=Mathf.Max(pendingRank,r);
        void Update()
        {
            if(!manager) return;
            if(claimHold>0) {claimHold-=Time.unscaledDeltaTime; if(claimHold<=0) Refresh();}
            bool playing=GameFlowManager.Instance && GameFlowManager.Instance.currentState==GameFlowManager.GameState.Playing;
            bool allowed=playing && !IsOpen && !(reveal && reveal.IsRevealing);
            tracker.SetActive(allowed && manager.PinnedId!=null);
            if(!allowed) {toast.SetActive(false); return;}
            if(toastRemaining<=0 && pendingRank>=0)
            {
                toastTitle.SetLocalized(Localizer.GetOr("quest.toast.rank","COLLECTOR RANK UP"));
                toastDetail.SetLocalized(CollectorQuestManager.RankName(pendingRank)); pendingRank=-1;
                toastRemaining=4f;
                if(scrapbook) scrapbook.ShowAchievement(null,true);
                var audio=SimpleAudioManager.instance;
                if(audio && audio.GameplaySounds) audio.PlayUI(audio.GameplaySounds.QuestCompleteSFX);
            }
            else if(toastRemaining<=0 && pendingCompletions>0)
            {
                toastTitle.SetLocalized(Localizer.GetOr("quest.toast.complete","QUEST COMPLETE"));
                var single=manager.Quests.FirstOrDefault(q=>q.Id==singleCompletedId);
                toastDetail.SetLocalized(pendingCompletions==1 && single!=null ? manager.Title(single) :
                    Localizer.FormatText(Localizer.GetOr("quest.scrapbook.batch","{0} new achievements!"),pendingCompletions));
                pendingCompletions=0; singleCompletedId=null; toastRemaining=4f;
                if(scrapbook) scrapbook.ShowAchievement(single,false);
                var audio=SimpleAudioManager.instance;
                if(audio && audio.GameplaySounds) audio.PlayUI(audio.GameplaySounds.QuestCompleteSFX);
            }
            toast.SetActive(toastRemaining>0); if(scrapbook && toastRemaining>0) scrapbook.AnimateNotification(toastRemaining); toastRemaining-=Time.unscaledDeltaTime;
        }
    }
}
