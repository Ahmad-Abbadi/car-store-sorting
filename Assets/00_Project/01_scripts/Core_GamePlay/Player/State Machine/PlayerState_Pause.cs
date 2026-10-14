using UnityEngine;

/// <summary>
/// Paused: input off only. Global time is already stopped (timeScale 0), so this just
/// cuts input via the gate and zeroes the animator so an unscaled-time clip can't keep
/// playing. Cursor visibility/lock is owned by the GameFlow state, not here. Driven by
/// the <see cref="PlayerStateMachine"/> from GameFlow's Paused state.
/// </summary>
public class PlayerState_Pause : BasePlayerState
{
    // Animator speed captured on enter so resume restores it faithfully (rather than
    // assuming 1). Explicitly zeroing speed freezes the character regardless of the
    // Animator's update mode, so an unscaled-time animator can't keep playing while paused.
    private float _resumeAnimatorSpeed = 1f;

    public override void OnStateEnter()
    {
        // Cut all input (movement + look) through the single gate.
        if (player.Input != null) player.Input.Disable();

        // Freeze the current animation clip in place.
        Animator animator = player.Animator;
        if (animator != null)
        {
            _resumeAnimatorSpeed = animator.speed;
            animator.speed = 0f;
        }
    }

    public override void OnStateExit()
    {
        // Restore input (movement + look).
        if (player.Input != null) player.Input.Enable();

        // Un-freeze the animator at whatever speed it had before the pause.
        Animator animator = player.Animator;
        if (animator != null) animator.speed = _resumeAnimatorSpeed;
    }

    public override void OnStateUpdate()
    {
        // Intentionally reads nothing — no movement, no camera.
    }
}
