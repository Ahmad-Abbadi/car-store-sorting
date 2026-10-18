using System.Linq;
using TMPro;
using UnityEngine;
namespace TinyRiders
{
    public sealed class ThemeShelfHeader : MonoBehaviour
    {
        public string themeId;
        public TMP_Text label;
        private void OnEnable() { Localizer.LanguageChanged += Refresh; Refresh(); }
        private void Start() => Refresh();
        private void OnDisable() => Localizer.LanguageChanged -= Refresh;
        public void Refresh()
        {
            var theme = ThemedCollectionSession.Instance?.catalog.themes.FirstOrDefault(t => t.id == themeId);
            if (theme == null || label == null) return;
            label.SetLocalized(theme.DisplayName);
            label.color = Color.Lerp(theme.color, new Color(.025f, .035f, .055f), .8f);
        }
    }
}
