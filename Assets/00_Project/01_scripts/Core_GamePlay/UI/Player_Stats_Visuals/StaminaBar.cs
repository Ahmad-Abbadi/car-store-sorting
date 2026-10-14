using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(Slider))]
public class StaminaBar : MonoBehaviour
{
    [Header("Not-Enough Feedback")]
    [Tooltip("Graphic flashed red. Defaults to the slider fill image.")]
    [SerializeField] private Image flashTarget;
    [Tooltip("What shakes. Defaults to this bar's RectTransform.")]
    [SerializeField] private RectTransform shakeTarget;
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private float flashDuration = 0.12f;
    [SerializeField] private float shakeDuration = 0.3f;
    [Tooltip("Shake distance in UI pixels — keep small.")]
    [SerializeField] private float shakeStrength = 10f;
    [SerializeField] private int shakeVibrato = 12;

    private Slider slider;
    private StatsBehaviour targetStats;

    // Cached so the flash and shake always settle back to their resting values.
    private Color _baseFillColor;
    private Vector2 _baseAnchoredPos;

    void Awake()
    {
        // Automatically fetch the slider component on this GameObject
        slider = GetComponent<Slider>();
        // Set the maximum value of the slider based on our constants
        slider.maxValue = StatsBehaviour.FullStamina;

        // Default the feedback targets so this works with zero wiring.
        if (flashTarget == null && slider.fillRect != null)
            flashTarget = slider.fillRect.GetComponent<Image>();
        if (shakeTarget == null)
            shakeTarget = transform as RectTransform;

        if (flashTarget != null) _baseFillColor = flashTarget.color;
        if (shakeTarget != null) _baseAnchoredPos = shakeTarget.anchoredPosition;
    }

    void OnEnable()
    {
        StaminaBehaviour.OnBlocked += PlayNotEnough;
    }

    void OnDisable()
    {
        StaminaBehaviour.OnBlocked -= PlayNotEnough;
    }

    public void SetStatsBehaviour(StatsBehaviour stats)
    {
        targetStats = stats;
        targetStats.OnStaminaUpdate += UpdateStamina;

        // Update the UI immediately with the starting value
        UpdateStamina(targetStats.Stamina);
    }

    public void UpdateStamina(float currentStamina)
    {
        if (slider != null)
        {
            slider.value = currentStamina;
        }
    }

    /// <summary>Flashes the fill red and gives the bar a small shake for a refused action.</summary>
    public void PlayNotEnough()
    {
        if (flashTarget != null)
        {
            flashTarget.DOKill();
            flashTarget.color = _baseFillColor;
            // Base -> red -> base, on unscaled time so it plays even while paused.
            flashTarget.DOColor(flashColor, flashDuration)
                       .SetLoops(2, LoopType.Yoyo).SetUpdate(true);
        }

        if (shakeTarget != null)
        {
            shakeTarget.DOKill(true);
            shakeTarget.anchoredPosition = _baseAnchoredPos;
            shakeTarget.DOShakeAnchorPos(shakeDuration, shakeStrength, shakeVibrato, 90f, false, true)
                       .SetUpdate(true);
        }
    }

    // Best Practice: Always unsubscribe from events when the object is destroyed
    void OnDestroy()
    {
        if (targetStats != null)
        {
            targetStats.OnStaminaUpdate -= UpdateStamina;
        }
        if (flashTarget != null) flashTarget.DOKill();
        if (shakeTarget != null) shakeTarget.DOKill();
    }
}
