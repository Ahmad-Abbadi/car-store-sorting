using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary> One item name entry; fades color and scale between selected and dimmed. </summary>
[RequireComponent(typeof(TMP_Text))]
public class EquipmentItemNameUI : MonoBehaviour
{
    [Tooltip("Label showing the item name. Auto-found if empty.")]
    [SerializeField] private TMP_Text label;

    [Header("Selected")]
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private float selectedScale = 1.1f;

    [Header("Dimmed")]
    [SerializeField] private Color dimmedColor = new Color(0.85f, 0.85f, 0.85f, 0.55f);
    [SerializeField] private float dimmedScale = 1f;

    [Header("Warning")]
    [Tooltip("Tint while the item is flagged, e.g. a item to throw away.")]
    [SerializeField] private Color warningColor = new Color(1f, 0.25f, 0.2f);
    [Tooltip("Color the warning pulses toward.")]
    [SerializeField] private Color warningPulseColor = new Color(0.55f, 0.05f, 0.05f);
    [Tooltip("Seconds per slow warning pulse.")]
    [SerializeField, Min(0.1f)] private float warningPulsePeriod = 1.4f;
    [Tooltip("Extra scale at the pulse peak.")]
    [SerializeField, Min(0f)] private float warningPulseScale = 0.06f;

    [Header("Fade")]
    [Tooltip("Seconds to blend between states.")]
    [SerializeField] private float fadeDuration = 0.15f;

    private float current; // 0 = dimmed, 1 = selected
    private float target;
    private bool warning;

    private void Awake()
    {
        if (label == null) label = GetComponent<TMP_Text>();
        Apply(current);
    }

    private void Update()
    {
        current = fadeDuration > 0f
            ? Mathf.MoveTowards(current, target, Time.unscaledDeltaTime / fadeDuration)
            : target;
        Apply(current);

        // Warning keeps it running for the pulse; otherwise idle until next Highlight.
        if (!warning && Mathf.Approximately(current, target)) enabled = false;
    }

    /// <summary> Sets the displayed item name. </summary>
    public void SetName(string itemName) => label.SetLocalized(itemName);

    /// <summary> Makes the name clickable; the action runs on tap. </summary>
    public void SetTapAction(Action onTap)
    {
        if (label == null) label = GetComponent<TMP_Text>();
        label.raycastTarget = true;

        Button button = GetComponent<Button>();
        if (button == null) button = gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onTap?.Invoke());
    }

    /// <summary> Tints the name with the warning color, keeping the highlight's fade. </summary>
    public void SetWarning(bool warned)
    {
        if (warning == warned) return;
        warning = warned;
        Apply(current);
        enabled = warning || !Mathf.Approximately(current, target);
    }

    /// <summary> Highlights or dims this entry; instant skips the fade. </summary>
    public void Highlight(bool selected, bool instant = false)
    {
        target = selected ? 1f : 0f;

        if (instant)
        {
            current = target;
            Apply(current);
            enabled = warning;
            return;
        }

        enabled = warning || !Mathf.Approximately(current, target);
    }

    /// <summary> Applies blended color and scale for value t. </summary>
    private void Apply(float t)
    {
        Color color = Color.Lerp(dimmedColor, selectedColor, t);
        float scale = Mathf.Lerp(dimmedScale, selectedScale, t);

        if (warning)
        {
            // Slow pulse between the warning colors, with a slight swell.
            float pulse = (Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / warningPulsePeriod) + 1f) * 0.5f;
            Color red = Color.Lerp(warningColor, warningPulseColor, pulse);
            color = new Color(red.r, red.g, red.b, color.a);
            scale *= 1f + warningPulseScale * pulse;
        }

        label.color = color;
        transform.localScale = Vector3.one * scale;
    }
}
