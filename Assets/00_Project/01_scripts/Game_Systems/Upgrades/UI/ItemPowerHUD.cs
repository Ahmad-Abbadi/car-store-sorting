using UnityEngine;
using UnityEngine.UI;

/// <summary> HUD slot for one item power: hidden until bought, radial fill while cooling down. </summary>
public class ItemPowerHUD : MonoBehaviour
{
    [SerializeField] private ItemPowerBase power;

    [Tooltip("Shown only once unlocked. Must be a child, not this object.")]
    [SerializeField] private GameObject root;

    [Tooltip("Optional. Gets the upgrade's shop icon.")]
    [SerializeField] private Image icon;

    [Tooltip("Filled image (Radial 360) covering the icon during cooldown.")]
    [SerializeField] private Image cooldownFill;

    [Tooltip("Green filled image (Radial 360) draining while the effect is active.")]
    [SerializeField] private Image durationFill;

    [Tooltip("Icon tint while active or cooling down.")]
    [SerializeField] private Color unavailableTint = new Color(1f, 1f, 1f, 0.4f);

    private void Start()
    {
        if (icon != null && power != null && power.Icon != null) icon.sprite = power.Icon;
        MakeTappable();
    }

    /// <summary> Makes the icon a button that fires the power, for touch. </summary>
    private void MakeTappable()
    {
        if (icon == null || power == null) return;

        icon.raycastTarget = true;
        if (cooldownFill != null) cooldownFill.raycastTarget = false;
        if (durationFill != null) durationFill.raycastTarget = false;

        Button button = icon.GetComponent<Button>();
        if (button == null) button = icon.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(power.RequestUse);
        UIButtonAudioFeedback.Ensure(button);
    }

    private void Update()
    {
        if (power == null) return;

        bool unlocked = power.IsUnlocked;
        if (root != null && root.activeSelf != unlocked) root.SetActive(unlocked);
        if (!unlocked) return;

        // Green drains while active, then the dark cooldown drains.
        if (durationFill != null) durationFill.fillAmount = power.Active01;
        if (cooldownFill != null) cooldownFill.fillAmount = power.IsActive ? 0f : power.Cooldown01;
        if (icon != null) icon.color = power.IsReady ? Color.white : unavailableTint;
    }
}
