using UnityEngine;
using KinematicCharacterController;
using KinematicCharacterController.Examples;

/// <summary>
/// Base class for every FPS movement state. Each state is a component placed on a
/// child of the character; the <see cref="PlayerStateMachine"/> on the character
/// root drives exactly one of them at a time via the three lifecycle hooks.
///
/// By design a state only ever talks to the <see cref="Player"/> ref holder — it
/// reaches the KCC motor and controller (and everything else) <b>through</b> Player
/// rather than caching its own component references. The <see cref="Motor"/> and
/// <see cref="Character"/> shortcuts below are just conveniences over that.
///
/// The shared input plumbing (<see cref="FeedMovementInputs"/> / <see cref="StopMovement"/>)
/// lives here so "a state simply reads the input" stays a one-liner in each state.
/// </summary>
public abstract class BasePlayerState : MonoBehaviour
{
    [Tooltip("The player ref holder. Auto-found on a parent if left empty.")]
    [SerializeField] protected Player player;

    // Reached through the Player ref holder — the states' single access point.
    protected ExampleCharacterController Character => player.Character;
    protected KinematicCharacterMotor Motor => player.Motor;
    protected PlayerStateMachine Machine => player.StateMachine;

    protected virtual void Awake()
    {
        if (player == null) player = GetComponentInParent<Player>();
    }

    // --- Lifecycle, called by PlayerStateMachine ---

    /// <summary>Runs once when this state becomes active.</summary>
    public abstract void OnStateEnter();

    /// <summary>Runs once when this state is replaced.</summary>
    public abstract void OnStateExit();

    /// <summary>Runs every frame while this state is active.</summary>
    public abstract void OnStateUpdate();

    // --- Shared helpers the states build on ---

    /// <summary>True while there is meaningful move-axis input this frame.</summary>
    protected bool HasMoveInput => player.Input != null && player.Input.HasMoveInput;

    /// <summary>True the frame the jump key is pressed.</summary>
    protected bool JumpPressed => player.Input != null && player.Input.JumpPressed;

    /// <summary>True while the sprint key is held.</summary>
    protected bool SprintHeld => player.Input != null && player.Input.SprintHeld;

    /// <summary>True while stamina allows a jump; true when no stamina system is present.</summary>
    protected bool CanJump => player.Stamina == null || player.Stamina.CanJump;

    /// <summary>True while stamina allows sprinting; true when no stamina system is present.</summary>
    protected bool CanSprint => player.Stamina == null || player.Stamina.CanSprint;

    /// <summary>True while the motor is standing on stable ground.</summary>
    protected bool IsGrounded => Motor != null && Motor.GroundingStatus.IsStableOnGround;

    /// <summary>
    /// The look yaw the body should follow — the mouse-look accumulator, never read
    /// back off a transform. Falls back to the look transform if no look is wired.
    /// </summary>
    protected Quaternion LookYaw() =>
        player.Look != null ? player.Look.YawRotation : player.LookTransform.rotation;

    /// <summary>
    /// Reads WASD (relative to where the player is looking), runs it through the
    /// ledge guard, and feeds it to the KCC controller. Optionally forwards a jump
    /// press this frame — only the jump state passes <c>true</c>.
    /// </summary>
    protected void FeedMovementInputs(bool allowJump)
    {
        ExampleCharacterController character = Character;
        if (character == null) return;

        PlayerInput input = player.Input;
        float right = input != null ? input.Right : 0f;
        float forward = input != null ? input.Forward : 0f;

        Quaternion lookYaw = LookYaw();

        // Let the ledge guard veto directions that walk off an edge. We resolve the
        // move to the camera-planar world frame the controller uses, filter it, then
        // convert back to axes so the controller stays untouched.
        LedgeGuard ledge = player.LedgeGuard;
        if (ledge != null && (right != 0f || forward != 0f))
        {
            Vector3 planar = Vector3.ProjectOnPlane(lookYaw * Vector3.forward, Vector3.up);
            if (planar.sqrMagnitude > 1e-6f)
            {
                Quaternion camPlanar = Quaternion.LookRotation(planar.normalized, Vector3.up);
                Vector3 world = camPlanar * Vector3.ClampMagnitude(new Vector3(right, 0f, forward), 1f);
                Vector3 filtered = Quaternion.Inverse(camPlanar) * ledge.FilterMove(world);
                right = filtered.x;
                forward = filtered.z;
            }
        }

        PlayerCharacterInputs inputs = new PlayerCharacterInputs
        {
            MoveAxisForward = forward,
            MoveAxisRight = right,
            // Feed the yaw accumulator; the body yaws to it inside UpdateRotation.
            CameraRotation = lookYaw,
            JumpDown = allowJump && JumpPressed,
        };

        character.SetInputs(ref inputs);
    }

    /// <summary>
    /// Feeds a zeroed input so the character stops. Used by the paused state, which
    /// otherwise reads no input at all — without this the motor would keep coasting
    /// on the last input it was handed.
    /// </summary>
    protected void StopMovement()
    {
        ExampleCharacterController character = Character;
        if (character == null) return;

        PlayerCharacterInputs inputs = new PlayerCharacterInputs
        {
            MoveAxisForward = 0f,
            MoveAxisRight = 0f,
            CameraRotation = LookYaw(),
            JumpDown = false,
        };

        character.SetInputs(ref inputs);
    }
}
