using UnityEngine;

/// <summary> Hand item reacting to the two item buttons. </summary>
public interface IItemClickable
{
    /// <summary> Fired once on primary click (LMB). Skipped when the item is also holdable. </summary>
    void OnClick(ItemsHandler handler);

    /// <summary> Fired once on secondary click (RMB). Always a tap, never a hold. </summary>
    void OnSecondaryClick(ItemsHandler handler);
}
