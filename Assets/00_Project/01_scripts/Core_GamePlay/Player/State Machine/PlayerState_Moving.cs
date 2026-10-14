using UnityEngine;

/// <summary>
/// Grounded and walking. Reads WASD every frame and feeds it to the KCC controller;
/// falls back to Idle when input stops, promotes to Sprint while the sprint key is
/// held, or hands off to Jump on a jump press.
/// </summary>
public class PlayerState_Moving : BasePlayerState
{
    public override void OnStateEnter() { }

    public override void OnStateExit() { }

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

        if (SprintHeld && CanSprint)
            Machine.ChangeState(Machine.Sprint);
    }
}
