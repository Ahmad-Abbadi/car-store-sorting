using TMPro;
using UnityEngine;

namespace ToyShop.UI
{
    /// <summary>Shared styling for authored canvases and the store HUD generated at runtime.</summary>
    public static class ToyShopStyle
    {
        public static void Surface(UnityEngine.UI.Image image, Color color, bool shadow = true)
        {
            var theme = ToyShopTheme.Current;
            image.color = color;
            if (theme.rounded) { image.sprite = theme.rounded; image.type = UnityEngine.UI.Image.Type.Sliced; }
            if (!shadow) return;
            var shade = image.GetComponent<UnityEngine.UI.Shadow>();
            if (!shade) shade = image.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            shade.effectColor = new Color(theme.navy.r, theme.navy.g, theme.navy.b, .16f);
            shade.effectDistance = theme.shadowOffset;
        }

        public static void Button(UnityEngine.UI.Button button)
        {
            UIButtonAudioFeedback.Ensure(button);
            var theme = ToyShopTheme.Current;
            string name = button.name.ToLowerInvariant();
            bool primary = name.Contains("play") || name.Contains("continue") || name.Contains("start") || name.Contains("buy");
            bool danger = name.Contains("reset") || name.Contains("yes") || name.Contains("close");
            Color color = primary ? theme.blue : danger ? theme.coral : theme.muted;
            var image = button.targetGraphic as UnityEngine.UI.Image;
            if (!image) image = button.GetComponent<UnityEngine.UI.Image>();
            if (image) Surface(image, color);
            button.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
            var states = button.colors;
            states.normalColor = Color.white;
            states.highlightedColor = new Color(.93f, .96f, 1f);
            states.pressedColor = new Color(.78f, .84f, .94f);
            states.selectedColor = new Color(.88f, .94f, 1f);
            states.disabledColor = new Color(.65f, .68f, .72f, .7f);
            states.fadeDuration = theme.buttonDuration;
            button.colors = states;
            if (!button.GetComponent<ToyShopButtonFeedback>()) button.gameObject.AddComponent<ToyShopButtonFeedback>();
            foreach (var text in button.GetComponentsInChildren<TMP_Text>(true))
                text.color = primary ? Color.white : theme.navy;
            foreach (var icon in button.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                if (icon != image && (icon.name == "Icon" || icon.name == "X")) icon.color = primary ? Color.white : theme.navy;
        }

        public static void Apply(GameObject root)
        {
            var theme = ToyShopTheme.Current;
            foreach (var image in root.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                string name = image.name.ToLowerInvariant();
                if (image.GetComponent<UnityEngine.UI.Button>()) continue;
                bool scrim = name == "dim" || name == "backdrop" || name == "confirm_panel" ||
                    (name == "panel" && image.rectTransform.anchorMin == Vector2.zero && image.rectTransform.anchorMax == Vector2.one);
                if (scrim) { image.color = new Color(theme.navy.r, theme.navy.g, theme.navy.b, .72f); continue; }
                bool surface = name == "frame" || name == "window" || name == "menu card" || name == "upgrade card" ||
                    name.Contains("counter") || name.Contains("directory background") || name == "progress background";
                if (surface) Surface(image, theme.cream);
                else if (name == "header") Surface(image, theme.gold, false);
                else if (name == "icon holder")
                {
                    image.color = theme.muted;
                    if (theme.circle) { image.sprite = theme.circle; image.type = UnityEngine.UI.Image.Type.Simple; }
                }
                else if (name == "icon" && image.GetComponentInParent<UpgradeCardUI>()) image.color = theme.navy;
                else if (name == "fill" || name == "bar_fg") image.color = theme.mint;
                else if (name == "bar_bg" || (name == "background" && image.GetComponentInParent<UnityEngine.UI.Slider>())) Surface(image, theme.muted, false);
                else if (name == "handle") image.color = theme.blue;
                else if (name == "item icon") image.color = theme.blue;
                else if (name == "backing") image.color = theme.cream;
            }
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                // Use the existing Fredoka font with its configured multilingual fallback chain.
                if (theme.font && (!text.font || !text.font.name.Contains("Bhaijaan")))
                { text.font = theme.font; text.fontSharedMaterial = theme.font.material; }
                bool onSurface = false;
                for (Transform p = text.transform.parent; p != null; p = p.parent)
                {
                    var image = p.GetComponent<UnityEngine.UI.Image>();
                    if (image && image.color == theme.cream) { onSurface = true; break; }
                }
                text.color = onSurface ? theme.navy : theme.cream;
                text.raycastTarget = false;
                if (!text.enableAutoSizing && (text.name == "Title" ||
                    (text.transform.parent && text.transform.parent.name == "Sensitivity_Row")))
                {
                    text.fontSizeMax = text.fontSize;
                    text.fontSizeMin = text.fontSize * .7f;
                    text.enableAutoSizing = true;
                }
                if (!onSurface && !text.GetComponentInParent<UnityEngine.UI.Button>())
                {
                    var shadow = text.GetComponent<UnityEngine.UI.Shadow>();
                    if (!shadow) shadow = text.gameObject.AddComponent<UnityEngine.UI.Shadow>();
                    shadow.effectColor = new Color(theme.navy.r, theme.navy.g, theme.navy.b, .65f);
                    shadow.effectDistance = new Vector2(0, -2);
                }
            }
            foreach (var button in root.GetComponentsInChildren<UnityEngine.UI.Button>(true)) Button(button);
            foreach (var slider in root.GetComponentsInChildren<UnityEngine.UI.Slider>(true))
            {
                var states = slider.colors;
                states.normalColor = states.highlightedColor = states.selectedColor = Color.white;
                states.pressedColor = new Color(.9f, .94f, 1);
                slider.colors = states;
            }
            foreach (var toggle in root.GetComponentsInChildren<UnityEngine.UI.Toggle>(true))
            {
                if (toggle.targetGraphic is UnityEngine.UI.Image background) Surface(background, theme.muted, false);
                if (toggle.graphic) toggle.graphic.color = theme.blue;
                var states = toggle.colors;
                states.normalColor = states.highlightedColor = states.selectedColor = Color.white;
                states.pressedColor = new Color(.85f, .9f, 1);
                toggle.colors = states;
            }
        }
    }
}
