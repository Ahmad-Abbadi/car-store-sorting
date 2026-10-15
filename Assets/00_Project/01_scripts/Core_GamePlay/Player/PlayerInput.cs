using UnityEngine;

/// <summary>
/// The player's single input authority — the middle man between the Input System and
/// every player system. This is the ONLY player script that touches the generated
/// <see cref="GameControls"/> asset; all others (movement, look, items, interaction,
/// animation) read through here. One flag gates them: while disabled the Player action map
/// is switched off, so gameplay reads report neutral values and the look system is stopped.
/// The Global map (pause) stays enabled so the game can always be un-paused.
///
/// Reached through the <see cref="Player"/> ref holder (<see cref="Player.Input"/>).
/// </summary>
public class PlayerInput : MonoBehaviour
{
    [Header("Look")]
    [Tooltip("Scales raw mouse pixel delta. 0.1 matches the old Mouse X/Y axis feel.")]
    [SerializeField] private float lookScale = 0.1f;

    [Header("Scrolling")]
    [Tooltip("Scroll magnitude needed to count as one step.")]
    [SerializeField] private float scrollThreshold = 0.1f;

    [Header("Governed Systems")]
    [Tooltip("Optional. Look/cursor owner toggled together with input so one call cuts everything.")]
    [SerializeField] private PlayerCameraBehaviour look;

    [Header("Startup")]
    [SerializeField] private bool startEnabled = true;

    /// <summary>Whether the player may drive input right now. Gated by the state machine.</summary>
    public bool IsEnabled { get; private set; }

    private GameControls _controls;
    public bool CollectionPressed => IsEnabled && UnityEngine.InputSystem.Keyboard.current != null &&
        UnityEngine.InputSystem.Keyboard.current.cKey.wasPressedThisFrame;

    /// <summary>The generated actions, created on first touch so Awake order never matters.</summary>
    private GameControls Controls
    {
        get
        {
            if (_controls == null)
            {
                _controls = new GameControls();
                _controls.Global.Enable(); // pause must survive the gate
            }
            return _controls;
        }
    }

    // Gates the map only; the look component keeps whatever state it was authored with.
    private void Awake()
    {
        IsEnabled = startEnabled;
        ApplyGate();
    }

    private void OnEnable() => ApplyGate();

    private void OnDisable()
    {
        if (_controls == null) return;
        _controls.Player.Disable();
    }

    private void OnDestroy()
    {
        _controls?.Disable();
        _controls?.Dispose();
        _controls = null;
    }

    /// <summary> Touch controls in the scene; null on builds without them. </summary>
    private static TouchInput Touch => TouchInput.Instance;

    // --- Movement reads (neutral while disabled) ---

    /// <summary>Steer as a vector — x = right, y = forward; zero while disabled.</summary>
    public Vector2 MoveAxis
    {
        get
        {
            if (!IsEnabled) return Vector2.zero;
            Vector2 keys = Controls.Player.Move.ReadValue<Vector2>();
            if (keys.sqrMagnitude > 0.0001f || Touch == null) return keys;
            return Touch.Move;
        }
    }

    /// <summary>Forward/back steer this frame (-1..1); 0 while disabled.</summary>
    public float Forward => MoveAxis.y;

    /// <summary>Left/right steer this frame (-1..1); 0 while disabled.</summary>
    public float Right => MoveAxis.x;

    /// <summary>True while there is meaningful steer input this frame.</summary>
    public bool HasMoveInput => MoveAxis.sqrMagnitude > 0.0001f;

    /// <summary>True only on the frame jump goes down; false while disabled.</summary>
    public bool JumpPressed => IsEnabled && (Controls.Player.Jump.WasPressedThisFrame() || (Touch != null && Touch.JumpPressed));

    /// <summary>True while sprint is held; false while disabled or sprint is locked.</summary>
    public bool SprintHeld => IsEnabled && SprintEnabled && (Controls.Player.Sprint.IsPressed() || (Touch != null && Touch.SprintHeld));

    /// <summary> Whether sprinting is allowed at all. On unless locked by upgrades. </summary>
    public bool SprintEnabled { get; private set; } = true;

    /// <summary> Allows or locks sprinting. </summary>
    public void SetSprintEnabled(bool enabled) => SprintEnabled = enabled;

    // --- Look read (neutral while disabled) ---

