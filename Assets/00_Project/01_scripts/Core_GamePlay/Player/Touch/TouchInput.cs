using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

/// <summary>
/// Touch controls: left-side drag moves, right-side drag looks, plus on-screen buttons.
/// Shows its canvas only on touch devices; PlayerInput merges these reads.
/// </summary>
[DefaultExecutionOrder(-100)] // sample touches before any gameplay script reads them
public class TouchInput : MonoBehaviour
{
    /// <summary> The scene's touch controls; null when none exist. </summary>
    public static TouchInput Instance { get; private set; }

    [Header("References")]
    [Tooltip("Canvas child shown only while touch controls are active.")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private TouchButton jumpButton;
    [SerializeField] private TouchButton interactButton;
    [SerializeField] private TouchButton placeButton;
    [SerializeField] private TouchButton pauseButton;

    [Tooltip("Stash arrows; shown only while the stash names run past the screen.")]
    [SerializeField] private TouchButton stashUpButton;
    [SerializeField] private TouchButton stashDownButton;

    [Header("Move Stick")]
    [Tooltip("Center-anchored container moved to the touch start point.")]
    [SerializeField] private RectTransform stickRoot;
    [SerializeField] private RectTransform stickKnob;
    [SerializeField] private RectTransform sprintIcon;
    [SerializeField] private Graphic sprintIconGraphic;

    [Tooltip("Drag distance for full speed, in canvas units.")]
    [SerializeField] private float stickRadius = 120f;
    [SerializeField, Range(0f, 0.5f)] private float deadZone = 0.15f;

    [Tooltip("Drag distance that starts sprinting, in canvas units.")]
    [SerializeField] private float sprintDistance = 230f;

    [Header("Look")]
    [Tooltip("Look units per pixel at 1080p height. Mouse is about 0.1.")]
    [SerializeField] private float lookSensitivity = 0.12f;

    [Header("Tap")]
    [SerializeField] private float tapMaxSeconds = 0.3f;
    [SerializeField] private float tapMaxPixels = 24f;

    [Header("Device")]
    [Tooltip("Debug: act as a touch device even with a mouse and keyboard.")]
    [SerializeField] private bool forceTouch;
    [Tooltip("Ignore mouse events this long after a touch, to skip browser compat events.")]
    [SerializeField] private float mouseGraceSeconds = 0.75f;

    private enum Role { None, Move, Look, Button }

    /// <summary> One finger's role and path since it landed. </summary>
    private class Track
    {
        public Role Role;
        public Vector2 Start;
        public Vector2 Last;
        public float StartTime;
        public float Travel;
        public TouchButton Button;
    }

    private readonly Dictionary<int, Track> _tracks = new Dictionary<int, Track>();
    private readonly HashSet<int> _seen = new HashSet<int>();
    private readonly List<int> _gone = new List<int>();
    private readonly List<RaycastResult> _hits = new List<RaycastResult>();

    private bool _touchMode;
    private float _lastTouchTime = float.NegativeInfinity;
    private Vector2 _move;
    private Vector2 _look;
    private bool _sprint;
    private int _tapFrame = -1;
    private int _moveId = -1;
    private int _lookId = -1;

    /// <summary> True once a touch device is detected; flips back on keyboard or mouse use. </summary>
    public bool IsTouchMode => _touchMode;

    /// <summary> True while the controls are shown and driving input. </summary>
    public bool IsActive { get; private set; }

    /// <summary> Move stick value, x = right, y = forward. </summary>
    public Vector2 Move => _move;

    /// <summary> Look delta this frame, already scaled. </summary>
    public Vector2 LookDelta => _look;

    /// <summary> True while the stick is dragged past the sprint distance. </summary>
    public bool SprintHeld => _sprint;

    /// <summary> True on the frame the jump button went down. </summary>
    public bool JumpPressed => jumpButton != null && jumpButton.PressedThisFrame;

    /// <summary> True on the frame the interact button went down. </summary>
    public bool InteractPressed => interactButton != null && interactButton.PressedThisFrame;

    /// <summary> True while the interact button is held. </summary>
    public bool InteractHeld => interactButton != null && interactButton.Held;

    /// <summary> True on the frame the place button went down. </summary>
    public bool PlacePressed => placeButton != null && placeButton.PressedThisFrame;

    /// <summary> True while the place button is held. </summary>
    public bool PlaceHeld => placeButton != null && placeButton.Held;

    /// <summary> True on the frame the pause button went down. </summary>
    public bool PausePressed => pauseButton != null && pauseButton.PressedThisFrame;

    /// <summary> True on the frame the stash up arrow went down. </summary>
    public bool StashUpPressed => stashUpButton != null && stashUpButton.PressedThisFrame;

    /// <summary> True on the frame the stash down arrow went down. </summary>
    public bool StashDownPressed => stashDownButton != null && stashDownButton.PressedThisFrame;

    /// <summary> True on the frame a short tap lifted outside the buttons. </summary>
    public bool TapThisFrame => _tapFrame == Time.frameCount;

    private void Awake()
    {
        Instance = this;
        _touchMode = forceTouch || Application.isMobilePlatform;
        if (canvas != null) canvas.gameObject.SetActive(false);
    }

    private void OnDisable() => ReleaseAll();

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        UpdateMode();

        bool active = _touchMode && PlayerAllowsInput();
        if (active != IsActive)
        {
            IsActive = active;
            if (canvas != null) canvas.gameObject.SetActive(active);
            if (!active) ReleaseAll();
        }
        if (!IsActive) return;

        _look = Vector2.zero;
        Touchscreen screen = Touchscreen.current;
        if (screen == null)
        {
            ReleaseAll();
            return;
        }

        UpdateStashArrows();
        ReadTouches(screen);
        UpdateStickVisual();
    }

