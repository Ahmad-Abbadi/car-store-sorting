using UnityEngine;
using System;

/// <summary>
/// Stamina rules for the player's jump and sprint. Owns no value of its own — it spends
/// and regenerates the pool held by <see cref="StatsBehaviour"/> (the single source of
/// truth + UI event), reached through the <see cref="Player"/> ref holder. Consumption is
/// self-driven by watching the state machine; the public <see cref="CanJump"/> /
/// <see cref="CanSprint"/> gates let the jump/sprint states refuse when the pool is too low.
/// </summary>
public class StaminaBehaviour : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("Player ref holder. Auto-found on a parent if left empty.")]
    [SerializeField] private Player player;

    [Header("Jump")]
    [Tooltip("Stamina spent per jump. Also the minimum required to be allowed to jump.")]
    [SerializeField] private float jumpCost = 15f;

    [Header("Sprint")]
    [Tooltip("Stamina drained per second while sprinting.")]
    [SerializeField] private float sprintDrainPerSecond = 20f;
    [Tooltip("Minimum stamina required to start or keep sprinting.")]
    [SerializeField] private float sprintMinRequired = 5f;

    [Header("Regen")]
    [Tooltip("Stamina regained per second once idle of spending. 0 disables regen.")]
    [SerializeField] private float regenPerSecond = 12f;
    [Tooltip("Seconds after the last spend before regen resumes.")]
    [SerializeField] private float regenDelay = 1f;

    // Counts down after each spend; regen only runs once it reaches zero.
    private float _regenCooldown;

    // Last frame's state, so a jump is charged only once on entry.
    private BasePlayerState _prevState;

    // Last frame's "wants to sprint", so a refused sprint pulses only once.
    private bool _wasWantingSprint;

    /// <summary>Fired when a jump/sprint is attempted but the pool is too low. UI hook.</summary>
    public static event Action OnBlocked;

    private StatsBehaviour Stats => player != null ? player.Stats : null;
    private PlayerStateMachine Machine => player != null ? player.StateMachine : null;

    /// <summary>True while the pool can afford a jump's full cost.</summary>
    public bool CanJump => Stats != null && Stats.Stamina >= jumpCost;

    /// <summary>True while the pool can sustain a sprint.</summary>
    public bool CanSprint => Stats != null && Stats.Stamina >= sprintMinRequired;

    private void Awake()
    {
        if (player == null) player = GetComponentInParent<Player>();
    }

    private void Update()
    {
        if (Stats == null || Machine == null) return;

        BasePlayerState state = Machine.CurrentState;

        // Sprint drains continuously; a jump is charged the frame it begins.
        if (state == Machine.Sprint)
            Spend(sprintDrainPerSecond * Time.deltaTime);
        else if (state == Machine.Jump && _prevState != Machine.Jump)
            Spend(jumpCost);

        _prevState = state;

        DetectBlockedAttempts();
        Regen();
    }

    /// <summary>Fires the not-enough pulse when a gated action is attempted but unaffordable.</summary>
    private void DetectBlockedAttempts()
    {
        PlayerInput input = player.Input;
        if (input == null) return;

        bool grounded = player.Motor != null && player.Motor.GroundingStatus.IsStableOnGround;

        // Jump press denied — one pulse per press (the read is already edge-based).
        if (input.JumpPressed && grounded && !CanJump)
            OnBlocked?.Invoke();

        // Sprint is a hold — pulse once when a fresh sprint attempt is refused.
        bool wantSprint = input.SprintHeld && input.HasMoveInput && grounded;
        if (wantSprint && !_wasWantingSprint && !CanSprint)
            OnBlocked?.Invoke();
        _wasWantingSprint = wantSprint;
    }

    /// <summary>Draws down the pool and holds off regen for a moment.</summary>
    private void Spend(float amount)
    {
        if (amount <= 0f) return;
        Stats.Stamina -= amount;      // setter clamps 0..max and fires the UI event
        _regenCooldown = regenDelay;  // pause regen after any spend
    }

    /// <summary>Refills the pool once the post-spend cooldown has elapsed.</summary>
    private void Regen()
    {
        if (_regenCooldown > 0f) { _regenCooldown -= Time.deltaTime; return; }
        if (regenPerSecond <= 0f || Stats.Stamina >= StatsBehaviour.FullStamina) return;
        Stats.Stamina += regenPerSecond * Time.deltaTime;
    }
}
