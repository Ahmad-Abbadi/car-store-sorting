using System;
using UnityEngine;

/// <summary> Base for any system that must boot before the game starts. </summary>
public abstract class BaseSystemInitializer : MonoBehaviour
{
    /// <summary> True once this system finished initializing successfully. </summary>
    public bool IsInitialized { get; private set; }

    /// <summary> Runs the system setup, then fires the matching callback. </summary>
    public async Awaitable<bool> Initialize(Action onInitialized = null, Action onFailed = null)
    {
        bool success;
        try
        {
            success = await OnInitialize();
        }
        catch (OperationCanceledException)
        {
            success = false;
        }
        catch (Exception e)
        {
            Debug.LogException(e, this);
            success = false;
        }

        IsInitialized = success;
        if (success) onInitialized?.Invoke();
        else onFailed?.Invoke();
        return success;
    }

    /// <summary> System-specific setup. Return true on success, false on failure. </summary>
    protected abstract Awaitable<bool> OnInitialize();

    /// <summary> Awaits until the condition is true. Returns false if the timeout hits first. </summary>
    protected async Awaitable<bool> WaitUntil(Func<bool> condition, float timeoutSeconds = 0f)
    {
        float startTime = Time.realtimeSinceStartup;
        while (!condition())
        {
            if (timeoutSeconds > 0f && Time.realtimeSinceStartup - startTime >= timeoutSeconds)
                return false;

            await Awaitable.NextFrameAsync(destroyCancellationToken);
        }

        return true;
    }
}
