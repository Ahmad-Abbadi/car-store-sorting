/// <summary> Key for picking a gameplay sound from the inspector. </summary>
public enum GameplaySoundKey
{
    Wrong,
    Collect,
    Throw,
    ShelfPlacement,
    CompleteShelf,
    WrongSection
}

/// <summary> Keyed lookup: resolves a GameplaySoundKey to its sound. </summary>
public partial class GameplaySounds_SO
{
    /// <summary> Sound for the key; random pick for list sections. </summary>
    public GameSound Get(GameplaySoundKey key)
    {
        switch (key)
        {
            case GameplaySoundKey.Wrong: return WrongSFX;
            case GameplaySoundKey.Collect: return GetRandomCollectSFX();
            case GameplaySoundKey.Throw: return GetRandomThrowSFX();
            case GameplaySoundKey.ShelfPlacement: return GetRandomShelfPlacementSFX();
            case GameplaySoundKey.CompleteShelf: return CompleteShelfSFX;
            case GameplaySoundKey.WrongSection: return WrongSectionSFX;
            default: return default;
        }
    }
}
