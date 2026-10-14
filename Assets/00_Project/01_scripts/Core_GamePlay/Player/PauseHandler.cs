using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary> Reads the pause key and raises a toggle event; the pause menu listens and shows itself. </summary>
public class PauseHandler : MonoBehaviour
{
    [Tooltip("Player input authority. Auto-found on a parent Player (or the Player singleton) if empty.")]
    [SerializeField] private PlayerInput input;

    /// <summary> Raised when the player presses pause during play or while paused. </summary>
    public static event Action OnPauseToggled;

    // Newest last; each returns true when it consumed the press.
    private static readonly List<Func<bool>> _interceptors = new List<Func<bool>>();

    /// <summary> Lets an open panel consume the pause key before the pause menu. </summary>
    public static void AddInterceptor(Func<bool> interceptor)
    {
        if (interceptor != null && !_interceptors.Contains(interceptor)) _interceptors.Add(interceptor);
    }

    /// <summary> Removes a pause key interceptor. </summary>
    public static void RemoveInterceptor(Func<bool> interceptor) => _interceptors.Remove(interceptor);

    public void Update()
    {
        if (input == null) input = ResolveInput();

        // PausePressed is ungated, so it fires even while the game is paused.
        if (input == null || !input.PausePressed) return;

        if (TryIntercept()) return;

        GameFlowManager flow = GameFlowManager.Instance;
        if (flow == null) return;

        // Only toggle from the interruptible states; ignore terminal/menu states.
        if (flow.currentState == GameFlowManager.GameState.Playing || flow.IsPaused)
            OnPauseToggled?.Invoke();
    }

    /// <summary> Offers the press to interceptors, newest first. </summary>
    private static bool TryIntercept()
    {
        for (int i = _interceptors.Count - 1; i >= 0; i--)
            if (_interceptors[i].Invoke()) return true;
        return false;
    }

    private PlayerInput ResolveInput()
    {
        Player p = GetComponentInParent<Player>();
        if (p == null) p = Player.Instance;
        return p != null ? p.Input : null;
    }
}
