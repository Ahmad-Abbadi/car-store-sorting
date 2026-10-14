using UnityEngine;

public class AutoDisabler : MonoBehaviour
{
    [Tooltip("Time in seconds before the GameObject is automatically disabled.")]
    [SerializeField] private float disableDelay = 2f;

    private void OnEnable()
    {
        // Schedule the DisableObject method to run after the delay
        Invoke(nameof(DisableObject), disableDelay);
    }

    private void OnDisable()
    {
        // Cancel the invoke in case the object gets disabled manually before the timer ends
        CancelInvoke(nameof(DisableObject));
    }

    private void DisableObject()
    {
        gameObject.SetActive(false);
    }
}