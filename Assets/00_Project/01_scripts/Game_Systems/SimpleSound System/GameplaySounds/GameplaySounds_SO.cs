using System.Collections.Generic;
using UnityEngine;

/// <summary> Gameplay SFX library; each section lives in its own partial file. </summary>
[CreateAssetMenu(fileName = "GameplaySounds", menuName = "ScriptableObject/GameplaySounds")]
public partial class GameplaySounds_SO : ScriptableObject
{
    /// <summary> Random sound from the list; clipless when empty. </summary>
    private static GameSound GetRandom(List<GameSound> sounds)
    {
        if (sounds == null || sounds.Count == 0) return default;
        return sounds[Random.Range(0, sounds.Count)];
    }

    public GameSound TestingSound   ; 
}
