using UnityEngine;
using UnityEngine.UI;

/// <summary> Ring of dots stepping clockwise with a fading tail; shows the game is alive. </summary>
public class LoadingSpinner : MonoBehaviour
{
    [Tooltip("Dots in clockwise ring order.")]
    [SerializeField] private Image[] dots = new Image[0];

    [Tooltip("Lead-dot steps per second.")]
    [Min(0.1f)]
    [SerializeField] private float stepsPerSecond = 10f;

    [Tooltip("Alpha of the dimmest tail dot.")]
    [Range(0f, 1f)]
    [SerializeField] private float minAlpha = 0.2f;

    [Tooltip("Scale of the smallest tail dot.")]
    [Range(0f, 1f)]
    [SerializeField] private float minScale = 0.5f;

    private float _time;

    private void Update()
    {
        int count = dots.Length;
        if (count == 0) return;

        _time += Time.unscaledDeltaTime; // keeps spinning while time is paused
        int lead = Mathf.FloorToInt(_time * stepsPerSecond) % count;

        for (int i = 0; i < count; i++)
        {
            Image dot = dots[i];
            if (dot == null) continue;

            int behind = (lead - i + count) % count;
            float t = count > 1 ? 1f - behind / (float)(count - 1) : 1f;

            Color color = dot.color;
            color.a = Mathf.Lerp(minAlpha, 1f, t);
            dot.color = color;
            dot.rectTransform.localScale = Vector3.one * Mathf.Lerp(minScale, 1f, t);
        }
    }
}
