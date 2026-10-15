using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;

/// <summary> Shelf accepting any item in a free spot; shows status for its category. </summary>
[SelectionBase]
public class SortingShelf : MonoBehaviour, IInteractable, IInteractableSecondary, IHoverable
{
    [Header("Action Prompt")]
    [Tooltip("Primary prompt verb; empty hides it.")]
    [SerializeField] private string actionPrompt = "Take back";
    [Tooltip("Secondary prompt verb; empty hides it.")]
    [SerializeField] private string secondaryActionPrompt = "Place";
    [Tooltip("Show the secondary prompt above the primary one.")]
    [SerializeField] private bool secondaryPromptFirst = true;
    [Tooltip("Label above the prompts; empty hides it.")]
    [SerializeField] private string hoverPrompt = "";

    /// <summary> Primary prompt verb. </summary>
    public string ActionPrompt => Localizer.FromEnglish("prompt", actionPrompt);
    private const string PlaceOnKey = "prompt.place_item_on";

    private SortableItem_SO _promptItem; // cache for the "Place {item} On {section}" prompt
    private string _promptCode;
    private string _promptText;

    /// <summary> Secondary prompt; "Place {item} On {section}" while a item is held. </summary>
    public string SecondaryActionPrompt
    {
        get
        {
            PlayerSortingItemsBehaviour sorting = Player.Instance != null ? Player.Instance.SortingBehaviour : null;
            if (string.IsNullOrEmpty(secondaryActionPrompt) || (_catagory == null && acceptedItem == null) || sorting == null
                || !sorting.TryGetHeldSortable(out SortableItem_SO data))
                return Localizer.FromEnglish("prompt", secondaryActionPrompt);

            if (data == _promptItem && Localizer.CurrentCode == _promptCode) return _promptText;
            _promptItem = data;
            _promptCode = Localizer.CurrentCode;
            _promptText = Localizer.FormatText(Localizer.GetOr(PlaceOnKey, "Place {0} On {1}"), data.DisplayName,
                acceptedItem != null ? acceptedItem.DisplayName : _catagory.DisplayName);
            return _promptText;
        }
    }
    /// <summary> Lists the secondary prompt first when true. </summary>
    public bool SecondaryPromptFirst => secondaryPromptFirst;
    /// <summary> Label shown above the prompts. </summary>
    public string HoverPrompt => Localizer.FromEnglish("prompt", hoverPrompt);

    [Tooltip("Category this shelf accepts.")]
    [SerializeField] private Catagory_SO _catagory;
    [Tooltip("Exact variant accepted by this shelf. When set, it takes precedence over category.")]
    [SerializeField] private SortableItem_SO acceptedItem;

    [Tooltip("Spots items land on; manual placement follows the player's aim.")]
    [SerializeField] private List<Transform> placingSpots = new List<Transform>();

    [Header("Placement Jump")]
    [SerializeField] private float jumpDuration = 0.45f;
    [SerializeField] private float jumpPower = 0.5f;
    [SerializeField] private Ease jumpEase = Ease.OutQuad;

    [Header("Full Feedback")]
    [SerializeField] private Vector3 fullPunchScale = new Vector3(0.25f, 0.25f, 0.25f);
    [SerializeField] private float fullPunchDuration = 0.35f;
    [SerializeField] private int fullPunchVibrato = 8;
    [SerializeField] private float fullPunchElasticity = 0.8f;
    [Tooltip("Delay between each item's punch, in spot order.")]
    [SerializeField] private float fullPunchStagger = 0.06f;
    [Tooltip("Outline color each item flashes during the Full feedback.")]
    [SerializeField] private Color fullFlashColor = Color.yellow;
    [Tooltip("Seconds the outline stays on before restoring.")]
    [SerializeField] private float fullFlashDuration = 0.3f;

    [Header("Hover Hologram")]
    [Tooltip("Resources path of the hologram material.")]
    [SerializeField] private string hologramMaterialPath = HologramCreator.DefaultMaterialPath;

