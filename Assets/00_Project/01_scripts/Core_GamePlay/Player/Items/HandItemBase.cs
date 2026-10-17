using UnityEngine;

// Item held in the hand; reacts to the use button.
public class HandItemBase : MonoBehaviour
{
    [Header("Data")]

    [SerializeField] ItemDataSO data;

    // Data describing this item and its world/hand forms.
    public ItemDataSO Data => data;

    // True while this item is the equipped one.
    public bool IsEquipped { get; internal set; }

    internal HeldItemFraming Framing;
    internal bool IsFlyingToHand;

    private ItemVisualGroup _heldOutline;
    private bool _heldOutlineVisible;

    /// <summary>Black silhouette on the carried car, including its themed accessories.</summary>
    internal void SetHeldCarOutline(bool visible)
    {
        visible &= Framing != null && Framing.IsThemedCar;
        if (_heldOutlineVisible == visible) return;
        _heldOutlineVisible = visible;
        if (visible)
        {
            _heldOutline = GetComponent<ItemVisualGroup>();
            if (_heldOutline == null) _heldOutline = gameObject.AddComponent<ItemVisualGroup>();
            _heldOutline.ShowPrimary(Color.black, SharedOutline.Width);
        }
        else if (_heldOutline != null) _heldOutline.HidePrimary();
    }

    private void OnDisable() => SetHeldCarOutline(false);

    // Lets the handler stamp the source data when equipped from an ItemDataSO.
    internal void SetData(ItemDataSO value) => data = value;

    // Called right after this item is equipped.
    public virtual void OnEquipped(ItemsHandler handler) { }

    // Called right before this item is unequipped.
    public virtual void OnUnequipped(ItemsHandler handler) { }
}
