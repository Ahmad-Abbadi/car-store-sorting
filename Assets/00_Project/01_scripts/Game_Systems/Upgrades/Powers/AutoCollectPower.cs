using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary> Key 2: random items matching the held one fly to the player and get equipped. </summary>
public class AutoCollectPower : ItemPowerBase
{
    [SerializeField] private AutoCollectUpgradeSO upgrade;

    private readonly List<WorldItemBase> _matches = new List<WorldItemBase>();
    private readonly List<WorldItemBase> _launched = new List<WorldItemBase>();
    private int _inFlight;
    private Tween _timeout;

    public override bool IsUnlocked => upgrade != null && upgrade.CurrentValues.unlocked;
    public override Sprite Icon => upgrade != null ? upgrade.Icon : null;
    protected override float Cooldown => upgrade != null ? upgrade.cooldown : 0f;
    protected override bool KeyPressed => Player != null && Player.Input != null && Player.Input.AutoCollectPowerPressed;

    /// <summary> Flight target: the hand socket, else the player root. </summary>
    private Transform Target =>
        Player.ItemHandler != null && Player.ItemHandler.HoldSocket != null ? Player.ItemHandler.HoldSocket : Player.transform;

    protected override bool TryActivate()
    {
        ItemDataSO held = HeldItem;
        if (held == null || Player.ItemCollector == null) return false;

        int count = upgrade.CurrentValues.maxItemsPerUse ;
        if (count <= 0) return false; // hand is full

        // Highlighted items count too; this power takes them over.
        FindMatches(held, _matches, includePowerControlled: true);
        count = Mathf.Min(count, _matches.Count);
        if (count == 0) return false;

        PickRandom(_matches, count);
        _inFlight = count;
        _launched.Clear();
        for (int i = 0; i < count; i++)
        {
            _launched.Add(_matches[i]);
            Launch(_matches[i], i * upgrade.stagger);
        }

        // HUD drains over the planned flights, or the cap if it hits first.
        
        SetEffectLength( upgrade.maxDuration);
        _timeout = DOVirtual.DelayedCall(upgrade.maxDuration, DropRemaining).SetLink(gameObject);
        return true;
    }

    /// <summary> Free inventory slots; unlimited capacity counts as no limit. </summary>
    

    /// <summary> Moves a random pick of count items to the front of the list. </summary>
    private static void PickRandom(List<WorldItemBase> list, int count)
    {
        for (int i = 0; i < count; i++)
        {
            int j = Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary> Pops the item up, then homes it into the moving hand. </summary>
    private void Launch(WorldItemBase item, float delay)
    {
        item.BeginPowerControl(this, asTrigger: true); // passes through walls
        item.SetPowerOutline(upgrade.outlineColor.a > 0f, upgrade.outlineColor);

        Transform t = item.transform;
        Vector3 top = t.position + Vector3.up * upgrade.riseHeight;

        Sequence seq = DOTween.Sequence()
            .AppendInterval(delay)
            .Append(t.DOMove(top, upgrade.riseDuration).SetEase(upgrade.riseEase))
            .Append(DOVirtual.Float(0f, 1f, upgrade.flightDuration,
                p => t.position = Vector3.LerpUnclamped(top, Target.position, p)) // target read live
                .SetEase(upgrade.flightEase))
            .SetLink(item.gameObject)
            .OnComplete(() => Arrive(item))
            .OnKill(OnFlightEnded); // complete, picked up early or taken over

        item.SetPowerTween(seq);
    }

    /// <summary> Collects and equips the item; drops it instead if the stash filled up. </summary>
    private void Arrive(WorldItemBase item)
    {
        if (item == null || item.PowerOwner != (object)this) return;

        // Same item already in hand: stash it without unequipping, so placement keeps working.
        ItemCollectingBehaviour collector = Player.ItemCollector;
        item.transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
        if (collector.TryCollectIntoHeld(item.Data) || collector.TryCollectItem(item.Data, position, rotation))
        {
            collector.StampCollectedInstance(TinyRiders.ThemedWorldInstance.Read(item));
            item.gameObject.SetActive(false);
            if (!(TinyRiders.ThemedCollectionSession.Instance?.Recycle(item) ?? false)) Destroy(item.gameObject);
        }
        else item.EndPowerControl(upgrade.dropSpeed);
    }

    /// <summary> Duration cap hit: drops every item still flying. </summary>
    private void DropRemaining()
    {
        _timeout = null;
        // Index loop: the last drop ends the effect, which clears the list.
        for (int i = 0; i < _launched.Count; i++)
        {
            WorldItemBase item = _launched[i];
            if (item != null && item.PowerOwner == (object)this) item.EndPowerControl(upgrade.dropSpeed); // ends its flight
        }
    }

    /// <summary> Starts the cooldown once the last flight is over. </summary>
    private void OnFlightEnded()
    {
        if (--_inFlight > 0) return;

        _inFlight = 0;
        _launched.Clear();
        _timeout?.Kill();
        _timeout = null;
        FinishEffect();
    }
}
