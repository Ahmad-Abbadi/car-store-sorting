using UnityEngine;
using DG.Tweening;
using GameFeedback;

/// <summary> Collects an item: stashes it, then equips it in the hand. </summary>
public class ItemCollectingBehaviour : MonoBehaviour
{
    [Tooltip("Player whose stash and item handler receive the item.")]
    [SerializeField] private Player player;

    [Header("Pickup flight")]
    [Tooltip("Seconds the item takes to fly from the ground into the hand. 0 equips instantly.")]
    [SerializeField] private float flightDuration = 0.35f;

    [Tooltip("Easing of the flight into the hand.")]
    [SerializeField] private Ease flightEase = Ease.OutQuad;

    [Header("Can't collect")]
    [Tooltip("Played on the refused item when the stash is full.")]
    [SerializeField] private FeedbackReceiver cantCollectFeedback;

    // Tween flying the equipped item into the hand, or null.
    private Sequence _flight;

    /// <summary> Stashes then equips the item. False if it could not be stashed. </summary>
    public bool TryCollectItem(ItemDataSO item)
    {
        if (item == null || player == null) return false;
        if (player.StashHandler == null || player.ItemHandler == null) return false;

        if (!player.StashHandler.TryAddItem(item)) return false;

        player.ItemHandler.Equip_ItemData(item);
        PlayCollectSFX();
        return true;
    }

    /// <summary> Stashes the world item, then flies its hand form into the hand. </summary>
    public bool TryCollectItem(WorldItemBase worldItem)
    {
        if (worldItem == null) return false;

        // Pose is read before the world item goes away.
        worldItem.transform.GetPositionAndRotation(out Vector3 fromPosition, out Quaternion fromRotation);
        if (!TryCollectItem(worldItem.Data, fromPosition, fromRotation))
        {
            if (StashHandler.IsFull) PlayCantCollect(worldItem.gameObject);
            return false;
        }

        Destroy(worldItem.gameObject);
        return true;
    }

    /// <summary> Stashes the item, then flies its hand form in from the given pose. </summary>
    public bool TryCollectItem(ItemDataSO data, Vector3 fromPosition, Quaternion fromRotation)
    {
        if (data == null || player == null) return false;
        if (player.StashHandler == null || player.ItemHandler == null) return false;

        if (!player.StashHandler.TryAddItem(data)) return false; // full stash refuses

        player.ItemHandler.UnEquip_Juicy(); // old item sinks out, the new item is on its way
        FlyIntoHand(data, fromPosition, fromRotation);
        PlayCollectSFX();
        return true;
    }

    /// <summary> Stashes an item matching the equipped one, keeping the hand equipped. </summary>
    public bool TryCollectIntoHeld(ItemDataSO data)
    {
        if (data == null || player == null || player.StashHandler == null || player.ItemHandler == null) return false;

        HandItemBase held = player.ItemHandler.CurrentEquipped;
        if (held == null || held.Data != data) return false; // caller falls back to a normal pickup

        if (!player.StashHandler.TryAddItem(data)) return false; // selects the new, identical slot
        PlayCollectSFX();
        return true;
    }

    /// <summary> Plays a random pickup clip from the gameplay sound library. </summary>
    private void PlayCollectSFX()
    {
        SimpleAudioManager audio = SimpleAudioManager.instance;
        if (audio == null || audio.GameplaySounds == null) return;

        audio.PlayJuicyGlobalClip(audio.GameplaySounds.GetRandomCollectSFX());
    }

    /// <summary> Shakes and flashes the refused item and capacity counter. </summary>
    private void PlayCantCollect(GameObject item)
    {
        if (cantCollectFeedback != null) cantCollectFeedback.Play(item);
        FloatingTextEffect.Show(Localizer.FromEnglish("feedback", "Inventory Full"), item.transform.position + Vector3.up * 0.5f, new Color(1f, 0.3f, 0.3f));
    }

    /// <summary> Equips the item at once, then tweens it from the world pose into the hand. </summary>
    private void FlyIntoHand(ItemDataSO data, Vector3 fromPosition, Quaternion fromRotation)
    {
        ClearFlying(false); // a second pickup supersedes; the old item is already sinking

        // Equipped before the flight so the hand is never empty (placing keeps working).
        player.ItemHandler.Equip_ItemData(data);
        HandItemBase hand = player.ItemHandler.CurrentEquipped;
        if (hand == null || flightDuration <= 0f) return; // nothing to animate

        // Parented to the socket so the flight tracks the player while it plays.
        Transform t = hand.transform;
        t.SetPositionAndRotation(fromPosition, fromRotation);

        Tween move = t.DOLocalMove(data.HeldPosition, flightDuration).SetEase(flightEase);
        Tween turn = t.DOLocalRotateQuaternion(Quaternion.Euler(data.HeldRotation), flightDuration)
                      .SetEase(flightEase);

        Sequence flight = DOTween.Sequence().Join(move).Join(turn).SetLink(hand.gameObject);
        flight.OnUpdate(() => { if (!hand.IsEquipped) ClearFlying(false); }) // unequipped mid-air: sink takes over
              .OnKill(() => { if (_flight == flight) _flight = null; });
        _flight = flight;
    }

    /// <summary> Stops the flight; snapping puts the item straight into its held pose. </summary>
    private void ClearFlying(bool snapToHand)
    {
        Sequence flight = _flight;
        _flight = null;
        flight?.Kill(snapToHand);
    }

    private void OnDisable() => ClearFlying(true);
}
