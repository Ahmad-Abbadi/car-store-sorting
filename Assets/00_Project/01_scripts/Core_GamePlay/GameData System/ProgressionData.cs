using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ProgressionData", menuName = "ScriptableObject/ProgressionData")]
public class ProgressionData : ScriptableObject
{
    public event Action OnProgressionChanged;

    [Header("Level Progress")]
    // Progression is a (world, level) coordinate. Both are 0-based: world 0 / level 0 is the very
    // first level. The LevelMapperSO resolves these into a scene name and handles rolling the level
    // index over into the next world when it overflows the current world's level list.
    //
    // "Current" is the level the player is on right now (which can be an earlier level replayed from
    // the level-select panel). "Highest" is the furthest level ever reached — the "last open level"
    // the game boots into, and the ceiling that gates which panel buttons are unlocked.
    [SerializeField, Min(0)] private int _currentWorldIndex = 0;
    [SerializeField, Min(0)] private int _currentLevelIndex = 0;

    [Header("Furthest reached (last open level)")]
    [SerializeField, Min(0)] private int _highestWorldIndex = 0;
    [SerializeField, Min(0)] private int _highestLevelIndex = 0;

    /// <summary>Zero-based index of the world the player is currently in.</summary>
    public int CurrentWorldIndex
    {
        get => Math.Max(0, _currentWorldIndex);
        set
        {
            int targetValue = Math.Max(0, value);
            if (_currentWorldIndex == targetValue) return;

            _currentWorldIndex = targetValue;
            NotifyChange();
        }
    }

    /// <summary>Zero-based index of the level within the current world.</summary>
    public int CurrentLevelIndex
    {
        get => Math.Max(0, _currentLevelIndex);
        set
        {
            int targetValue = Math.Max(0, value);
            if (_currentLevelIndex == targetValue) return;

            _currentLevelIndex = targetValue;
            NotifyChange();
        }
    }

    /// <summary>
    /// One-based level number within the current world (i.e. <see cref="CurrentLevelIndex"/> + 1).
    /// Kept for the per-world level gates (e.g. "show this on level 3").
    /// </summary>
    public int CurrentLevel => CurrentLevelIndex + 1;

    /// <summary>Zero-based world index of the furthest level the player has ever reached.</summary>
    public int HighestWorldIndex => Math.Max(0, _highestWorldIndex);

    /// <summary>Zero-based level index (within <see cref="HighestWorldIndex"/>) of the furthest level reached.</summary>
    public int HighestLevelIndex => Math.Max(0, _highestLevelIndex);

    /// <summary>
    /// True when the level the player is currently on is the furthest one they've reached — i.e. they're
    /// on the frontier, not replaying an earlier, already-cleared level from the level-select panel.
    /// </summary>
    public bool IsOnLastUnlockedLevel =>
        CurrentWorldIndex == HighestWorldIndex && CurrentLevelIndex == HighestLevelIndex;

    /// <summary>
    /// Advances to the next level within the current world. The LevelMapperSO / LevelsManager rolls
    /// the resulting index over into the next world if it overflows the current world's level list.
    /// </summary>
    public void NextLevel()
    {
        CurrentLevelIndex++;
    }

    /// <summary>
    /// Promotes the furthest-reached ("last open") level to <paramref name="worldIndex"/> /
    /// <paramref name="levelIndex"/> if that coordinate lies beyond the current record. Coordinates
    /// compare lexicographically (world first, then level). Returns true if the record moved.
    /// </summary>
    public bool MarkHighest(int worldIndex, int levelIndex)
    {
        worldIndex = Math.Max(0, worldIndex);
        levelIndex = Math.Max(0, levelIndex);

        bool beyond = worldIndex > _highestWorldIndex ||
                     (worldIndex == _highestWorldIndex && levelIndex > _highestLevelIndex);
        if (!beyond) return false;

        _highestWorldIndex = worldIndex;
        _highestLevelIndex = levelIndex;
        NotifyChange();
        return true;
    }

    /// <summary>
    /// True if the given (world, level) is at or below the furthest level reached — i.e. the player
    /// has unlocked it and may replay it from the level-select panel.
    /// </summary>
    public bool IsUnlocked(int worldIndex, int levelIndex)
    {
        worldIndex = Math.Max(0, worldIndex);
        levelIndex = Math.Max(0, levelIndex);

        return worldIndex < _highestWorldIndex ||
              (worldIndex == _highestWorldIndex && levelIndex <= _highestLevelIndex);
    }

    [ContextMenu("NotifyChanges")]
    public void NotifyChange() => OnProgressionChanged?.Invoke();

    [ContextMenu("Reset Progression")]
    public void ResetProgression()
    {
        _currentWorldIndex = 0;
        _currentLevelIndex = 0;
        _highestWorldIndex = 0;
        _highestLevelIndex = 0;
        NotifyChange();
    }
}