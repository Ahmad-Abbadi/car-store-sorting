using System.Collections.Generic;
using UnityEngine;

/// <summary> A sorting category; shelves accept items sharing it. </summary>
[CreateAssetMenu(fileName = "Catagory", menuName ="ScriptableObjects/Gameplay/Catagory")]
public class Catagory_SO : ScriptableObject
{
    public string CatagoryName;
    public string CatagoryID;
    public Sprite CatagoryIcon;

    /// <summary> Localization key of the name: category.ID. </summary>
    public string NameKey => "category." + (string.IsNullOrWhiteSpace(CatagoryID) ? name : CatagoryID);

    /// <summary> Localized name; falls back to the typed name, then the asset name. </summary>
    public string DisplayName => Localizer.GetOr(NameKey, string.IsNullOrWhiteSpace(CatagoryName) ? name : CatagoryName);

    [Tooltip("Auto-filled by SortableItem_SO. Don't edit by hand.")]
    public List<SortableItem_SO> Items = new List<SortableItem_SO>();

#if UNITY_EDITOR
    /// <summary> Raised in the editor whenever any category's item list changes. </summary>
    public static event System.Action EditorItemsChanged;

    /// <summary> Adds the item if missing; returns true when changed. </summary>
    public bool Register(SortableItem_SO item)
    {
        Items.RemoveAll(i => i == null);
        if (item == null || Items.Contains(item)) return false;
        Items.Add(item);
        UnityEditor.EditorUtility.SetDirty(this);
        return true;
    }

    /// <summary> Removes the item if present; returns true when changed. </summary>
    public bool Unregister(SortableItem_SO item)
    {
        if (!Items.Remove(item)) return false;
        UnityEditor.EditorUtility.SetDirty(this);
        return true;
    }

    /// <summary> Notifies listeners (e.g. wall signs) that item lists changed. </summary>
    public static void RaiseItemsChanged() => EditorItemsChanged?.Invoke();

    /// <summary> Rebuilds every category's Items list from all SortableItem_SO assets. </summary>
    [ContextMenu("Refresh All Categories")]
    public void RefreshAllFromContext() => RefreshAll();

    /// <summary> Rebuilds every category's Items list from all SortableItem_SO assets. </summary>
    public static void RefreshAll()
    {
        var cats = LoadAll<Catagory_SO>();
        var items = LoadAll<SortableItem_SO>();

        foreach (var cat in cats)
        {
            var fresh = new List<SortableItem_SO>();
            foreach (var item in items)
                if (item.Catagory == cat) fresh.Add(item);

            if (!SameList(cat.Items, fresh))
            {
                cat.Items = fresh;
                UnityEditor.EditorUtility.SetDirty(cat);
            }
        }
        RaiseItemsChanged();
    }

    /// <summary> Loads every asset of type T in the project. </summary>
    public static List<T> LoadAll<T>() where T : Object
    {
        var list = new List<T>();
        foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:" + typeof(T).Name))
        {
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null) list.Add(asset);
        }
        return list;
    }

    private static bool SameList(List<SortableItem_SO> a, List<SortableItem_SO> b)
    {
        if (a == null || a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
            if (a[i] != b[i]) return false;
        return true;
    }
#endif
}
