using UnityEngine;

/// <summary> Plays a footstep clip every stride of grounded travel; sprint uses its own stride. </summary>
public class PlayerFootsteps : MonoBehaviour
{
    [Tooltip("Player whose motor and state machine drive the steps.")]
    [SerializeField] private Player player;

    [Header("Stride")]
    [Tooltip("Meters walked between two steps.")]
    [SerializeField] private float walkStride = 1.8f;

    [Tooltip("Meters sprinted between two steps.")]
    [SerializeField] private float sprintStride = 2.4f;

    [Tooltip("Planar speed below this counts as standing still.")]
    [SerializeField] private float minSpeed = 0.2f;

    private float _travelled; // meters since the last step

    private void Awake()
    {
        if (player == null) player = GetComponentInParent<Player>();
    }

    private void Update()
    {
        if (player == null || player.Motor == null) return;

        Vector3 velocity = Vector3.ProjectOnPlane(player.Motor.BaseVelocity, player.Motor.CharacterUp);
        float speed = velocity.magnitude;

        // Airborne or idle: first step lands right after moving again.
        if (!player.Motor.GroundingStatus.IsStableOnGround || speed < minSpeed)
        {
            _travelled = 0f;
            return;
        }

        _travelled += speed * Time.deltaTime;
        if (_travelled < CurrentStride()) return;

        _travelled = 0f;
        PlayStepSFX();
    }

    /// <summary> Sprint stride while in the sprint state, else walk stride. </summary>
    private float CurrentStride()
    {
        PlayerStateMachine machine = player.StateMachine;
        bool sprinting = machine != null && machine.CurrentState == machine.Sprint;
        return sprinting ? sprintStride : walkStride;
    }

    /// <summary> Plays a random step clip from the gameplay sound library. </summary>
    private void PlayStepSFX()
    {
        SimpleAudioManager audio = SimpleAudioManager.instance;
        if (audio == null || audio.GameplaySounds == null) return;

        audio.PlayJuicyGlobalClip(audio.GameplaySounds.GetRandomStepSound());
    }
}
