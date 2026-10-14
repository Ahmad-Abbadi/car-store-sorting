using System;
using UnityEngine;

/// <summary> Picks keyboard or touch prompt icons from the current device. </summary>
public static class InputIcons
{
    /// <summary> TMP sprite asset in Resources/Sprite Assets holding the touch icons. </summary>
    public const string TouchAsset = "InputIcons_Touch";

    /// <summary> Raised when the device switches between touch and keyboard. </summary>
    public static event Action Changed;

    /// <summary> True while the player is on a touch device. </summary>
    public static bool IsTouch => TouchInput.Instance != null && TouchInput.Instance.IsTouchMode;

    /// <summary> TMP sprite tag for a named touch icon. </summary>
    public static string TouchTag(string icon) => $"<sprite=\"{TouchAsset}\" name=\"{icon}\">";

    /// <summary> TMP sprite tag for a desktop sprite name, swapped to its touch icon on touch. </summary>
    public static string SpriteTag(string desktopSprite)
    {
        if (!IsTouch) return $"<sprite name=\"{desktopSprite}\">";

        switch (desktopSprite)
        {
            case "LMB": return TouchTag("Interact");
            case "RMB": return TouchTag("Place");
            default: return $"<sprite name=\"{desktopSprite}\">";
        }
    }

    /// <summary> Tells listeners the device changed. </summary>
    public static void RaiseChanged() => Changed?.Invoke();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Changed = null;
}
