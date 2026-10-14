using TMPro;
using UnityEngine;

/// <summary> Shows the carried item's name under the reticle on pickup or switch. </summary>
[RequireComponent(typeof(CanvasGroup))]
public class HeldItemNameUI : MonoBehaviour
{
    [Tooltip("Label showing the item name.")]
    [SerializeField] private TMP_Text label;
    [Tooltip("Seconds the name stays on screen.")]
    [SerializeField, Min(0f)] private float showSeconds = 3f;
    [Tooltip("Seconds to fade fully in or out.")]
    [SerializeField, Min(0f)] private float fadeDuration = 0.2f;

    private CanvasGroup _group;
    private SortableItem_SO _carried;
    private int _count;
    private bool _primed; // first state is recorded silently, so loads don't pop
    private float _hideAt;

    private void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.interactable = false;
        _group.blocksRaycasts = false;
    }

    private void OnEnable() => Localizer.LanguageChanged += Relabel;

    private void OnDisable() => Localizer.LanguageChanged -= Relabel;

    private void Update()
    {
        Track();

        float target = Time.unscaledTime < _hideAt ? 1f : 0f;
        if (Mathf.Approximately(_group.alpha, target)) return;
        float step = fadeDuration <= 0f ? 1f : Time.unscaledDeltaTime / fadeDuration;
        _group.alpha = Mathf.MoveTowards(_group.alpha, target, step);
    }

    /// <summary> Shows the name when the selected item changes or a item is picked up. </summary>
    private void Track()
    {
        StashHandler stash = Player.Instance != null ? Player.Instance.StashHandler : null;
        InventoryHandler inventory = stash != null ? stash.Inventory : null;
        if (inventory == null) return;

        SortableItem_SO carried = inventory.GetItemAt(stash.SelectedIndex) as SortableItem_SO;
        int count = inventory.Count;

        if (_primed)
        {
            bool pickedUp = count > _count;
            if (carried == null) _hideAt = 0f;
            else if (carried != _carried || pickedUp) Show(carried);
        }

        _primed = true;
        _carried = carried;
        _count = count;
    }

    private void Show(SortableItem_SO item)
    {
        if (label != null) label.SetLocalized(item.DisplayName);
        _hideAt = Time.unscaledTime + showSeconds;
    }

    /// <summary> Redraws the shown name in the new language. </summary>
    private void Relabel()
    {
        if (_carried != null && label != null) label.SetLocalized(_carried.DisplayName);
    }
}
