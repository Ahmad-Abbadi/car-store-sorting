/// <summary> A UI panel that declares the world conditions it needs while shown. </summary>
public interface IUIPanel
{
    /// <summary> Stop world time (timeScale 0) while shown. </summary>
    bool RequirePause { get; }

    /// <summary> Free the cursor for clicking while shown. </summary>
    bool RequireMouse { get; }

    /// <summary> Stop player input and animation while shown. </summary>
    bool PausePlayer { get; }

    /// <summary> Root the player while world time keeps running. </summary>
    bool FreezePlayer { get; }

    /// <summary> Mute music while shown. </summary>
    bool MuteMusic { get; }
}
