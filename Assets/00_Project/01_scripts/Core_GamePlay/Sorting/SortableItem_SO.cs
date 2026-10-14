using UnityEngine;

/// <summary> Item data that can be sorted onto a shelf of its category. </summary>
[CreateAssetMenu(fileName = "SortableItem", menuName = "ScriptableObjects/Gameplay/Sortable Item")]
public class SortableItem_SO : ItemDataSO
{
    public Catagory_SO Catagory;
    public PlacedOnShelfItemBase PlacedOnShelfPrefab;

#if UNITY_EDITOR
    // Category this item is currently registered in.
    [SerializeField, HideInInspector] private Catagory_SO registeredCatagory;

    /// <summary> Moves this item into its category's Items list when Catagory changes. </summary>
    private void OnValidate()
    {
        bool changed = false;
        if (registeredCatagory != Catagory && registeredCatagory != null)
            changed |= registeredCatagory.Unregister(this);
        if (Catagory != null)
            changed |= Catagory.Register(this);

        if (registeredCatagory != Catagory)
        {
            registeredCatagory = Catagory;
            UnityEditor.EditorUtility.SetDirty(this);
        }
        if (changed) Catagory_SO.RaiseItemsChanged();
    }
#endif
}
