using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class SimpleAudioManager
{
    private readonly HashSet<AudioListener> _suppressedListeners = new HashSet<AudioListener>();
    private AudioListener _sceneListener;

    private void RefreshAfterSceneUnloaded(Scene scene)
    {
        if (instance == this) RefreshSceneListener(SceneManager.GetActiveScene());
    }

    /// <summary>Keeps additive loading cameras from competing with the current scene's listener.</summary>
    private void RefreshSceneListener(Scene preferred)
    {
        _suppressedListeners.RemoveWhere(listener => listener == null);
        var candidates = new List<AudioListener>();
        foreach (var listener in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            if (listener.enabled || _suppressedListeners.Contains(listener)) candidates.Add(listener);

        AudioListener selected = null;
        if (preferred.name != SceneLoader.LoadingSceneName)
            selected = candidates.Find(listener => listener.gameObject.scene == preferred);
        if (selected == null && _sceneListener != null && candidates.Contains(_sceneListener)) selected = _sceneListener;
        if (selected == null && candidates.Count > 0) selected = candidates[0];

        foreach (var listener in candidates)
        {
            if (listener == selected)
            {
                listener.enabled = true;
                _suppressedListeners.Remove(listener);
            }
            else if (listener.enabled)
            {
                listener.enabled = false;
                _suppressedListeners.Add(listener);
            }
        }
        _sceneListener = selected;
    }

    private void RestoreSuppressedListeners()
    {
        foreach (var listener in _suppressedListeners)
            if (listener != null) listener.enabled = true;
        _suppressedListeners.Clear();
        _sceneListener = null;
    }
}
