using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary> Stacked item names from the stash; newest on top, selected highlighted. </summary>
public class EquipmentItemsUI : MonoBehaviour
{
    [Tooltip("Parent with the VerticalLayoutGroup that holds the name entries.")]
    [SerializeField] private RectTransform listRoot;

    [Tooltip("Prefab spawned per item.")]
    [SerializeField] private EquipmentItemNameUI namePrefab;

    [Tooltip("Optional count/capacity label.")]
    [SerializeField] private TMP_Text counterLabel;

    [Header("Touch Scroll")]
    [Tooltip("Rows moved per arrow press when the names run past the screen.")]
    [SerializeField, Min(1)] private int rowsPerScroll = 3;

    private readonly List<EquipmentItemNameUI> entries = new List<EquipmentItemNameUI>();
    private readonly Vector3[] corners = new Vector3[4];
    private IReadOnlyList<ItemDataSO> items;
    private int itemCount;
    private int selectedIndex;

    private Vector2 basePosition;
    private float scroll;      // canvas units; positive moves the names up
    private float minScroll;
    private float maxScroll;
    private float rowStep = 54f;
    private bool measureNeeded;

    private static Predicate<ItemDataSO> _warningFilter;
    private static event Action WarningFilterChanged;

    /// <summary> True while the names run past the screen edge and need scrolling. </summary>
    public static bool Overflowing { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _warningFilter = null;
        WarningFilterChanged = null;
        Overflowing = false;
    }

    /// <summary> Names of items matching the filter show in the warning color; null clears. </summary>
    public static void SetWarningFilter(Predicate<ItemDataSO> isWarned)
    {
        _warningFilter = isWarned;
        WarningFilterChanged?.Invoke();
    }

    private void OnEnable()
    {
        basePosition = listRoot.anchoredPosition;
        scroll = 0f;
        EnsureRaycaster();

        StashHandler.StashChanged += OnStashChanged;
        StashHandler.SelectionChanged += OnSelectionChanged;
        WarningFilterChanged += RefreshWarnings;
        Localizer.LanguageChanged += RebuildNames;

        // Catch up on state published before we subscribed.
        selectedIndex = StashHandler.CurrentSelection;
        OnStashChanged(StashHandler.CurrentStash);
    }

    private void OnDisable()
    {
        StashHandler.StashChanged -= OnStashChanged;
        StashHandler.SelectionChanged -= OnSelectionChanged;
        WarningFilterChanged -= RefreshWarnings;
        Localizer.LanguageChanged -= RebuildNames;

        Overflowing = false;
        listRoot.anchoredPosition = basePosition;
    }

    private void LateUpdate()
    {
        if (measureNeeded)
        {
            measureNeeded = false;
            Measure();
        }

        Player player = Player.Instance;
        PlayerInput input = player != null ? player.Input : null;
        if (input == null) return;

        if (input.StashScrollUpPressed) Scroll(-1);
        else if (input.StashScrollDownPressed) Scroll(1);
    }

    /// <summary> Clicks need a raycaster on the canvas; the prefab has none. </summary>
    private void EnsureRaycaster()
    {
        Canvas canvas = listRoot.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        GameObject host = canvas.rootCanvas.gameObject;
        if (host.GetComponent<GraphicRaycaster>() == null) host.AddComponent<GraphicRaycaster>();
    }

    /// <summary> Scrolls the names by rowsPerScroll rows; +1 moves them up. </summary>
    private void Scroll(int direction)
    {
        Measure();
        scroll += direction * rowsPerScroll * rowStep;
        ApplyScroll();
    }

    /// <summary> Finds how far the names run past the screen and the scroll range that reveals them. </summary>
    private void Measure()
    {
        listRoot.anchoredPosition = basePosition;
        LayoutRebuilder.ForceRebuildLayoutImmediate(listRoot);

        Canvas canvas = listRoot.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas != null ? (RectTransform)canvas.rootCanvas.transform : null;
        minScroll = maxScroll = 0f;

        if (canvasRect != null && itemCount > 0)
        {
            float top = float.NegativeInfinity;
            float bottom = float.PositiveInfinity;
            float firstCenter = 0f;
            float secondCenter = 0f;

            for (int row = 0; row < itemCount; row++)
            {
                RectTransform rect = (RectTransform)entries[row].transform;
                rect.GetWorldCorners(corners);
                float rowBottom = canvasRect.InverseTransformPoint(corners[0]).y;
                float rowTop = canvasRect.InverseTransformPoint(corners[1]).y;
                top = Mathf.Max(top, rowTop);
                bottom = Mathf.Min(bottom, rowBottom);
                if (row == 0) firstCenter = (rowTop + rowBottom) * 0.5f;
                else if (row == 1) secondCenter = (rowTop + rowBottom) * 0.5f;
            }

            if (itemCount > 1) rowStep = Mathf.Max(1f, Mathf.Abs(firstCenter - secondCenter));
            minScroll = Mathf.Min(0f, canvasRect.rect.yMax - top);
            maxScroll = Mathf.Max(0f, canvasRect.rect.yMin - bottom);
        }

        Overflowing = minScroll < -0.5f || maxScroll > 0.5f;
        ApplyScroll();
    }

