using UnityEngine;

/// <summary>
/// Walking — a simple, steady head-bob. Active while the player is in the Moving state.
/// </summary>
public class CameraState_Walking : BaseCameraState
{
    [Tooltip("Bob speed while walking.")]
    [SerializeField] private float frequency = 8f;

    [Tooltip("Vertical bob amplitude.")]
    [SerializeField] private float vertical = 0.05f;

    [Tooltip("Horizontal sway amplitude.")]
    [SerializeField] private float horizontal = 0.04f;

    [Tooltip("Seconds to wait before bobbing when starting from idle. Skipped when coming from another bobbing state.")]
    [SerializeField] private float bobStartDelay = 0.15f;

    private float _delayTimer;

    public override void OnStateEnter()
    {
        // Only delay when bob starts fresh, not when coming from Sprint/Jump.
        bool fromRest = machine.PreviousState == null || machine.PreviousState is CameraState_Idle;
        _delayTimer = fromRest ? bobStartDelay : 0f;
    }

    public override void OnStateExit() { }

    public override void OnStateUpdate()
    {
        if (_delayTimer > 0f)
        {
            _delayTimer -= Time.deltaTime;
            return;
        }

        ApplyBob(frequency, vertical, horizontal);
    }
}
