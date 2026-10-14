using UnityEngine;
using UnityEngine.UI;

/// <summary> Icon button that toggles the saved SFX or music mute. </summary>
[RequireComponent(typeof(Button))]
public class AudioMuteButton : MonoBehaviour
{
    public enum Channel { Sfx, Music }

    [SerializeField] private Channel _channel;
    [SerializeField] private Image _icon;
    [SerializeField] private Sprite _onSprite;
    [SerializeField] private Sprite _mutedSprite;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(Toggle);
    }

    private void OnEnable()
    {
        SimpleAudioManager.OnMuteSettingsChanged += Refresh;
        Refresh();
    }

    private void OnDisable() => SimpleAudioManager.OnMuteSettingsChanged -= Refresh;

    private void OnDestroy()
    {
        if (_button) _button.onClick.RemoveListener(Toggle);
    }

    private bool IsMuted => _channel == Channel.Sfx ? SimpleAudioManager.SfxMuted : SimpleAudioManager.MusicMuted;

    /// <summary> Flips this channel's saved mute. </summary>
    public void Toggle()
    {
        if (_channel == Channel.Sfx) SimpleAudioManager.SfxMuted = !IsMuted;
        else SimpleAudioManager.MusicMuted = !IsMuted;
    }

    /// <summary> Swaps the icon to match the current mute state. </summary>
    private void Refresh()
    {
        if (_icon) _icon.sprite = IsMuted ? _mutedSprite : _onSprite;
    }
}
