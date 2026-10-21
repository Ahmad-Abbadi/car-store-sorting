using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary> Boots every registered system in order, then raises the complete event. </summary>
public class GameInitializer : MonoBehaviour
{
    [Tooltip("Systems to initialize, in order.")]
    [SerializeField] private List<BaseSystemInitializer> systems = new();

    [Tooltip("Keep booting the remaining systems when one fails.")]
    [SerializeField] private bool continueOnFailure = true;

    /// <summary> Raised after all systems have been processed. </summary>
    public UnityEvent OnInitializeComplete;

    /// <summary> Raised if a system fails and Continue On Failure is off. </summary>
    public UnityEvent OnInitializeFailed;

    private async void Start()
    {
        try
        {
            await InitializeAll();
        }
        catch (System.Exception e)
        {
            Debug.LogException(e, this);
        }
    }

    /// <summary> Initializes each system one after another. </summary>
    private async Awaitable InitializeAll()
    {
        LogStage("Beginning system initialization.");
        foreach (var system in systems)
        {
            if (system == null)
            {
                LogStage("Missing system reference; skipped by existing startup policy.");
                continue;
            }

            LogStage($"Initializing {system.GetType().Name}.");
            bool success = await system.Initialize();
            LogStage($"{system.GetType().Name}: {(success ? "ready" : "failed")}.");
            if (success) continue;

            Debug.LogWarning($"[GameInitializer] '{system.name}' failed to initialize.", system);
            if (continueOnFailure) continue;

            OnInitializeFailed?.Invoke();
            return;
        }

        LogStage("Systems processed; requesting initial scene.");
        OnInitializeComplete?.Invoke();
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private void LogStage(string message) => Debug.Log("[Startup] " + message, this);
}
