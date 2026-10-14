using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary> Fake loading bar driven by a Slider; fill persists across scenes. </summary>
public class FakeLoadingScreen : MonoBehaviour
{
    /// <summary>Shared fill amount (0..1) that persists across scenes/objects.</summary>
    public static float CurrentFill { get; private set; }

    /// <summary>Reset the shared bar back to empty (call before a fresh loading sequence).</summary>
    public static void ResetFill() => CurrentFill = 0f;

    [Header("Slider")]
    [Tooltip("Slider showing progress. Fill maps to the slider's min..max range.")]
    public Slider loadingSlider;

    [Header("Fill")]
    [Tooltip("How long THIS screen takes to fill from the stored value up to Target Progress.")]
    [Min(0f)]
    public float loadingDuration = 3f;

    [Tooltip("The fill amount (0..1) the bar should reach when THIS screen finishes.")]
    [Range(0f, 1f)]
    public float targetProgress = 1f;

    [Tooltip("Reset the shared bar to 0 when this screen enables (use on the FIRST screen only).")]
    public bool resetFillOnEnable = false;

    [Tooltip("Smooth the fill so it doesn't move perfectly linearly (feels more real).")]
    public bool easeProgress = true;

    void OnEnable()
    {
        if (resetFillOnEnable) ResetFill();

        // Refill the bar to the stored value immediately, so there's no visible reset.
        SetFill(CurrentFill);
        StartCoroutine(Fill());
    }

    IEnumerator Fill()
    {
        float start = CurrentFill;
        float target = Mathf.Clamp01(targetProgress);

        float elapsed = 0f;
        while (elapsed < loadingDuration)
        {
            elapsed += Time.unscaledDeltaTime; // works even when the game is paused
            float t = loadingDuration > 0f ? Mathf.Clamp01(elapsed / loadingDuration) : 1f;
            if (easeProgress) t = Mathf.SmoothStep(0f, 1f, t);
            SetFill(Mathf.Lerp(start, target, t));
            yield return null;
        }

        SetFill(target);
    }

    /// <summary> Stores the shared fill and pushes it to the slider. </summary>
    void SetFill(float value01)
    {
        CurrentFill = Mathf.Clamp01(value01);
        if (loadingSlider != null) loadingSlider.SetValueWithoutNotify(Mathf.Lerp(loadingSlider.minValue, loadingSlider.maxValue, CurrentFill));
    }
}
