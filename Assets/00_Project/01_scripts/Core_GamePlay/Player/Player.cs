using UnityEngine;
using KinematicCharacterController;
using KinematicCharacterController.Examples;

/// <summary>
/// Central reference holder for the player. Systems that need a player component
/// reach it through here instead of each caching its own refs — most importantly
/// the FPS movement states, which are only allowed to touch <see cref="Player"/>
/// (see <see cref="BasePlayerState"/>).
///
/// This holds <b>only</b> references to the player's other components — no input
/// config, no behaviour. Those live on the components themselves: movement/jump
/// input on <see cref="PlayerInput"/>, look + cursor on
/// <see cref="PlayerCameraBehaviour"/>. Every ref is wired in the inspector; there
/// are no auto-find fallbacks. Add a ref here whenever a state needs a new component.
/// </summary>
public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }

    [Tooltip("The KCC controller that resolves inputs into motion.")]
    public ExampleCharacterController Character;

    [Tooltip("The KCC motor.")]
    public KinematicCharacterMotor Motor;

    [Tooltip("Transform whose forward defines movement direction — the yaw/look transform (the FPS Vcam).")]
    public Transform LookTransform;

    [Tooltip("Mouse look — owns the absolute yaw the body follows and the camera pitch.")]
    public FpsMouseLook Look;

    [Tooltip("Optional. Vetoes move directions that walk off a ledge.")]
    public LedgeGuard LedgeGuard;

    [Tooltip("The player's input authority / middle man. Gated by the state machine.")]
    public PlayerInput Input;

    [Tooltip("Owns mouse-look enable/disable + cursor lock.")]
    public PlayerCameraBehaviour CameraBehaviour;

    [Tooltip("The character's Animator. Frozen (speed 0) by the Paused state so it stops animating too.")]
    public Animator Animator;

    [Tooltip("The movement state machine.")]
    public PlayerStateMachine StateMachine;

    [Tooltip("Health/stamina stats for the player.")]
    public StatsBehaviour Stats;

    [Tooltip("Stamina rules — gates and drains jump/sprint against the Stats pool.")]
    public StaminaBehaviour Stamina;

    public ItemsHandler ItemHandler;
    public StashHandler StashHandler;

    [Tooltip("Stashes then equips collected items.")]
    public ItemCollectingBehaviour ItemCollector;

    [Tooltip("Places held sortable items on shelves and takes them back.")]
    public PlayerSortingItemsBehaviour SortingBehaviour;


    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
    }
}
