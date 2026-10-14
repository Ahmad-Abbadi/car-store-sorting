using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Grounded sprint. Scales the controller's stable move speed while the sprint key is
/// held, then hands back to Moving on release, Idle when input stops, or Jump on a
/// jump press. The boosted speed is restored on exit, so every hand-off is clean.
/// Also widens the Cinemachine FOV while sprinting and restores the base FOV after.
/// </summary>
public class PlayerState_Sprint : BasePlayerState
{
    [Tooltip("Walk speed is scaled by this while sprinting.")]
    [SerializeField] private float sprintMultiplier = 1.6f;

    [Header("FOV")]
    [Tooltip("Cinemachine camera whose FOV widens while sprinting.")]
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [Tooltip("Degrees added to the base FOV while sprinting.")]
    [SerializeField] private float sprintFovOffset = 10f;
    [Tooltip("Seconds to blend the full offset in or out.")]
    [SerializeField] private float fovBlendTime = 0.2f;

    // Walk speed captured on enter so exit restores it, whatever the base is.
    private float _baseMoveSpeed;

    // FOV captured before the first blend-in; held until the blend-out finishes.
    private float _baseFov;
    private bool _sprinting;
    private Coroutine _fovRoutine;

    public override void OnStateEnter()
    {
        if (Character == null) return;
        _baseMoveSpeed = Character.MaxStableMoveSpeed;
        Character.MaxStableMoveSpeed = _baseMoveSpeed * sprintMultiplier;

        _sprinting = true;
        StartFovBlend();
    }

    public override void OnStateExit()
    {
        if (Character != null && _baseMoveSpeed > 0f)
            Character.MaxStableMoveSpeed = _baseMoveSpeed;

        _sprinting = false;
    }

    /// <summary> Starts the FOV blend unless one is already running. </summary>
    private void StartFovBlend()
    {
        if (cinemachineCamera == null || _fovRoutine != null) return;
        _baseFov = cinemachineCamera.Lens.FieldOfView;
        _fovRoutine = StartCoroutine(BlendFov());
    }

    /// <summary> Moves FOV toward the sprint or base target; ends once back at base. </summary>
    private IEnumerator BlendFov()
    {
        float speed = fovBlendTime > 0f ? sprintFovOffset / fovBlendTime : float.MaxValue;
        while (true)
        {
            float target = _sprinting ? _baseFov + sprintFovOffset : _baseFov;
            var lens = cinemachineCamera.Lens;
            lens.FieldOfView = Mathf.MoveTowards(lens.FieldOfView, target, speed * Time.deltaTime);
            cinemachineCamera.Lens = lens;

            if (!_sprinting && Mathf.Approximately(lens.FieldOfView, _baseFov)) break;
            yield return null;
        }
        _fovRoutine = null;
    }

    private void OnDisable()
    {
        // Coroutine dies with the object; put the base FOV back so it can't stick.
        if (_fovRoutine == null) return;
        _fovRoutine = null;
        if (cinemachineCamera != null)
        {
            var lens = cinemachineCamera.Lens;
            lens.FieldOfView = _baseFov;
            cinemachineCamera.Lens = lens;
        }
    }

    public override void OnStateUpdate()
    {
        FeedMovementInputs(allowJump: false);

        if (JumpPressed && IsGrounded && CanJump)
        {
            Machine.ChangeState(Machine.Jump);
            return;
        }

        if (!HasMoveInput)
        {
            Machine.ChangeState(Machine.Idle);
            return;
        }

        // Drop back to walking when the key is released or the pool runs dry.
        if (!SprintHeld || !CanSprint)
            Machine.ChangeState(Machine.Moving);
    }
}