    [Header("Status Display")]
    [Tooltip("Shows the name of the item occupying the shelf.")]
    [SerializeField] private TMP_Text itemNameText;
    [Tooltip("Enabled while the shelf holds the correct category.")]
    [SerializeField] private GameObject correctIndicator;
    [Tooltip("Enabled while the shelf holds a wrong category.")]
    [SerializeField] private GameObject wrongIndicator;
    [Tooltip("Enabled once the shelf is completed; replaces Correct.")]
    [SerializeField] private GameObject completedIndicator;

    [Header("Save")]
    [Tooltip("Unique id in the level save. Auto-filled in the editor.")]
    [SerializeField] private string saveId;

    private SortableItem_SO _cachedItem; // representative occupant for the name display
    private PlacedOnShelfItemBase[] _occupants;
    private readonly List<PlacedOnShelfItemBase> _placeOrder = new List<PlacedOnShelfItemBase>(); // oldest to newest
    private PlacedOnShelfItemBase _hoveredItem; // latest item outlined while the shelf is hovered
    private bool _isHovered;
    private GameObject _hologram; // preview of the held item on the aimed free spot
    private int _hologramSpot = -1;
    private SortableItem_SO _hologramData;
    private InteractionHandler _interaction;
    private Sequence _fullSequence;

    /// <summary> Fired on any shelf when a correct item is placed. </summary>
    public static event Action<SortingShelf, SortableItem_SO> OnRightItemPlaced;

    /// <summary> Fired on any shelf when a correct item is removed. </summary>
    public static event Action<SortingShelf, SortableItem_SO> OnRightItemRemoved;

    /// <summary> Fired on any shelf when a item of another category is placed. </summary>
    public static event Action<SortingShelf, SortableItem_SO> OnWrongItemPlaced;

    /// <summary> Fired on any shelf when a wrong-category item is taken off it. </summary>
    public static event Action<SortingShelf, SortableItem_SO> OnWrongItemRemoved;

    /// <summary> Fired on any shelf when it fills with one correct type. </summary>
    public static event Action<SortingShelf> OnShelfFilled;

    /// <summary> True while every spot holds a settled item of the correct type. </summary>
    public bool IsCompleted => _occupants != null && IsFullOfOneType();

    /// <summary> Global switch: world items hitting any shelf place themselves on it. </summary>
    public static bool DistanceShelfingEnabled { get; private set; }

    /// <summary> Turns distance shelfing on or off for every shelf. </summary>
    public static void SetDistanceShelfingEnabled(bool enabled) => DistanceShelfingEnabled = enabled;

    /// <summary> Category this shelf accepts. </summary>
    public Catagory_SO Catagory => _catagory;
    public SortableItem_SO AcceptedItem => acceptedItem;

    /// <summary> Representative item type on this shelf, or null while empty. </summary>
    public SortableItem_SO CachedItem => _cachedItem;

    /// <summary> Unique id of this shelf in the level save. </summary>
    public string SaveId => saveId;

    /// <summary> True while at least one spot is free. </summary>
    public bool HasFreeSpot => _occupants != null && GetNextFreeSpot() >= 0;

    /// <summary> Number of placing spots on this shelf. </summary>
    public int SpotCount => placingSpots.Count;

    /// <summary> True when the spot exists and holds no item. </summary>
    public bool IsSpotFree(int spot) => _occupants != null && spot >= 0 && spot < _occupants.Length && _occupants[spot] == null;

    /// <summary> Transform of a placing spot, or null if out of range. </summary>
    public Transform GetSpot(int spot) => spot >= 0 && spot < placingSpots.Count ? placingSpots[spot] : null;
    public PlacedOnShelfItemBase GetOccupant(int spot) => _occupants != null && spot >= 0 && spot < _occupants.Length ? _occupants[spot] : null;
    public bool CanAccept(SortableItem_SO data, int spot, out string reason)
    {
        reason = null;
        if (data == null || data.PlacedOnShelfPrefab == null) reason = "Collectible unavailable";
        else if (!IsSpotFree(spot) || GetSpot(spot) == null) reason = "Choose an empty slot";
        else if (TryGetComponent<TinyRiders.ThemeShelfBinding>(out var theme) && !theme.Accepts(data, out reason)) return false;
        return reason == null;
    }

