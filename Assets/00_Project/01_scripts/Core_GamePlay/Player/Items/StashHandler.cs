using System;
using System.Collections.Generic;
using UnityEngine;

// this script is shity , it uses fucking static everything, it think he's him
public class StashHandler : MonoBehaviour
{
    [Tooltip("Inventory collected items go into.")]
    [SerializeField] private InventoryHandler inventory;

    [Tooltip("Handler that equips/unequips hand items.")]
    [SerializeField] private ItemsHandler itemsHandler;

    [Tooltip("Player input authority. Auto-found on a parent Player if left empty.")]
    [SerializeField] private PlayerInput input;

    [Tooltip("Items granted to the stash on start.")]
    [SerializeField] private List<ItemDataSO> startingItems = new List<ItemDataSO>();

    public event Action<int> OnIndexUpdate;

    /// <summary> Broadcasts current stash contents to any UI. </summary>
    public static event Action<IReadOnlyList<ItemDataSO>> StashChanged;
    /// <summary> Broadcasts the selected index to any UI. </summary>
    public static event Action<int> SelectionChanged;
    /// <summary> Latest stash contents, for UI that subscribes late. </summary>
    public static IReadOnlyList<ItemDataSO> CurrentStash { get; private set; }
    /// <summary> Latest selected index, for UI that subscribes late. </summary>
    public static int CurrentSelection { get; private set; }
    /// <summary> Latest total slot count (capacity) for the UI grid. </summary>
    public static int CurrentSlotCount { get; private set; }
    /// <summary> Latest inventory capacity; 0 means unlimited. </summary>
    public static int CurrentCapacity { get; private set; }
    private static StashHandler _active;
    public static void RequestSelect(int index) => _active?.SelectIndex(index);
    /// <summary> True while the active stash is at capacity. </summary>
    public static bool IsFull => _active != null && _active.inventory != null && _active.inventory.IsFull;



    private int selectedItem_Index;
    private bool _loadedFromSave; // skips startingItems

    /// <summary> Currently selected inventory index. </summary>
    public int SelectedIndex => selectedItem_Index;

    /// <summary> Inventory backing this stash. </summary>
    public InventoryHandler Inventory => inventory;

    /// <summary> Total selectable slots — capacity, or item count when unlimited. </summary>
    private int SlotCount => inventory == null ? 0 : (inventory.Capacity > 0 ? inventory.Capacity : inventory.Count);

    /// <summary> UI entry point: requests selecting an item by index. </summary>

    private void Awake()
    {
        if (input == null)
        {
            Player p = GetComponentInParent<Player>();
            if (p != null) input = p.Input;
        }


    }

    private void OnEnable()
    {
        _active = this;
        if (inventory != null) inventory.OnChanged += PublishStash;
        PublishStash();
        PublishSelection(selectedItem_Index);
    }

    private void OnDisable()
    {
        if (inventory != null) inventory.OnChanged -= PublishStash;
        if (_active == this) _active = null;
    }

    private void Start()
    {
        if (inventory == null) return;

        if (!_loadedFromSave && startingItems != null)
            foreach (ItemDataSO item in startingItems) inventory.TryAddItem(item); // fires OnChanged

        if (inventory.GetItemAt(selectedItem_Index)!= null)
        {
          EquipSelectedItem();
        }
    }

    private void Update()
    {
        if (input == null || itemsHandler == null) return;

        HandleScrollInput(); // the mouse wheel is the only inventory input
    }

    /// <summary> Broadcasts the current stash contents. </summary>
    private void PublishStash()
    {
        CurrentStash = inventory != null ? inventory.Items : null;
        CurrentSlotCount = SlotCount;
        CurrentCapacity = inventory != null ? inventory.Capacity : 0;
        StashChanged?.Invoke(CurrentStash);
    }

    /// <summary> Broadcasts and records the selected index. </summary>
    private void PublishSelection(int index)
    {
        CurrentSelection = index;
        SelectionChanged?.Invoke(index);
    }

    /// <summary> Loads saved items and selection instead of startingItems. Call before Start. </summary>
    public void LoadFromSave(IEnumerable<ItemDataSO> items, int selectedIndex)
    {
        if (inventory == null) return;

        inventory.SetItems(items); // fires OnChanged
        selectedItem_Index = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, inventory.Count - 1));
        _loadedFromSave = true;
        PublishSelection(selectedItem_Index);
    }

    /// <summary> Adds an item to the stash and selects it. False if null or full. </summary>
    public bool TryAddItem(ItemDataSO itemData)
    {
        if (itemData == null || inventory == null) return false;
        if (!inventory.TryAddItem(itemData)) return false;

        selectedItem_Index = inventory.Count - 1;
        inventory.NotifyEquipped(selectedItem_Index);
        OnIndexUpdate?.Invoke(selectedItem_Index);
        PublishSelection(selectedItem_Index);
        return true;
    }

    /// <summary> True while the stash already holds this item. </summary>
    public bool Carries(ItemDataSO itemData) => inventory != null && inventory.Contains(itemData);

    /// <summary> Spends one item out of the stash, then re-equips whatever is left. </summary>
    public bool TryRemove(ItemDataSO itemData)
    {
        if (itemData == null || inventory == null || itemsHandler == null) return false;
        if (!inventory.RemoveItem(itemData)) return false;

        // List shrank: keep same index (item after it slid down), else fall back to last.
        selectedItem_Index = Mathf.Clamp(selectedItem_Index, 0, Mathf.Max(0, inventory.Count - 1));
        EquipSelectedItem();
        return true;
    }

    /// <summary> Steps the selection on scroll; clamps at both ends, no looping. </summary>
    public void HandleScrollInput()
    {
        if (input == null || inventory == null || inventory.Count == 0) return;

        int last = inventory.Count - 1;
        if (input.ScrollUp)
            SelectIndex(Mathf.Min(selectedItem_Index + 1, last));
        else if (input.ScrollDown)
            SelectIndex(Mathf.Max(selectedItem_Index - 1, 0));
    }

    /// <summary> Selects the slot at index; ignores out-of-range or empty slots. </summary>
    public void SelectIndex(int index)
    {
        if (inventory == null || itemsHandler == null) return;
        if (index == selectedItem_Index) return; // already selected, avoid re-equip churn

        if (inventory.GetItemAt(index) == null) return; // out of range or empty slot
        selectedItem_Index = index;
        EquipSelectedItem(); // Unequip, then equip the newly selected item
    }

    private void EquipSelectedItem()
    {
        itemsHandler.UnEquip_Juicy();

        ItemDataSO itemToEquip = inventory.GetItemAt(selectedItem_Index); // null on empty slots
        if (itemToEquip != null)
        {
            itemsHandler.Equip_Juicy(itemToEquip);
            inventory.NotifyEquipped(selectedItem_Index);
        }

        OnIndexUpdate?.Invoke(selectedItem_Index);
        PublishSelection(selectedItem_Index);
    }
}