using System.Collections.Generic;
using UnityEngine;

/// <summary> Every saveable item; resolves saved ids back to item data. </summary>
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "ScriptableObject/Item Database")]
public class ItemDatabaseSO : ScriptableObject
{
    [Tooltip("All items that can appear in a save.")]
    [SerializeField] private List<ItemDataSO> items = new List<ItemDataSO>();

    private Dictionary<string, ItemDataSO> _byId;

    /// <summary> Finds the item with the given save id. </summary>
    public bool TryGet(string id, out ItemDataSO item)
    {
        if (_byId == null) Build();

        item = null;
        return id != null && _byId.TryGetValue(id, out item);
    }

    /// <summary> Builds the id lookup; warns on duplicate ids. </summary>
    private void Build()
    {
        _byId = new Dictionary<string, ItemDataSO>();
        foreach (ItemDataSO item in items)
        {
            if (item == null) continue;
            if (!_byId.TryAdd(item.SaveId, item))
                Debug.LogWarning($"{name}: duplicate item save id '{item.SaveId}'.", this);
        }
    }

    private void OnValidate() => _byId = null; // rebuild after inspector edits

#if UNITY_EDITOR
    /// <summary> Fills the list with every ItemDataSO asset in the project. </summary>
    [ContextMenu("Collect All Items")]
    private void CollectAllItems()
    {
        items.Clear();
        foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:ItemDataSO"))
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            ItemDataSO item = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDataSO>(path);
            if (item != null) items.Add(item);
        }

        _byId = null;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