    // Imported shelf markers already face the aisle. Keep that orientation for
    // placement, previews, and restored saves instead of turning cars back inward.
    private Quaternion PlacementRotation => Quaternion.identity;

    private void Awake()
    {
        _occupants = new PlacedOnShelfItemBase[placingSpots.Count];
        // Existing generated scenes get the same aiming surface as newly built scenes.
        if (acceptedItem != null && placingSpots.Count == 7) EnsureCarPlacementSurface();
        RefreshStatus();
    }

    private void EnsureCarPlacementSurface()
    {
        Transform surface = transform.Find("Car Placement Surface");
        if (surface == null)
        {
            GameObject go = new GameObject("Car Placement Surface");
            surface = go.transform;
            surface.SetParent(transform, false);
        }
        surface.gameObject.layer = gameObject.layer;
        Transform display = transform.Find("TR_Shelf_Base_01");
        bool newDisplay = display != null;
        float face = newDisplay && Mathf.Abs(Mathf.DeltaAngle(display.localEulerAngles.y, 0f)) < 90f ? 1f : -1f;
        surface.localPosition = newDisplay
            ? new Vector3(0f, 1.82f, face * 0.49f)
            : new Vector3(0f, 0.91f, 0.34f);
        surface.localRotation = Quaternion.identity;
        BoxCollider collider = surface.GetComponent<BoxCollider>();
        if (collider == null) collider = surface.gameObject.AddComponent<BoxCollider>();
        collider.center = Vector3.zero;
        collider.size = newDisplay
            ? new Vector3(3.48f, 3.24f, 0.08f)
            : new Vector3(1.45f, 1.85f, 0.08f);
        if (surface.GetComponent<SortingShelfPlacementSurface>() == null)
            surface.gameObject.AddComponent<SortingShelfPlacementSurface>();
    }

    private void OnEnable() => Localizer.LanguageChanged += RefreshStatus;

    private void OnDisable() => Localizer.LanguageChanged -= RefreshStatus;

    /// <summary> Fills results with (spot, item) pairs, oldest placement first. </summary>
    public void GetPlacedItems(List<(int spot, SortableItem_SO data)> results)
    {
        results.Clear();
        foreach (PlacedOnShelfItemBase item in _placeOrder)
        {
            int spot = Array.IndexOf(_occupants, item);
            if (item != null && spot >= 0) results.Add((spot, item.Data));
        }
    }

    /// <summary> Instantly places a saved item. No jump, sounds or coin events. </summary>
    public bool RestoreItem(int spot, SortableItem_SO data)
    {
        if (!CanAccept(data, spot, out _)) return false;

        PlacedOnShelfItemBase item = Instantiate(data.PlacedOnShelfPrefab);
        item.Init(data);
        item.AttachTo(this);
        if (!IsCorrectCatagory(data)) item.PlayWrongFeedback(false); // stay red after reload
        item.Settle();

        Transform t = item.transform;
        t.SetParent(placingSpots[spot], true); // keep world scale, like AddItem
        t.localPosition = Vector3.zero;
        t.localRotation = PlacementRotation;

        _occupants[spot] = item;
        _placeOrder.Add(item);
        if (_cachedItem == null) _cachedItem = data;
        RefreshStatus();
        return true;
    }
    /// <summary> Overrides the accepted category at runtime. </summary>
    public void SetCatagory(Catagory_SO catagory)
    {
        _catagory = catagory;
        _promptItem = null; // rebuild the place prompt
        if (_occupants != null) RefreshStatus();
    }

