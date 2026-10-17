using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Equips items into the hand, routes the two item buttons, and owns throwing.
/// Equipping a world item destroys it and spawns its hand form.
/// </summary>
[DefaultExecutionOrder(10000)]
public class ItemsHandler : MonoBehaviour
{
    public Player player;

    [Header("Hold Socket")]
    [Tooltip("Transform non-camera-fixed hand items are parented to.")]
    [SerializeField] private Transform holdSocket;

    [Tooltip("Transform items flagged CameraFixed are parented to.")]
    [SerializeField] private Transform cameraFixedSocket;

    [Tooltip("Show camera-fixed cars large in the lower-right foreground, above the item name.")]
    public bool FrameCameraFixedCars;

    [Header("Car Inspection")]
    [Tooltip("Degrees of car rotation per mouse pixel while holding the middle mouse button.")]
    [SerializeField, Min(0f)] private float inspectionSensitivity = .25f;
    private Quaternion _inspectionRotation = Quaternion.identity;

    /// <summary>Only settled, visible camera-fixed themed cars accept inspection input.</summary>
    public bool CanInspectHeldCar => isActiveAndEnabled && FrameCameraFixedCars &&
        CurrentEquipped != null && CurrentEquipped.Data != null && CurrentEquipped.Data.CameraFixed &&
        CurrentEquipped.Framing != null && CurrentEquipped.Framing.IsThemedCar &&
        !CurrentEquipped.IsFlyingToHand && _hiddenRenderers == null &&
        (_equipSequence == null || !_equipSequence.IsActive());

    private Camera _presentationCamera;

    [Tooltip("Layer applied to the spawned held item and all its children.")]
    [SerializeField] private LayerMask heldItemLayer;

    [Header("Equip Animation")]
    [Tooltip("Socket-local offset from the held pose the item enters from and exits to.")]
    [SerializeField] private Vector3 equipOffset = new Vector3(0f, -0.25f, 0f);

    [Tooltip("Seconds the item takes to rise into the hand. 0 = instant.")]
    [SerializeField] private float equipDuration = 0.2f;

    [Tooltip("Easing of the equip rise.")]
    [SerializeField] private Ease equipEase = Ease.OutBack;

    [Tooltip("Seconds the item takes to sink before it is removed. 0 = instant.")]
    [SerializeField] private float unequipDuration = 0.15f;

    [Tooltip("Easing of the unequip sink.")]
    [SerializeField] private Ease unequipEase = Ease.InQuad;

    [Header("Throw — Aim")]
    [Tooltip("Transform the throw aims from. Falls back to Camera.main if left empty.")]
    [SerializeField] private Transform aimSource;

    [Tooltip("Degrees the throw is pitched above the aim, for a lob.")]
    [SerializeField] private float throwArcAngle = 10f;

    [Header("Throw — Pull")]
    [Tooltip("0..1 of the way toward the equip start point the item is pulled before launch.")]
    [Range(0f, 1f)]
    [SerializeField] private float throwPullAmount = 0.5f;

    [Tooltip("Seconds the pull takes before launch. 0 = launch instantly.")]
    [SerializeField] private float throwPullDuration = 0.12f;

    [Tooltip("Easing of the pull.")]
    [SerializeField] private Ease throwPullEase = Ease.OutQuad;

    [Header("Throw — Input")]
    [Tooltip("Secondary-interactable targets block throwing. Auto-found under player if empty.")]
    [SerializeField] private InteractionHandler interaction;

    [Header("Throw — Force")]
    [Tooltip("Small forward impulse applied when dropping an item. Keep this low so it lands nearby.")]
    [FormerlySerializedAs("instantThrowForce")]
    [SerializeField] private float throwForce = 0.5f;

    [Tooltip("Random tumble impulse applied to the thrown item. 0 = none.")]
    [SerializeField] private float throwSpin = 0.15f;

    [Tooltip("Mass given to a thrown item whose prefab has no Rigidbody.")]
    [SerializeField] private float fallbackThrowMass = 1f;

    /// <summary> 0..1 progress of the active item hold, else 0. </summary>
    public float HoldProgress01 { get; private set; }

    // Item types that may not be thrown, keyed by whoever blocked them.
    private static readonly Dictionary<object, ItemDataSO> ThrowBlocks = new Dictionary<object, ItemDataSO>();

