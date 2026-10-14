using TMPro;
using UnityEngine;

/// <summary> Swaps a key label like "[Enter]" for a touch icon on touch devices. </summary>
[RequireComponent(typeof(TMP_Text))]
public class InputIconText : MonoBehaviour
{
    [Tooltip("Icon name in the touch sprite asset, e.g. Tap.")]
    [SerializeField] private string touchIcon = "Tap";

    private TMP_Text _label;
    private string _desktopText;

    private void Awake()
    {
        _label = GetComponent<TMP_Text>();
        _desktopText = _label.text;
    }

    private void OnEnable()
    {
        InputIcons.Changed += Apply;
        Apply();
    }

    private void OnDisable() => InputIcons.Changed -= Apply;

    /// <summary> Writes the text for the current device. </summary>
    private void Apply()
    {
        _label.text = InputIcons.IsTouch ? InputIcons.TouchTag(touchIcon) : _desktopText;
    }
}
