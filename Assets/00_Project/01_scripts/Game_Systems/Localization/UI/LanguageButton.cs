using UnityEngine;
using UnityEngine.UI;

/// <summary> Menu button that shows the active flag and opens the language picker. </summary>
[RequireComponent(typeof(Button))]
public class LanguageButton : MonoBehaviour
{
    [Tooltip("Shows the flag of the active language; optional.")]
    [SerializeField] private Image flag;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(Open);
    }

    private void OnEnable()
    {
        Localizer.LanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable() => Localizer.LanguageChanged -= Refresh;

    private void OnDestroy()
    {
        if (_button != null) _button.onClick.RemoveListener(Open);
    }

    private static void Open() => LanguagePickerUI.Show(canClose: true);

    private void Refresh()
    {
        if (flag == null || Localizer.Current == null) return;
        flag.sprite = Localizer.Current.Flag;
        flag.enabled = flag.sprite != null;
    }
}
