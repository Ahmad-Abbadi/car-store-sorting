using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary> Routes scene loads through the Loading scene, which loads the target async. </summary>
public static class SceneLoader
{
    /// <summary> Name of the loading scene; must be in the build list. </summary>
    public const string LoadingSceneName = "Loading Scene";

    /// <summary> Scene the loading screen is currently loading. </summary>
    public static string TargetScene { get; private set; }

    /// <summary> True from the request until the target scene activates. </summary>
    public static bool IsLoading { get; private set; }

    /// <summary>Negative means use the loading scene's authored minimum.</summary>
    public static float MinimumDurationOverride { get; private set; } = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        TargetScene = null;
        IsLoading = false;
        MinimumDurationOverride = -1f;
    }

    /// <summary> Adds the loading scene async on top, so the current scene never freezes on screen. </summary>
    public static void LoadWithLoadingScreen(string targetScene, float minimumDuration = -1f)
    {
        if (IsLoading) return;

        IsLoading = true;
        TargetScene = targetScene;
        MinimumDurationOverride = minimumDuration;
        Time.timeScale = 1f;

        AsyncOperation op = SceneManager.LoadSceneAsync(LoadingSceneName, LoadSceneMode.Additive);
        op.completed += _ => SceneManager.SetActiveScene(SceneManager.GetSceneByName(LoadingSceneName));
    }

    /// <summary> Called by the loading screen once the target scene activates. </summary>
    public static void MarkDone() => IsLoading = false;
}
