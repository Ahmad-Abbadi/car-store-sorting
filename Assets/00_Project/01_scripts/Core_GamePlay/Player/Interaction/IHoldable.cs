using UnityEngine;

/// <summary>
/// Something the player must press-and-hold on to activate (e.g. hold-to-open, hold-to-charge).
/// The <see cref="InteractionHandler"/> drives the whole lifecycle and exposes the live
/// progress via <see cref="InteractionHandler.HoldProgress01"/> for UI/feedback.
/// </summary>
public interface IHoldable
{
    /// <summary> Verb shown as "Hold [LMB] Action" in the screen prompt. Empty hides it. </summary>
    string HoldPrompt { get; }

    /// <summary>Seconds the player must keep holding before the action completes.</summary>
    float HoldDuration { get; set; }

    /// <summary>Fired the moment the hold begins (button pressed while targeting this object).</summary>
    void OnHoldStart(InteractionHandler handler);

    /// <summary>Fired every frame the hold is still charging, before it completes.</summary>
    void OnHoldStay(InteractionHandler handler);

    /// <summary>Fired once when the accumulated hold time reaches <see cref="HoldDuration"/>.</summary>
    void OnHoldComplete(InteractionHandler handler);

    /// <summary>Fired when the hold is released or the target is lost before completing.</summary>
    void OnHoldCancel(InteractionHandler handler);
}
