using System;
using ByteBrewSDK;
using UnityEngine;

/// <summary>Optional WebGL analytics. An unavailable service must never prevent startup.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(ByteBrew), typeof(GameAnalytics))]
public sealed class ByteBrewInitializer : BaseSystemInitializer
{
    private static ByteBrewInitializer instance;
    private bool attempted;

    public static bool ReportingEnabled { get; private set; }

    private void Awake()
    {
        if (instance && instance != this) { Destroy(gameObject); return; }
        instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    public static bool HasWebCredentials(ByteBrewSettings settings) => settings && settings.webEnabled
        && !string.IsNullOrWhiteSpace(settings.webGameID) && !string.IsNullOrWhiteSpace(settings.webSDKKey);

    protected override Awaitable<bool> OnInitialize()
    {
        if (!attempted)
        {
            attempted = true;
#if UNITY_WEBGL && !UNITY_EDITOR
            if (HasWebCredentials(Resources.Load<ByteBrewSettings>("ByteBrewSettings")))
            {
                try
                {
                    ByteBrew.InitializeByteBrew();
                    ReportingEnabled = ByteBrew.IsInitilized;
                }
                catch (Exception e)
                {
                    ReportingEnabled = false;
                    Debug.LogWarning("[ByteBrew] Analytics unavailable; continuing startup (" + e.GetType().Name + ").");
                }
            }
            else Debug.LogWarning("[ByteBrew] Web credentials missing or Web disabled; analytics skipped.");
#endif
        }
        // SDK readiness is checked when sending events. Startup never waits for the network.
        var completion = new AwaitableCompletionSource<bool>();
        completion.SetResult(true);
        return completion.Awaitable;
    }

    private void OnDestroy()
    {
        if (instance != this) return;
        ReportingEnabled = false;
        instance = null;
    }
}
