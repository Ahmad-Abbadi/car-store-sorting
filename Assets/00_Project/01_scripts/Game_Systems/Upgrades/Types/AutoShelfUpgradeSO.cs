using System;
using UnityEngine;

/// <summary> Auto-shelve values for one level. </summary>
[Serializable]
public struct AutoShelfUpgradeValues
{
    [Tooltip("Items hitting a fitting shelf place themselves on it.")]
    public bool enabled;
}

/// <summary> Auto-shelve upgrade: turns on distance shelfing for every shelf. </summary>
[CreateAssetMenu(fileName = "Upgrade_AutoShelve", menuName = "ScriptableObject/Upgrades/Auto Shelve")]
public class AutoShelfUpgradeSO : UpgradeSO<AutoShelfUpgradeValues> { }
