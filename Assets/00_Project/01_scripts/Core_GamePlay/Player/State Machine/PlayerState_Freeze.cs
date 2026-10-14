using UnityEngine;

/// <summary>
/// Frozen while world time keeps running (GameFlow's Freeze). Cuts movement + look
/// input and roots the player by disabling the KCC motor (no gravity/coasting), but
/// leaves the animator playing and the cursor locked — this is an in-world beat, not
/// a menu. Restores the motor and look on exit. Driven by the <see cref="PlayerStateMachine"/>.
/// </summary>
public class PlayerState_Freeze : BasePlayerState
{
    public override void OnStateEnter()
    {
        // Cut all input (movement + look) through the single gate.
        if (player.Input != null) player.Input.Disable();

        // Freeze physics: zero velocity, then unregister the motor from KCC simulation.
        if (Motor != null)
        {
            Motor.BaseVelocity = Vector3.zero;
            Motor.enabled = false;
        }
    }

    public override void OnStateExit()
    {
        // Re-enter KCC simulation and restore input.
        if (Motor != null) Motor.enabled = true;
        if (player.Input != null) player.Input.Enable();
    }

    public override void OnStateUpdate()
    {
        // Reads nothing — input is disabled and the motor is off.
    }
}
