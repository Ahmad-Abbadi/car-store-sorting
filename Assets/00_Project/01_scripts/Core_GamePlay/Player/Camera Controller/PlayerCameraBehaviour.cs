using UnityEngine;

/// <summary>
/// Owns the "is the player allowed to look around" concern: it enables/disables the
/// mouse-look component and locks/frees the hardware cursor <b>together</b>, so
/// freezing the camera on pause and recapturing it on resume is a single call.
///
/// Reached through the <see cref="Player"/> ref holder (<see cref="Player.CameraBehaviour"/>).
/// The paused state calls <see cref="Freeze"/> / <see cref="Unfreeze"/> instead of
/// toggling the look component and the cursor itself.
/// </summary>
public class PlayerCameraBehaviour : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The mouse/camera look component to toggle (e.g. FpsMouseLook or CharacterCameraController).")]
    [SerializeField] private MonoBehaviour lookComponent;

    [Header("Cursor")]
    [Tooltip("When on, Freeze()/Unfreeze() also free and re-lock the hardware cursor.")]
    [SerializeField] private bool manageCursor = true;

    /// <summary>Stop looking and free the cursor — for pause / UI screens.</summary>
    public void Freeze()
    {
        if (lookComponent != null) lookComponent.enabled = false;

        if (manageCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    /// <summary>Toggle mouse-look only, leaving the cursor as-is (for in-world freezes).</summary>
    public void SetLook(bool enabled)
    {
        if (lookComponent != null) lookComponent.enabled = enabled;
    }

    /// <summary>Resume looking and recapture the cursor — on unpause.</summary>
    public void Unfreeze()
    {
        if (lookComponent != null) lookComponent.enabled = true;

        if (manageCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
