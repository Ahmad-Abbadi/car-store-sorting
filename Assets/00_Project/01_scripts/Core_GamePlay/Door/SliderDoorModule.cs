using UnityEngine;

/// <summary> Slides one panel along a local direction. </summary>
public class SliderDoorModule : DoorModuleBase
{
    [Header("Slide")]
    [SerializeField] private Transform panel;
    [SerializeField] private Vector3 slideDirection = Vector3.right;
    [SerializeField] private float slideDistance = 2f;

    private Vector3 closedPosition;

    public override DoorType ModuleType => DoorType.SlideDoor;

    protected override void CacheClosedPose()
    {
        if (panel == null) panel = transform;
        closedPosition = panel.localPosition;
    }

    protected override void ApplyProgress(float t)
    {
        Vector3 offset = slideDirection.normalized * (slideDistance * t);
        panel.localPosition = closedPosition + offset;
    }
}
