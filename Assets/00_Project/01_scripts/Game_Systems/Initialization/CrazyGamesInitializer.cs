using CrazyGames;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Initializes CrazyGames before the menu and reports actual playable sessions.</summary>
public sealed class CrazyGamesInitializer : BaseSystemInitializer
{
    private static CrazyGamesInitializer instance;
    private bool sdkReady;
    private bool reportingGameplay;
    private bool transitioning;
    private int sceneLoadedFrame;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        GameFlowManager.OnStateChanged += HandleStateChanged;
        GameFlowManager.OnSceneTransition += HandleTransition;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        SceneManager.sceneUnloaded += HandleSceneUnloaded;
    }

    protected override async Awaitable<bool> OnInitialize()
    {
        // Unsupported platforms and whitelisted hosts continue without SDK calls.
        if (!CrazySDK.IsAvailable) return true;

        bool completed = false;
        CrazySDK.Init(() => completed = true);
        if (!await WaitUntil(() => completed, 15f))
        {
            Debug.LogWarning("[CrazyGames] SDK initialization timed out; continuing without platform events.", this);
            return false;
        }

        sdkReady = CrazySDK.IsInitialized;
        Debug.Log($"[CrazyGames] SDK {CrazySDK.Version} initialized (Basic Launch, no ads).", this);
        return sdkReady;
    }

    private void LateUpdate()
    {
        Scene scene = SceneManager.GetActiveScene();
        var flow = GameFlowManager.Instance;
        bool playable = scene.isLoaded && (scene.name == "Level" || scene.name == "ThemedSorting" || scene.name == TutorialProgress.SceneName);
        bool ready = playable && Time.frameCount > sceneLoadedFrame && !transitioning && !SceneLoader.IsLoading
            && flow != null && flow.currentState == GameFlowManager.GameState.Playing
            && Player.Instance != null && Player.Instance.isActiveAndEnabled;
        SetReporting(ready);
    }

    private void HandleStateChanged(GameFlowManager.GameState state)
    {
        // Starts wait until LateUpdate, avoiding transient Playing states during restart.
        if (state != GameFlowManager.GameState.Playing) SetReporting(false);
    }

    private void HandleTransition(string target)
    {
        transitioning = true;
        SetReporting(false);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        sceneLoadedFrame = Time.frameCount;
        if (mode == LoadSceneMode.Single) transitioning = false;
        SetReporting(false);
    }

    private void HandleSceneUnloaded(Scene scene) => SetReporting(false);

    private void SetReporting(bool playing)
    {
        if (!sdkReady || !CrazySDK.IsAvailable || !CrazySDK.IsInitialized || CrazySDK.IsShutDown) return;
        if (reportingGameplay == playing) return;

        if (playing) CrazySDK.Game.GameplayStart();
        else CrazySDK.Game.GameplayStop();
        reportingGameplay = playing;
    }

    private void OnDestroy()
    {
        if (instance != this) return;
        SetReporting(false);
        GameFlowManager.OnStateChanged -= HandleStateChanged;
        GameFlowManager.OnSceneTransition -= HandleTransition;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneUnloaded -= HandleSceneUnloaded;
        instance = null;
    }
}
