using UnityEngine;

/// <summary>
/// Grounded, no move input. Feeds a (zero) input so the motor stays put, and hands
/// off to Moving when the player walks or to Jump when the jump key is pressed.
/// </summary>
public class PlayerState_Idle : BasePlayerState
{
    public override void OnStateEnter() { }

    public override void OnStateExit() { }

    public override void OnStateUpdate()
    {
        // Read input (resolves to ~zero while idle). Jump is owned by the jump state.
        FeedMovementInputs(allowJump: false);

        if (JumpPressed && IsGrounded && CanJump)
        {
            Machine.ChangeState(Machine.Jump);
            return;
        }

        if (HasMoveInput)
            Machine.ChangeState(Machine.Moving);
    }
}
