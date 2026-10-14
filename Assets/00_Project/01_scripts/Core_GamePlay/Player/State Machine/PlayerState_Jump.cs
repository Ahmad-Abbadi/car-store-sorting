using UnityEngine;

/// <summary>
/// The airborne state, entered on a jump press. Feeds the jump on enter, then keeps
/// feeding movement for air control. Once the character has actually left the ground
/// and landed again, it returns to Moving or Idle depending on current input.
/// </summary>
public class PlayerState_Jump : BasePlayerState
{
    // The motor is still grounded the frame we ask to jump; wait until it truly
    // leaves the ground before we start watching for the landing.
    private bool _leftGround;

    public override void OnStateEnter()
    {
        _leftGround = false;
        // Forward the jump press that got us here this same frame.
        FeedMovementInputs(allowJump: true);
        PlayJumpSFX();
    }

    /// <summary> Plays the jump clip from the gameplay sound library. </summary>
    private void PlayJumpSFX()
    {
        SimpleAudioManager audio = SimpleAudioManager.instance;
        if (audio == null || audio.GameplaySounds == null) return;

        audio.PlayJuicyGlobalClip(audio.GameplaySounds.JumpSound);
    }

    public override void OnStateExit() { }

    public override void OnStateUpdate()
    {
        // Air control — read movement, but no re-jump while already airborne.
        FeedMovementInputs(allowJump: false);

        if (!IsGrounded)
            _leftGround = true;

        if (_leftGround && IsGrounded)
            Machine.ChangeState(HasMoveInput ? (BasePlayerState)Machine.Moving : Machine.Idle);
    }
}
