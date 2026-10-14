/// <summary> Movement style a door uses to open. </summary>
public enum DoorType
{
    SlideDoor,
    RotationDoor,
    RotationDoubleDoor
}

/// <summary>
/// A component that knows how to physically move one door.
/// Door only asks for open/closed, the module owns the motion.
/// </summary>
public interface IDoorModule
{
    /// <summary> Style this module implements, checked against the Door setting. </summary>
    DoorType ModuleType { get; }

    /// <summary> True once open has been requested, even while still moving. </summary>
    bool IsOpen { get; }

    /// <summary> True while the door is still travelling to its target. </summary>
    bool IsMoving { get; }

    /// <summary> Starts moving toward open or closed. </summary>
    void SetOpen(bool open);

    /// <summary> Jumps straight to open or closed with no motion. </summary>
    void SnapTo(bool open);
}
