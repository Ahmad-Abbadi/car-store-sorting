using UnityEngine;
using DG.Tweening;

/// <summary>
/// Plays a shake on a target every so often, using a random interval in a min/max
/// range so several of these on screen never sync up. Drop it on any UI element
/// (or assign a separate target) for idle "hey, look at me" juice.
/// </summary>
public class PeriodicShake : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("What to shake. Leave empty to shake this object.")]
    [SerializeField] Transform target;

    [Header("Timing")]
    [Tooltip("Minimum seconds between shakes.")]
    [SerializeField] float minInterval = 3f;
    [Tooltip("Maximum seconds between shakes. Each wait is a random value between min and max.")]
    [SerializeField] float maxInterval = 6f;
    [Tooltip("Wait this long before the first shake (random 0..firstDelay). 0 = could fire almost immediately.")]
    [SerializeField] float firstDelay = 2f;

    [Header("Shake")]
    [SerializeField] float duration = 0.5f;
    [Tooltip("How far the target moves (local units / UI pixels).")]
    [SerializeField] float strength = 15f;
    [SerializeField] int vibrato = 12;
    [SerializeField] float randomness = 90f;
    [Tooltip("Shake scale as well as position for extra pop. 0 = position only.")]
    [SerializeField] float scalePunch = 0.1f;

    [Header("Sound")]
    [SerializeField] GameSound shakeSound;

    float _timer;
    Vector3 _basePosition;
    Vector3 _baseScale;

    Transform Target => target != null ? target : transform;

    void OnEnable()
    {
        _basePosition = Target.localPosition;
        _baseScale = Target.localScale;
        _timer = Random.Range(0f, Mathf.Max(0f, firstDelay));
    }

    void OnDisable()
    {
        // Kill any running shake and snap back so we don't get left off-centre.
        Target.DOKill();
        Target.localPosition = _basePosition;
        Target.localScale = _baseScale;
    }

    void Update()
    {
        _timer -= Time.unscaledDeltaTime;
        if (_timer > 0f) return;

        PlayShake();
        _timer = Random.Range(minInterval, Mathf.Max(minInterval, maxInterval));
    }

    public void PlayShake()
    {
        Transform t = Target;
        t.DOKill(true);
        // Re-anchor in case something else nudged it while idle.
        t.localPosition = _basePosition;
        t.localScale = _baseScale;

        t.DOShakePosition(duration, strength, vibrato, randomness);

        if (scalePunch > 0f)
            t.DOPunchScale(Vector3.one * scalePunch, duration, vibrato);

        if (shakeSound.IsValid && SimpleAudioManager.instance != null)
            SimpleAudioManager.instance.PlayGlobal(shakeSound);
    }

    void OnDestroy()
    {
        Target.DOKill();
    }
}
