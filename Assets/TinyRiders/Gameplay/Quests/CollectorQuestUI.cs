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
        public UnityEngine.UI.Button journey, challenges, completed, close;
        public TMP_Text journeyLabel, challengesLabel, completedLabel, closeLabel;
        public UnityEngine.UI.ScrollRect scroll;
        public bool IsOpen => window && window.activeSelf;
        CollectorQuestManager manager;
        readonly List<CollectorQuestCard> cards=new List<CollectorQuestCard>();
        readonly Queue<(string titleKey, string fallback, string detail)> messages=new Queue<(string,string,string)>();
        int tab;
        float toastRemaining;
        GoldenCarReveal reveal;

        void Awake()
        {
            window.SetActive(false); toast.SetActive(false); tracker.SetActive(false);
            journey.onClick.AddListener(()=>SelectTab(0)); challenges.onClick.AddListener(()=>SelectTab(1));
            completed.onClick.AddListener(()=>SelectTab(2)); close.onClick.AddListener(Close);
        }
        void Start()
        {
            manager=CollectorQuestManager.Instance;
            reveal=FindFirstObjectByType<GoldenCarReveal>();
            if(manager) {manager.Changed+=Refresh; manager.Completed+=OnCompleted; manager.RankedUp+=OnRankedUp;}
            Localizer.LanguageChanged+=Refresh; Refresh();
        }
        void OnDisable() { Close(); PauseHandler.RemoveInterceptor(InterceptPause); }
        void OnDestroy()
        {
            if(manager) {manager.Changed-=Refresh; manager.Completed-=OnCompleted; manager.RankedUp-=OnRankedUp;}
            Localizer.LanguageChanged-=Refresh; PauseHandler.RemoveInterceptor(InterceptPause);
        }
        public void Open()
        {
            if(!manager || !manager.Ready || IsOpen) return;
            var flow=GameFlowManager.Instance;
            if(flow && flow.currentState!=GameFlowManager.GameState.Playing) return;
            window.SetActive(true); PauseHandler.AddInterceptor(InterceptPause); Refresh();
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(journey.gameObject);
        }
        public void Close()
        {
            if(window) window.SetActive(false);
            PauseHandler.RemoveInterceptor(InterceptPause);
        }
        bool InterceptPause() {if(!IsOpen) return false; Close(); return true;}
        void SelectTab(int value) {tab=value; Refresh(); scroll.verticalNormalizedPosition=1;}
        public void Claim(string id)
        {
            if(!IsOpen || !manager) return;
            var q=manager.Quests.FirstOrDefault(q=>q.Id==id);
            if(q!=null && manager.Claim(id))
            {
                var audio=SimpleAudioManager.instance;
                if(audio && audio.GameplaySounds) audio.PlayUI(audio.GameplaySounds.QuestClaimSFX);
            }
        }
        void Refresh()
        {
            if(!manager) return;
            heading.SetLocalized(Localizer.GetOr("quest.ui.title","COLLECTOR QUESTS"));
            subtitle.SetLocalized(Localizer.GetOr("quest.ui.subtitle","Small goals. A spectacular collection."));
            rank.SetLocalized(CollectorQuestManager.RankName(manager.Rank));
            int[] levels={0,100,250,500,1000}; int current=levels[manager.Rank];
            int next=manager.Rank<4 ? levels[manager.Rank+1] : current;
            xp.SetLocalized(manager.Rank<4 ? manager.XP+" / "+next+" XP" : manager.XP+" XP");
            rankFill.fillAmount=next>current ? (float)(manager.XP-current)/(next-current) : 1;
            journeyLabel.SetLocalized(Localizer.GetOr("quest.tab.journey","Journey"));
            challengesLabel.SetLocalized(Localizer.GetOr("quest.tab.challenges","Challenges"));
            completedLabel.SetLocalized(Localizer.GetOr("quest.tab.completed","Completed"));
            closeLabel.SetLocalized(Localizer.GetOr("quest.close","Close"));
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
            journey.GetComponent<UnityEngine.UI.Image>().color=tab==0 ? theme.gold : theme.muted;
            challenges.GetComponent<UnityEngine.UI.Image>().color=tab==1 ? theme.gold : theme.muted;
            completed.GetComponent<UnityEngine.UI.Image>().color=tab==2 ? theme.gold : theme.muted;
            var visible=manager.Quests.Where(q=>tab==2 ? q.Claimed : !q.Claimed && q.Challenge==(tab==1)).ToArray();
            for(int i=cards.Count;i<visible.Length;i++) cards.Add(Instantiate(cardPrefab,content));
            for(int i=0;i<cards.Count;i++)
            {
                cards[i].gameObject.SetActive(i<visible.Length);
                if(i<visible.Length) cards[i].Bind(this,manager,visible[i]);
            }
            empty.gameObject.SetActive(visible.Length==0);
            empty.SetLocalized(Localizer.GetOr(tab==1 ? "quest.empty.challenges" : tab==2 ? "quest.empty.completed" : "quest.empty.journey",
                tab==1 ? "All challenges complete for this store" : tab==2 ? "Your claimed quests will appear here" : "Collector journey complete!"));
        }
        void OnCompleted(CollectorQuestView q) => messages.Enqueue(("quest.toast.complete","QUEST COMPLETE",manager.Title(q)));
        void OnRankedUp(int r) => messages.Enqueue(("quest.toast.rank","COLLECTOR RANK UP",CollectorQuestManager.RankName(r)));
        void Update()
        {
            if(!manager) return;
            bool playing=GameFlowManager.Instance && GameFlowManager.Instance.currentState==GameFlowManager.GameState.Playing;
            bool allowed=playing && !IsOpen && !(reveal && reveal.IsRevealing);
            tracker.SetActive(allowed && manager.PinnedId!=null);
            if(!allowed) {toast.SetActive(false); return;}
            if(toastRemaining<=0 && messages.Count>0)
            {
                var m=messages.Dequeue(); toastTitle.SetLocalized(Localizer.GetOr(m.titleKey,m.fallback)); toastDetail.SetLocalized(m.detail);
                toastRemaining=3.5f;
                var audio=SimpleAudioManager.instance;
                if(audio && audio.GameplaySounds) audio.PlayUI(audio.GameplaySounds.QuestCompleteSFX);
            }
            toast.SetActive(toastRemaining>0); toastRemaining-=Time.unscaledDeltaTime;
        }
    }
}
