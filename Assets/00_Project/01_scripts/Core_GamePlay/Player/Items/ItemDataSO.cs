using UnityEngine;

// Pairs an item's world form with its hand form.
[CreateAssetMenu(fileName = "ItemData", menuName = "ScriptableObject/Item Data")]
public class ItemDataSO : ScriptableObject
{

    public Sprite ItemImage ;

    [Tooltip("Name shown in UI. Falls back to asset name when empty.")]
    [SerializeField] private string displayName;

    /// <summary> Localization key of the name: item.SaveId.name. </summary>
    public string NameKey => "item." + SaveId + ".name";

    /// <summary> Localized name; falls back to the typed name, then the asset name. </summary>
    public string DisplayName => Localizer.GetOr(NameKey, string.IsNullOrWhiteSpace(displayName) ? name : displayName);

    /// <summary> Name typed in the Inspector, used as the English source text. </summary>
    public string SourceName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;

    [Tooltip("Id used in save files. Falls back to asset name. Keep stable once shipped.")]
    [SerializeField] private string saveId;

    /// <summary> Save id; asset name when empty. </summary>
    public string SaveId => string.IsNullOrWhiteSpace(saveId) ? name : saveId;


    [Tooltip("World prefab placed in the level.")]
    [SerializeField] private WorldItemBase worldItem;

    [Tooltip("Hand prefab spawned when equipped.")]
    [SerializeField] private HandItemBase handItem;

    // World form of this item.
    public WorldItemBase WorldItem => worldItem;

    // Hand form spawned into the player's hand.
    public HandItemBase HandItem => handItem;

    [Header("Held postions")]
    [Tooltip("Spawn on the handler's camera-fixed socket instead of the hold socket.")]
    public bool CameraFixed;
    public Vector3 HeldPosition;
    public Vector3 HeldRotation;

    [Header("Icon Placement")]
    [Tooltip("Override the slot icon's default position and rotation.")]
    public bool OverrideIconPlacement = false;
    public Vector2 IconPosition;
    public Vector2 IconRotation;

}