    /// <summary> Fired when an item leaves the hand as a throw. </summary>
    public static event System.Action<ItemDataSO> OnThrown;

    /// <summary> Fired when a throw is refused because the item type is blocked. </summary>
    public static event System.Action<ItemDataSO> OnThrowBlocked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        ThrowBlocks.Clear();
        OnThrown = null;
        OnThrowBlocked = null;
    }

    /// <summary> Stops this item type from being thrown until the owner unblocks it. </summary>
    public static void BlockThrow(object owner, ItemDataSO item)
    {
        if (owner == null) return;
        if (item == null) ThrowBlocks.Remove(owner);
        else ThrowBlocks[owner] = item;
    }

    /// <summary> Removes the owner's throw block, if any. </summary>
    public static void UnblockThrow(object owner)
    {
        if (owner != null) ThrowBlocks.Remove(owner);
    }

    /// <summary> True while any owner blocks throwing this item type. </summary>
    public static bool IsThrowBlocked(ItemDataSO item) => item != null && ThrowBlocks.ContainsValue(item);

    /// <summary> Sets the small forward impulse of a dropped item. </summary>
    public void SetThrowForce(float force) => throwForce = Mathf.Max(0f, force);

    /// <summary> Hand item currently equipped, or null. </summary>
    public HandItemBase CurrentEquipped => _equipped.Item;

    /// <summary> Socket hand items are parented to. </summary>
    public Transform HoldSocket => holdSocket;

    /// <summary> Socket this item is held on: camera-fixed when flagged, else the hold socket. </summary>
    public Transform GetSocketFor(ItemDataSO data)
        => data != null && data.CameraFixed && cameraFixedSocket != null ? cameraFixedSocket : holdSocket;

    /// <summary> Socket-local position the equip rise starts from. </summary>
    public Vector3 GetEquipStartPosition(ItemDataSO data)
        => CurrentEquipped != null && CurrentEquipped.Data == data
            ? GetEquipStartPosition(CurrentEquipped) : data.HeldPosition + equipOffset;

    /// <summary>Shared destination for equip, pickup flight, and the settled held model.</summary>
    public HeldItemPose GetHeldPose(HandItemBase hand)
    {
        if (hand.Framing == null) hand.Framing = new HeldItemFraming(hand.transform);
        if (_presentationCamera == null) _presentationCamera = Camera.main;
        if (FrameCameraFixedCars && hand.Data.CameraFixed &&
            hand.Framing.IsThemedCar &&
            hand.Framing.TryResolve(_presentationCamera, hand.transform.parent, hand.Data.HeldRotation,
                hand == CurrentEquipped ? _inspectionRotation : Quaternion.identity, out var fitted))
            return fitted;
        return new HeldItemPose(hand.Data.HeldPosition, Quaternion.Euler(hand.Data.HeldRotation), hand.Framing.OriginalScale);
    }

    private void ApplyHeldPose(HandItemBase hand)
    {
        var pose = GetHeldPose(hand);
        hand.transform.SetLocalPositionAndRotation(pose.Position, pose.Rotation);
        hand.transform.localScale = pose.Scale;
    }

    private void LateUpdate()
    {
        var hand = CurrentEquipped;
        if (hand != null) hand.SetHeldCarOutline(_hiddenRenderers == null && hand.IsEquipped);
        if (hand != null && FrameCameraFixedCars && hand.Data.CameraFixed && hand.Framing.IsThemedCar && !hand.IsFlyingToHand &&
            (_equipSequence == null || !_equipSequence.IsActive())) ApplyHeldPose(hand);
    }

    private Vector3 GetEquipStartPosition(HandItemBase hand) => GetHeldPose(hand).Position + equipOffset;

    private EquippedItem _equipped = EquippedItem.None;

    private Sequence _equipSequence;          // juicy rise of the current item
    private Sequence _unequipSequence;        // juicy sink of the previous item
    private GameObject _sinking;              // previous item sinking out
    private List<Renderer> _hiddenRenderers;  // current item's renderers, hidden until its rise starts

    private bool _holding;       // item hold in progress for this press
    private float _holdElapsed;  // seconds into the active item hold
    private bool _holdCompleted; // guards Complete against double-firing

    private void Awake()
    {
        if (holdSocket == null) holdSocket = transform;
        if (interaction == null && player != null) interaction = player.GetComponentInChildren<InteractionHandler>(true);
    }

    private void Update()
    {
        if (player == null || player.Input == null) return;

        Vector2 drag = player.Input.CarInspectionDelta;
        if (drag.sqrMagnitude > 0f)
            _inspectionRotation = (Quaternion.AngleAxis(drag.x * inspectionSensitivity, Vector3.up) *
                Quaternion.AngleAxis(-drag.y * inspectionSensitivity, Vector3.right) * _inspectionRotation).normalized;

        if (UpdateThrowInput()) return;

        if (!_equipped.IsValid) return;
        UpdateInput();
    }

    // ---- Equip / Unequip -------------------------------------------------

    /// <summary> Consumes a world item and equips its hand form. </summary>
    public void Equip(WorldItemBase worldItem)
    {
        if (worldItem == null || worldItem.Data == null) return;

        HandItemBase prefab = worldItem.Data.HandItem;
        if (prefab == null) return;

        Destroy(worldItem.gameObject);     // remove the world item
        SpawnHand(prefab, worldItem.Data); // spawn + equip the hand form
    }

    /// <summary> Equips the hand form described by this item data. </summary>
    public void Equip_ItemData(ItemDataSO Data)
    {
        HandItemBase prefab = Data.HandItem;
        if (prefab == null) return;

        SpawnHand(prefab, Data); // spawn + equip the hand form
    }

    /// <summary> Removes the current hand item instantly. </summary>
    public void Unequip()
    {
        HandItemBase item = Detach();
        if (item != null) Destroy(item.gameObject);
    }

    // ---- Juicy Equip / Unequip ------------------------------------------

    /// <summary> Consumes a world item and equips its hand form with the rise animation. </summary>
    public void Equip_Juicy(WorldItemBase worldItem)
    {
        if (worldItem == null || worldItem.Data == null || worldItem.Data.HandItem == null) return;

        ItemDataSO data = worldItem.Data;
        Destroy(worldItem.gameObject); // remove the world item
        Equip_Juicy(data);
    }

    /// <summary> Sinks the held item, then rises this item's hand form into place. </summary>
    public void Equip_Juicy(ItemDataSO data)
    {
        if (data == null || data.HandItem == null) return;

        UnEquip_Juicy(); // old item starts sinking; the rise waits for it
        HandItemBase hand = SpawnHand(data.HandItem, data);
        PlayEquipSequence(hand);
    }

    /// <summary> Sinks the current hand item by the offset, then destroys it. </summary>
    public void UnEquip_Juicy()
    {
        bool neverShown = _hiddenRenderers != null; // still waiting on a previous sink

        HandItemBase item = Detach();
        if (item == null) return;

        if (neverShown) Destroy(item.gameObject); // never seen, so nothing to animate
        else PlayUnequipSequence(item);
    }

    // ---- Shared equip plumbing ------------------------------------------

    /// <summary> Ends any hold, clears the equipped slot and returns the old item, or null. </summary>
    private HandItemBase Detach()
    {
        player?.Input?.CancelCarInspection();
        _inspectionRotation = Quaternion.identity;
        if (!_equipped.IsValid) return null;

        if (_holding) EndHold(_holdCompleted); // tear down while still cached

        HandItemBase item = _equipped.Item;
        item.SetHeldCarOutline(false);
        _equipped = EquippedItem.None;

        item.IsEquipped = false;
        item.OnUnequipped(this);

        KillEquipSequence(); // stop its rise, if any
        return item;
    }

    /// <summary> Instantiates a hand item, stamps its data, poses it, makes it current. </summary>
    private HandItemBase SpawnHand(HandItemBase prefab, ItemDataSO source)
    {
        Unequip(); // drop whatever is held (no-op after UnEquip_Juicy)

        // Camera-fixed items use their own socket; fall back to the hold socket.
        Transform socket = GetSocketFor(source);

        HandItemBase hand = Instantiate(prefab, socket);
        if (source != null) hand.SetData(source); // stamp so the item knows its data
        if (player != null && player.StashHandler != null)
            TinyRiders.ThemedWorldInstance.Stamp(hand,
                player.StashHandler.Inventory?.InstanceIdAt(player.StashHandler.SelectedIndex));
        Transform t = hand.transform;
        ApplyHeldPose(hand);

        ApplyLayerRecursive(t, LayerFromMask(heldItemLayer)); // put held item on chosen layer

        _equipped = new EquippedItem(hand);
        hand.IsEquipped = true;
        hand.OnEquipped(this);
        hand.SetHeldCarOutline(true);

        return hand;
    }

    // ---- Juicy sequences -------------------------------------------------

    /// <summary> Waits for any sink, then rises the item from the offset into its held pose. </summary>
    private void PlayEquipSequence(HandItemBase hand)
    {
        Transform t = hand.transform;
        t.localPosition = GetEquipStartPosition(hand);

        float wait = SinkTimeLeft();
        if (wait > 0f) _hiddenRenderers = HideRenderers(hand); // unseen until the old item is gone

        _equipSequence = DOTween.Sequence()
            .AppendInterval(wait)
            .AppendCallback(ShowHiddenRenderers)
            .Append(DOTween.To(() => 0f, progress =>
            {
                var pose = GetHeldPose(hand);
                t.localPosition = pose.Position + equipOffset * (1f - progress);
                t.localRotation = pose.Rotation;
                t.localScale = pose.Scale;
            }, 1f, equipDuration).SetEase(equipEase))
            .SetLink(hand.gameObject)
            .OnComplete(() => _equipSequence = null);
    }

    /// <summary> Sinks the item by the equip offset, then destroys it. </summary>
    private void PlayUnequipSequence(HandItemBase item)
    {
        FinishUnequipSequence(); // only one item sinks at a time

        Transform t = item.transform;
        Vector3 target = item.Data != null
            ? GetEquipStartPosition(item)
            : t.localPosition + equipOffset;

        _sinking = item.gameObject;
        _unequipSequence = DOTween.Sequence()
            .Append(t.DOLocalMove(target, unequipDuration).SetEase(unequipEase))
            .SetLink(_sinking)
            .OnComplete(FinishUnequipSequence);
    }

    /// <summary> Ends the sink right away and destroys the sinking item. </summary>
    private void FinishUnequipSequence()
    {
        _unequipSequence?.Kill();
        _unequipSequence = null;

        if (_sinking != null) Destroy(_sinking);
        _sinking = null;
    }

    /// <summary> Seconds left on the active sink, else 0. </summary>
    private float SinkTimeLeft()
        => _unequipSequence != null && _unequipSequence.IsActive()
            ? _unequipSequence.Duration() - _unequipSequence.Elapsed()
            : 0f;

    /// <summary> Kills the current item's rise and forgets its hidden renderers. </summary>
    private void KillEquipSequence()
    {
        _equipSequence?.Kill();
        _equipSequence = null;
        _hiddenRenderers = null;
    }

    /// <summary> Disables every enabled renderer on the item; returns them. </summary>
    private static List<Renderer> HideRenderers(HandItemBase hand)
    {
        var hidden = new List<Renderer>();
        foreach (Renderer r in hand.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled) continue;
            r.enabled = false;
            hidden.Add(r);
        }
        return hidden;
    }

    /// <summary> Re-enables renderers hidden while waiting for the sink. </summary>
    private void ShowHiddenRenderers()
    {
        if (_hiddenRenderers == null) return;
        foreach (Renderer r in _hiddenRenderers)
            if (r != null) r.enabled = true;
        _hiddenRenderers = null;
    }

    /// <summary> Sets layer on the object and every descendant. </summary>
    private static void ApplyLayerRecursive(Transform root, int layer)
    {
        if (root == null || layer < 0) return;
        root.gameObject.layer = layer;
        for (int i = 0; i < root.childCount; i++)
            ApplyLayerRecursive(root.GetChild(i), layer);
    }

    /// <summary> Returns first layer index in the mask, or -1 if none set. </summary>
    private static int LayerFromMask(LayerMask mask)
    {
        int value = mask.value;
        if (value == 0) return -1;
        for (int i = 0; i < 32; i++)
            if ((value & (1 << i)) != 0) return i;
        return -1;
    }

    // ---- Item button dispatch -------------------------------------------

    /// <summary> Routes the primary (LMB) and secondary (RMB) item buttons. </summary>
    private void UpdateInput()
    {
        PlayerInput input = player.Input;

        if (_holding)
        {
            TickHold();
            return;
        }

        // Secondary is always a tap; it never starts a hold.
        if (input.ClickSecondaryPressed)
            _equipped.Clickable?.OnSecondaryClick(this);

        if (!input.ClickPrimaryPressed) return;

        // Holdable wins so one press never triggers both.
        if (_equipped.Holdable != null)
            BeginHold();
        else
            _equipped.Clickable?.OnClick(this);
    }

    /// <summary> Starts charging the equipped item's hold. </summary>
    private void BeginHold()
    {
        _holding = true;
        _holdElapsed = 0f;
        _holdCompleted = false;
        HoldProgress01 = 0f;
        _equipped.Holdable.OnHoldStart(this);
    }

    /// <summary> Advances the active hold, completing once and feeding Stay until release. </summary>
    private void TickHold()
    {
        IItemHoldable holdable = _equipped.Holdable;

        // Button released (or input gated) → end (cancelled if never completed).
        if (!player.Input.ClickPrimaryHeld)
        {
            EndHold(_holdCompleted);
            return;
        }

        float duration = Mathf.Max(0f, holdable.HoldDuration);
        _holdElapsed += Time.deltaTime;
        HoldProgress01 = duration <= 0f ? 1f : Mathf.Clamp01(_holdElapsed / duration);

        // Complete fires once; Stay keeps feeding until release.
        if (!_holdCompleted && _holdElapsed >= duration)
        {
            _holdCompleted = true;
            holdable.OnHoldComplete(this);
        }

        holdable.OnHoldStay(this);
    }

    /// <summary> Ends the hold: cancels if unfinished, always fires End. </summary>
    private void EndHold(bool completed)
    {
        IItemHoldable holdable = _equipped.Holdable;

        _holding = false;
        _holdElapsed = 0f;
        _holdCompleted = false;
        HoldProgress01 = 0f;

        if (holdable == null) return;

        if (!completed) holdable.OnHoldCanceled(this);
        holdable.OnHoldEnd(this);
    }

    // ---- Throwing --------------------------------------------------------

    /// <summary> Throws the equipped item at fixed force on secondary interact press. </summary>
    private bool UpdateThrowInput()
    {
        if (!_equipped.IsValid || !player.Input.InteractSecondaryPressed) return false;
        if (TargetHasSecondaryInteract()) return false; // interact wins over throw

        ItemDataSO data = _equipped.Item.Data;
        if (IsThrowBlocked(data))
        {
            OnThrowBlocked?.Invoke(data);
            return true; // press consumed, nothing thrown
        }

        if (_holding) EndHold(_holdCompleted); // a throw supersedes an item hold

        ExecuteThrow(throwForce);
        return true;
    }

    /// <summary> True when the reticle target has its own secondary interaction. </summary>
    private bool TargetHasSecondaryInteract()
        => interaction != null && interaction.Current.InteractableSecondary != null;

    /// <summary> Spawns the world form on the hand item, pulls it back, then launches it. </summary>
    private void ExecuteThrow(float force)
    {
        if (!_equipped.IsValid) return;

        ItemDataSO data = _equipped.Item.Data;
        if (data == null || data.WorldItem == null) return;

        Transform hand = _equipped.Item.transform;
        Vector3 pullTarget = GetEquipStartPosition(_equipped.Item);
        WorldItemBase thrown = Instantiate(data.WorldItem, hand.position, hand.rotation);
        string instanceId = player.StashHandler?.Inventory?.InstanceIdAt(player.StashHandler.SelectedIndex);
        TinyRiders.ThemedWorldInstance.Stamp(thrown, instanceId);
        TinyRiders.ThemedCollectionSession.Instance?.Dropped(thrown, instanceId);
        // Spend the selected physical copy immediately; saving during a throw cannot save it twice.
        bool themedCopy = !string.IsNullOrEmpty(instanceId);

        Unequip(); // thrown copy replaces it, so no sink
        if (themedCopy) player.StashHandler.TryRemove(data);
        PlayThrowPull(thrown.gameObject, data, force, pullTarget);
    }

    /// <summary> Tweens the item toward the equip start point, following the socket, then releases. </summary>
    private void PlayThrowPull(GameObject thrown, ItemDataSO data, float force, Vector3 pullTarget)
    {
        Rigidbody body = GetThrowBody(thrown);
        body.isKinematic = true; // hold still while the pull drives it
        if (thrown.TryGetComponent(out WorldItemBase thrownItem)) thrownItem.BeginPowerControl(this, true);

        if (throwPullDuration <= 0f)
        {
            ReleaseThrow(thrown, body, data, force);
            return;
        }

        // Pose stored socket-local so the pull tracks player movement.
        Transform t = thrown.transform;
        Transform socket = GetSocketFor(data);
        Vector3 startLocal = socket.InverseTransformPoint(t.position);
        Quaternion localRot = Quaternion.Inverse(socket.rotation) * t.rotation;
        Vector3 endLocal = Vector3.Lerp(startLocal, pullTarget, throwPullAmount);

        DOTween.To(() => 0f, p =>
            {
                t.position = socket.TransformPoint(Vector3.LerpUnclamped(startLocal, endLocal, p));
                t.rotation = socket.rotation * localRot;
            }, 1f, throwPullDuration)
            .SetEase(throwPullEase)
            .SetLink(thrown)
            .OnKill(() => ReleaseThrow(thrown, body, data, force)); // also fires if the item is destroyed
    }

    /// <summary> Launches the pulled item, if it still exists, then spends it from the stash. </summary>
    private void ReleaseThrow(GameObject thrown, Rigidbody body, ItemDataSO data, float force)
    {
        if (thrown != null)
        {
            if (thrown.TryGetComponent(out WorldItemBase controlled))
                controlled.EndThrowControl(GetThrowDirection().normalized * force, Random.insideUnitSphere * throwSpin);
            else LaunchThrown(body, GetThrowDirection(), force);
            PlayThrowSFX();

            if (thrown.TryGetComponent(out WorldItemBase thrownItem))
            {
                if (!thrownItem.ThrowControlled) ItemPhysicsCulling.Track(thrownItem);
            }
        }

        if (player.StashHandler != null && string.IsNullOrEmpty(TinyRiders.ThemedWorldInstance.Read(thrown != null ? thrown.GetComponent<WorldItemBase>() : null)))
            player.StashHandler.TryRemove(data); // themed copies were spent before the throw animation
        OnThrown?.Invoke(data);
    }

    /// <summary> Plays a random throw clip from the gameplay sound library. </summary>
    private void PlayThrowSFX()
    {
        SimpleAudioManager audio = SimpleAudioManager.instance;
        if (audio == null || audio.GameplaySounds == null) return;

        audio.PlayJuicyGlobalClip(audio.GameplaySounds.GetRandomThrowSFX());
    }

    /// <summary> Arced throw direction from the aim source, or the body's forward. </summary>
    private Vector3 GetThrowDirection()
    {
        Transform aim = ResolveAimSource();
        if (aim == null) return transform.forward; // no camera to aim from

        return Quaternion.AngleAxis(-throwArcAngle, aim.right) * aim.forward;
    }

    /// <summary> Returns the thrown object's Rigidbody, adding one if missing. </summary>
    private Rigidbody GetThrowBody(GameObject thrown)
    {
        if (thrown.TryGetComponent(out Rigidbody body)) return body;

        body = thrown.AddComponent<Rigidbody>();
        body.mass = fallbackThrowMass;
        return body;
    }

    /// <summary> Releases the body with a small forward impulse. </summary>
    private void LaunchThrown(Rigidbody body, Vector3 direction, float force)
    {
        body.isKinematic = false;
        body.AddForce(direction.normalized * force, ForceMode.Impulse);

        if (throwSpin > 0f)
            body.AddTorque(Random.insideUnitSphere * throwSpin, ForceMode.Impulse);
    }

    /// <summary> Cached aim transform, resolved to Camera.main on first use. </summary>
    private Transform ResolveAimSource()
    {
        if (aimSource == null && Camera.main != null) aimSource = Camera.main.transform;
        return aimSource;
    }

    // ---- Lifetime --------------------------------------------------------

    /// <summary> Safety net: tear down a hold if disabled mid-press. </summary>
    private void OnDisable()
    {
        player?.Input?.CancelCarInspection();
        if (_holding) EndHold(_holdCompleted);
    }

    /// <summary> Socket this item would be held on. </summary>
    public Transform GetItemHoldPosition(ItemDataSO item)
    {
        if (item.CameraFixed) return cameraFixedSocket;
        else return holdSocket;
    }
}
