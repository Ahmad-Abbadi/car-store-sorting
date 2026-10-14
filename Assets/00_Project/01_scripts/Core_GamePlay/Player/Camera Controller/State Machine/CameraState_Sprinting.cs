using UnityEngine;

/// <summary>
/// Sprinting — a faster, wider head-bob than walking. Active while the player is sprinting.
/// </summary>
public class CameraState_Sprinting : BaseCameraState
{
    [Tooltip("Bob speed while sprinting — faster than walking.")]
    [SerializeField] private float frequency = 13f;

    [Tooltip("Vertical bob amplitude.")]
    [SerializeField] private float vertical = 0.09f;

    [Tooltip("Horizontal sway amplitude.")]
    [SerializeField] private float horizontal = 0.07f;

    public override void OnStateEnter() { }

    public override void OnStateExit() { }

    public override void OnStateUpdate() => ApplyBob(frequency, vertical, horizontal);
}
