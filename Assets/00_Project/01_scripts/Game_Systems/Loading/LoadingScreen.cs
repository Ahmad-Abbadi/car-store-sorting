using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary> Loads the target scene async, fills a bar, then fades out once the new scene settles. </summary>
public class LoadingScreen : MonoBehaviour
{
    // Async progress stops here until activation is allowed.
    private const float AsyncReadyProgress = 0.9f;

    // Caps a hitch frame so it doesn't eat the minimum time.
    private const float MaxStepSeconds = 0.1f;

    // A frame faster than this counts as settled.
    private const float SettledFrameSeconds = 0.05f;

    [Header("UI")]
    [Tooltip("Slider showing progress. Fill maps to the slider's min..max range.")]
    [SerializeField] private Slider loadingSlider;

    [Header("Loading")]
    [Tooltip("Scene to load when none was requested through SceneLoader.")]
    [SerializeField] private string fallbackScene = "Level";

    [Tooltip("Minimum seconds on this screen, even if the scene loads sooner.")]
    [Min(0f)]
    [SerializeField] private float minimumDuration = 5f;

    [Header("Exit")]
    [Tooltip("Smooth frames in a row the new scene needs before the screen fades out.")]
    [Min(1)]
    [SerializeField] private int settledFrames = 5;

    [Tooltip("Max seconds to keep covering the new scene's first hitchy frames.")]
    [Min(0f)]
    [SerializeField] private float maxSettleTime = 3f;

    [Tooltip("Seconds to fade the screen out over the new scene.")]
    [Min(0f)]
    [SerializeField] private float fadeOutDuration = 0.4f;

    private IEnumerator Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SetFill(0f);
        BringCanvasToFront();

        yield return null; // draw over the old scene before tearing it down
        yield return UnloadOtherScenes();
        yield return Resources.UnloadUnusedAssets();

        string target = string.IsNullOrEmpty(SceneLoader.TargetScene) ? fallbackScene : SceneLoader.TargetScene;
        AsyncOperation load = SceneManager.LoadSceneAsync(target);
        load.allowSceneActivation = false;

        float elapsed = 0f;
        while (load.progress < AsyncReadyProgress || elapsed < minimumDuration)
        {
            elapsed += Mathf.Min(Time.unscaledDeltaTime, MaxStepSeconds);
            float real = Mathf.Clamp01(load.progress / AsyncReadyProgress);
            float time = minimumDuration > 0f ? Mathf.Clamp01(elapsed / minimumDuration) : 1f;
            SetFill(Mathf.Min(real, time));
            yield return null;
        }

        SetFill(1f);
        yield return null; // draw the full bar once before the switch

        // Survive the switch so the new scene's startup hitches stay hidden.
        GameObject root = transform.root.gameObject;
        DontDestroyOnLoad(root);
        load.allowSceneActivation = true;
        yield return load;

        SceneLoader.MarkDone();
        yield return WaitForSettle();
        yield return FadeOut(root);
        Destroy(root);
    }

    /// <summary> Waits for a run of smooth frames, or the max settle time. </summary>
    private IEnumerator WaitForSettle()
    {
        float deadline = Time.realtimeSinceStartup + maxSettleTime;
        int calm = 0;
        while (calm < settledFrames && Time.realtimeSinceStartup < deadline)
        {
            yield return null;
            calm = Time.unscaledDeltaTime <= SettledFrameSeconds ? calm + 1 : 0;
        }
    }

    /// <summary> Fades the whole loading UI to transparent. </summary>
    private IEnumerator FadeOut(GameObject root)
    {
        CanvasGroup group = root.GetComponent<CanvasGroup>();
        if (group == null) group = root.AddComponent<CanvasGroup>();

        for (float t = 0f; t < fadeOutDuration; t += Time.unscaledDeltaTime)
        {
            group.alpha = 1f - t / fadeOutDuration;
            yield return null;
        }
        group.alpha = 0f;
    }

    private void OnDestroy() => SceneLoader.MarkDone();

    /// <summary> Keeps the loading UI above the old scene's canvases while both are loaded. </summary>
    private void BringCanvasToFront()
    {
        Canvas canvas = loadingSlider != null ? loadingSlider.GetComponentInParent<Canvas>() : null;
        if (canvas != null) canvas.rootCanvas.sortingOrder = short.MaxValue;
    }

    /// <summary> Unloads every scene except this one, behind the loading screen. </summary>
    private IEnumerator UnloadOtherScenes()
    {
        Scene own = gameObject.scene;
        for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene == own || !scene.isLoaded) continue;

            AsyncOperation unload = SceneManager.UnloadSceneAsync(scene);
            if (unload != null) yield return unload;
        }
    }

    /// <summary> Sets the bar fill (0..1). </summary>
    private void SetFill(float value01)
    {
        if (loadingSlider != null) loadingSlider.SetValueWithoutNotify(Mathf.Lerp(loadingSlider.minValue, loadingSlider.maxValue, Mathf.Clamp01(value01)));
    }
}
