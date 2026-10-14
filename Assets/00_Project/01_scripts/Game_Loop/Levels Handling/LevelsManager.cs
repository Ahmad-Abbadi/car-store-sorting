using System.Collections;
using UnityEngine;

/// <summary>
/// Central authority for the normal-level flow. It owns the (world, level) → scene resolution and the
/// three ways the player moves between levels:
///
///   • <see cref="LoadLastOpenLevel"/> — the boot entry. Jumps straight to the furthest level the
///     player has unlocked (the "last open level"). This is the only time the level mapper runs.
///   • <see cref="LoadLevel(int)"/>     — the level-select panel. Jumps to a specific level by its
///     flat 1-based number; locked levels are refused.
///   • <see cref="GoToNextLevel"/>      — advances one level from the current one and loads it
///     directly (no hop back through the "level mapper" scene).
///
/// Persistent singleton (DontDestroyOnLoad). Place ONE in the first scene (the main menu) so the
/// level-select panel, the prestige button, and the boot loader can all reach it via
/// <see cref="Instance"/>. Progression itself lives on the <see cref="ProgressionData"/> SO referenced
/// by the <see cref="LevelMapperSO"/>; this manager just drives it.
/// </summary>
public class LevelsManager : MonoBehaviour
{
    public static LevelsManager Instance { get; private set; }

    /// <summary>
    /// Fired every time a level is (re)loaded through this manager, carrying the destination level's flat
    /// 1-based number. This is the "level started" signal — analytics uses it to count attempts/reach per
    /// level and to start the play-time clock for the level just entered.
    /// </summary>
    public static event System.Action<int> OnLevelEntered;

    /// <summary>
    /// Fired the moment a level is actually WON, carrying the completed level's flat 1-based number. In
    /// this timerless, win-only mode the win is <see cref="GoToNextLevel"/> (the Next Level button), so
    /// this fires there — for the level being left, BEFORE the next one loads. Unlike keying off the next
    /// entry, this reports the level on its own completion, so the very first level of a session and the
    /// last level both get counted. Paired with <see cref="OnLevelEntered"/> it also yields a
    /// started-vs-completed win-rate funnel.
    /// </summary>
    public static event System.Action<int> OnLevelCompleted;

    /// <summary>
    /// Raises <see cref="OnLevelEntered"/> for a level loaded OUTSIDE this manager. Needed because at boot
    /// <see cref="Instance"/> can still be null when <c>levelMapperManager</c> loads the very first level via
    /// its standalone fallback path — that path bypasses <see cref="LoadCoord"/>, so without this the boot
    /// level (level 1 for a new player) would never emit a "started" event and go untracked.
    /// </summary>
    public static void RaiseLevelEntered(int levelNumber) => OnLevelEntered?.Invoke(levelNumber);

    [Header("Data")]
    [Tooltip("Worlds + scene names and the ProgressionData ref.")]
    [SerializeField] LevelMapperSO data;

    [Header("Next Level Transition")]
    [Tooltip("Seconds to wait before the next level loads. Used only when no ScreenFader is present.")]
    [SerializeField] float nextLevelDelay = 0.5f;

    /// <summary>The progression SO this manager reads/writes. May be null if unconfigured.</summary>
    public ProgressionData ProgressionData => data != null ? data.progressionData : null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    // ------------------------------------------------------------------ public entry points

    /// <summary>
    /// Boot entry: load the furthest level the player has unlocked. Syncs the "current" level to that
    /// furthest point so a fresh session resumes where progress left off.
    /// </summary>
    [ContextMenu("Load Last Open Level")]
    public void LoadLastOpenLevel()
    {
        var progression = ProgressionData;
        int worldIndex = progression != null ? progression.HighestWorldIndex : 0;
        int levelIndex = progression != null ? progression.HighestLevelIndex : 0;
        LoadCoord(worldIndex, levelIndex);
    }

    /// <summary>
    /// Level-select panel: load a specific level by its flat, 1-based number (Level 1 = the very
    /// first level of the first world). Out-of-range or still-locked levels are refused.
    /// </summary>
    public void LoadLevel(int levelNumber)
    {
        if (!CoordFromNumber(levelNumber, out int worldIndex, out int levelIndex))
        {
            Debug.LogWarning($"LevelsManager: level number {levelNumber} is out of range.", this);
            return;
        }
        LoadLevel(worldIndex, levelIndex);
    }