    public void SetAcceptedItem(SortableItem_SO item)
    {
        acceptedItem = item;
        _promptItem = null;
        if (_occupants != null) RefreshStatus();
    }

    /// <summary> Gives the most recently placed item back to the player. </summary>
    public void OnInteract(InteractionHandler handler)
    {
        if (_placeOrder.Count > 0) TryTakeBack(_placeOrder[_placeOrder.Count - 1]);
    }

    /// <summary> Starts outlining the latest settled item and shows the held item's hologram. </summary>
    public void OnHoverEnter()
    {
        _isHovered = true;
        RefreshHoverOutline();
        RefreshPlacementPreview();
    }

    /// <summary> Keeps the outline on the latest item; shows a hologram if none exists. </summary>
    public void OnHoverStay()
    {
        RefreshHoverOutline();
        RefreshPlacementPreview();
    }

    /// <summary> Clears the outline and removes the hologram. </summary>
    public void OnHoverExit()
    {
        _isHovered = false;
        RefreshHoverOutline();
        ClearPlacementPreview();
    }

    /// <summary>Moves the held item's preview to the free spot nearest the reticle.</summary>
    internal void RefreshPlacementPreview()
    {
        PlayerSortingItemsBehaviour sorting = Player.Instance != null ? Player.Instance.SortingBehaviour : null;
        if (sorting == null || !sorting.TryGetHeldSortable(out SortableItem_SO data) ||
            data == null || data.PlacedOnShelfPrefab == null)
        {
            ClearHologram();
            return;
        }

        int spot = GetAimedFreeSpot(GetInteraction());
        if (GetComponent<TinyRiders.ThemeShelfBinding>() != null && !CanAccept(data, spot, out _))
        { ClearHologram(); return; }
        if (spot < 0)
        {
            ClearHologram();
            return;
        }

        if (_hologram != null && _hologramData != data) ClearHologram();
        if (_hologram != null && _hologramSpot == spot) return;
        if (_hologram != null)
        {
            _hologram.transform.SetParent(placingSpots[spot], true);
            _hologram.transform.localPosition = Vector3.zero;
            _hologram.transform.localRotation = PlacementRotation;
            _hologramSpot = spot;
            return;
        }

        _hologram = HologramCreator.Create(data.PlacedOnShelfPrefab.gameObject, hologramMaterialPath);
        if (_hologram == null) return;
        _hologramData = data;

        // Keep world scale, like AddItem, then snap to the spot.
        Transform t = _hologram.transform;
        t.SetParent(placingSpots[spot], true);
        t.localPosition = Vector3.zero;
        t.localRotation = PlacementRotation;
        _hologramSpot = spot;
    }

    internal void ClearPlacementPreview() => ClearHologram();

    /// <summary> Destroys the current hologram, if any. </summary>
    private void ClearHologram()
    {
        if (_hologram == null) return;
        Destroy(_hologram);
        _hologram = null;
        _hologramSpot = -1;
        _hologramData = null;
    }

    private InteractionHandler GetInteraction()
    {
        if (_interaction == null && Player.Instance != null)
            _interaction = Player.Instance.GetComponentInChildren<InteractionHandler>(true);
        return _interaction;
    }

    private int GetAimedFreeSpot(InteractionHandler handler)
    {
        if (handler == null || !handler.HasCurrentHit) return GetNextFreeSpot();

        Vector3 point = handler.CurrentHitPoint;
        int nearest = -1;
        float nearestDistance = float.PositiveInfinity;
        for (int i = 0; i < _occupants.Length; i++)
        {
            if (placingSpots[i] == null || (_occupants[i] != null && GetComponent<TinyRiders.ThemeShelfBinding>() == null)) continue;
            float distance = (placingSpots[i].position - point).sqrMagnitude;
            if (distance >= nearestDistance) continue;
            nearest = i;
            nearestDistance = distance;
        }
        return nearest;
    }

