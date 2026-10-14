using UnityEngine;

/// <summary> Swings one leaf around a local rotation direction. </summary>
public class RotationDoorModule : DoorModuleBase
{
    [Header("Rotation")]
    [SerializeField] private Transform leaf;
    [SerializeField] private Vector3 rotationDirection = Vector3.up;
    [SerializeField] private float openAngle = 90f;

    private Quaternion closedRotation;

    public override DoorType ModuleType => DoorType.RotationDoor;

    protected override void CacheClosedPose()
    {
        if (leaf == null) leaf = transform;
        closedRotation = leaf.localRotation;
    }

    protected override void ApplyProgress(float t)
    {
        Vector3 axis = rotationDirection.sqrMagnitude > 0f ? rotationDirection.normalized : Vector3.up;
        leaf.localRotation = closedRotation * Quaternion.AngleAxis(openAngle * t, axis);
    }
}
