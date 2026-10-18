using TMPro;
using UnityEngine;

namespace TinyRiders.Quests
{
    public sealed class CollectorQuestBoard : MonoBehaviour, IInteractable
    {
        public CollectorQuestUI interfaceUI;
        public TMP_Text title, subtitle, status;
        public GameObject rewardIndicator;
        CollectorQuestManager manager;
        public string ActionPrompt => Localizer.GetOr("quest.board.open", "Collector Quests");
        public void OnInteract(InteractionHandler handler) { if(interfaceUI) interfaceUI.Open(); }
        void Start()
        {
            manager=CollectorQuestManager.Instance;
            if(manager) manager.Changed+=Refresh;
            Localizer.LanguageChanged+=Refresh; Refresh();
        }
        void OnDestroy()
        { if(manager) manager.Changed-=Refresh; Localizer.LanguageChanged-=Refresh; }
        void Refresh()
        {
            title.SetLocalized(Localizer.GetOr("quest.board.title","COLLECTOR\nQUESTS").Replace("\\n","\n"));
            subtitle.SetLocalized(Localizer.GetOr("quest.board.subtitle","SORT • DISCOVER • GROW"));
            int count=manager ? manager.ClaimableCount : 0;
            rewardIndicator.SetActive(count>0);
            status.SetLocalized(count>0 ? Localizer.FormatText(Localizer.GetOr("quest.board.ready","{0} rewards ready!"),count) :
                Localizer.GetOr("quest.board.invite","Your next goal starts here"));
        }
    }

}
