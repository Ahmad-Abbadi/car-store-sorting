using UnityEngine;

/// <summary> Feeds the hovered target's prompt getters into the ActionPromptController. </summary>
public class ActionPromptProcessor : MonoBehaviour
{
    [Tooltip("Interaction handler whose hover events drive the prompt.")]
    [SerializeField] private InteractionHandler handler;

    [Tooltip("Prompt UI. Auto-found in the scene if left empty.")]
    [SerializeField] private ActionPromptController controller;

    private InteractTarget _target;

    private void Awake()
    {
        if (handler == null) handler = GetComponentInChildren<InteractionHandler>();
        if (controller == null) controller = FindAnyObjectByType<ActionPromptController>(FindObjectsInactive.Include);
    }

    private void OnEnable()
    {
        if (handler == null) return;
        handler.HoverEnter += OnHoverEnter;
        handler.HoverExit += OnHoverExit;
        _target = handler.Current.IsValid ? handler.Current : InteractTarget.None;
    }

    private void OnDisable()
    {
        if (handler != null)
        {
            handler.HoverEnter -= OnHoverEnter;
            handler.HoverExit -= OnHoverExit;
        }
        _target = InteractTarget.None;
        if (controller != null) controller.Hide();
    }

    /// <summary> Stores the new hovered target. </summary>
    private void OnHoverEnter(InteractTarget target) => _target = target;

    /// <summary> Clears the target if it's the one we were showing. </summary>
    private void OnHoverExit(InteractTarget target)
    {
        if (_target.SameGO(target)) _target = InteractTarget.None;
    }

    private void LateUpdate()
    {
        if (controller == null) return;

        if (_target.GO == null || handler == null || !handler.isActiveAndEnabled)
        {
            controller.Hide();
            return;
        }

        // Mirrors the handler: a holdable wins the primary button over a tap.
        bool isHold = _target.Holdable != null;
        string primary = isHold ? _target.Holdable.HoldPrompt : _target.Interactable?.ActionPrompt;
        string secondary = _target.InteractableSecondary?.SecondaryActionPrompt;
        string hover = _target.Hoverable?.HoverPrompt;

        if (string.IsNullOrEmpty(primary) && string.IsNullOrEmpty(secondary) && string.IsNullOrEmpty(hover))
        {
            controller.Hide();
            return;
        }

        bool secondaryFirst = _target.InteractableSecondary != null && _target.InteractableSecondary.SecondaryPromptFirst;
        controller.SetPrompts(hover, primary, isHold, secondary, secondaryFirst);
        controller.SetHoldProgress(handler.HoldProgress01);
        controller.Show();
    }
}
