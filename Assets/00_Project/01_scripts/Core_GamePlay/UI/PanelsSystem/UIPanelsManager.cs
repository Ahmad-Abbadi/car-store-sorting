using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


[DefaultExecutionOrder(-100)]
/// <summary> Tracks open panels and applies the strongest condition via GameFlow + audio. </summary>
public class UIPanelsManager : MonoBehaviour
{
    public static UIPanelsManager Instance { get; private set; }

    // Per-instance: a fresh scene gets a fresh list, so nothing leaks across reloads.
    private readonly List<IUIPanel> _activePanels = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        SceneManager.sceneUnloaded += HandleSceneUnloaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneUnloaded -= HandleSceneUnloaded;
        if (Instance == this) Instance = null;
    }

    /// <summary> Drops panels from the unloaded scene; new-scene panels stay registered. </summary>
    private void HandleSceneUnloaded(Scene scene)
    {
        _activePanels.RemoveAll(p => IsGone(p) || ((Component)p).gameObject.scene == scene);
    }

    /// <summary> Unity-aware null check; interface == misses destroyed panels. </summary>
    private static bool IsGone(IUIPanel panel) => panel is not Component c || c == null;

    /// <summary> Adds a shown panel and re-evaluates world conditions. </summary>
    public void Activate(IUIPanel panel)
    {
        if (panel == null || _activePanels.Contains(panel)) return;
        _activePanels.Add(panel);
        CheckConditions();
    }

    /// <summary> Removes a hidden panel and re-evaluates world conditions. </summary>
    public void Deactivate(IUIPanel panel)
    {
        if (panel != null && _activePanels.Remove(panel)) CheckConditions();
    }

    /// <summary> Applies the strongest condition any open panel requires. </summary>
    public void CheckConditions()
    {
        _activePanels.RemoveAll(IsGone); // drop destroyed panels

        bool pause = false, freeze = false, mute = false;
        foreach (IUIPanel panel in _activePanels)
        {
            // A free cursor or a player-pause both map to the Paused state in this game.
            if (panel.RequirePause || panel.PausePlayer || panel.RequireMouse) pause = true;
            if (panel.FreezePlayer) freeze = true;
            if (panel.MuteMusic) mute = true;
        }

        ApplyMusic(mute);
        ApplyGameFlow(pause, freeze);
    }

    /// <summary> Mutes or restores audio through the audio manager (respects its lock). </summary>
    private void ApplyMusic(bool mute)
    {
        if (SimpleAudioManager.instance != null)
            SimpleAudioManager.instance.ToggleMute(mute);
    }

    /// <summary> Drives GameFlow, but only from the interruptible play states. </summary>
    private void ApplyGameFlow(bool pause, bool freeze)
    {
        GameFlowManager flow = GameFlowManager.Instance;
        if (flow == null) return;

        // Never override menu or terminal states (MainMenu, LevelLost, LevelFinished).
        GameFlowManager.GameState s = flow.currentState;
        bool interruptible = s == GameFlowManager.GameState.Playing
                          || s == GameFlowManager.GameState.Paused
                          || s == GameFlowManager.GameState.Freeze;
        if (!interruptible) return;

        if (pause) flow.PauseGame(false);
        else if (freeze) flow.FreezeGame();
        else flow.ResumeGame();
    }
}
