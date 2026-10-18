using TMPro;
using UnityEngine;

namespace TinyRiders.Quests
{
    public sealed class CollectorQuestCard : MonoBehaviour
    {
        public TMP_Text title, description, rewards, progress, state, pinLabel, claimLabel, replaceLabel;
        public UnityEngine.UI.Image fill, stripe;
        public UnityEngine.UI.Button pin, claim, replace;
        public UnityEngine.UI.RawImage illustration;
        public GameObject pushpin, seal;
        public TMP_Text collectedStamp;
        CollectorQuestUI owner;
        string id;
        public string QuestId => id;
        public void Bind(CollectorQuestUI ui, CollectorQuestManager manager, CollectorQuestView q)
        {
            owner=ui; id=q.Id;
            claim.interactable=true;
            if(collectedStamp) collectedStamp.transform.parent.gameObject.SetActive(false);
            if(illustration && ui.scrapbook) illustration.texture=ui.scrapbook.Picture(q);
            if(pushpin) pushpin.SetActive(manager.PinnedId==q.Id && !q.Claimed);
            if(seal) seal.SetActive(q.Complete);
            title.SetLocalized(manager.Title(q)); description.SetLocalized(manager.Description(q));
            rewards.SetLocalized(Localizer.FormatText(Localizer.GetOr("quest.rewards","{0} coins  +  {1} XP"),q.Coins,q.XP));
            progress.SetLocalized(q.Progress+" / "+q.Target);
            fill.fillAmount=q.Target>0 ? (float)q.Progress/q.Target : 0;
            stripe.color=q.Complete ? ToyShop.UI.ToyShopTheme.Current.mint : ToyShop.UI.ToyShopTheme.Current.blue;
            GetComponent<UnityEngine.UI.Image>().color=q.Claimed ? new Color(.91f,.91f,.81f) : q.Complete ? new Color(1f,.94f,.73f) : new Color(1f,.985f,.92f);
            state.SetLocalized(q.Claimed ? Localizer.GetOr("quest.claimed","CLAIMED") : q.Complete ?
                Localizer.GetOr("quest.ready","READY TO CLAIM") : Localizer.GetOr("quest.in_progress","IN PROGRESS"));
            pin.gameObject.SetActive(!q.Claimed); claim.gameObject.SetActive(q.Complete && !q.Claimed);
            replace.gameObject.SetActive(q.Challenge && !q.Complete);
            pinLabel.SetLocalized(Localizer.GetOr(manager.PinnedId==id ? "quest.unpin" : "quest.pin",manager.PinnedId==id ? "Unpin" : "Pin"));
            claimLabel.SetLocalized(Localizer.GetOr("quest.claim","Claim")); replaceLabel.SetLocalized(Localizer.GetOr("quest.replace","Replace"));
            pin.onClick.RemoveAllListeners(); claim.onClick.RemoveAllListeners(); replace.onClick.RemoveAllListeners();
            pin.onClick.AddListener(()=>manager.Pin(manager.PinnedId==id ? null : id));
            claim.onClick.AddListener(()=>owner.Claim(id)); replace.onClick.AddListener(()=>manager.ReplaceChallenge(id));
            // RemoveAllListeners also removes the shared audio listener; rebind it last.
            UIButtonAudioFeedback.Ensure(pin); UIButtonAudioFeedback.Ensure(claim); UIButtonAudioFeedback.Ensure(replace);
        }
        public void ShowCollected()
        {
            state.SetLocalized(Localizer.GetOr("quest.scrapbook.collected","COLLECTED"));
            claim.interactable=false; if(seal) seal.SetActive(true);
            if(collectedStamp) {collectedStamp.SetLocalized(Localizer.GetOr("quest.scrapbook.collected","COLLECTED"));collectedStamp.transform.parent.gameObject.SetActive(true);}
        }
    }
}
