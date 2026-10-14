using System;
using UnityEngine;

/// <summary>
/// Player hit feedback: watches <see cref="StatsBehaviour"/> for health drops, shakes the camera,
/// plays a hurt sound, and fires <see cref="OnPlayerDamaged"/> so UI can react. Heals are ignored.
/// </summary>
public class PlayerDamageEffect : MonoBehaviour
{
    /// <summary> Fired on each hit with a 0..1 intensity; UI subscribes to this, no hard refs. </summary>
    public static event Action<float> OnPlayerDamaged;

    [Header("Source")]
    [Tooltip("Player health source. Auto-found on this object, then in the scene, if empty.")]
    [SerializeField] private StatsBehaviour stats;

    [Header("Feedback")]
    [Tooltip("Camera shake to trigger on hit. Auto-found on the main camera if empty.")]
    [SerializeField] private CameraShake cameraShake;

    [Tooltip("Damage that maps to full-strength feedback. Bigger hits still clamp at 1.")]
    [SerializeField] private float damageForFullEffect = 25f;
    [Tooltip("Smallest intensity any hit produces, so tiny chip damage is still felt.")]
    [SerializeField, Range(0f, 1f)] private float minIntensity = 0.25f;

    [Tooltip("Least seconds between shakes. Stops damage-over-time (freezing) shaking every frame.")]
    [SerializeField] private float minInterval = 1f;

    [Header("Sound")]
    [SerializeField] private GameSound hurtSound;

    private float _lastHealth;
    private float _pendingLost;
    private float _lastReactTime = -999f;

    private void Awake()
    {
        if (stats == null) stats = GetComponent<StatsBehaviour>();
        if (stats == null) stats = FindFirstObjectByType<StatsBehaviour>();

        if (cameraShake == null && Camera.main != null)
            cameraShake = Camera.main.GetComponent<CameraShake>();
    }

    private void OnEnable()
    {
        if (stats == null) return;
        _lastHealth = stats.Health;
        stats.OnHealthUpdate += HandleHealthUpdate;
    }

    private void OnDisable()
    {
        if (stats != null) stats.OnHealthUpdate -= HandleHealthUpdate;
    }

    // Health event carries only the new value; a decrease means damage.
    private void HandleHealthUpdate(float newHealth)
    {
        float lost = _lastHealth - newHealth;
        _lastHealth = newHealth;
        if (lost <= 0f) return; // heal or no change

        // Bank the damage; continuous ticks (freezing) fold into one throttled reaction.
        _pendingLost += lost;
        if (Time.unscaledTime - _lastReactTime < minInterval) return;

        _lastReactTime = Time.unscaledTime;
        float intensity = Mathf.Max(minIntensity, Mathf.Clamp01(_pendingLost / Mathf.Max(1f, damageForFullEffect)));
        _pendingLost = 0f;
        Play(intensity);
    }

    /// <summary> Runs the full hit reaction at the given 0..1 intensity. </summary>
    private void Play(float intensity)
    {
        if (cameraShake != null) cameraShake.Shake(intensity);

        if (hurtSound.IsValid && SimpleAudioManager.instance != null)
            SimpleAudioManager.instance.PlayGlobal(hurtSound);

        OnPlayerDamaged?.Invoke(intensity);
    }
}
