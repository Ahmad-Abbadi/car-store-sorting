using TMPro;
using UnityEngine;

namespace TinyRiders.Quests
{
    /// <summary>Decorative card captions reuse the board's translated three-part motto.</summary>
    public sealed class CollectorNoticeboardLabels : MonoBehaviour
    {
        public TMP_Text[] captions = new TMP_Text[0];
        void OnEnable() { Localizer.LanguageChanged += Refresh; Refresh(); }
        void OnDisable() { Localizer.LanguageChanged -= Refresh; }
        public void Refresh()
        {
            var words = Localizer.GetOr("quest.board.subtitle", "SORT • DISCOVER • GROW").Split('•');
            for (int i = 0; i < captions.Length; i++)
                if (captions[i]) captions[i].SetLocalized(i < words.Length ? words[i].Trim() : "");
        }
    }
}
