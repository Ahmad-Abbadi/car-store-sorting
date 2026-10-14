using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One "world" in the normal-level progression: a display name plus the ordered list of scene
/// names that make up its levels. The <see cref="LevelMapperSO"/> holds a list of these, and
/// progression is tracked as a (world index, level index) pair on <see cref="ProgressionData"/>.
/// </summary>
[CreateAssetMenu(fileName = "WorldData", menuName = "ScriptableObjects/WorldData")]
public class WorldData : ScriptableObject
{
    [Tooltip("Display name for this world.")]
    public string WorldName;

    [Tooltip("Scene names for each level in this world, in play order. Index 0 is the first level.")]
    public List<string> SceneNames = new List<string>();
}
