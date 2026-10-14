using TMPro;
using UnityEngine;

/// <summary>Localized model destination sign.</summary>
public sealed class CarModelSign : MonoBehaviour
{
    [SerializeField] private Catagory_SO model;
    [SerializeField] private TMP_Text label;
    [SerializeField] private int modelNumber;

    private void OnEnable()
    {
        Localizer.LanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable() => Localizer.LanguageChanged -= Refresh;

    public void Configure(Catagory_SO category, TMP_Text text, int number)
    {
        model = category;
        label = text;
        modelNumber = number;
        Refresh();
    }

    private void Refresh()
    {
        if (label == null || model == null) return;
        label.SetLocalized(Localizer.FormatText(Localizer.GetOr("store.model_sign", "MODEL {0}  •  {1}"),
            modelNumber.ToString("00"), model.DisplayName));
    }
}
