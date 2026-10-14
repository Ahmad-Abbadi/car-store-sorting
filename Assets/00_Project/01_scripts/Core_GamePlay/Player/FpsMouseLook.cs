using UnityEngine;

/// <summary>
/// First-person mouse look for a KCC character. Maintains an <b>absolute</b> world
/// yaw + clamped pitch from the mouse and drives them one-way:
///
///   • yaw   → exposed as <see cref="YawRotation"/>; the movement states feed it to
///             the KCC controller, which yaws the body inside UpdateRotation.
///   • pitch → applied locally to <see cref="pitchPivot"/> only — never the body.
///
/// The camera (a child of the body under the pitch pivot) therefore inherits the
/// body's yaw and adds local pitch. Nothing reads a rotation back off a transform,
/// so there is no feedback loop with the motor.
/// </summary>
[DefaultExecutionOrder(-100)] // Accumulate yaw before the states (default order) read it.
public class FpsMouseLook : MonoBehaviour
{
    [Header("Tuning")]
    [Tooltip("Degrees of rotation per unit of raw mouse delta.")]
    [SerializeField] private float sensitivity = 2f;

    [Tooltip("Flip vertical look direction.")]
    [SerializeField] private bool invertY = false;

    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    [Header("Refs")]
    [Tooltip("Pivot that pitches up/down — a child of the body, parent of the camera.")]
    [SerializeField] private Transform pitchPivot;

    [Tooltip("Player input authority. Auto-found on a parent Player if left empty.")]
    [SerializeField] private PlayerInput input;

    private float _yaw;
    private float _pitch;

    /// <summary>World-space yaw only — the direction the body should face.</summary>
    public Quaternion YawRotation => Quaternion.Euler(0f, _yaw, 0f);

    /// <summary> Current world yaw in degrees. </summary>
    public float Yaw => _yaw;

    /// <summary> Current camera pitch in degrees. </summary>
    public float Pitch => _pitch;

    /// <summary> Sets absolute yaw and clamped pitch, e.g. when restoring a save. </summary>
    public void SetLook(float yaw, float pitch)
    {
        _yaw = yaw;
        _pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    /// <summary> Look sensitivity; settable from outside (e.g. settings UI). </summary>
    public float Sensitivity
    {
        get => sensitivity;
        set => sensitivity = Mathf.Max(0f, value);
    }

    private void Awake()
    {
        // Seed from whatever the transforms are currently authored at.
        _yaw = transform.eulerAngles.y;
        _pitch = pitchPivot != null ? NormalizeAngle(pitchPivot.localEulerAngles.x) : 0f;

        if (input == null)
        {
            Player p = GetComponentInParent<Player>();
            if (p != null) input = p.Input;
        }
    }

    /// <summary> Accumulate yaw + pitch from the mouse; read once per frame. </summary>
    private void Update()
    {
        // Look comes through the gate; it returns zero while input is disabled.
        Vector2 look = input != null ? input.LookDelta : Vector2.zero;

        _yaw += look.x * sensitivity;

        float dy = look.y * sensitivity;
        _pitch += invertY ? dy : -dy;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
    }

    /// <summary> Apply pitch locally after the body has settled this frame. </summary>
    private void LateUpdate()
    {
        if (pitchPivot != null)
            pitchPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    // Convert Unity's 0..360 euler back to -180..180 so clamping behaves.
    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        return angle > 180f ? angle - 360f : angle;
    }
}
