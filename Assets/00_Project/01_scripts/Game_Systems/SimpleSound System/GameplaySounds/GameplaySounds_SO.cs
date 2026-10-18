using System.Collections.Generic;
using UnityEngine;

/// <summary> Gameplay SFX library; each section lives in its own partial file. </summary>
[CreateAssetMenu(fileName = "GameplaySounds", menuName = "ScriptableObject/GameplaySounds")]
public partial class GameplaySounds_SO : ScriptableObject
{
    /// <summary> Random sound from the list; clipless when empty. </summary>
    private readonly Dictionary<List<GameSound>, int> _lastVariations = new Dictionary<List<GameSound>, int>();

    private GameSound GetRandom(List<GameSound> sounds)
    {
        if (sounds == null || sounds.Count == 0) return default;
        int next = Random.Range(0, sounds.Count);
        if (sounds.Count > 1 && _lastVariations.TryGetValue(sounds, out int last))
            next = (last + Random.Range(1, sounds.Count)) % sounds.Count;
        _lastVariations[sounds] = next;
        return sounds[next];
    }

    public GameSound UIClick;

    public GameSound TestingSound   ; 
}