    /// <summary>
    /// Level-select panel: load a specific (world, level). Refused while that level is still locked.
    /// </summary>
    public void LoadLevel(int worldIndex, int levelIndex)
    {
        var progression = ProgressionData;
        if (progression != null && !progression.IsUnlocked(worldIndex, levelIndex))
        {
            Debug.Log($"LevelsManager: level (world {worldIndex}, level {levelIndex}) is locked.", this);
            return;
        }
        LoadCoord(worldIndex, levelIndex);
    }

    /// <summary>
    /// Advances one level from the player's current position and loads it directly. Rolls over into
    /// the next world when the current world runs out; clamps at the very last level.
    /// </summary>
    [ContextMenu("Go To Next Level")]
    public void GoToNextLevel()
    {
        var progression = ProgressionData;
        int worldIndex = progression != null ? progression.CurrentWorldIndex : 0;
        int levelIndex = (progression != null ? progression.CurrentLevelIndex : 0) + 1;

        // The player just won the level they're on. Announce completion BEFORE advancing so analytics can
        // close out and report the level being left (its own win = its duration), independent of whether a
        // next level exists — this is what lets the last level of a session get reported.
        OnLevelCompleted?.Invoke(NumberFromCoord(worldIndex, levelIndex - 1));

        if (!HasNextLevel())
            Debug.Log("LevelsManager: already on the last level — reloading it.", this);

        // Fade (if a ScreenFader exists) or wait the inspector delay, then wipe stats and load.
        BeginNextLevelTransition(worldIndex, levelIndex);
    }

    /// <summary> Fades out via ScreenFader if present, otherwise waits the inspector delay, then loads. </summary>
    private void BeginNextLevelTransition(int worldIndex, int levelIndex)
    {
        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.FadeIn(() => LoadNextLevel(worldIndex, levelIndex));
            return;
        }

