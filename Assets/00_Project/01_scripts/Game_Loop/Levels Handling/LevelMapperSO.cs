using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "levelMapperData", menuName = "ScriptableObjects/LevelMapperData")]
public class LevelMapperSO : ScriptableObject
{
    public ProgressionData progressionData;

    [Tooltip("Worlds in play order. Each world holds its own ordered list of level scenes. " +
             "Progression is tracked as (world index, level index) on the ProgressionData.")]
    public List<WorldData> worlds = new List<WorldData>();
}
