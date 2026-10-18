using TMPro;
using UnityEngine;

/// <summary> Keeps a TMP label in the active language. </summary>
[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [Tooltip("Localization key, e.g. settings.music.")]
    [SerializeField] private string key;

    private TMP_Text _label;
    private object[] _args;

    private TMP_Text Label => _label != null ? _label : (_label = GetComponent<TMP_Text>());

    /// <summary> Current localization key. </summary>
    public string Key => key;

    private void OnEnable()
    {
        Localizer.LanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable() => Localizer.LanguageChanged -= Refresh;

    /// <summary> Changes the key and optional {0}, {1}... values, then redraws. </summary>
    public void SetKey(string newKey, params object[] args)
    {
        key = newKey;
        _args = args;
        Refresh();
    }

    /// <summary> Sets the {0}, {1}... values and redraws. </summary>
    public void SetArgs(params object[] args)
    {
        _args = args;
        Refresh();
    }

    private void Refresh()
    {
        if (string.IsNullOrEmpty(key)) return;
        if (!Label.enableAutoSizing)
        {
            Label.fontSizeMax = Label.fontSize;
            Label.fontSizeMin = Mathf.Min(Label.fontSizeMin, Label.fontSize * .5f);
            Label.enableAutoSizing = true;
        }
        Label.SetLocalized(_args == null || _args.Length == 0 ? Localizer.Get(key) : Localizer.Format(key, _args));
    }
}

/// <summary> TMP helpers that apply Arabic shaping when needed. </summary>
public static class LocalizedTextExtensions
{
    /// <summary> Sets localized text, shaping and ordering Arabic when that language is active. </summary>
    public static void SetLocalized(this TMP_Text label, string text)
    {
        if (label == null) return;

        var data = Localizer.Data;
        var font = Localizer.IsRtl ? data.arabicFont : data.latinFont;
        if (font != null && label.font != font) label.font = font;

        if (!Localizer.IsRtl || !ArabicText.NeedsShaping(text))
        {
            if (label.isRightToLeftText) label.isRightToLeftText = false;
            label.text = text;
            return;
        }

        bool singleLine = text.Length <= ArabicText.MaxVisualLength && text.IndexOf('\n') < 0;
        label.isRightToLeftText = !singleLine;
        label.text = singleLine ? ArabicText.ToVisual(text) : ArabicText.ToLayout(text);
    }
}
