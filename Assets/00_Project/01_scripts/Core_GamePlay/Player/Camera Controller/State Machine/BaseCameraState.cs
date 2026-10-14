using UnityEngine;

/// <summary>
/// Base class for every camera-feel state. Each state is a component on a child of the
/// camera rig; the <see cref="CameraStateMachine"/> drives exactly one at a time via the
/// three lifecycle hooks, mirroring the movement state machine's shape.
///
/// States only ever talk to the <see cref="CameraStateMachine"/> — they reach the bob
/// pivot and shared phase through it rather than caching their own refs.
/// </summary>
public abstract class BaseCameraState : MonoBehaviour
{
    [Tooltip("The camera state machine. Auto-found on a parent if left empty.")]
    [SerializeField] protected CameraStateMachine machine;

    // The transform the bob/breathing writes to — reached through the machine.
    protected Transform Pivot => machine != null ? machine.CameraPivot : null;

    protected virtual void Awake()
    {
        if (machine == null) machine = GetComponentInParent<CameraStateMachine>();
    }

    // --- Lifecycle, called by CameraStateMachine ---

    /// <summary> Runs once when this state becomes active. </summary>
    public abstract void OnStateEnter();

    /// <summary> Runs once when this state is replaced. </summary>
    public abstract void OnStateExit();

    /// <summary> Runs every frame while this state is active. </summary>
    public abstract void OnStateUpdate();

    // --- Shared helper the bob/breathing states build on ---

    /// <summary> Advances shared phase and eases a sine head-bob onto the pivot. </summary>
    protected void ApplyBob(float frequency, float vertical, float horizontal)
    {
        if (Pivot == null) return;

        machine.BobTimer += Time.deltaTime * frequency;
        float t = machine.BobTimer;

        // Vertical at full frequency, horizontal at half — a gentle figure-eight.
        Vector3 target = machine.RestLocalPosition +
                         new Vector3(Mathf.Cos(t * 0.5f) * horizontal, Mathf.Sin(t) * vertical, 0f);

        Pivot.localPosition = Vector3.Lerp(Pivot.localPosition, target, Time.deltaTime * machine.BobSmoothing);
    }
}