        StopAllCoroutines();
        StartCoroutine(NextLevelAfterDelay(worldIndex, levelIndex));
    }

    /// <summary> Waits the configured delay (unscaled) then loads the next level. </summary>
    private IEnumerator NextLevelAfterDelay(int worldIndex, int levelIndex)
    {
        if (nextLevelDelay > 0f) yield return new WaitForSecondsRealtime(nextLevelDelay);
        LoadNextLevel(worldIndex, levelIndex);
    }

    /// <summary> Loads the resolved next level. </summary>
    private void LoadNextLevel(int worldIndex, int levelIndex)
    {
        // LoadCoord normalizes the overflow, marks the new level as unlocked, and loads.
        LoadCoord(worldIndex, levelIndex);
    }
    public static void TryGoToNextLevel()
    {
        if(Instance== null ) return; 
        Instance.GoToNextLevel();
    }
    // ------------------------------------------------------------------ queries (used by the UI)

    /// <summary>Total number of levels across every configured world.</summary>
    public int TotalLevels
    {
        get
        {
            var worlds = data != null ? data.worlds : null;
            if (worlds == null) return 0;

            int total = 0;
            for (int i = 0; i < worlds.Count; i++) total += WorldLevelCount(i);
            return total;
        }
    }

    /// <summary>True if the given flat 1-based level number is unlocked (reachable from the panel).</summary>
    public bool IsLevelUnlocked(int levelNumber)
    {
        if (!CoordFromNumber(levelNumber, out int worldIndex, out int levelIndex)) return false;
        var progression = ProgressionData;
        return progression == null || progression.IsUnlocked(worldIndex, levelIndex);
    }

    /// <summary>The furthest unlocked level as a flat 1-based number (the "last open level").</summary>
    public int HighestLevelNumber
    {
        get
        {
            var progression = ProgressionData;
            if (progression == null) return 1;
            return NumberFromCoord(progression.HighestWorldIndex, progression.HighestLevelIndex);
        }
    }

    /// <summary>The level the player is currently on, as a flat 1-based number.</summary>
    public int CurrentLevelNumber
    {
        get
        {
            var progression = ProgressionData;
            if (progression == null) return 1;
            return NumberFromCoord(progression.CurrentWorldIndex, progression.CurrentLevelIndex);
        }
    }

    /// <summary>True if there is a level beyond the player's current one.</summary>
    public bool HasNextLevel() => CurrentLevelNumber < TotalLevels;

    // ------------------------------------------------------------------ core

    // Resolves (world, level) → scene, persists the coordinate, unlocks it, and hands off to the
    // GameFlowManager. All three public entry points funnel through here.
    private void LoadCoord(int worldIndex, int levelIndex)
    {
        if (data == null || data.worlds == null || data.worlds.Count == 0)
        {
            Debug.LogError("LevelsManager: no worlds configured on the LevelMapperSO.", this);
            return;
        }

        if (!Normalize(ref worldIndex, ref levelIndex))
        {
            Debug.LogError($"LevelsManager: world {worldIndex} has no scenes.", this);
            return;
        }

        var progression = ProgressionData;
        if (progression != null)
        {
            progression.CurrentWorldIndex = worldIndex;
            progression.CurrentLevelIndex = levelIndex;
            progression.MarkHighest(worldIndex, levelIndex); // reaching a level unlocks it (in-memory)
        }

        string sceneName = data.worlds[worldIndex].SceneNames[levelIndex];
        Debug.Log($"<color=green>LevelsManager → world {worldIndex} " +
                  $"('{data.worlds[worldIndex].WorldName}'), level {levelIndex}: {sceneName}</color>");

        // Announce the switch BEFORE the load so analytics can close out the level being left and start
        // the clock for this one (enter → next enter = the leaving level's duration).
        OnLevelEntered?.Invoke(NumberFromCoord(worldIndex, levelIndex));

        if (GameFlowManager.Instance != null) GameFlowManager.Instance.LoadScene(sceneName);
        else Debug.LogError("LevelsManager: GameFlowManager.Instance is null; cannot load scene.", this);
    }

    // Rolls a level index that overflows its world forward into later worlds, then clamps the result
    // to a valid, non-empty level. Returns false only if there is nowhere valid to land.
    private bool Normalize(ref int worldIndex, ref int levelIndex)
    {
        var worlds = data != null ? data.worlds : null;
        if (worlds == null || worlds.Count == 0) return false;

        worldIndex = Mathf.Max(0, worldIndex);
        levelIndex = Mathf.Max(0, levelIndex);

        while (worldIndex < worlds.Count - 1 && levelIndex >= WorldLevelCount(worldIndex))
        {
            levelIndex -= WorldLevelCount(worldIndex);
            worldIndex++;
        }

        worldIndex = Mathf.Clamp(worldIndex, 0, worlds.Count - 1);
        levelIndex = Mathf.Clamp(levelIndex, 0, Mathf.Max(0, WorldLevelCount(worldIndex) - 1));

        return WorldLevelCount(worldIndex) > 0;
    }

    // Flat 1-based level number → (world, level). Returns false if the number is out of range.
    private bool CoordFromNumber(int levelNumber, out int worldIndex, out int levelIndex)
    {
        worldIndex = 0;
        levelIndex = 0;

        int remaining = levelNumber - 1; // to 0-based
        if (remaining < 0) return false;

        var worlds = data != null ? data.worlds : null;
        if (worlds == null) return false;

        for (int w = 0; w < worlds.Count; w++)
        {
            int count = WorldLevelCount(w);
            if (remaining < count)
            {
                worldIndex = w;
                levelIndex = remaining;
                return true;
            }
            remaining -= count;
        }
        return false;
    }

    // (world, level) → flat 1-based level number.
    private int NumberFromCoord(int worldIndex, int levelIndex)
    {
        int number = 0;
        for (int w = 0; w < worldIndex; w++) number += WorldLevelCount(w);
        return number + levelIndex + 1;
    }

    private int WorldLevelCount(int worldIndex)
    {
        var worlds = data != null ? data.worlds : null;
        if (worlds == null || worldIndex < 0 || worldIndex >= worlds.Count) return 0;

        WorldData world = worlds[worldIndex];
        return (world != null && world.SceneNames != null) ? world.SceneNames.Count : 0;
    }
}
