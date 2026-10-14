using UnityEngine;

/// <summary>
/// Classic first-person look split:
///   • horizontal look (mouse X) yaws the <b>character body</b> left/right, and
///   • vertical look (mouse Y) pitches <b>only the camera</b> up/down.
///
/// Input is fully decoupled: this reads an <see cref="ILookInput"/> provider, so
/// swapping <see cref="MouseLookInput"/> for a new-Input-System / touch provider
/// requires no changes here.
///
/// Expected hierarchy:
///   CharacterBody (this, yaws)
///     └─ CameraPivot (pitches)
///          └─ Camera
/// </summary>
[DefaultExecutionOrder(100)] // run after input components have sampled this frame
public class CharacterCameraController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Transform that yaws left/right (the character root). Defaults to this transform if empty.")]
    [SerializeField] private Transform characterBody;

    [Tooltip("Transform that pitches up/down — the camera or its pivot. Should be a child of the character body.")]
    [SerializeField] private Transform cameraPivot;

    [Tooltip("Component that supplies look input (must implement ILookInput). Auto-found on this object / its children if left empty.")]
    [SerializeField] private MonoBehaviour inputSource;

    [Header("Tuning")]
    [Tooltip("Degrees of rotation per unit of raw look delta.")]
    [SerializeField] private float sensitivity = 2f;

    [Tooltip("Flip vertical look direction.")]
    [SerializeField] private bool invertY = false;

    [Tooltip("How far the camera may pitch down / up, in degrees.")]
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    [Header("Cursor")]
    [Tooltip("Lock + hide the cursor while this controller is active.")]
    [SerializeField] private bool lockCursor = true;

    private ILookInput _input;
    private float _pitch; // accumulated camera pitch in degrees

    private void Awake()
    {
        if (characterBody == null) characterBody = transform;
        ResolveInput();

        // Seed pitch from whatever the pivot is currently authored at.
        if (cameraPivot != null)
            _pitch = NormalizeAngle(cameraPivot.localEulerAngles.x);
    }

    private void OnEnable()
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        if (_input == null) return;

        // Read once, scale once — the controller owns sensitivity/inversion,
        // the provider owns *where the numbers come from*.
        Vector2 look = _input.LookDelta * sensitivity;

        ApplyYaw(look.x);
        ApplyPitch(look.y);
    }

    /// <summary>Horizontal look rotates the whole character about its up axis.</summary>
    private void ApplyYaw(float amount)
    {
        characterBody.Rotate(Vector3.up, amount, Space.Self);
    }

    /// <summary>Vertical look pitches only the camera pivot, clamped so it can't flip over.</summary>
    private void ApplyPitch(float amount)
    {
        if (cameraPivot == null) return;

        _pitch += invertY ? amount : -amount;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
        cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    private void ResolveInput()
    {
        _input = inputSource as ILookInput;
        if (_input == null) _input = GetComponentInChildren<ILookInput>();

        if (_input == null)
            Debug.LogError(
                $"{nameof(CharacterCameraController)}: no {nameof(ILookInput)} provider found. " +
                $"Add a {nameof(MouseLookInput)} (or another ILookInput) to this object.", this);
    }

    // Convert Unity's 0..360 euler back to -180..180 so clamping behaves.
    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        return angle > 180f ? angle - 360f : angle;
    }
}
