using System.Collections.Generic;
using UnityEngine;

/// <summary> Collecting section: item pickup, throw and refused pickup. </summary>
public partial class GameplaySounds_SO
{
    [Header("Collecting Sounds")]
    public List<GameSound> CollectingSounds = new List<GameSound>();
    public List<GameSound> ThrowingSounds = new List<GameSound>();
    [Tooltip("Plays when a pickup is refused, e.g. full stash.")]
    public GameSound WrongSFX;

    /// <summary> Random pickup sound; clipless when none assigned. </summary>
    public GameSound GetRandomCollectSFX() => GetRandom(CollectingSounds);

    /// <summary> Random throw sound; clipless when none assigned. </summary>
    public GameSound GetRandomThrowSFX() => GetRandom(ThrowingSounds);
}
