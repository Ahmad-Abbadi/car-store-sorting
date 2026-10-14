using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary> Groups shelves under one category and displays its icon and name. </summary>
public class Section : MonoBehaviour
{
    [Tooltip("Category pushed to the display and every shelf.")]
    [SerializeField] private Catagory_SO _catagory;

    [SerializeField] private Image _displayImage;
    [SerializeField] private TMP_Text _displayName;

    [Tooltip("Shelves that receive this section's category.")]
    [SerializeField] private List<SortingShelf> _shelves = new List<SortingShelf>();

    /// <summary> Fired when the last shelf of a section fills correctly. </summary>
    public static event Action<Section> OnSectionCompleted;

    /// <summary> Category this section applies. </summary>
    public Catagory_SO Catagory => _catagory;

    /// <summary> True while every shelf of this section is completed. </summary>
    public bool IsCompleted
    {
        get
        {
            bool any = false;
            foreach (SortingShelf shelf in _shelves)
            {
                if (shelf == null) continue;
                if (!shelf.IsCompleted) return false;
                any = true;
            }
            return any;
        }
    }

    private void OnEnable()
    {
        SortingShelf.OnShelfFilled += HandleShelfFilled;
        Localizer.LanguageChanged += RefreshName;
        RefreshName();
    }

    private void OnDisable()
    {
        SortingShelf.OnShelfFilled -= HandleShelfFilled;
        Localizer.LanguageChanged -= RefreshName;
    }

    /// <summary> Shows the category name in the active language. </summary>
    private void RefreshName()
    {
        if (_displayName != null && _catagory != null) _displayName.SetLocalized(_catagory.DisplayName);
    }

    /// <summary> Raises completion when one of this section's shelves fills the last gap. </summary>
    private void HandleShelfFilled(SortingShelf shelf)
    {
        if (_shelves.Contains(shelf) && IsCompleted) OnSectionCompleted?.Invoke(this);
    }

    private void OnValidate()
    {
        if (_catagory == null) return;

        if (_displayImage != null) _displayImage.sprite = _catagory.CatagoryIcon;
        if (_displayName != null) _displayName.text = _catagory.CatagoryName; // English editor preview

        foreach (var shelf in _shelves)
        {
            if (shelf == null) continue;
            shelf.SetCatagory(_catagory);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(shelf); // persist change on other objects
#endif
        }
    }
}
