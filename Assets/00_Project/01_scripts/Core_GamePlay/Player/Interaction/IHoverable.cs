using UnityEngine;

/// <summary>
/// Something that reacts to simply being looked at (highlight, outline, tooltip…),
/// independent of whether the player actually interacts with it.
/// </summary>
public interface IHoverable
{
    /// <summary> Label shown above the action prompts, e.g. object name. Empty hides it. </summary>
    string HoverPrompt { get; }

    /// <summary>Fired the frame the player's reticle first lands on this object.</summary>
    void OnHoverEnter();

    /// <summary>Fired the frame the player's reticle leaves this object.</summary>
    void OnHoverExit();

    /// <summary>Fired every frame the object stays under the reticle.</summary>
    void OnHoverStay();
}
