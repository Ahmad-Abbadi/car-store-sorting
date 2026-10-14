using System;
using UnityEngine;

/// <summary> Longer reach values for one level. </summary>
[Serializable]
public struct ReachUpgradeValues
{
    [Tooltip("Interaction ray length, in world units.")]
    [Min(0f)] public float distance;
}

/// <summary> Longer reach upgrade: extends the interaction distance per level. </summary>
[CreateAssetMenu(fileName = "Upgrade_LongerReach", menuName = "ScriptableObject/Upgrades/Longer Reach")]
public class ReachUpgradeSO : UpgradeSO<ReachUpgradeValues>
{
    protected override string FormatValues(ReachUpgradeValues v) => v.distance.ToString("0.#");
}