    /// <summary> Clamps the scroll and moves the list by it. </summary>
    private void ApplyScroll()
    {
        scroll = Mathf.Clamp(scroll, minScroll, maxScroll);

        Canvas canvas = listRoot.GetComponentInParent<Canvas>();
        RectTransform parent = listRoot.parent as RectTransform;
        float canvasScale = canvas != null ? canvas.rootCanvas.transform.lossyScale.y : 1f;
        float parentScale = parent != null ? parent.lossyScale.y : 1f;
        float local = parentScale > 0f ? scroll * canvasScale / parentScale : scroll;
        listRoot.anchoredPosition = basePosition + Vector2.up * local;
    }

    /// <summary> Selects the stash item shown on this row, while input is live. </summary>
    private void OnEntryTapped(int row)
    {
        Player player = Player.Instance;
        if (player == null || player.Input == null || !player.Input.IsEnabled) return;

        StashHandler.RequestSelect(RowToIndex(row));
    }

    /// <summary> Redraws the names after a language change. </summary>
    private void RebuildNames() => OnStashChanged(items);

    /// <summary> Rebuilds names from the stash; snaps highlight to avoid rebinding flicker. </summary>
    private void OnStashChanged(IReadOnlyList<ItemDataSO> stash)
    {
        items = stash;
        itemCount = items != null ? items.Count : 0;

        for (int row = 0; row < itemCount; row++)
        {
            EquipmentItemNameUI entry = GetEntry(row);
            entry.gameObject.SetActive(true);
            entry.SetName(ItemName(items[RowToIndex(row)]));
        }

        for (int row = itemCount; row < entries.Count; row++)
            entries[row].gameObject.SetActive(false);

        RefreshHighlight(instant: true);
        RefreshWarnings();
        RefreshCounter();
        measureNeeded = true;
    }

    /// <summary> Re-tints every visible name against the current warning filter. </summary>
    private void RefreshWarnings()
    {
        for (int row = 0; row < itemCount; row++)
        {
            ItemDataSO item = items[RowToIndex(row)];
            entries[row].SetWarning(_warningFilter != null && item != null && _warningFilter(item));
        }
    }

    /// <summary> Fades highlight to the newly selected item. </summary>
    private void OnSelectionChanged(int index)
    {
        selectedIndex = index;
        RefreshHighlight(instant: false);
    }

    /// <summary> Highlights the selected row, dims the rest. </summary>
    private void RefreshHighlight(bool instant)
    {
        for (int row = 0; row < itemCount; row++)
            entries[row].Highlight(RowToIndex(row) == selectedIndex, instant);
    }

    /// <summary> Shows count/capacity, or count only when unlimited. </summary>
    private void RefreshCounter()
    {
        if (counterLabel == null) return;

        int capacity = StashHandler.CurrentCapacity;
        counterLabel.text = capacity > 0 ? $"{itemCount}/{capacity}" : itemCount.ToString();
    }

    /// <summary> Returns the pooled entry for a row, spawning if needed. </summary>
    private EquipmentItemNameUI GetEntry(int row)
    {
        while (entries.Count <= row)
        {
            int rowIndex = entries.Count;
            EquipmentItemNameUI entry = Instantiate(namePrefab, listRoot);
            entry.SetTapAction(() => OnEntryTapped(rowIndex));
            entries.Add(entry);
        }

        return entries[row];
    }

    /// <summary> Newest item sits on the top row. </summary>
    private int RowToIndex(int row) => itemCount - 1 - row;

    private static string ItemName(ItemDataSO item) => item != null ? item.DisplayName : string.Empty;
}
