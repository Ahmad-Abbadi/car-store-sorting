using UnityEngine;

/// <summary>
/// <see cref="ILookInput"/> adapter that reads look through the <see cref="PlayerInput"/>
/// gate, so the camera stops looking whenever player input is disabled.
///
/// The source lives inside PlayerInput (the generated GameControls asset);
/// <see cref="CharacterCameraController"/> needs no changes.
/// </summary>
public class MouseLookInput : MonoBehaviour, ILookInput
{
    [Tooltip("Player input authority. Auto-found on a parent Player if left empty.")]
    [SerializeField] private PlayerInput input;

    private void Awake()
    {
        if (input == null)
        {
            Player p = GetComponentInParent<Player>();
            if (p != null) input = p.Input;
        }
    }

    // Look is read through the single input gate; it returns zero while input is disabled.
    public Vector2 LookDelta => input != null ? input.LookDelta : Vector2.zero;
}
