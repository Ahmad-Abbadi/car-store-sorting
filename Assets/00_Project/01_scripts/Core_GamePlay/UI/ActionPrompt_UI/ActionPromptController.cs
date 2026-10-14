using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary> Screen-space prompt under the reticle: "[LMB] Action" lines with sprite icons. </summary>
[RequireComponent(typeof(CanvasGroup))]
public class ActionPromptController : MonoBehaviour
{
    [Header("Text")]
    [Tooltip("Label above the action lines (IHoverable).")]
    [SerializeField] private TMP_Text hoverLabel;

    [Tooltip("Primary action line (IInteractable / IHoldable).")]
    [SerializeField] private TMP_Text primaryText;

    [Tooltip("Secondary action line (IInteractableSecondary).")]
    [SerializeField] private TMP_Text secondaryText;

    [Header("Hold")]
    [Tooltip("Root of the hold progress bar; hidden for tap actions.")]
    [SerializeField] private GameObject holdBar;

    [Tooltip("Filled image driven by hold progress.")]
    [SerializeField] private Image holdFill;

    [Header("Icons")]
    [Tooltip("Sprite name for the primary button in the TMP sprite asset.")]
    [SerializeField] private string primarySprite = "LMB";

    [Tooltip("Sprite name for the secondary button in the TMP sprite asset.")]
    [SerializeField] private string secondarySprite = "RMB";

    [Header("Fade")]
    [Tooltip("Seconds to fade fully in or out.")]
    [SerializeField] private float fadeDuration = 0.1f;

    private CanvasGroup _group;
    private bool _visible;
    private string _hover, _primary, _secondary;
    private bool _primaryIsHold;
    private bool _secondaryFirst;

    private void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.interactable = false;
        _group.blocksRaycasts = false;
        SetHoldProgress(0f);
    }

    private void OnEnable() => InputIcons.Changed += OnDeviceChanged;

    private void OnDisable() => InputIcons.Changed -= OnDeviceChanged;

    /// <summary> Forgets the built lines so the next prompt rebuilds with the new icons. </summary>
    private void OnDeviceChanged()
    {
        _primary = null;
        _secondary = null;
    }

    private void Update()
    {
        float target = _visible ? 1f : 0f;
        if (Mathf.Approximately(_group.alpha, target)) return;

        float step = fadeDuration <= 0f ? 1f : Time.unscaledDeltaTime / fadeDuration;
        _group.alpha = Mathf.MoveTowards(_group.alpha, target, step);
    }

    /// <summary> Fades the prompt in. </summary>
    public void Show() => _visible = true;

    /// <summary> Fades the prompt out; text stays so the fade doesn't blank early. </summary>
    public void Hide() => _visible = false;

    /// <summary> Sets all prompt lines; rebuilds text only when something changed. Empty hides a line. </summary>
    public void SetPrompts(string hover, string primary, bool primaryIsHold, string secondary, bool secondaryFirst = false)
    {
        if (secondaryFirst != _secondaryFirst)
        {
            _secondaryFirst = secondaryFirst;
            ApplyOrder();
        }

        if (hover != _hover)
        {
            _hover = hover;
            SetLine(hoverLabel, hover);
        }

        if (primary != _primary || primaryIsHold != _primaryIsHold)
        {
            _primary = primary;
            _primaryIsHold = primaryIsHold;
            SetLine(primaryText, Format(primary, primarySprite, primaryIsHold));
            if (holdBar != null) holdBar.SetActive(primaryIsHold && !string.IsNullOrEmpty(primary));
        }

        if (secondary != _secondary)
        {
            _secondary = secondary;
            SetLine(secondaryText, Format(secondary, secondarySprite, false));
        }
    }

    /// <summary> Moves the secondary line above or below the primary line and hold bar. </summary>
    private void ApplyOrder()
    {
        if (primaryText == null || secondaryText == null) return;

        Transform primary = primaryText.transform;
        Transform secondary = secondaryText.transform;
        Transform bar = holdBar != null ? holdBar.transform : null;

        if (_secondaryFirst)
        {
            secondary.SetSiblingIndex(primary.GetSiblingIndex());
        }
        else
        {
            int last = bar != null ? bar.GetSiblingIndex() : primary.GetSiblingIndex();
            secondary.SetSiblingIndex(last);
        }
    }

    /// <summary> Drives the hold fill, 0..1. </summary>
    public void SetHoldProgress(float progress01)
    {
        if (holdFill != null) holdFill.fillAmount = progress01;
    }

    /// <summary> Builds "[icon] Action" or "Hold [icon] Action"; empty action returns empty. </summary>
    private static string Format(string action, string sprite, bool hold)
    {
        if (string.IsNullOrEmpty(action)) return string.Empty;
        string icon = InputIcons.SpriteTag(sprite);
        string holdWord = Localizer.GetOr("prompt.hold", "Hold");

        // Arabic reads right to left: the icon sits on the right of the action.
        if (Localizer.IsRtl)
        {
            string visual = ArabicText.ToVisual(action);
            return hold ? $"{visual} {icon} {ArabicText.ToVisual(holdWord)}" : $"{visual} {icon}";
        }
        return hold ? $"{holdWord} {icon} {action}" : $"{icon} {action}";
    }

    /// <summary> Writes text and toggles the line off when empty. </summary>
    private static void SetLine(TMP_Text line, string text)
    {
        if (line == null) return;
        bool has = !string.IsNullOrEmpty(text);
        line.gameObject.SetActive(has);
        if (has) line.SetLocalized(text);
    }
}
