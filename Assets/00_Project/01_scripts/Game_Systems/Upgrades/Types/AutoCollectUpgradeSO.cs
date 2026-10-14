using System;
using DG.Tweening;
using UnityEngine;

/// <summary> Auto-Collect values for one level. </summary>
[Serializable]
public struct AutoCollectUpgradeValues
{
    [Tooltip("Whether the Auto-Collect power can be used.")]
    public bool unlocked;

    [Tooltip("Count cap: most items pulled per use, also capped by free slots.")]
    [Min(0)] public int maxItemsPerUse;
}

/// <summary> Auto-Collect upgrade: matching items fly to the player and get equipped. </summary>
[CreateAssetMenu(fileName = "Upgrade_AutoCollect", menuName = "ScriptableObject/Upgrades/Auto Collect")]
public class AutoCollectUpgradeSO : UpgradeSO<AutoCollectUpgradeValues>
{
    protected override string FormatValues(AutoCollectUpgradeValues v) => v.maxItemsPerUse.ToString();

    [Header("Power")]
    [Tooltip("Seconds before reuse; starts once the last item lands.")]
    [Min(0f)] public float cooldown = 45f;

    [Tooltip("Duration cap: seconds before items still flying are dropped.")]
    [Min(0.1f)] public float maxDuration = 4f;

    [Tooltip("Seconds between each item starting its flight.")]
    [Min(0f)] public float stagger = 0.3f;

    [Header("Rise")]
    [Tooltip("Height items pop up before homing in, in world units.")]
    [Min(0f)] public float riseHeight = 1f;

    [Tooltip("Seconds to pop up.")]
    [Min(0.01f)] public float riseDuration = 0.35f;

    public Ease riseEase = Ease.OutBack;

    [Header("Flight")]
    [Tooltip("Seconds to fly from the top of the rise to the player.")]
    [Min(0.01f)] public float flightDuration = 0.8f;

    public Ease flightEase = Ease.InQuad;

    [Header("Drop")]
    [Tooltip("Downward speed for items dropped by a full stash or the duration cap.")]
    [Min(0.5f)] public float dropSpeed = 3f;

    [Header("Outline")]
    [Tooltip("Outline shown while flying. Alpha 0 hides it.")]
    public Color outlineColor = new Color(0.3f, 1f, 0.4f);
}
