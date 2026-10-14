using System.Collections.Generic;
using UnityEngine;

/// <summary> Shelf section: item placement and shelf completion. </summary>
public partial class GameplaySounds_SO
{
    [Header("Shelfs SFX")]
    public List<GameSound> ShelfPlacementSounds = new List<GameSound>();
    public GameSound CompleteShelfSFX;
    public GameSound WrongSectionSFX;

    /// <summary> Random shelf-placement sound; clipless when none assigned. </summary>
    public GameSound GetRandomShelfPlacementSFX() => GetRandom(ShelfPlacementSounds);
}
