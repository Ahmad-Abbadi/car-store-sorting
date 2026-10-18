using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary> One picker row: flag, native name and a highlight when it is the active language. </summary>
public class LanguageButtonUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private Image flag;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Color normalColor = new Color32(0x4A, 0x4E, 0x6A, 0xFF);
    [SerializeField] private Color selectedColor = new Color32(0x3C, 0xE0, 0x2E, 0xFF);

    /// <summary> Fills the row and calls onPick with the language code when pressed. </summary>
    public void Bind(LanguageInfo info, bool selected, Action<string> onPick)
    {
        if (flag != null)
        {
            flag.sprite = info.Flag;
            flag.enabled = info.Flag != null;
        }

        if (label != null)
        {
            label.isRightToLeftText = false;
            label.text = ArabicText.NeedsShaping(info.NativeName) ? ArabicText.ToVisual(info.NativeName) : info.NativeName;
        }

        var theme = ToyShop.UI.ToyShopTheme.Current;
        if (background != null) background.color = selected ? theme.mint : theme.muted;
        if (label != null) label.color = theme.navy;

        button.onClick.RemoveAllListeners();
        string code = info.Code;
        button.onClick.AddListener(() => onPick(code));
        UIButtonAudioFeedback.Ensure(button);
    }
}
