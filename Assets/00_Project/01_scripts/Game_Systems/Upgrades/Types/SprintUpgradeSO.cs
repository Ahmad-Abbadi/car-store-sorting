using System;
using UnityEngine;

/// <summary> Unlock sprint values for one level. </summary>
[Serializable]
public struct SprintUpgradeValues
{
    [Tooltip("Whether the player may sprint.")]
    public bool unlocked;
}

/// <summary> Unlock sprint upgrade: allows sprinting once bought. </summary>
[CreateAssetMenu(fileName = "Upgrade_UnlockSprint", menuName = "ScriptableObject/Upgrades/Unlock Sprint")]
public class SprintUpgradeSO : UpgradeSO<SprintUpgradeValues> { }
