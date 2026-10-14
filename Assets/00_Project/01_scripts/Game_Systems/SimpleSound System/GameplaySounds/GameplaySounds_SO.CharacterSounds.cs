using System.Collections.Generic;
using UnityEngine;

/// <summary> Character section: footsteps and jump. </summary>
public partial class GameplaySounds_SO
{
    [Header("Character Sounds")]
    public List<GameSound> StepsSounds = new List<GameSound>();
    public GameSound JumpSound;

    /// <summary> Random footstep sound; clipless when none assigned. </summary>
    public GameSound GetRandomStepSound() => GetRandom(StepsSounds);
}
