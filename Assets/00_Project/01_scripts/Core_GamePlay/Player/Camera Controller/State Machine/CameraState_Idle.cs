using UnityEngine;

/// <summary>
/// Standing still — a slow, shallow vertical sway that reads as breathing. Active while
/// the player is idle (also paused/frozen, where zero deltaTime holds it steady).
/// </summary>
public class CameraState_Idle : BaseCameraState
{
    [Tooltip("Breaths per unit time — kept low for a calm rest.")]
    [SerializeField] private float frequency = 1.2f;

    [Tooltip("Vertical breathing amplitude.")]
    [SerializeField] private float vertical = 0.02f;

    [Tooltip("Horizontal drift — usually near zero at rest.")]
    [SerializeField] private float horizontal = 0.005f;

    public override void OnStateEnter() { }

    public override void OnStateExit() { }

    public override void OnStateUpdate() => ApplyBob(frequency, vertical, horizontal);
}
