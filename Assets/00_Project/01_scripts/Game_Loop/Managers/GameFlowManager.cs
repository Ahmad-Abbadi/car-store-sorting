using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

public class GameFlowManager : MonoBehaviour
{
    // --- SINGLETON SETUP ---
    public static GameFlowManager Instance { get; private set; }

    public enum GameState { MainMenu, Playing, Paused, Freeze, LevelFinished, LevelLost }

    // Scene that starts in the MainMenu state; every other scene starts Playing.
    private const string MainMenuSceneName = "MainMenu Scene";

    [Header("Status")]
    public GameState currentState;

    public bool IsPaused => currentState == GameState.Paused;

    /// <summary> True when the current pause should open the pause menu (pause button only). </summary>
    public bool ShouldOpenPauseMenu { get; private set; }

    /// <summary> True while input is locked but the world keeps running. </summary>
    public bool IsFrozen => currentState == GameState.Freeze;

    /// <summary> True once the level has been lost. </summary>
    public bool IsLevelLost => currentState == GameState.LevelLost;

    // Events
    public static event Action<GameState> OnStateChanged;
    public static event Action<string> OnSceneTransition;

    /// <summary> Raised once when the level is lost, before the state switch. </summary>
    public static event Action OnLevelLost;

    // Registered state objects, keyed by id.
    private readonly Dictionary<GameState, GameStateBase> _states = new();
    private GameStateBase _current;

    private void Awake()
    {
        // Singleton Logic
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        RegisterStates();

        // Player death drives the loss automatically; no scene reference needed.
        StatsBehaviour.OnDeath += HandlePlayerDeath;

        // Resets the state on every scene, including the first one.
        SceneManager.sceneLoaded += HandleSceneLoaded;

        SetState(GameState.Playing);
    }

    /// <summary> Force-enters the scene's default state so stale state never carries over. </summary>
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;

        ForceState(scene.name == MainMenuSceneName ? GameState.MainMenu : GameState.Playing);

        // Re-apply panels that registered before this reset (e.g. shown on load).
        UIPanelsManager.Instance?.CheckConditions();
    }

    /// <summary> Exits and re-enters a state even if it is already current. </summary>
    private void ForceState(GameState newState)
    {
        _current?.Exit();
        _current = null;
        SetState(newState);
    }

    /// <summary> Collects child state components and starts them hidden. </summary>
    private void RegisterStates()
    {
        _states.Clear();
        foreach (var state in GetComponentsInChildren<GameStateBase>(true))
        {
            if (_states.ContainsKey(state.StateId))
            {
                Debug.LogWarning($"Duplicate state '{state.StateId}' on {state.name}; ignored.");
                continue;
            }

            _states.Add(state.StateId, state);
            state.gameObject.SetActive(false); // machine toggles these
        }
    }

    private void OnDestroy()
    {
        StatsBehaviour.OnDeath -= HandlePlayerDeath;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Update() => _current?.Tick();

    /// <summary> Bridges the player's death event to the lose flow. </summary>
    private void HandlePlayerDeath(StatsBehaviour stats) => LoseLevel();

    /// <summary>
    /// The master function to change states.
    /// </summary>
    public void SetState(GameState newState)
    {
        if (_current != null && currentState == newState) return;

        _current?.Exit();

        currentState = newState;
        _current = _states.TryGetValue(newState, out var next) ? next : null;

        if (_current == null)
            Debug.LogWarning($"No state object registered for '{newState}'.");

        _current?.Enter();

        OnStateChanged?.Invoke(currentState);
        Debug.Log($"<color=orange>Game State: {newState}</color>");
    }

    // --- Convenience wrappers (parameterless — hook straight to UI Button OnClick) ---

    /// <summary>Resume active play. Mirror of <see cref="PauseGame"/>.</summary>
    public void ResumeGame() => SetState(GameState.Playing);

    /// <summary>Continue play — resume from a pause. Hook straight to a Continue button.</summary>
    public void Continue() => ResumeGame();

    /// <summary>Restart the level: unpause, reset the player, then reload the scene.</summary>
    public void Restart()
    {
        ResumeGame();      // state -> Playing; the state machine snaps the player back to Idle
        ResetPlayer();     // refill health/stamina and clear the death flag
        ReloadCurrentScene();
    }

    /// <summary> Resets the player back to a fresh state (full stats, not dead). </summary>
    private static void ResetPlayer()
    {
        if (Player.Instance != null && Player.Instance.Stats != null)
            Player.Instance.Stats.ResetStats();
    }

    /// <summary>Pause and open the pause menu. Hook straight to the pause button.</summary>
    public void PauseGame() => PauseGame(true);

    /// <summary>Pause the game; openMenu tells the PauseMenu whether to show.</summary>
    public void PauseGame(bool openMenu)
    {
        ShouldOpenPauseMenu = openMenu;
        SetState(GameState.Paused);
    }

    /// <summary>Lock player input while the world keeps running (cutscenes, scripted beats).</summary>
    public void FreezeGame() => SetState(GameState.Freeze);

    /// <summary>Release a freeze back to active play.</summary>
    public void UnfreezeGame() => SetState(GameState.Playing);

    /// <summary>Ends the level as a loss. Safe to call more than once.</summary>
    public void LoseLevel()
    {
        if (currentState == GameState.LevelLost || currentState == GameState.LevelFinished) return;

        OnLevelLost?.Invoke();
        SetState(GameState.LevelLost);

        // Guaranteed on death even if no LevelLost state object is wired in the scene.
        ForceFreeCursor();
    }

    /// <summary> Frees the hardware cursor so the death menu is usable. </summary>
    private static void ForceFreeCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // --- SCENE TRANSITIONS ---

    public void LoadScene(string sceneName)
    {
        OnSceneTransition?.Invoke(sceneName);
        Time.timeScale = 1f;
         SceneManager.LoadScene(sceneName);
    }

    public void ReloadCurrentScene()
    {
        Debug.Log("<color=red> ReloadCurrentScene </color> ");
        LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
