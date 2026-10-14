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

    // Lets the handler stamp the source data when equipped from an ItemDataSO.
    internal void SetData(ItemDataSO value) => data = value;

    // Called right after this item is equipped.
    public virtual void OnEquipped(ItemsHandler handler) { }

    // Called right before this item is unequipped.
    public virtual void OnUnequipped(ItemsHandler handler) { }
}
