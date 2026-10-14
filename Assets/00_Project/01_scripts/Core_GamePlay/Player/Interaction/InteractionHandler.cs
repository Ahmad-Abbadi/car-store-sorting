using System;
using UnityEngine;

/// <summary>
/// Drives first-person interaction: every frame it casts a ray out of the middle of the
/// camera, figures out what (if anything) is under the reticle, and routes hover / tap /
/// hold callbacks to whichever of <see cref="IHoverable"/>, <see cref="IInteractable"/>,
/// <see cref="IInteractableSecondary"/> and <see cref="IHoldable"/> that object implements.
///
/// Hover is automatic (enter/stay/exit). Primary interact (E) fires a one-shot
/// <see cref="IInteractable.OnInteract"/>, or starts a timed hold when the target is an
/// <see cref="IHoldable"/> — completing at <see cref="IHoldable.HoldDuration"/> or cancelling
/// when released early / when the player looks away. Secondary interact (F) is tap-only and
/// routes to <see cref="IInteractableSecondary.OnInteractSecondary"/>.
/// </summary>
[DefaultExecutionOrder(110)] // after CharacterCameraController (100) so we cast this frame's orientation
public class InteractionHandler : MonoBehaviour
{
    [Header("Ray Source")]
    [Tooltip("Camera the ray shoots from. Falls back to Camera.main if left empty.")]
    [SerializeField] private Camera sourceCamera;

    [Tooltip("Only colliders on these layers are considered interactable.")]
    [SerializeField] private LayerMask interactableMask = ~0;

    [Tooltip("How far the interaction ray reaches, in world units.")]
    [SerializeField] private float maxDistance = 3f;

    [Tooltip("Whether the ray should also detect trigger colliders.")]
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Input")]
    [Tooltip("Player input authority. Auto-found on a parent Player if left empty.")]
    [SerializeField] private PlayerInput input;

    [Header("Debug")]
    [Tooltip("Draw the interaction ray in the Scene view.")]
    [SerializeField] private bool drawDebugRay = false;

    /// <summary> Fired when the reticle lands on a new valid target. </summary>
    public event Action<InteractTarget> HoverEnter;

    /// <summary> Fired when the reticle leaves a valid target. </summary>
    public event Action<InteractTarget> HoverExit;

    /// <summary>What the reticle is pointing at right now (may be an invalid/empty target).</summary>
    public InteractTarget Current { get; private set; }

    /// <summary>World point hit by the current interaction ray, updated before hover and input callbacks.</summary>
    public Vector3 CurrentHitPoint { get; private set; }

    /// <summary>Whether the current interaction ray hit a collider.</summary>
    public bool HasCurrentHit { get; private set; }

    /// <summary>Normalised 0..1 progress of the active hold, or 0 when nothing is being held.</summary>
    public float HoldProgress01 { get; private set; }

    /// <summary>The camera the ray is currently shooting from.</summary>
    public Camera SourceCamera => sourceCamera;

    /// <summary> How far the interaction ray reaches, in world units. </summary>
    public float MaxDistance => maxDistance;

    /// <summary> Sets how far the interaction ray reaches. </summary>
    public void SetMaxDistance(float distance) => maxDistance = Mathf.Max(0f, distance);

    private IHoldable _activeHold; // the holdable being charged this press, if any
    private float _holdElapsed;    // seconds accumulated into the active hold
    private bool _holdCompleted;   // guards OnHoldComplete against firing twice

    private void Awake()
    {
        if (sourceCamera == null) sourceCamera = Camera.main;
        if (input == null)
        {
            Player p = GetComponentInParent<Player>();
            if (p != null) input = p.Input;
        }
    }

    private void Update()
    {
        if (sourceCamera == null) return;

        InteractTarget hit = Raycast();
        UpdateHover(hit);
        UpdateInput(hit);

        Current = hit;
    }

