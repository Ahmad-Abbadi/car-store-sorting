using UnityEngine;

/// <summary>
/// Something the player can trigger with a single tap/press while looking at it.
/// Put this on any MonoBehaviour sitting on (or under a collider on) the target object.
/// </summary>
public interface IInteractable
{
    /// <summary> Verb shown in the screen prompt, e.g. "Pick up". Empty hides it. </summary>
    string ActionPrompt { get; }

    /// <summary>Fired once when the player presses interact while this object is the current target.</summary>
    void OnInteract(InteractionHandler handler);
}
