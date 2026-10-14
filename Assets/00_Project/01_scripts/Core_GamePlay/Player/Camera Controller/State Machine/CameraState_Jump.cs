using UnityEngine;

/// <summary>
/// Jumping — a light head-bob applied while airborne. Never reparents anything;
/// it only animates the shared bob pivot, like the other movement states.
/// </summary>
public class CameraState_Jump : BaseCameraState
{
    [Tooltip("Bob speed while airborne.")]
    [SerializeField] private float frequency = 6f;

    [Tooltip("Vertical bob amplitude.")]
    [SerializeField] private float vertical = 0.05f;

    [Tooltip("Horizontal sway amplitude.")]
    [SerializeField] private float horizontal = 0.04f;

    public override void OnStateEnter() { }

    public override void OnStateExit() { }

    public override void OnStateUpdate() => ApplyBob(frequency, vertical, horizontal);
}