    /// <summary>Fires a ray from the centre of the camera and wraps whatever it hits in an InteractTarget.</summary>
    private InteractTarget Raycast()
    {
        Ray ray = sourceCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (drawDebugRay)
            Debug.DrawRay(ray.origin, ray.direction * maxDistance, Color.cyan);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactableMask, triggerInteraction))
        {
            CurrentHitPoint = hit.point;
            HasCurrentHit = true;
            return new InteractTarget(hit.collider.gameObject);
        }

        HasCurrentHit = false;
        return InteractTarget.None;
    }

    /// <summary>Sends hover enter/stay/exit as the reticle moves between targets.</summary>
    private void UpdateHover(in InteractTarget next)
    {
        // Same object as last frame → just keep it warm.
        if (Current.SameGO(next))
        {
            next.Hoverable?.OnHoverStay();
            return;
        }

        // Target changed: exit the old one, enter the new one.
        Current.Hoverable?.OnHoverExit();
        if (Current.IsValid) HoverExit?.Invoke(Current);

        next.Hoverable?.OnHoverEnter();
        next.Hoverable?.OnHoverStay();
        if (next.IsValid) HoverEnter?.Invoke(next);
    }

    /// <summary>Reads both interact buttons and dispatches tap vs. hold logic for the current target.</summary>
    private void UpdateInput(in InteractTarget target)
    {
        // A hold is in progress — advance or resolve it before anything else.
        if (_activeHold != null)
        {
            TickHold(target);
            return;
        }

        if (input == null) return;

        // Secondary (F) is always a one-shot tap; it never starts a hold.
        if (input.InteractSecondaryPressed)
            target.InteractableSecondary?.OnInteractSecondary(this);

        if (!input.InteractPrimaryPressed) return;

        // Prefer a hold when the target supports one; otherwise fall back to a plain tap.
        if (target.Holdable != null)
            BeginHold(target.Holdable);
        else
            target.Interactable?.OnInteract(this);
    }

    /// <summary>Starts charging a hold on the given holdable.</summary>
    private void BeginHold(IHoldable holdable)
    {
        _activeHold = holdable;
        _holdElapsed = 0f;
        _holdCompleted = false;
        HoldProgress01 = 0f;
        holdable.OnHoldStart(this);
    }

    /// <summary>Advances the active hold each frame and completes or cancels it as appropriate.</summary>
    private void TickHold(in InteractTarget target)
    {
        // Released the button (or input gated), or the holdable is no longer under the reticle → cancel.
        bool stillHeld = input != null && input.InteractPrimaryHeld;
        bool stillTargeted = ReferenceEquals(target.Holdable, _activeHold);
        if (!stillHeld || !stillTargeted)
        {
            CancelHold();
            return;
        }

        float duration = Mathf.Max(0f, _activeHold.HoldDuration);
        _holdElapsed += Time.deltaTime;
        HoldProgress01 = duration <= 0f ? 1f : Mathf.Clamp01(_holdElapsed / duration);

        if (!_holdCompleted && _holdElapsed >= duration)
            CompleteHold();
        else
            _activeHold.OnHoldStay(this);
    }

    /// <summary>Fires the completion callback once and clears the active hold.</summary>
    private void CompleteHold()
    {
        _holdCompleted = true;
        HoldProgress01 = 1f;
        IHoldable finished = _activeHold;
        ClearHold();
        finished.OnHoldComplete(this);
    }

    /// <summary>Fires the cancel callback and clears the active hold.</summary>
    private void CancelHold()
    {
        IHoldable cancelled = _activeHold;
        ClearHold();
        cancelled.OnHoldCancel(this);
    }

    /// <summary>Resets all hold bookkeeping back to "nothing being held".</summary>
    private void ClearHold()
    {
        _activeHold = null;
        _holdElapsed = 0f;
        _holdCompleted = false;
        HoldProgress01 = 0f;
    }

    /// <summary>Safety net: if the component is disabled mid-hold, cancel cleanly.</summary>
    private void OnDisable()
    {
        if (_activeHold != null) CancelHold();
    }
}
