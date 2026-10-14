using UnityEngine;
using DG.Tweening;

/// <summary> Reusable positional + rotational camera shake, scaled by a 0..1 intensity. </summary>
public class CameraShake : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Transform to shake. Leave empty to shake this object. Use the Camera leaf, not the pivot.")]
    [SerializeField] private Transform shakeTarget;

    [Header("Shake")]
    [Tooltip("Shake length in seconds at full intensity.")]
    [SerializeField] private float duration = 0.35f;
    [Tooltip("Positional shake distance (local units) at full intensity.")]
    [SerializeField] private float positionStrength = 0.25f;
    [Tooltip("Rotational shake (degrees) at full intensity; z is roll.")]
    [SerializeField] private Vector3 rotationStrength = new Vector3(2f, 2f, 6f);
    [SerializeField] private int vibrato = 12;
    [SerializeField] private float randomness = 90f;

    private Transform Target => shakeTarget != null ? shakeTarget : transform;
    private Vector3 _basePosition;
    private Quaternion _baseRotation;
    private float _activeUntil;
    private float _activeIntensity;

    private void Awake()
    {
        _basePosition = Target.localPosition;
        _baseRotation = Target.localRotation;
    }

    /// <summary> Plays a shake scaled by intensity (0..1); larger hits shake harder. </summary>
    public void Shake(float intensity01)
    {
        intensity01 = Mathf.Clamp01(intensity01);
        if (intensity01 <= 0f) return;

        // A shake already running at least this hard keeps going, so rapid hits never restart it every frame.
        if (Time.unscaledTime < _activeUntil && intensity01 <= _activeIntensity) return;

        _activeUntil = Time.unscaledTime + duration;
        _activeIntensity = intensity01;

        Transform t = Target;

        // Kill and re-anchor so overlapping hits never leave the camera off-centre.
        t.DOKill(true);
        t.localPosition = _basePosition;
        t.localRotation = _baseRotation;

        t.DOShakePosition(duration, positionStrength * intensity01, vibrato, randomness)
         .SetUpdate(true)
         .OnComplete(() => t.localPosition = _basePosition); // Explicitly reset at end

        t.DOShakeRotation(duration, rotationStrength * intensity01, vibrato, randomness)
         .SetUpdate(true)
         .OnComplete(() => t.localRotation = _baseRotation); // Explicitly reset at end
    }

    private void OnDisable()
    {
        Target.DOKill();
        Target.localPosition = _basePosition;
        Target.localRotation = _baseRotation;
    }
}