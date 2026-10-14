using UnityEngine;

/// <summary>
/// Abstraction over "look" input. The camera/character controller reads this
/// instead of touching the Input System directly, so the source can be swapped — mouse,
/// a gamepad right-stick, a touch drag — without the
/// controller changing at all.
/// </summary>
public interface ILookInput
{
    /// <summary>
    /// Per-frame look delta. x = horizontal (drives yaw), y = vertical (drives pitch).
    /// Returned raw/unscaled: the controller applies sensitivity and inversion.
    ///
    /// Providers are expected to hand back a per-frame *delta* already. A mouse
    /// gives that naturally; a rate-based source (analog stick) should multiply
    /// by Time.deltaTime inside its own provider so the controller stays neutral.
    /// </summary>
    Vector2 LookDelta { get; }
}
