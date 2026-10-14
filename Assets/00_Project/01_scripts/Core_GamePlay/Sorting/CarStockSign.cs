using TMPro;
using UnityEngine;

/// <summary>Keeps a store wayfinding label in the current language.</summary>
public sealed class CarStockSign : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private string key;
    [SerializeField] private string fallback;

    private void OnEnable()
    {
        Localizer.LanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable() => Localizer.LanguageChanged -= Refresh;

    public void Configure(TMP_Text text, string localizationKey, string englishFallback)
    {
        label = text;
        key = localizationKey;
        fallback = englishFallback;
        Refresh();
    }

    private void Refresh()
    {
        if (label != null)
            label.SetLocalized(Localizer.GetOr(key, fallback));
    }
}
