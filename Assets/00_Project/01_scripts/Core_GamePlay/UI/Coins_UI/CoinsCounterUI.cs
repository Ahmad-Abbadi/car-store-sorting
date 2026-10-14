using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary> Standalone coin counter; counts to the new balance with a punch. </summary>
public class CoinsCounterUI : MonoBehaviour
{
    [Tooltip("Text showing the balance.")]
    [SerializeField] private TMP_Text amountText;

    [Tooltip("Rect punched on change. Falls back to this transform.")]
    [SerializeField] private RectTransform punchTarget;

    [Tooltip("Display format; {0} is the amount.")]
    [SerializeField] private string format = "{0}$";

    [Header("Count")]
    [Tooltip("Seconds to count from the old to the new balance.")]
    [SerializeField] private float countDuration = 0.35f;
    [SerializeField] private Ease countEase = Ease.OutQuad;

    [Header("Punch")]
    [SerializeField] private Vector3 punchScale = new Vector3(0.25f, 0.25f, 0.25f);
    [SerializeField] private float punchDuration = 0.3f;
    [SerializeField] private int punchVibrato = 6;
    [SerializeField] private float punchElasticity = 0.5f;

    private int _shown; // value currently on screen
    private Tween _count;
    private Tween _punch;

    private void Awake()
    {
        if (punchTarget == null) punchTarget = transform as RectTransform;
    }

    private void OnEnable() => CoinsManager.OnCoinsChanged += Show_Juicy;

    private void OnDisable()
    {
        CoinsManager.OnCoinsChanged -= Show_Juicy;
        _count?.Kill();
        _punch?.Complete();
    }

    private void Start()
    {
        Show(CoinsManager.Instance != null ? CoinsManager.Instance.Coins : 0);
    }

    /// <summary> Snaps the counter to a value. </summary>
    public void Show(int coins)
    {
        _count?.Kill();
        SetShown(coins);
    }

    /// <summary> Counts up or down to a value, then punches. </summary>
    public void Show_Juicy(int coins)
    {
        _count?.Kill();
        _count = DOTween.To(() => _shown, SetShown, coins, countDuration)
            .SetEase(countEase)
            .SetUpdate(true) // keeps animating while a panel pauses time
            .SetLink(gameObject);

        if (punchTarget == null) return;
        _punch?.Complete(); // back to base scale before punching again
        _punch = punchTarget.DOPunchScale(punchScale, punchDuration, punchVibrato, punchElasticity)
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    private void SetShown(int value)
    {
        _shown = value;
        if (amountText != null) amountText.text = string.Format(format, value);
    }
}
