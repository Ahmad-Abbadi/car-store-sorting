using System;
using DG.Tweening;
using UnityEngine;

/// <summary> Highlight Items values for one level. </summary>
[Serializable]
public struct HighlightItemsUpgradeValues
{
    [Tooltip("Whether the Highlight Items power can be used.")]
    public bool unlocked;

    [Tooltip("Seconds the items float at the top, after rising.")]
    [Min(0f)] public float duration;
}

/// <summary> Highlight Items upgrade: matching items float and glow on key press. </summary>
[CreateAssetMenu(fileName = "Upgrade_HighlightItems", menuName = "ScriptableObject/Upgrades/Highlight Items")]
public class HighlightItemsUpgradeSO : UpgradeSO<HighlightItemsUpgradeValues>
{
    protected override string FormatValues(HighlightItemsUpgradeValues v) => v.duration.ToString("0.#");

    [Header("Power")]
    [Tooltip("Seconds before reuse; starts once the items drop.")]
    [Min(0f)] public float cooldown = 20f;

    [Header("Rise")]
    [Tooltip("Height items rise above where they rested, in world units.")]
    [Min(0f)] public float floatHeight = 1.5f;

    [Tooltip("Seconds to rise to the float height.")]
    [Min(0.01f)] public float riseDuration = 0.6f;

    public Ease riseEase = Ease.OutBack;

    [Tooltip("Random extra rise delay per item, so they don't move in lockstep.")]
    [Min(0f)] public float riseDelayJitter = 0.25f;

    [Header("Bob")]
    [Tooltip("Up/down distance of the floating bob, in world units.")]
    [Min(0f)] public float bobAmplitude = 0.15f;

    [Tooltip("Seconds for one full up-and-down bob.")]
    [Min(0.01f)] public float bobPeriod = 1.2f;

    [Header("Drop")]
    [Tooltip("Downward speed on release; must beat culling's rest speed or items freeze mid-air.")]
    [Min(0.5f)] public float dropSpeed = 3f;

    [Header("Outline")]
    public Color outlineColor = new Color(1f, 0.85f, 0.2f);
}
