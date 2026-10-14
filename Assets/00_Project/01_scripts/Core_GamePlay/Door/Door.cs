using UnityEngine;

/// <summary> Owns door state and delegates the motion to its module component. </summary>
[DisallowMultipleComponent]
public class Door : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private DoorType doorType = DoorType.SlideDoor;
    [SerializeField] private MonoBehaviour doorModule;
    [SerializeField] private bool startOpen;

    private IDoorModule module;

    public bool IsOpen => module != null && module.IsOpen;
    public bool IsMoving => module != null && module.IsMoving;

    private void Awake()
    {
        module = doorModule as IDoorModule ?? GetComponent<IDoorModule>();

        if (module == null)
        {
            Debug.LogError($"{name}: no IDoorModule component assigned.", this);
            enabled = false;
            return;
        }

        if (module.ModuleType != doorType)
            Debug.LogWarning($"{name}: module is {module.ModuleType} but door type is {doorType}.", this);

        module.SnapTo(startOpen);
    }

    /// <summary> Opens the door. </summary>
    public void Open() => module?.SetOpen(true);

    /// <summary> Closes the door. </summary>
    public void Close() => module?.SetOpen(false);

    /// <summary> Flips between open and closed. </summary>
    public void Toggle() => module?.SetOpen(!IsOpen);

    private void OnValidate()
    {
        if (doorModule != null && doorModule is not IDoorModule)
        {
            Debug.LogWarning($"{name}: {doorModule.GetType().Name} is not an IDoorModule.", this);
            doorModule = null;
        }
    }
}