    /// <summary> Moves the hover outline to the latest settled item, or clears it. </summary>
    private void RefreshHoverOutline()
    {
        PlacedOnShelfItemBase latest = _placeOrder.Count > 0 ? _placeOrder[_placeOrder.Count - 1] : null;
        PlacedOnShelfItemBase target = _isHovered && latest != null && latest.IsSettled ? latest : null;
        if (target == _hoveredItem) return;

        if (_hoveredItem != null) _hoveredItem.OnHoverExit();
        _hoveredItem = target;
        if (_hoveredItem != null) _hoveredItem.OnHoverEnter();
    }

    /// <summary>Places the held item on the aimed free spot, regardless of its type.</summary>
    public void OnInteractSecondary(InteractionHandler handler)
    {
        PlayerSortingItemsBehaviour sorting = Player.Instance != null ? Player.Instance.SortingBehaviour : null;
        if (sorting == null || !sorting.TryGetHeldSortable(out SortableItem_SO data)) return;

        int spot = GetAimedFreeSpot(handler);
        if (GetComponent<TinyRiders.ThemeShelfBinding>() != null && !CanAccept(data, spot, out string reason))
        {
            TinyRiders.ThemedCollectionSession.Instance?.Report(reason);
            FloatingTextEffect.Show(reason, transform.position + Vector3.up * 3, new Color(1f, .65f, .4f));
            return;
        }
        if (spot < 0)
        {
            OnShelfFull();
            return;
        }

        if (!sorting.TryGetPlacedItemFromHand(out PlacedOnShelfItemBase placed)) return;
        ClearHologram();
        AddItem(placed, spot);
    }

