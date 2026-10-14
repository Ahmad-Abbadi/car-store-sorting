using UnityEngine;

/// <summary> Shared open/close driver, subclasses only apply the pose. </summary>
public abstract class DoorModuleBase : MonoBehaviour, IDoorModule
{
    [Header("Motion")]
    [SerializeField, Min(0.01f)] private float duration = 1f;
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private float progress;
    private float target;

    public abstract DoorType ModuleType { get; }
    public bool IsOpen => target > 0.5f;
    public bool IsMoving => !Mathf.Approximately(progress, target);

    protected virtual void Awake()
    {
        CacheClosedPose();
        ApplyProgress(0f);
    }

    public void SetOpen(bool open) => target = open ? 1f : 0f;

    public void SnapTo(bool open)
    {
        target = progress = open ? 1f : 0f;
        ApplyProgress(ease.Evaluate(progress));
    }

    private void Update()
    {
        if (!IsMoving) return;

        progress = Mathf.MoveTowards(progress, target, Time.deltaTime / duration);
        ApplyProgress(ease.Evaluate(progress));
    }

    /// <summary> Stores the closed pose so motion is relative to the scene setup. </summary>
    protected abstract void CacheClosedPose();

    /// <summary> Places the door at eased progress, 0 closed to 1 open. </summary>
    protected abstract void ApplyProgress(float t);
}
