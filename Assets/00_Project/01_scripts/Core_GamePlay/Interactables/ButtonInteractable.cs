using UnityEngine;
using UnityEngine.Events;

/// <summary> World button the player interacts with; fires a UnityEvent on press. </summary>
public class ButtonInteractable : MonoBehaviour, IInteractable
{
    [Header("Action Prompt")]
    [Tooltip("Primary prompt verb; empty hides it.")]
    [SerializeField] private string actionPrompt = "Press";

    /// <summary> Primary prompt verb. </summary>
    public string ActionPrompt => Localizer.FromEnglish("prompt", actionPrompt);

    [Header("Event")]
    [Tooltip("Fired when the button is pressed.")]
    [SerializeField] UnityEvent onPressed;

    /// <summary> Fired when the button is pressed. </summary>
    public UnityEvent OnPressed => onPressed;

    /// <summary> Pressed through the interaction system. </summary>
    public void OnInteract(InteractionHandler handler) => Press();

    /// <summary> Presses the button from code or another event. </summary>
    public void Press() => onPressed?.Invoke();
}
