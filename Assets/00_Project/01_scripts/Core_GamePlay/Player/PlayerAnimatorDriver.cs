using UnityEngine;

/// <summary>
/// Drives the character's <see cref="Animator"/> from movement <b>input</b> — not
/// the motor's measured velocity — using a 2D directional pair for a blend tree:
///   • <c>Speed_X</c> = strafe (right axis, -1..1)
///   • <c>Speed_Y</c> = forward/back (forward axis, -1..1)
///
/// Also mirrors the player's movement state onto the animator:
///   • <c>IsGrounded</c> (bool) = the motor's ground check
///   • <c>IsSprinting</c> (int) = 1 while sprint is held AND stamina allows it, else 0
///   • <c>Jump</c> (trigger) = fired the frame a stamina-affordable grounded jump starts
///
/// Reading the input state directly keeps animation decoupled from the movement
/// backend (KCC today, anything later) and makes it trivial to extend. Values are
/// damped for smooth blending and clamped to the unit circle so a diagonal never
/// over-drives the blend tree.
/// </summary>
public class PlayerAnimatorDriver : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Animator to drive. Auto-found in children if left empty.")]
    [SerializeField] private Animator animator;

    [Header("Input")]
    [Tooltip("Player input authority. Auto-found on a parent Player if left empty.")]
    [SerializeField] private PlayerInput input;

    [Header("Parameters (2D blend)")]
    [Tooltip("Float parameter for strafe (left/right), -1..1.")]
    [SerializeField] private string speedXParameter = "Speed_X";

    [Tooltip("Float parameter for forward/back, -1..1.")]
    [SerializeField] private string speedYParameter = "Speed_Y";

    [Tooltip("Seconds to smooth the values toward their target. 0 = instant.")]
    [SerializeField] private float speedDampTime = 0.1f;

    [Header("Parameters (state)")]
    [Tooltip("Bool parameter — true while the motor is grounded.")]
    [SerializeField] private string isGroundedParameter = "IsGrounded";

    [Tooltip("Int parameter — 1 while the sprint key is held, else 0.")]
    [SerializeField] private string isSprintingParameter = "IsSprinting";

    [Tooltip("Trigger fired the frame a grounded jump starts.")]
    [SerializeField] private string jumpParameter = "Jump";

    // Reached through the Player ref holder — the ground check reads the motor here.
    private Player _player;

    private int _speedXHash;
    private int _speedYHash;
    private int _isGroundedHash;
    private int _isSprintingHash;
    private int _jumpHash;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();

        _player = GetComponentInParent<Player>();
        if (input == null && _player != null) input = _player.Input;

        _speedXHash = Animator.StringToHash(speedXParameter);
        _speedYHash = Animator.StringToHash(speedYParameter);
        _isGroundedHash = Animator.StringToHash(isGroundedParameter);
        _isSprintingHash = Animator.StringToHash(isSprintingParameter);
        _jumpHash = Animator.StringToHash(jumpParameter);
    }

    private void Update()
    {
        if (animator == null) return;

        // Movement intent straight from the input gate, so the character animates the
        // moment you press a key — and drops to idle the moment input is disabled.
        Vector2 move = input != null ? new Vector2(input.Right, input.Forward) : Vector2.zero;
        if (move.sqrMagnitude > 1f) move.Normalize(); // keep diagonals within the unit circle

        animator.SetFloat(_speedXHash, move.x, speedDampTime, Time.deltaTime);
        animator.SetFloat(_speedYHash, move.y, speedDampTime, Time.deltaTime);

        // Ground check straight from the motor, reached through the Player ref holder.
        bool grounded = _player != null && _player.Motor != null &&
                        _player.Motor.GroundingStatus.IsStableOnGround;
        animator.SetBool(_isGroundedHash, grounded);

        // Stamina gates — default to allowed when no stamina system is present.
        StaminaBehaviour stamina = _player != null ? _player.Stamina : null;
        bool canJump = stamina == null || stamina.CanJump;
        bool canSprint = stamina == null || stamina.CanSprint;

        // Sprint intent from the input gate, but only while stamina can sustain it.
        animator.SetInteger(_isSprintingHash, input != null && input.SprintHeld && canSprint ? 1 : 0);

        // Fire the jump trigger only when a grounded jump can actually be afforded.
        if (grounded && canJump && input != null && input.JumpPressed)
            animator.SetTrigger(_jumpHash);
    }
}
