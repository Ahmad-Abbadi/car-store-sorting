using UnityEngine;

/// <summary>
/// The movement state controller. Lives on the character root, owns the player states
/// (which sit on child objects), and drives exactly one at a time.
///
/// Transitions between Idle/Moving/Jump are decided by the states themselves (they read
/// input and call <see cref="ChangeState"/>). Pause/Freeze are driven globally: this
/// machine listens to <see cref="GameFlowManager.OnStateChanged"/> and forces the
/// matching state — <see cref="pause"/> when time stops, <see cref="freeze"/> when the
/// world keeps running but input is locked.
/// </summary>
public class PlayerStateMachine : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("The player ref holder. Auto-found on this object / parents if empty.")]
    [SerializeField] private Player player;

    [Header("States (on child objects)")]
    [SerializeField] private PlayerState_Idle idle;
    [SerializeField] private PlayerState_Moving move;
    [SerializeField] private PlayerState_Sprint sprint;
    [SerializeField] private PlayerState_Jump jump;
    [SerializeField] private PlayerState_Pause paused;
    [SerializeField] private PlayerState_Freeze freeze;

    public Player Player => player;
    public PlayerState_Idle Idle => idle;
    public PlayerState_Moving Moving => move;
    public PlayerState_Sprint Sprint => sprint;
    public PlayerState_Jump Jump => jump;
    public PlayerState_Pause Paused => paused;
    public PlayerState_Freeze Freeze => freeze;

    public BasePlayerState CurrentState { get; private set; }

    private void Awake()
    {
        if (player == null) player = GetComponentInParent<Player>();

        // Auto-collect any state left unassigned so wiring is forgiving.
        if (idle == null) idle = GetComponentInChildren<PlayerState_Idle>(true);
        if (move == null) move = GetComponentInChildren<PlayerState_Moving>(true);
        if (sprint == null) sprint = GetComponentInChildren<PlayerState_Sprint>(true);
        if (jump == null) jump = GetComponentInChildren<PlayerState_Jump>(true);
        if (paused == null) paused = GetComponentInChildren<PlayerState_Pause>(true);
        if (freeze == null) freeze = GetComponentInChildren<PlayerState_Freeze>(true);
    }

    private void OnEnable()
    {
        GameFlowManager.OnStateChanged += HandleGameState;
    }

    private void OnDisable()
    {
        GameFlowManager.OnStateChanged -= HandleGameState;
    }

    private void Start()
    {
        // Start paused if the game isn't already in play, else idle.
        bool playing = GameFlowManager.Instance == null ||
                       GameFlowManager.Instance.currentState == GameFlowManager.GameState.Playing;
        ChangeState(playing ? (BasePlayerState)idle : paused);
    }

    private void Update()
    {
        CurrentState?.OnStateUpdate();
    }

    /// <summary>Exit the current state and enter <paramref name="next"/>.</summary>
    public void ChangeState(BasePlayerState next)
    {
        if (next == null || next == CurrentState) return;

        CurrentState?.OnStateExit();
        CurrentState = next;
        CurrentState.OnStateEnter();
    }

    private void HandleGameState(GameFlowManager.GameState state)
    {
        switch (state)
        {
            // Time keeps running but input is locked — freeze the player in place.
            case GameFlowManager.GameState.Freeze:
                ChangeState(freeze);
                break;

            // Time is stopped — just cut input; global timeScale handles the rest.
            case GameFlowManager.GameState.Paused:
            case GameFlowManager.GameState.LevelFinished:
            case GameFlowManager.GameState.LevelLost:
            case GameFlowManager.GameState.MainMenu:
                ChangeState(paused);
                break;

            case GameFlowManager.GameState.Playing:
                // Only resume from a locked state; don't stomp an in-progress jump.
                if (CurrentState == paused || CurrentState == freeze) ChangeState(idle);
                break;
        }
    }
}
