using TMPro;
using UnityEngine;

/// <summary>Localized target label for a car variant shelf.</summary>
public sealed class CarShelfLabel : MonoBehaviour
{
    [SerializeField] private SortableItem_SO item;
    [SerializeField] private TMP_Text label;

    private void OnEnable()
    {
        Localizer.LanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable() => Localizer.LanguageChanged -= Refresh;

    public void Configure(SortableItem_SO target, TMP_Text text)
    {
        item = target;
        label = text;
        Refresh();
    }

    public void Refresh()
    {
        if (item == null || label == null) return;
        string source = item.SourceName;
        int split = source.LastIndexOf(' ');
        string color = split >= 0 ? source.Substring(split + 1) : source;
        label.SetLocalized(Localizer.GetOr("store.color." + color, color));
    }
}
