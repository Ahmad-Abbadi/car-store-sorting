using System.Linq;
using TMPro;
using UnityEngine;
namespace TinyRiders
{
    public sealed class ThemeShelfHeader : MonoBehaviour
    {
        public string themeId;
        public TMP_Text label;
        private void Start() => Refresh();
        public void Refresh()
        {
            var theme = ThemedCollectionSession.Instance?.catalog.themes.FirstOrDefault(t => t.id == themeId);
            if (theme == null || label == null) return;
            label.text = theme.displayName;
            label.color = Color.Lerp(theme.color, new Color(.025f, .035f, .055f), .8f);
        }
    }
}
