using System;
using UnityEngine;

/// <summary> Swings two leaves, each with its own rotation direction. </summary>
public class RotationDoubleDoorModule : DoorModuleBase
{
    /// <summary> One swinging half of the double door. </summary>
    [Serializable]
    private class DoorLeaf
    {
        public Transform leaf;
        public Vector3 rotationDirection = Vector3.up;
        public float openAngle = 90f;

        [NonSerialized] public Quaternion ClosedRotation;

        /// <summary> Remembers the authored closed rotation. </summary>
        public void Cache()
        {
            if (leaf != null) ClosedRotation = leaf.localRotation;
        }

        /// <summary> Rotates this leaf to eased progress. </summary>
        public void Apply(float t)
        {
            if (leaf == null) return;

            Vector3 axis = rotationDirection.sqrMagnitude > 0f ? rotationDirection.normalized : Vector3.up;
            leaf.localRotation = ClosedRotation * Quaternion.AngleAxis(openAngle * t, axis);
        }
    }

    [Header("Leaves")]
    [SerializeField] private DoorLeaf doorOne = new DoorLeaf();
    [SerializeField] private DoorLeaf doorTwo = new DoorLeaf();

    public override DoorType ModuleType => DoorType.RotationDoubleDoor;

    protected override void CacheClosedPose()
    {
        doorOne.Cache();
        doorTwo.Cache();
    }

    protected override void ApplyProgress(float t)
    {
        doorOne.Apply(t);
        doorTwo.Apply(t);
    }
}