    /// <summary> Shelves a world item that hits this shelf. </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (!DistanceShelfingEnabled) return;
        WorldItemBase item = collision.collider.GetComponentInParent<WorldItemBase>();
        if (item == null) return;
        TryDistanceShelf(item);
    }

    /// <summary> Shelves any valid world item when a spot is free. </summary>
    public bool TryDistanceShelf(WorldItemBase item)
    {
        if (!DistanceShelfingEnabled || item == null || !item.gameObject.activeSelf) return false;

        SortableItem_SO data = item.Data as SortableItem_SO;
        if (data == null || data.PlacedOnShelfPrefab == null) return false;

        int spot = GetNextFreeSpot();
        if (spot < 0) return false;
        if (GetComponent<TinyRiders.ThemeShelfBinding>() != null &&
            (!CanAccept(data, spot, out _) || !(TinyRiders.ThemedCollectionSession.Instance?.CanPlace(TinyRiders.ThemedWorldInstance.Read(item)) ?? false))) return false;

        item.transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
        PlacedOnShelfItemBase placed = Instantiate(data.PlacedOnShelfPrefab, position, rotation);
        placed.Init(data);
        TinyRiders.ThemedWorldInstance.Stamp(placed, TinyRiders.ThemedWorldInstance.Read(item));

        item.gameObject.SetActive(false); // blocks repeat hits before Destroy runs
        Destroy(item.gameObject);

        AddItem(placed, spot);
        return true;
    }

    /// <summary> Claims the spot and jumps the item onto it. </summary>
    public void AddItem(PlacedOnShelfItemBase item, int spot)
    {
        if (item == null || !CanAccept(item.Data, spot, out _)) return;
        bool themed = GetComponent<TinyRiders.ThemeShelfBinding>() != null;
        if (themed && !(TinyRiders.ThemedCollectionSession.Instance?.CanPlace(TinyRiders.ThemedWorldInstance.Read(item)) ?? false)) return;
        bool correct = IsCorrectCatagory(item.Data);

        _occupants[spot] = item;
        _placeOrder.Add(item);
        if (_cachedItem == null) _cachedItem = item.Data;
        NotifyRightItemPlaced(item.Data);

        item.AttachTo(this);
        if (!correct) OnWrongCatagory(item);
        LevelSaveManager.MarkDirty();
        Transform t = item.transform;
        t.SetParent(placingSpots[spot], true);
        if (themed) TinyRiders.ThemedCollectionSession.Instance?.Placed(item, this, spot);
        if (correct) PlayPlacementSFX(placingSpots[spot].position);

        DOTween.Sequence()
            .Join(t.DOLocalJump(Vector3.zero, jumpPower, 1, jumpDuration).SetEase(jumpEase))
            .Join(t.DOLocalRotateQuaternion(PlacementRotation, jumpDuration).SetEase(jumpEase))
            .SetLink(item.gameObject)
            .OnComplete(() =>
            {
                item.Settle();
                item.OnPlaceFeedback();
                if (correct) item.PlayCorrectLanded();
                else item.PlayWrongLanded();
                RefreshStatus();
                if (!IsFullOfOneType()) return;
                OnShelfCompleted();
                OnShelfFilled?.Invoke(this);
            });
    }

    /// <summary> Returns the item to the player's hand and frees its spot. </summary>
    public bool TryTakeBack(PlacedOnShelfItemBase item)
    {
        int spot = System.Array.IndexOf(_occupants, item);
        if (spot < 0) return false;

        PlayerSortingItemsBehaviour sorting = Player.Instance != null ? Player.Instance.SortingBehaviour : null;
        if (sorting == null || !sorting.TryCollectPlacedItem(item)) return false;

        RemoveItem(spot);
        Destroy(item.gameObject);
        LevelSaveManager.MarkDirty();
        return true;
    }

    /// <summary> Frees a spot and refreshes the representative item from remaining occupants. </summary>
    private void RemoveItem(int spot)
    {
        PlacedOnShelfItemBase removed = _occupants[spot];
        _placeOrder.Remove(removed);
        _occupants[spot] = null;
        if (removed != null) NotifyRightItemRemoved(removed.Data);
        _fullSequence?.Kill(); // no longer full; stop pending punches
        _cachedItem = null;
        foreach (PlacedOnShelfItemBase occupant in _placeOrder)
        {
            if (occupant == null) continue;
            _cachedItem = occupant.Data;
            break;
        }
        RefreshStatus();
    }

    /// <summary> True when the item counts as correct on this shelf. </summary>
    public bool IsCorrectItem(SortableItem_SO data) => IsCorrectCatagory(data);

    /// <summary> True when the item's category matches this shelf. </summary>
    protected virtual bool IsCorrectCatagory(SortableItem_SO data)
    {
        var themed = GetComponent<TinyRiders.ThemeShelfBinding>();
        if (themed != null)
            return TinyRiders.ThemedCollectionSession.Instance?.Definition(data)?.themeId == themed.themeId;
        if (acceptedItem != null) return data == acceptedItem;
        return _catagory != null && data != null && data.Catagory == _catagory;
    }

    /// <summary> Fires OnRightItemPlaced when the item fits this shelf, else OnWrongItemPlaced. </summary>
    private void NotifyRightItemPlaced(SortableItem_SO data)
    {
        if (IsCorrectCatagory(data)) OnRightItemPlaced?.Invoke(this, data);
        else OnWrongItemPlaced?.Invoke(this, data);
    }

    /// <summary> Fires OnRightItemRemoved when the item fit this shelf, else OnWrongItemRemoved. </summary>
    private void NotifyRightItemRemoved(SortableItem_SO data)
    {
        if (IsCorrectCatagory(data)) OnRightItemRemoved?.Invoke(this, data);
        else OnWrongItemRemoved?.Invoke(this, data);
    }

    /// <summary> Updates the name text and enables one status indicator. </summary>
    protected void RefreshStatus()
    {
        if (_occupants == null) return; // Awake hasn't run yet

        bool hasItem = false;
        bool hasWrongItem = false;
        foreach (PlacedOnShelfItemBase item in _occupants)
        {
            if (item == null) continue;
            hasItem = true;
            if (!IsCorrectCatagory(item.Data)) hasWrongItem = true;
        }
        bool completed = hasItem && IsFullOfOneType();
        bool correct = hasItem && !hasWrongItem;

        if (itemNameText != null)
        {
            itemNameText.SetLocalized(hasItem && _cachedItem != null ? _cachedItem.DisplayName : string.Empty);
            itemNameText.enabled = hasItem; // empty text still costs a draw call
        }
        if (correctIndicator != null) correctIndicator.SetActive(correct && !completed);
        if (wrongIndicator != null) wrongIndicator.SetActive(hasItem && !correct);
        if (completedIndicator != null) completedIndicator.SetActive(completed);
    }

    /// <summary> True when every spot holds a settled, correct item of the same type. </summary>
    private bool IsFullOfOneType()
    {
        if (_occupants.Length == 0) return false;
        SortableItem_SO firstType = null;

        for (int i = 0; i < _occupants.Length; i++)
        {
            PlacedOnShelfItemBase item = _occupants[i];
            if (item == null || !item.IsSettled || !IsCorrectCatagory(item.Data)) return false;
            if (i == 0) firstType = item.Data;
            else if (GetComponent<TinyRiders.ThemeShelfBinding>() == null && item.Data != firstType) return false;
        }
        return true;
    }

    /// <summary> Punches every item's scale in a staggered wave, in spot order. </summary>
    private void PlayFullFeedback()
    {
        _fullSequence?.Kill();
        _fullSequence = DOTween.Sequence().SetLink(gameObject);

        for (int i = 0; i < _occupants.Length; i++)
        {
            PlacedOnShelfItemBase item = _occupants[i];
            _fullSequence.InsertCallback(i * fullPunchStagger, () => PunchFull(item));
        }
    }

    /// <summary> Snaps running scale tweens, then punches and flashes one item. </summary>
    private void PunchFull(PlacedOnShelfItemBase item)
    {
        if (item == null) return;

        item.DetachForAnimation();
        Transform t = item.transform;
        t.DOComplete(); // avoid scale drift from the landing punch
        t.DOPunchScale(fullPunchScale, fullPunchDuration, fullPunchVibrato, fullPunchElasticity)
            .SetLink(item.gameObject);
        item.FlashOutline(fullFlashColor, fullFlashDuration);
    }

    /// <summary> First free spot index, or -1 when full. </summary>
    private int GetNextFreeSpot()
    {
        for (int i = 0; i < _occupants.Length; i++)
            if (_occupants[i] == null) return i;
        return -1;
    }

