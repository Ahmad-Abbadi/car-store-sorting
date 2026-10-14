using UnityEngine;

// Hand item reacting to press-and-hold on the primary item button (LMB).
// Lifecycle: Start -> Stay(each frame) -> [Complete] -> [Canceled] -> End.
public interface IItemHoldable
{
    // Seconds held before Complete fires. 0 = same frame.
    float HoldDuration { get; }

    // Fired once when the hold begins.
    void OnHoldStart(ItemsHandler handler);

    // Fired every frame the button stays down.
    void OnHoldStay(ItemsHandler handler);

    // Fired once when the hold reaches HoldDuration.
    void OnHoldComplete(ItemsHandler handler);

    // Fired when released before completing.
    void OnHoldCanceled(ItemsHandler handler);

    // Fired last whenever the hold ends.
    void OnHoldEnd(ItemsHandler handler);
}
