using UnityEngine;

/// <summary> World item whose data is sortable; category comes from that data. </summary>
public class SortableWorldItem : WorldItemBase
{
    /// <summary> Data as sortable item data, or null if mis-assigned. </summary>
    public SortableItem_SO SortableData => Data as SortableItem_SO;

    /// <summary> Category read from the sortable data. </summary>
    public Catagory_SO Catagory => SortableData != null ? SortableData.Catagory : null;
}