#if UNITY_EDITOR
    /// <summary> Gives scene shelves a unique save id; duplicated shelves get a fresh one. </summary>
    private void OnValidate()
    {
        if (Application.isPlaying || UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this)) return;
        if (UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(gameObject) != null) return;
        if (!string.IsNullOrEmpty(saveId) && !HasDuplicateSaveId()) return;

        saveId = Guid.NewGuid().ToString("N");
        UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        UnityEditor.EditorUtility.SetDirty(this);
    }

    /// <summary> True if another shelf in this scene shares the save id. </summary>
    private bool HasDuplicateSaveId()
    {
        foreach (SortingShelf other in FindObjectsByType<SortingShelf>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (other != this && other.gameObject.scene == gameObject.scene && other.saveId == saveId) return true;
        return false;
    }

    /// <summary> Test-only: instantly fills every free spot with the given item. Returns count placed. </summary>
    public int DebugFill(SortableItem_SO data)
    {
        if (data == null || data.PlacedOnShelfPrefab == null) return 0;

        SyncOccupantsForDebug();
        int placed = 0;
        for (int i = 0; i < _occupants.Length; i++)
        {
            if (_occupants[i] != null || placingSpots[i] == null) continue;

            PlacedOnShelfItemBase item = SpawnDebugItem(data.PlacedOnShelfPrefab);
            item.Init(data);
            item.AttachTo(this);
            if (!IsCorrectCatagory(data)) item.PlayWrongFeedback(false);
            item.Settle();

            Transform t = item.transform;
            t.SetParent(placingSpots[i], true); // keep world scale, like AddItem
            t.localPosition = Vector3.zero;
            t.localRotation = PlacementRotation;

            _occupants[i] = item;
            _placeOrder.Add(item);
            placed++;
            if (Application.isPlaying) NotifyRightItemPlaced(data);
        }

        if (placed > 0 && _cachedItem == null) _cachedItem = data;
        RefreshStatus();
        return placed;
    }

    /// <summary> Test-only: destroys every item on the shelf. Returns count removed. </summary>
    public int DebugEmpty()
    {
        SyncOccupantsForDebug();

        int removed = 0;
        for (int i = 0; i < _occupants.Length; i++)
        {
            if (_occupants[i] == null) continue;

            GameObject go = _occupants[i].gameObject;
            if (Application.isPlaying) NotifyRightItemRemoved(_occupants[i].Data);
            _occupants[i] = null;
            if (Application.isPlaying) Destroy(go);
            else UnityEditor.Undo.DestroyObjectImmediate(go);
            removed++;
        }

        _placeOrder.Clear();
        _cachedItem = null;
        RefreshStatus();
        return removed;
    }

    /// <summary> Spawns a prefab instance; undoable prefab link in edit mode. </summary>
    private PlacedOnShelfItemBase SpawnDebugItem(PlacedOnShelfItemBase prefab)
    {
        if (Application.isPlaying) return Instantiate(prefab);

        GameObject go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab.gameObject, gameObject.scene);
        UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Fill Shelf");
        return go.GetComponent<PlacedOnShelfItemBase>();
    }

    /// <summary> Rebuilds occupants from spot children when Awake hasn't run (edit mode). </summary>
    private void SyncOccupantsForDebug()
    {
        if (Application.isPlaying && _occupants != null) return;

        _occupants = new PlacedOnShelfItemBase[placingSpots.Count];
        _placeOrder.Clear();
        _cachedItem = null;
        for (int i = 0; i < placingSpots.Count; i++)
        {
            if (placingSpots[i] == null) continue;

            _occupants[i] = placingSpots[i].GetComponentInChildren<PlacedOnShelfItemBase>(true);
            if (_occupants[i] != null) _placeOrder.Add(_occupants[i]);
            if (_cachedItem == null && _occupants[i] != null) _cachedItem = _occupants[i].Data;
        }
    }
