using UnityEngine;

/// <summary>
/// Something the player can trigger with the secondary interact button (F) while looking at it.
/// Sits alongside <see cref="IInteractable"/>: an object may implement either or both, and the
/// two buttons never share a callback.
/// </summary>
public interface IInteractableSecondary
{
    /// <summary> Verb shown in the secondary screen prompt. Empty hides it. </summary>
    string SecondaryActionPrompt { get; }

    /// <summary> True lists the secondary prompt above the primary one. </summary>
    bool SecondaryPromptFirst { get; }

    /// <summary>Fired once when the player presses secondary interact on this object.</summary>
    void OnInteractSecondary(InteractionHandler handler);
}