    /// <summary> Switches between touch and desktop mode from the last input used. </summary>
    private void UpdateMode()
    {
        if (forceTouch)
        {
            SetMode(true);
            return;
        }

        Touchscreen screen = Touchscreen.current;
        if (screen != null && screen.primaryTouch.press.isPressed)
        {
            _lastTouchTime = Time.unscaledTime;
            SetMode(true);
            return;
        }

        if (!_touchMode || Time.unscaledTime - _lastTouchTime < mouseGraceSeconds) return;

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        bool keyed = keyboard != null && keyboard.anyKey.wasPressedThisFrame;
        bool clicked = mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame);
        bool moved = mouse != null && mouse.delta.ReadValue().sqrMagnitude > 4f;
        if (keyed || clicked || moved) SetMode(false);
    }

    /// <summary> Sets touch mode and tells the prompt icons when it changes. </summary>
    private void SetMode(bool touchMode)
    {
        if (_touchMode == touchMode) return;

        _touchMode = touchMode;
        InputIcons.RaiseChanged();
    }

    /// <summary> True when the player may take input right now. </summary>
    private static bool PlayerAllowsInput()
    {
        Player player = Player.Instance;
        return player != null && player.Input != null && player.Input.IsEnabled;
    }

    private float Scale => canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;

    /// <summary> Routes every finger to move, look or a button and updates their values. </summary>
    private void ReadTouches(Touchscreen screen)
    {
        _seen.Clear();
        var touches = screen.touches;
        for (int i = 0; i < touches.Count; i++)
        {
            TouchControl touch = touches[i];
            TouchPhase phase = touch.phase.ReadValue();
            if (phase == TouchPhase.None) continue;

            int id = touch.touchId.ReadValue();
            Vector2 pos = touch.position.ReadValue();

            if (phase == TouchPhase.Ended || phase == TouchPhase.Canceled)
            {
                if (_tracks.TryGetValue(id, out Track ended)) EndTrack(id, ended, pos);
                continue;
            }

            _seen.Add(id);
            if (!_tracks.TryGetValue(id, out Track track))
            {
                track = BeginTrack(id, pos);
                _tracks[id] = track;
            }
            else
            {
                UpdateTrack(track, pos);
            }
        }

        // Fingers that vanished without an end phase.
        _gone.Clear();
        foreach (KeyValuePair<int, Track> pair in _tracks)
            if (!_seen.Contains(pair.Key)) _gone.Add(pair.Key);
        foreach (int id in _gone) EndTrack(id, _tracks[id], _tracks[id].Last);
    }

    /// <summary> Decides what a new finger controls. </summary>
    private Track BeginTrack(int id, Vector2 pos)
    {
        var track = new Track { Start = pos, Last = pos, StartTime = Time.unscaledTime };

        TouchButton button = ButtonAt(pos);
        if (button != null)
        {
            track.Role = Role.Button;
            track.Button = button;
            button.Press();
        }
        else if (IsOverSelectable(pos))
        {
            track.Role = Role.None; // a HUD button owns this finger
        }
        else if (pos.x < Screen.width * 0.5f)
        {
            if (_moveId < 0)
            {
                track.Role = Role.Move;
                _moveId = id;
                PlaceStick(pos);
            }
        }
        else if (_lookId < 0)
        {
            track.Role = Role.Look;
            _lookId = id;
        }
        return track;
    }

    /// <summary> Applies a finger's new position to its role. </summary>
    private void UpdateTrack(Track track, Vector2 pos)
    {
        Vector2 delta = pos - track.Last;
        track.Travel += delta.magnitude;
        track.Last = pos;

        if (track.Role == Role.Move) UpdateMove(pos - track.Start);
        else if (track.Role == Role.Look) _look += delta * (lookSensitivity * 1080f / Mathf.Max(1f, Screen.height));
    }

    /// <summary> Releases a finger's role and detects taps. </summary>
    private void EndTrack(int id, Track track, Vector2 pos)
    {
        _tracks.Remove(id);

        switch (track.Role)
        {
            case Role.Button:
                track.Button.Release();
                break;
            case Role.Move:
                _moveId = -1;
                _move = Vector2.zero;
                _sprint = false;
                break;
            case Role.Look:
                _lookId = -1;
                break;
        }

        bool tapRole = track.Role == Role.Move || track.Role == Role.Look;
        if (tapRole && track.Travel <= tapMaxPixels && Time.unscaledTime - track.StartTime <= tapMaxSeconds)
            _tapFrame = Time.frameCount;
    }

    /// <summary> Turns the drag offset into stick value and sprint state. </summary>
    private void UpdateMove(Vector2 offsetPixels)
    {
        float scale = Scale;
        Vector2 offset = offsetPixels / scale; // canvas units
        float dist = offset.magnitude;
        float normalized = dist / Mathf.Max(1f, stickRadius);

        if (normalized < deadZone) _move = Vector2.zero;
        else _move = offset.normalized * Mathf.Clamp01((normalized - deadZone) / (1f - deadZone));

        // Sprint latches past the distance and drops a little inside it.
        if (!_sprint && dist >= sprintDistance) _sprint = true;
        else if (_sprint && dist < sprintDistance * 0.9f) _sprint = false;

        if (stickKnob != null) stickKnob.anchoredPosition = Vector2.ClampMagnitude(offset, stickRadius);
        if (sprintIcon != null && dist > 0.01f) sprintIcon.anchoredPosition = offset.normalized * sprintDistance;
    }

    /// <summary> Moves the stick visuals to where the finger landed. </summary>
    private void PlaceStick(Vector2 screenPos)
    {
        if (stickRoot == null || canvas == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, screenPos, null, out Vector2 local);
        stickRoot.anchoredPosition = local;
        if (stickKnob != null) stickKnob.anchoredPosition = Vector2.zero;
        if (sprintIcon != null) sprintIcon.anchoredPosition = Vector2.up * sprintDistance;
    }

    /// <summary> Shows the stick while dragging and fades the sprint icon in as it nears. </summary>
    private void UpdateStickVisual()
    {
        if (stickRoot == null) return;

        bool dragging = _moveId >= 0;
        if (stickRoot.gameObject.activeSelf != dragging) stickRoot.gameObject.SetActive(dragging);
        if (!dragging || sprintIconGraphic == null) return;

        float dist = _tracks.TryGetValue(_moveId, out Track track) ? (track.Last - track.Start).magnitude / Scale : 0f;
        float near = Mathf.InverseLerp(stickRadius, sprintDistance, dist);
        Color c = sprintIconGraphic.color;
        c.a = _sprint ? 1f : near * 0.7f;
        sprintIconGraphic.color = c;
        sprintIcon.localScale = Vector3.one * (_sprint ? 1.25f : 1f);
    }

    /// <summary> Shows the stash arrows only while the stash names overflow the screen. </summary>
    private void UpdateStashArrows()
    {
        bool show = EquipmentItemsUI.Overflowing;
        if (stashUpButton != null && stashUpButton.gameObject.activeSelf != show) stashUpButton.gameObject.SetActive(show);
        if (stashDownButton != null && stashDownButton.gameObject.activeSelf != show) stashDownButton.gameObject.SetActive(show);
    }

    /// <summary> First visible on-screen button under the point. </summary>
    private TouchButton ButtonAt(Vector2 pos)
    {
        TouchButton[] all = { jumpButton, interactButton, placeButton, pauseButton, stashUpButton, stashDownButton };
        foreach (TouchButton button in all)
            if (button != null && button.gameObject.activeInHierarchy && button.Contains(pos)) return button;
        return null;
    }

    /// <summary> True when the top UI under the point is a selectable such as a HUD button. </summary>
    private bool IsOverSelectable(Vector2 pos)
    {
        EventSystem system = EventSystem.current;
        if (system == null) return false;

        _hits.Clear();
        system.RaycastAll(new PointerEventData(system) { position = pos }, _hits);
        return _hits.Count > 0 && _hits[0].gameObject.GetComponentInParent<Selectable>() != null;
    }

    /// <summary> Drops every finger and clears all outputs. </summary>
    private void ReleaseAll()
    {
        _tracks.Clear();
        _moveId = -1;
        _lookId = -1;
        _move = Vector2.zero;
        _look = Vector2.zero;
        _sprint = false;
        if (jumpButton != null) jumpButton.Release();
        if (interactButton != null) interactButton.Release();
        if (placeButton != null) placeButton.Release();
        if (pauseButton != null) pauseButton.Release();
        if (stashUpButton != null) stashUpButton.Release();
        if (stashDownButton != null) stashDownButton.Release();
        if (stickRoot != null) stickRoot.gameObject.SetActive(false);
    }
}
