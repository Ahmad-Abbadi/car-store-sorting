using UnityEngine;
using UnityEngine.UI;

/// <summary> A single shared click for successful Button activation, including Submit input. </summary>
[DisallowMultipleComponent, RequireComponent(typeof(Button))]
public sealed class UIButtonAudioFeedback : MonoBehaviour
{
    private Button _button;

    public static void Ensure(Button button)
    {
        if (!button) return;
        var feedback = button.GetComponent<UIButtonAudioFeedback>();
        if (!feedback) feedback = button.gameObject.AddComponent<UIButtonAudioFeedback>();
        feedback.Bind();
    }

    private void OnEnable() => Bind();

    private void OnDisable()
    {
        if (_button) _button.onClick.RemoveListener(PlayClick);
    }

    private void Bind()
    {
        _button = GetComponent<Button>();
        _button.onClick.RemoveListener(PlayClick);
        if (isActiveAndEnabled) _button.onClick.AddListener(PlayClick);
    }

    private void PlayClick()
    {
        // A preceding button callback can close its panel; the accepted click still sounds.
        if (!_button || !_button.IsInteractable()) return;
        var audio = SimpleAudioManager.instance;
        if (audio && audio.GameplaySounds) audio.PlayUI(audio.GameplaySounds.UIClick);
    }
}
