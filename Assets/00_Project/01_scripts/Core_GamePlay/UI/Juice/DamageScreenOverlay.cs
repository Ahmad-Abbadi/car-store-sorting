using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Full-screen damage flash. Put on a stretched UI <see cref="Image"/> (a red fill or vignette
/// sprite) and it pulses its alpha whenever the player is hit, driven by the static damage event.
/// </summary>
[RequireComponent(typeof(Image))]
public class DamageScreenOverlay : MonoBehaviour
{
    [Header("Flash")]
    [Tooltip("Peak alpha at full-intensity hits. The event's 0..1 scales toward this.")]
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.55f;
    [Tooltip("Seconds for the flash to fade back to invisible.")]
    [SerializeField] private float fadeDuration = 0.4f;
    [Tooltip("Ease used while fading out.")]
    [SerializeField] private Ease fadeEase = Ease.OutCubic;

    private Image _image;

    private void Awake()
    {
        _image = GetComponent<Image>();
        SetAlpha(0f);
    }

    private void OnEnable() => PlayerDamageEffect.OnPlayerDamaged += Flash;
    private void OnDisable() => PlayerDamageEffect.OnPlayerDamaged -= Flash;

    /// <summary> Snaps the overlay to a hit-scaled alpha, then fades it out. </summary>
    private void Flash(float intensity01)
    {
        float peak = maxAlpha * Mathf.Clamp01(intensity01);

        _image.DOKill();
        SetAlpha(peak);
        _image.DOFade(0f, fadeDuration).SetEase(fadeEase).SetUpdate(true);
    }

    private void SetAlpha(float a)
    {
        Color c = _image.color;
        c.a = a;
        _image.color = c;
    }

    private void OnDestroy() => _image.DOKill();
}
