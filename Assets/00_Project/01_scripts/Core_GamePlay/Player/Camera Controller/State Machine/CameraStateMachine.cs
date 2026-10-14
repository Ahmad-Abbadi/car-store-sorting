using UnityEngine;

/// <summary>
/// The camera-feel controller. Lives on the camera rig, owns the camera states (on child
/// objects), and drives exactly one at a time. It mirrors the movement state machine —
/// the single source of truth for what the player is doing — instead of re-reading input,
/// so bob/breathing/jump always match the player's real movement state.
///
/// Runs in LateUpdate so the bob applies after movement and look have settled this frame.
/// </summary>
public class CameraStateMachine : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("The player ref holder — source of the current movement state.")]
    [SerializeField] private Player player;

    [Tooltip("Dedicated bob pivot — must be a PARENT of the Cinemachine vcam, nothing else writes it.")]
    [SerializeField] private Transform cameraPivot;

    [Header("Follow (vertical damping)")]
    [Tooltip("Pitch Pivot this rig follows. Empty = stay parented, no damping.")]
    [SerializeField] private Transform pitchPivot;

    [Tooltip("Unparent this rig on Awake so it follows the Pitch Pivot by itself.")]
    [SerializeField] private bool detachFromPlayer = true;

    [Tooltip("Seconds to settle vertically. 0 = no damping.")]
    [SerializeField] private float verticalDampTime = 0.1f;

    [Header("Feel")]
    [Tooltip("How quickly the pivot eases toward its bob target.")]
    [SerializeField] private float bobSmoothing = 10f;

    [Header("States (on child objects)")]
    [SerializeField] private CameraState_Idle idle;
    [SerializeField] private CameraState_Walking walking;
    [SerializeField] private CameraState_Sprinting sprinting;
    [SerializeField] private CameraState_Jump jump;

    public Transform CameraPivot => cameraPivot;
    public float BobSmoothing => bobSmoothing;

    /// <summary> The pivot's authored local position — the pose bob offsets from. </summary>
    public Vector3 RestLocalPosition { get; private set; }

    /// <summary> Shared bob phase so state swaps don't snap the wave. </summary>
    public float BobTimer;

    public BaseCameraState CurrentState { get; private set; }

    /// <summary> The state active before the current one; null at start. </summary>
    public BaseCameraState PreviousState { get; private set; }

    private float _smoothY;
    private float _velY;

    private void Awake()
    {
        if (player == null) player = GetComponentInParent<Player>();

        if (pitchPivot != null && detachFromPlayer) transform.SetParent(null, true);
        if (pitchPivot != null) _smoothY = pitchPivot.position.y;
        if (cameraPivot != null) RestLocalPosition = cameraPivot.localPosition;

        // Auto-collect any state left unassigned so wiring is forgiving.
        if (idle == null) idle = GetComponentInChildren<CameraState_Idle>(true);
        if (walking == null) walking = GetComponentInChildren<CameraState_Walking>(true);
        if (sprinting == null) sprinting = GetComponentInChildren<CameraState_Sprinting>(true);
        if (jump == null) jump = GetComponentInChildren<CameraState_Jump>(true);
    }

    private void Start() => ChangeState(idle);

    private void LateUpdate()
    {
        FollowPitchPivot();
        SyncToPlayer();
        CurrentState?.OnStateUpdate();
    }

    /// <summary> Follow the pivot's X/Z and rotation exactly; damp only Y. </summary>
    private void FollowPitchPivot()
    {
        if (pitchPivot == null) return;

        Vector3 target = pitchPivot.position;
        _smoothY = verticalDampTime > 0f
            ? Mathf.SmoothDamp(_smoothY, target.y, ref _velY, verticalDampTime)
            : target.y;

        transform.SetPositionAndRotation(new Vector3(target.x, _smoothY, target.z), pitchPivot.rotation);
    }

    /// <summary> Exit the current state and enter <paramref name="next"/>. </summary>
    public void ChangeState(BaseCameraState next)
    {
        if (next == null || next == CurrentState) return;

        CurrentState?.OnStateExit();
        PreviousState = CurrentState;
        CurrentState = next;
        CurrentState.OnStateEnter();
    }

    /// <summary> Map the player's movement state onto the matching camera state. </summary>
    private void SyncToPlayer()
    {
        PlayerStateMachine ps = player != null ? player.StateMachine : null;
        if (ps == null) return;

        BasePlayerState cur = ps.CurrentState;

        BaseCameraState target =
            cur == ps.Jump ? (BaseCameraState)jump :
            cur == ps.Sprint ? sprinting :
            cur == ps.Moving ? walking :
            idle; // Idle / Paused / Freeze all rest as idle breathing.

        ChangeState(target);
    }
}
