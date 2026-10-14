using System;
using UnityEngine;

/// <summary> Bigger inventory values for one level. </summary>
[Serializable]
public struct InventoryUpgradeValues
{
    [Tooltip("Max items held. 0 = unlimited.")]
    [Min(0)] public int capacity;
}

/// <summary> Bigger inventory upgrade: raises the stash capacity per level. </summary>
[CreateAssetMenu(fileName = "Upgrade_BiggerInventory", menuName = "ScriptableObject/Upgrades/Bigger Inventory")]
public class InventoryUpgradeSO : UpgradeSO<InventoryUpgradeValues>
{
    protected override string FormatValues(InventoryUpgradeValues v) => v.capacity.ToString();
}
