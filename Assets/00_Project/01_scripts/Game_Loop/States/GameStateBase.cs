using UnityEngine;
using UnityEngine.Events;

/// <summary> Base for one game state living on its own child GameObject. </summary>
public abstract class GameStateBase : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private GameFlowManager.GameState stateId;

    [Header("World Settings")]
    [Tooltip("Time.timeScale on enter. 0 freezes time, 1 keeps it running.")]
    [SerializeField] private float timeScale = 1f;

    [Header("Cursor Settings")]
    [SerializeField] private bool cursorVisible = false;
    [SerializeField] private CursorLockMode cursorLock = CursorLockMode.Locked;

    [Header("Per-State Hooks")]
    [SerializeField] private UnityEvent onEnter;
    [SerializeField] private UnityEvent onExit;

    /// <summary> Enum id this state represents. </summary>
    public GameFlowManager.GameState StateId => stateId;

    /// <summary> Applies shared settings, then runs state-specific enter logic. </summary>
    public void Enter()
    {
        gameObject.SetActive(true);

        Time.timeScale = timeScale;
        Cursor.visible = cursorVisible;
        Cursor.lockState = cursorLock;

        OnEnter();
        onEnter?.Invoke();
    }

    /// <summary> Runs state-specific exit logic, then hides the GameObject. </summary>
    public void Exit()
    {
        OnExit();
        onExit?.Invoke();

        gameObject.SetActive(false);
    }

    /// <summary> Override for state-specific enter behaviour. </summary>
    protected virtual void OnEnter() { }

    /// <summary> Override for state-specific exit behaviour. </summary>
    protected virtual void OnExit() { }

    /// <summary> Optional per-frame logic while this state is active. </summary>
    public virtual void Tick() { }
}