    /// <summary>Look delta this frame (x = yaw, y = pitch); zero while disabled.</summary>
    public Vector2 LookDelta
    {
        get
        {
            if (!IsEnabled) return Vector2.zero;
            Vector2 look = Controls.Player.Look.ReadValue<Vector2>() * lookScale;
            return Touch != null ? look + Touch.LookDelta : look;
        }
    }

    // --- World interaction reads (E / LMB, F / Q / RMB) ---

    /// <summary>Primary interact (E / LMB) went down this frame; false while disabled.</summary>
    public bool InteractPrimaryPressed => IsEnabled && (Controls.Player.Interaction_Primary.WasPressedThisFrame() || (Touch != null && Touch.InteractPressed));

    /// <summary>Primary interact (E / LMB) held; false while disabled.</summary>
    public bool InteractPrimaryHeld => IsEnabled && (Controls.Player.Interaction_Primary.IsPressed() || (Touch != null && Touch.InteractHeld));

    /// <summary>Secondary interact (F / Q / RMB) went down this frame; also throws.</summary>
    public bool InteractSecondaryPressed => IsEnabled && (Controls.Player.Interaction_Secondary.WasPressedThisFrame() || (Touch != null && Touch.PlacePressed));

    /// <summary>Secondary interact (F / Q / RMB) held; false while disabled.</summary>
    public bool InteractSecondaryHeld => IsEnabled && (Controls.Player.Interaction_Secondary.IsPressed() || (Touch != null && Touch.PlaceHeld));

    // --- Hand item reads (LMB / RMB) ---

    /// <summary>Primary item button (LMB) went down this frame; false while disabled.</summary>
    public bool ClickPrimaryPressed => IsEnabled && Controls.Player.Clickable_Primary.WasPressedThisFrame();

    /// <summary>Primary item button (LMB) held; false while disabled.</summary>
    public bool ClickPrimaryHeld => IsEnabled && Controls.Player.Clickable_Primary.IsPressed();

    /// <summary>Secondary item button (RMB) went down this frame; false while disabled.</summary>
    public bool ClickSecondaryPressed => IsEnabled && Controls.Player.Clickable_Secondary.WasPressedThisFrame();

    // --- Stash selection reads (wheel / number row) ---

    /// <summary>Scroll wheel delta this frame; 0 while disabled.</summary>
    public float ScrollDelta => IsEnabled ? Controls.Player.ItemScrolling.ReadValue<float>() : 0f;

    /// <summary>Wheel rolled up past the deadzone this frame; false while disabled.</summary>
    public bool ScrollUp => ScrollDelta > scrollThreshold;

    /// <summary>Wheel rolled down past the deadzone this frame; false while disabled.</summary>
    public bool ScrollDown => ScrollDelta < -scrollThreshold;

    // --- Power reads ---

    /// <summary>Highlight Items power key (1) pressed this frame; false while disabled.</summary>
    public bool HighlightPowerPressed => IsEnabled && Controls.Player.Power_HighlightItems.WasPressedThisFrame();

    /// <summary>Auto-Collect power key (2) pressed this frame; false while disabled.</summary>
    public bool AutoCollectPowerPressed => IsEnabled && Controls.Player.Power_AutoCollect.WasPressedThisFrame();

    // --- Ungated system read ---

    /// <summary>Pause toggle — stays live while input is disabled so the game can un-pause.</summary>
    public bool PausePressed => Controls.Global.Pause.WasPressedThisFrame() || (Touch != null && Touch.PausePressed);

    /// <summary>Touch stash up arrow went down this frame; false while disabled.</summary>
    public bool StashScrollUpPressed => IsEnabled && Touch != null && Touch.StashUpPressed;

    /// <summary>Touch stash down arrow went down this frame; false while disabled.</summary>
    public bool StashScrollDownPressed => IsEnabled && Touch != null && Touch.StashDownPressed;

    // --- Authority (driven by the state machine) ---

    /// <summary>Enable or disable all player input, toggling the look system with it.</summary>
    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
        ApplyGate();
        if (look != null) look.SetLook(enabled);
    }

    /// <summary>Restore player input (and look).</summary>
    public void Enable() => SetEnabled(true);

    /// <summary>Cut all player input (and look).</summary>
    public void Disable() => SetEnabled(false);

    /// <summary>Switches the Player map to match the gate; Global always stays on.</summary>
    private void ApplyGate()
    {
        if (IsEnabled) Controls.Player.Enable();
        else Controls.Player.Disable();
    }
}
