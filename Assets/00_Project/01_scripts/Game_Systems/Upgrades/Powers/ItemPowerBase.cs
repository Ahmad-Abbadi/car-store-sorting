using System.Collections.Generic;
using UnityEngine;

/// <summary> Key-triggered item power: unlock check, effect state and cooldown after it ends. </summary>
public abstract class ItemPowerBase : MonoBehaviour
{
    [Tooltip("Player whose held item and input drive the power. Falls back to Player.Instance.")]
    [SerializeField] private Player player;

    private bool _tapRequested;
    private float _cooldownEnd;
    private float _activeStart;
    private float _activeLength;

    protected Player Player => player != null ? player : (player = Player.Instance);

    /// <summary> True once the upgrade is bought. </summary>
    public abstract bool IsUnlocked { get; }

    /// <summary> Shop icon, for the HUD. </summary>
    public abstract Sprite Icon { get; }

    /// <summary> True while the effect plays. </summary>
    public bool IsActive { get; private set; }

    /// <summary> Seconds left before reuse. </summary>
    public float CooldownRemaining => Mathf.Max(0f, _cooldownEnd - Time.time);

    /// <summary> 1 right after the effect ends, 0 when ready. </summary>
    public float Cooldown01 => Cooldown > 0f ? Mathf.Clamp01(CooldownRemaining / Cooldown) : 0f;

    /// <summary> 1 when the effect starts, draining to 0 as its expected length runs out. </summary>
    public float Active01 => IsActive && _activeLength > 0f
        ? 1f - Mathf.Clamp01((Time.time - _activeStart) / _activeLength) : 0f;

    /// <summary> True when the key would fire the power. </summary>
    public bool IsReady => IsUnlocked && !IsActive && CooldownRemaining <= 0f;

    /// <summary> Full cooldown length, from the upgrade SO. </summary>
    protected abstract float Cooldown { get; }

    /// <summary> True on the frame this power's key is pressed. </summary>
    protected abstract bool KeyPressed { get; }

    /// <summary> Starts the effect and calls SetEffectLength; false spends no cooldown. </summary>
    protected abstract bool TryActivate();

    /// <summary> Item selected in the stash, or null when the hand is empty. </summary>
    protected ItemDataSO HeldItem
    {
        get
        {
            StashHandler stash = Player != null ? Player.StashHandler : null;
            return stash != null && stash.Inventory != null ? stash.Inventory.GetItemAt(stash.SelectedIndex) : null;
        }
    }

    protected virtual void Update()
    {
        bool tapped = _tapRequested;
        _tapRequested = false;
        if (!IsReady || !(tapped || KeyPressed)) return;
        if (TryActivate()) IsActive = true;
    }

    /// <summary> Fires the power on the next update, like its key; ignored while input is off. </summary>
    public void RequestUse()
    {
        if (Player != null && Player.Input != null && Player.Input.IsEnabled) _tapRequested = true;
    }

    /// <summary> Sets the expected effect length the HUD drains over. </summary>
    protected void SetEffectLength(float seconds)
    {
        _activeStart = Time.time;
        _activeLength = Mathf.Max(0f, seconds);
    }

    /// <summary> Ends the effect and starts the cooldown. </summary>
    protected void FinishEffect()
    {
        if (!IsActive) return;

        IsActive = false;
        _cooldownEnd = Time.time + Cooldown;
    }

    /// <summary> Fills the list with free world items sharing the given data. </summary>
    protected static void FindMatches(ItemDataSO data, List<WorldItemBase> results, bool includePowerControlled)
    {
        results.Clear();
        IReadOnlyList<WorldItemBase> all = WorldItemBase.All;
        for (int i = 0; i < all.Count; i++)
        {
            WorldItemBase item = all[i];
            if (item.Data != data) continue;

            bool powered = item.PowerOwner != null;
            if (powered && !includePowerControlled) continue;
            if (!powered && item.ThrowControlled) continue; // mid-throw

            results.Add(item);
        }
    }
}
