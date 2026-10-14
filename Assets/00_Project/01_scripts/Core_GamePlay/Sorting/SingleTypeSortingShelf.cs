using UnityEngine;

/// <summary> Sorting shelf that accepts any item, but only one item type is correct. </summary>
public class SingleTypeSortingShelf : SortingShelf
{
    [Tooltip("The only item type counted as correct on this shelf.")]
    [SerializeField] private SortableItem_SO _acceptedItem;

    /// <summary> Item type this shelf counts as correct. </summary>
    public SortableItem_SO AcceptedItem => _acceptedItem;

    /// <summary> Overrides the correct item type at runtime. </summary>
    public void SetAcceptedItem(SortableItem_SO item)
    {
        _acceptedItem = item;
        RefreshStatus();
    }

    /// <summary> True only when the item is the accepted type. </summary>
    protected override bool IsCorrectCatagory(SortableItem_SO data)
    {
        return _acceptedItem != null && data != null && data == _acceptedItem;
    }
}
