using System;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary> Stronger throw values for one level. </summary>
[Serializable]
public struct ThrowUpgradeValues
{
    [Tooltip("Impulse applied to every throw.")]
    [FormerlySerializedAs("instantThrowForce")]
    [Min(0f)] public float throwForce;
}

/// <summary> Stronger throw upgrade: raises the throw impulse per level. </summary>
[CreateAssetMenu(fileName = "Upgrade_StrongerThrow", menuName = "ScriptableObject/Upgrades/Stronger Throw")]
public class ThrowUpgradeSO : UpgradeSO<ThrowUpgradeValues>
{
    protected override string FormatValues(ThrowUpgradeValues v) => v.throwForce.ToString("0.#");
}
