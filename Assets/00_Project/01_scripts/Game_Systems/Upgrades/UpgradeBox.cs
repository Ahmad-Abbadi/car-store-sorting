using System;
using UnityEngine;

/// <summary> World box; interacting asks the upgrade panel to open. </summary>
public class UpgradeBox : MonoBehaviour, IInteractable
{
    [Header("Action Prompt")]
    [Tooltip("Primary prompt verb; empty hides it.")]
    [SerializeField] private string actionPrompt = "Upgrades";

    /// <summary> Primary prompt verb. </summary>
    public string ActionPrompt => Localizer.FromEnglish("prompt", actionPrompt);

    /// <summary> Fired when any upgrade box is interacted with. </summary>
    public static event Action OnOpenRequested;

    /// <summary> Instance-specific notification for this machine's presentation. </summary>
    public event Action Interacted;

    /// <summary> Requests the upgrade panel. </summary>
    public void OnInteract(InteractionHandler handler)
    {
        Interacted?.Invoke();
        OnOpenRequested?.Invoke();
    }
}