#endif

    /// <summary> Hook: a wrong-category item was placed; flashes it red and buzzes. </summary>
    protected virtual void OnWrongCatagory(PlacedOnShelfItemBase item)
    {
        item.PlayWrongFeedback();
    }

    /// <summary> Legacy hook retained for subclass compatibility; mixed types are now accepted. </summary>
    protected virtual void OnWrongItemType()
    {
        Debug.Log($"{name}: wrong item type.", this);
    }

    /// <summary> Hook: no free spot left. </summary>
    protected virtual void OnShelfFull()
    {
        Debug.Log($"{name}: shelf is full.", this);
    }

    /// <summary> Hook: last spot filled with one matching type; plays the Full feedback. </summary>
    protected virtual void OnShelfCompleted()
    {
        PlayFullFeedback();
        PlayCompleteSFX();
    }

    /// <summary> Plays a random placement clip at the given spot. </summary>
    private void PlayPlacementSFX(Vector3 position)
    {
        SimpleAudioManager audio = SimpleAudioManager.instance;
        if (audio == null || audio.GameplaySounds == null) return;

        audio.PlayJuicyClip(audio.GameplaySounds.GetRandomShelfPlacementSFX(), position);
    }

    /// <summary> Plays the shelf-complete jingle at unchanged pitch. </summary>
    private void PlayCompleteSFX()
    {
        SimpleAudioManager audio = SimpleAudioManager.instance;
        if (audio == null || audio.GameplaySounds == null) return;

        audio.PlayClipAt(audio.GameplaySounds.CompleteShelfSFX, transform.position);
    }
}
