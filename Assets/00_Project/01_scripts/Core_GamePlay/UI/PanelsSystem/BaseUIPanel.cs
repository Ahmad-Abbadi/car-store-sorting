using UnityEngine;

/// <summary> Base for any UI that needs pause/cursor/freeze/mute while it is shown. </summary>
public class BaseUIPanel : MonoBehaviour, IUIPanel
{
    [Header("Panel Conditions")]
    [Tooltip("Stop world time (timeScale 0) while shown.")]
    [SerializeField] protected bool requirePause;
    [Tooltip("Free the cursor for clicking while shown.")]
    [SerializeField] protected bool requireMouse;
    [Tooltip("Stop player input and animation while shown.")]
    [SerializeField] protected bool pausePlayer;
    [Tooltip("Root the player while world time keeps running.")]
    [SerializeField] protected bool freezePlayer;
    [Tooltip("Mute music while shown.")]
    [SerializeField] protected bool muteMusic;

    public bool RequirePause => requirePause;
    public bool RequireMouse => requireMouse;
    public bool PausePlayer => pausePlayer;
    public bool FreezePlayer => freezePlayer;
    public bool MuteMusic => muteMusic;

    /// <summary> Registers with the manager when the panel is shown. </summary>
    protected virtual void OnEnable() => UIPanelsManager.Instance?.Activate(this);

    /// <summary> Unregisters from the manager when the panel is hidden. </summary>
    protected virtual void OnDisable() => UIPanelsManager.Instance?.Deactivate(this);

    /// <summary> Drops the panel from the manager if destroyed (e.g. scene reload) while still shown. </summary>
    protected virtual void OnDestroy() => UIPanelsManager.Instance?.Deactivate(this);
}
