using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary> Key 1: every item matching the held one rises, bobs and glows, then drops. </summary>
public class HighlightItemsPower : ItemPowerBase
{
    [SerializeField] private HighlightItemsUpgradeSO upgrade;

    /// <summary> One floating item and its own start pose and timing. </summary>
    private struct Floater
    {
        public WorldItemBase Item;
        public float BaseY;
        public float Delay;
        public float Phase;
    }

    private readonly List<WorldItemBase> _matches = new List<WorldItemBase>();
    private readonly List<Floater> _floaters = new List<Floater>();
    private Tween _tween;

    private const float GuidedScanInterval = 0.5f;
    private readonly List<Floater> _guided = new List<Floater>();
    private readonly object _guidedOwner = new object(); // keeps guided items apart from key-1 ones
    private ItemDataSO _guidedItem;
    private Color _guidedColor;
    private float _guidedStart;
    private float _nextGuidedScan;

    public override bool IsUnlocked => upgrade != null && upgrade.CurrentValues.unlocked;
    public override Sprite Icon => upgrade != null ? upgrade.Icon : null;
    protected override float Cooldown => upgrade != null ? upgrade.cooldown : 0f;
    protected override bool KeyPressed => Player != null && Player.Input != null && Player.Input.HighlightPowerPressed;

    protected override bool TryActivate()
    {
        ItemDataSO held = HeldItem;
        if (held == null) return false;

        FindMatches(held, _matches, includePowerControlled: false);
        if (_matches.Count == 0) return false;

        _floaters.Clear();
        foreach (WorldItemBase item in _matches)
        {
            item.BeginPowerControl(this, asTrigger: false); // solid, so the reticle can still grab it
            item.SetPowerOutline(true, upgrade.outlineColor);
            _floaters.Add(new Floater
            {
                Item = item,
                BaseY = item.transform.position.y,
                Delay = Random.Range(0f, upgrade.riseDelayJitter),
                Phase = Random.Range(0f, Mathf.PI * 2f),
            });
        }

        // One tween drives every item, so the effect ends together.
        float total = upgrade.riseDelayJitter + upgrade.riseDuration + upgrade.CurrentValues.duration;
        SetEffectLength(total);
        _tween = DOVirtual.Float(0f, total, total, Animate)
            .SetEase(Ease.Linear)
            .SetLink(gameObject)
            .OnKill(DropAll); // fires on complete too
        return true;
    }

    /// <summary> Places each still-owned item at its rise plus bob height. </summary>
    private void Animate(float time) => AnimateFloaters(_floaters, this, time);

    /// <summary> Moves owned floaters; drops entries that were picked up or collected. </summary>
    private void AnimateFloaters(List<Floater> floaters, object owner, float time)
    {
        for (int i = floaters.Count - 1; i >= 0; i--)
        {
            Floater f = floaters[i];
            if (f.Item == null || f.Item.PowerOwner != owner) // picked up or collected
            {
                floaters.RemoveAt(i);
                continue;
            }

            float t = time - f.Delay;
            float rise01 = Mathf.Clamp01(t / upgrade.riseDuration);
            float height = upgrade.floatHeight * DOVirtual.EasedValue(0f, 1f, rise01, upgrade.riseEase);

            // Bob fades in over the rise so it doesn't jump.
            float bob = Mathf.Sin(t * Mathf.PI * 2f / upgrade.bobPeriod + f.Phase) * upgrade.bobAmplitude * rise01;

            Transform tr = f.Item.transform;
            Vector3 p = tr.position;
            p.y = f.BaseY + height + bob;
            tr.position = p;
        }
    }

    /// <summary> Floats every free item of this type until StopGuided; ignores unlock and cooldown. </summary>
    public void StartGuided(ItemDataSO item, Color outline)
    {
        StopGuided();
        if (item == null || upgrade == null) return;

        _guidedItem = item;
        _guidedColor = outline;
        _guidedStart = Time.time;
        _nextGuidedScan = 0f;
    }

    /// <summary> Fills results with items still floating in guided mode. </summary>
    public void GetGuidedItems(List<WorldItemBase> results)
    {
        results.Clear();
        foreach (Floater f in _guided)
            if (f.Item != null && f.Item.PowerOwner == _guidedOwner) results.Add(f.Item);
    }

    /// <summary> Drops every guided item and ends guided mode. </summary>
    public void StopGuided()
    {
        foreach (Floater f in _guided)
            if (f.Item != null && f.Item.PowerOwner == _guidedOwner) f.Item.EndPowerControl(upgrade.dropSpeed);

        _guided.Clear();
        _guidedItem = null;
    }

    protected override void Update()
    {
        base.Update();
        if (_guidedItem == null) return;

        float time = Time.time - _guidedStart;
        if (Time.time >= _nextGuidedScan)
        {
            _nextGuidedScan = Time.time + GuidedScanInterval;
            AddGuidedMatches(time);
        }
        AnimateFloaters(_guided, _guidedOwner, time);
    }

    /// <summary> Picks up newly free items of the guided type, e.g. dropped ones. </summary>
    private void AddGuidedMatches(float time)
    {
        FindMatches(_guidedItem, _matches, includePowerControlled: false);
        foreach (WorldItemBase item in _matches)
        {
            item.BeginPowerControl(_guidedOwner, asTrigger: false); // solid, so the reticle can still grab it
            item.SetPowerOutline(true, _guidedColor);
            _guided.Add(new Floater
            {
                Item = item,
                BaseY = item.transform.position.y,
                Delay = time + Random.Range(0f, upgrade.riseDelayJitter),
                Phase = Random.Range(0f, Mathf.PI * 2f),
            });
        }
    }

    /// <summary> Drops every item still floating, then starts the cooldown. </summary>
    private void DropAll()
    {
        _tween = null;
        foreach (Floater f in _floaters)
            if (f.Item != null && f.Item.PowerOwner == (object)this) f.Item.EndPowerControl(upgrade.dropSpeed);

        _floaters.Clear();
        FinishEffect();
    }

    private void OnDisable()
    {
        _tween?.Kill();
        if (upgrade != null) StopGuided();
    }
}
