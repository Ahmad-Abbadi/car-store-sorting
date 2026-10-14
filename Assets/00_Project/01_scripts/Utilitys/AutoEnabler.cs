using UnityEngine;
using DG.Tweening; // Ensure DOTween is installed

public class AutoEnabler : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject targetObject;
    [SerializeField] private float delayInSeconds = 2f;
    [SerializeField] private bool triggerOnStart = true;

    [Header("Animation Settings")]
    [SerializeField] private float scaleDuration = 0.5f;
    [SerializeField] private Ease scaleEase = Ease.OutBack; // OutBack gives it that nice "pop" overshoot
    [SerializeField] private Vector3 finalScale = Vector3.one;

    private void Start()
    {
        if (triggerOnStart)
        {
            TriggerEnable();
        }
    }

    [ContextMenu("Test Enable")]
    public void TriggerEnable()
    {
        if (targetObject == null) return;

        // Ensure the object starts invisible/tiny before the delay finishes
        targetObject.transform.localScale = Vector3.zero;

        // Use DOTween's delayed call to handle the timer
        DOVirtual.DelayedCall(delayInSeconds, () =>
        {
            targetObject.SetActive(true);

            // Smoothly scale up
            targetObject.transform.DOScale(finalScale, scaleDuration)
                .SetEase(scaleEase)
                .SetUpdate(true); // Works even if Time.timeScale is 0
        });
    }
}