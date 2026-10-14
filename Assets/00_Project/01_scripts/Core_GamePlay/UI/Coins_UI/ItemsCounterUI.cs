using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary> Standalone items counter; counts to the new collected value with a punch. </summary>
public class ItemsCounterUI : MonoBehaviour
{
    [Tooltip("Text showing collected / total.")]
    [SerializeField] private TMP_Text amountText;

    [Tooltip("Rect punched on change. Falls back to this transform.")]
    [SerializeField] private RectTransform punchTarget;

    [Tooltip("Display format; {0} is collected, {1} is total.")]
    [SerializeField] private string format = "{0} / {1}";

    [Header("Count")]
    [Tooltip("Seconds to count from the old to the new value.")]
    [SerializeField] private float countDuration = 0.35f;
    [SerializeField] private Ease countEase = Ease.OutQuad;

    [Header("Punch")]
    [SerializeField] private Vector3 punchScale = new Vector3(0.25f, 0.25f, 0.25f);
    [SerializeField] private float punchDuration = 0.3f;
    [SerializeField] private int punchVibrato = 6;
    [SerializeField] private float punchElasticity = 0.5f;

    private int _shown; // collected value currently on screen
    private int _total;
    private Tween _count;
    private Tween _punch;

    private void Awake()
    {
        if (punchTarget == null) punchTarget = transform as RectTransform;
    }

    private void OnEnable() => SortingProgressTracker.OnProgressChanged += HandleProgress;

    private void OnDisable()
    {
        SortingProgressTracker.OnProgressChanged -= HandleProgress;
        _count?.Kill();
        _punch?.Complete();
    }

    private void Start()
    {
        SortingProgressTracker tracker = SortingProgressTracker.Instance;
        if (tracker != null) Show(tracker.Collected, tracker.Total);
        else Show(0, 0);
    }

    /// <summary> Snaps the counter to the given values. </summary>
    public void Show(int collected, int total)
    {
        _count?.Kill();
        _total = total;
        SetShown(collected);
    }

    /// <summary> Counts up or down to the collected value, then punches. </summary>
    public void Show_Juicy(int collected, int total)
    {
        _total = total;
        _count?.Kill();
        _count = DOTween.To(() => _shown, SetShown, collected, countDuration)
            .SetEase(countEase)
            .SetUpdate(true)
            .SetLink(gameObject);

        if (punchTarget == null) return;
        _punch?.Complete(); // back to base scale before punching again
        _punch = punchTarget.DOPunchScale(punchScale, punchDuration, punchVibrato, punchElasticity)
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    // A new total only happens at level start, so snap instead of punching.
    private void HandleProgress(int collected, int total)
    {
        if (total != _total) Show(collected, total);
        else Show_Juicy(collected, total);
    }

    private void SetShown(int value)
    {
        _shown = value;
        if (amountText != null) amountText.text = string.Format(format, value, _total);
    }
}
