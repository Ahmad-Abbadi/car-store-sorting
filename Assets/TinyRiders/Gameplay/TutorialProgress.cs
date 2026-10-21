using System;
using UnityEngine;

/// <summary>Persists the onboarding decision; both Play and optional replay enter the real store.</summary>
public static class TutorialProgress
{
    public const string StatusKey = "FirstShelfTutorial.v1";
    public const string SceneName = "Tutorial";
    public static string StoreScene { get; private set; } = "Level";
    public static bool ReplayRequested { get; private set; }

    public static bool ShouldShow(Func<string, bool> hasKey) =>
        !hasKey(StatusKey) && !hasKey("LevelSave");

    public static string Destination(string storeScene, bool replay = false)
    {
        StoreScene = string.IsNullOrEmpty(storeScene) ? "Level" : storeScene;
        ReplayRequested = replay;
        return StoreScene;
    }

    public static bool ConsumeReplay()
    {
        bool replay = ReplayRequested;
        ReplayRequested = false;
        return replay;
    }

    public static void Finish(bool skipped)
    {
        // Replaying/skipping must not downgrade an earlier completion.
        if (!skipped || !PlayerPrefs.HasKey(StatusKey))
            PlayerPrefs.SetInt(StatusKey, skipped ? 1 : 2);
        PlayerPrefs.Save();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { StoreScene = "Level"; ReplayRequested = false; }
}
