using UnityEngine;
using UnityEngine.UI;

/// <summary> Swaps an Image between its keyboard sprite and a touch sprite by device. </summary>
[RequireComponent(typeof(Image))]
public class InputIconImage : MonoBehaviour
{
    [Tooltip("Sprite shown on touch devices. Empty keeps the keyboard sprite.")]
    [SerializeField] private Sprite touchSprite;

    private Image _image;
    private Sprite _desktopSprite;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _desktopSprite = _image.sprite;
    }

    private void OnEnable()
    {
        InputIcons.Changed += Apply;
        Apply();
    }

    private void OnDisable() => InputIcons.Changed -= Apply;

    /// <summary> Shows the sprite for the current device. </summary>
    private void Apply()
    {
        _image.sprite = InputIcons.IsTouch && touchSprite != null ? touchSprite : _desktopSprite;
    }
}
