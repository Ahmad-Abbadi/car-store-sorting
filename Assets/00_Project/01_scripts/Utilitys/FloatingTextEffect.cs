using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary> Pooled world-space text that rises and fades. Call FloatingTextEffect.Show. </summary>
public class FloatingTextEffect : MonoBehaviour
{
    private const float DefaultRise = 0.1f;
    private const float DefaultDuration = 1f;
    private const float DefaultSize = 0.5f;

    private static readonly Stack<FloatingTextEffect> Pool = new Stack<FloatingTextEffect>();

    private TextMeshPro _text;
    private Sequence _seq;

    /// <summary> Shows text at a world position, rising and fading out. </summary>
    public static void Show(string message, Vector3 worldPosition, Color? color = null,
        float size = DefaultSize, float rise = DefaultRise, float duration = DefaultDuration)
    {
        FloatingTextEffect fx = Pool.Count > 0 ? Pool.Pop() : Create();
        if (fx == null) fx = Create(); // pooled instance may be destroyed on scene change
        fx.Play(message, worldPosition, color ?? Color.white, size, rise, duration);
    }

    /// <summary> Builds a new pooled instance with its text component. </summary>
    private static FloatingTextEffect Create()
    {
        GameObject go = new GameObject("FloatingText");
        FloatingTextEffect fx = go.AddComponent<FloatingTextEffect>();
        fx._text = go.AddComponent<TextMeshPro>();
        fx._text.alignment = TextAlignmentOptions.Center;
        fx._text.fontStyle = FontStyles.Bold;
        fx._text.textWrappingMode = TextWrappingModes.NoWrap;
        fx._text.outlineWidth = 0.2f;
        fx._text.outlineColor = new Color32(0, 0, 0, 255);
        go.SetActive(false);
        return fx;
    }

    /// <summary> Positions the text and runs the rise-and-fade tween. </summary>
    private void Play(string message, Vector3 position, Color color, float size, float rise, float duration)
    {
        _seq?.Kill();

        _text.SetLocalized(message);
        _text.fontSize = size;
        color.a = 1f;
        _text.color = color;

        transform.position = position;
        Face();
        gameObject.SetActive(true);

        _seq = DOTween.Sequence()
            .Append(transform.DOMoveY(position.y + rise, duration).SetEase(Ease.OutQuad))
            .Join(_text.DOFade(0f, duration * 0.4f).SetDelay(duration * 0.6f))
            .OnComplete(Release);
    }

    /// <summary> Returns the instance to the pool. </summary>
    private void Release()
    {
        _seq = null;
        gameObject.SetActive(false);
        Pool.Push(this);
    }

    /// <summary> Billboards toward the camera. </summary>
    private void LateUpdate() => Face();

    private void Face()
    {
        Camera cam = Camera.main;
        if (cam != null) transform.rotation = cam.transform.rotation;
    }

    private void OnDestroy() => _seq?.Kill();
}
