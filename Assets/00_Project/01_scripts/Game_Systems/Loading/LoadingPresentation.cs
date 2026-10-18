using UnityEngine;

/// <summary>Shared, unscaled loading visuals. Scene loading remains owned by LoadingScreen.</summary>
[RequireComponent(typeof(Canvas), typeof(CanvasGroup))]
public sealed class LoadingPresentation : MonoBehaviour
{
    [SerializeField] private RectTransform fill;
    [SerializeField] private CanvasGroup[] activityDots;
    private bool indeterminate;
    private float animationTime;
    public float Progress { get; private set; }
    public bool IsIndeterminate => indeterminate;

    private void OnEnable()
    {
        animationTime = 0;
        SetOpacity(1);
        SetProgress(0);
    }

    private void Update()
    {
        animationTime += Time.unscaledDeltaTime;
        if (activityDots != null)
            for (int i = 0; i < activityDots.Length; i++)
                if (activityDots[i])
                    activityDots[i].alpha = .3f + .7f * (.5f + .5f * Mathf.Sin(animationTime * 5f - i * .9f));
        if (indeterminate)
        {
            float left = (.5f + .5f * Mathf.Sin(animationTime * 2f - Mathf.PI / 2)) * .72f;
            SetFillRange(left, left + .28f);
        }
    }

    public void SetIndeterminate()
    {
        indeterminate = true;
        SetFillRange(0, .28f);
    }

    public void SetProgress(float value)
    {
        indeterminate = false;
        Progress = Mathf.Clamp01(value);
        SetFillRange(0, Progress);
    }

    public void SetOpacity(float value) => GetComponent<CanvasGroup>().alpha = Mathf.Clamp01(value);

    private void SetFillRange(float left, float right)
    {
        if (!fill) return;
        fill.anchorMin = new Vector2(left, 0);
        fill.anchorMax = new Vector2(right, 1);
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        fill.gameObject.SetActive(right > left);
    }
}
