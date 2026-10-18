using UnityEngine;
using UnityEngine.UI;

/// <summary> One on-screen button; TouchInput presses it and PlayerInput reads its state. </summary>
[RequireComponent(typeof(RectTransform))]
public class TouchButton : MonoBehaviour
{
    [Tooltip("Circle behind the icon, tinted by state.")]
    [SerializeField] private Graphic background;

    [SerializeField, Range(0.5f, 1f)] private float pressedScale = 0.9f;

    private RectTransform _rect;
    private int _pressedFrame = -1;

    private RectTransform Rect => _rect != null ? _rect : (_rect = (RectTransform)transform);

    /// <summary> True while a finger holds the button. </summary>
    public bool Held { get; private set; }

    /// <summary> True on the frame a finger landed on the button. </summary>
    public bool PressedThisFrame => _pressedFrame == Time.frameCount;

    /// <summary> True when the screen point is inside the button. </summary>
    public bool Contains(Vector2 screenPoint) =>
        RectTransformUtility.RectangleContainsScreenPoint(Rect, screenPoint, null);

    /// <summary> Marks the button pressed this frame and held. </summary>
    public void Press()
    {
        Held = true;
        _pressedFrame = Time.frameCount;
        ApplyVisual();
    }

    /// <summary> Lets go of the button. </summary>
    public void Release()
    {
        Held = false;
        ApplyVisual();
    }

    private void OnDisable()
    {
        Held = false;
        _pressedFrame = -1;
        ApplyVisual();
    }

    private void ApplyVisual()
    {
        var theme = ToyShop.UI.ToyShopTheme.Current;
        if (background != null) background.color = Held ? theme.blue : theme.cream;
        Rect.localScale = Held ? Vector3.one * pressedScale : Vector3.one;
    }
}
