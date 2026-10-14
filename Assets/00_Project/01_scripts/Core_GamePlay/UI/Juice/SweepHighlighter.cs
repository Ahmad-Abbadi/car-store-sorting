using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Sweeps a highlight sprite (a shine/glint) across a button every so often, on a
/// random interval so multiple buttons don't flash in unison. The sweep image is a
/// child of the button; it's kept disabled between passes and only enabled for the
/// duration of each sweep. Assign the sprite you want in the <see cref="sweepImage"/>.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SweepHighlighter : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("The highlight/shine image. Must be a child of this button so it's masked to the button bounds.")]
    [SerializeField] Image sweepImage;

    [Header("Timing")]
    [Tooltip("Minimum seconds between sweeps.")]
    [SerializeField] float minInterval = 4f;
    [Tooltip("Maximum seconds between sweeps. Each wait is a random value between min and max.")]
    [SerializeField] float maxInterval = 8f;
    [Tooltip("Wait this long before the first sweep (random 0..firstDelay).")]
    [SerializeField] float firstDelay = 2f;

    [Header("Sweep")]
    [Tooltip("How long one pass takes.")]
    [SerializeField] float duration = 0.6f;
    [SerializeField] Ease ease = Ease.InOutSine;
    [Tooltip("Horizontal travel as a multiple of this button's width. 1 = starts one width off the left, ends one width off the right.")]
    [SerializeField] float travelWidthMultiplier = 1f;
    [Tooltip("Extra local-Z rotation on the sweep sprite (degrees) for a diagonal glint. 0 = vertical.")]
    [SerializeField] float tilt = 0f;

    [Header("Sound")]
    [SerializeField] GameSound sweepSound;

    RectTransform _self;
    RectTransform _sweepRect;
    float _timer;
    bool _sweeping;

    void Awake()
    {
        _self = (RectTransform)transform;
        if (sweepImage != null) _sweepRect = sweepImage.rectTransform;
    }

    void OnEnable()
    {
        HideSweep();
        _timer = Random.Range(0f, Mathf.Max(0f, firstDelay));
    }

    void OnDisable()
    {
        if (_sweepRect != null) _sweepRect.DOKill();
        HideSweep();
    }

    void Update()
    {
        if (_sweeping || sweepImage == null) return;

        _timer -= Time.unscaledDeltaTime;
        if (_timer > 0f) return;

        PlaySweep();
        _timer = Random.Range(minInterval, Mathf.Max(minInterval, maxInterval));
    }

    public void PlaySweep()
    {
        if (sweepImage == null || _sweeping) return;

        float width = _self.rect.width;
        float distance = width * Mathf.Max(0.01f, travelWidthMultiplier);

        _sweepRect.DOKill();
        _sweepRect.localRotation = Quaternion.Euler(0f, 0f, tilt);

        // Start off the left edge, travel to off the right edge.
        Vector2 pos = _sweepRect.anchoredPosition;
        _sweepRect.anchoredPosition = new Vector2(-distance, pos.y);

        _sweeping = true;
        sweepImage.enabled = true;
        sweepImage.gameObject.SetActive(true);

        if (sweepSound.IsValid && SimpleAudioManager.instance != null)
            SimpleAudioManager.instance.PlayGlobal(sweepSound);

        _sweepRect.DOAnchorPosX(distance, duration)
                  .SetEase(ease)
                  .SetUpdate(true) // unscaled so it still sweeps while the game is paused
                  .OnComplete(() =>
                  {
                      _sweeping = false;
                      HideSweep();
                      _sweepRect.anchoredPosition = new Vector2(pos.x, pos.y);
                  });
    }

    void HideSweep()
    {
        _sweeping = false;
        if (sweepImage != null)
        {
            sweepImage.enabled = false;
            sweepImage.gameObject.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (_sweepRect != null) _sweepRect.DOKill();
    }
}
