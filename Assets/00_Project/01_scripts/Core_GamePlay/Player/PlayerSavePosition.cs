using System;
using UnityEngine;

/// <summary> Saves the player's position and look on exit and restores them on load. </summary>
[DefaultExecutionOrder(-50)] // restores after LevelSaveManager, before gameplay Start
public class PlayerSavePosition : MonoBehaviour
{
    [Serializable]
    private class PositionSave
    {
        public string scene;
        public Vector3 position;
        public float yaw;
        public float pitch;
        public int roomLayoutRevision;
    }

    [Tooltip("The player's component hub.")]
    [SerializeField] private Player player;

    [Tooltip("PlayerPrefs key the position is stored under.")]
    [SerializeField] private string saveKey = "PlayerPositionSave";

    private bool _preserveUnreadableSave;
    private bool _ready;         // false until Load ran; blocks early saves
    private bool _levelFinished; // blocks saving once the level is won

    private void OnEnable() => LevelsManager.OnLevelCompleted += HandleLevelCompleted;

    private void OnDisable() => LevelsManager.OnLevelCompleted -= HandleLevelCompleted;

    private void Start()
    {
        Load();
        _ready = true;
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) Save();
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) Save();
    }

    private void OnApplicationQuit() => Save();

    /// <summary> Writes the current position and look to PlayerPrefs. </summary>
    [ContextMenu("Save Now")]
    public void Save()
    {
        if (!TryCapture(out string key, out string json)) return;
        bool hadSave = PlayerPrefs.HasKey(key);
        string previous = PlayerPrefs.GetString(key, string.Empty);
        try
        {
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
        }
        catch (Exception e)
        {
            if (hadSave) PlayerPrefs.SetString(key, previous); else PlayerPrefs.DeleteKey(key);
            LogSaveIssue("Position checkpoint failed; previous data retained. " + e.Message);
        }
    }

    internal bool TryCapture(out string key, out string json)
    {
        key = null; json = null;
        if (!_ready || _levelFinished || _preserveUnreadableSave || player == null || player.Motor == null) return false;
        var data = new PositionSave
        {
            scene = gameObject.scene.name,
            position = player.Motor.TransientPosition,
            yaw = player.Look != null ? player.Look.Yaw : player.Motor.TransientRotation.eulerAngles.y,
            pitch = player.Look != null ? player.Look.Pitch : 0f,
            roomLayoutRevision = TinyRiders.ThemedCollectionSession.Instance?.roomLayout?.revision ?? 0
        };
        json = JsonUtility.ToJson(data);
        key = saveKey;
        return true;
    }

    /// <summary> Deletes the saved position. </summary>
    [ContextMenu("Clear Save")]
    public void Clear()
    {
        PlayerPrefs.DeleteKey(saveKey);
        PlayerPrefs.Save();
        _levelFinished = true;
        _preserveUnreadableSave = false;
    }

    /// <summary> Wipes the save once the level is won; replays start fresh. </summary>
    private void HandleLevelCompleted(int levelNumber)
    {
        if (TinyRiders.ThemedCollectionSession.Instance != null) { Save(); return; }
        _levelFinished = true;
        Clear();
    }

    /// <summary> Moves the player to the saved pose; ignores another scene's save. </summary>
    private void Load()
    {
        if (player == null || player.Motor == null) return;

        string json = PlayerPrefs.GetString(saveKey, string.Empty);
        if (string.IsNullOrEmpty(json))
        {
            var initialLayout = TinyRiders.ThemedCollectionSession.Instance?.roomLayout;
            if (initialLayout != null && player.Look != null) player.Look.SetLook(0, initialLayout.spawnPitch);
            return;
        }

        PositionSave data;
        try
        {
            data = JsonUtility.FromJson<PositionSave>(json);
        }
        catch (ArgumentException e)
        {
            _preserveUnreadableSave = true;
            LogSaveIssue("Unreadable position retained. " + e.Message);
            return;
        }

        if (data == null || data.scene != gameObject.scene.name ||
            !float.IsFinite(data.position.x) || !float.IsFinite(data.position.y) || !float.IsFinite(data.position.z) ||
            !float.IsFinite(data.yaw) || !float.IsFinite(data.pitch))
        {
            _preserveUnreadableSave = true;
            LogSaveIssue("Invalid position retained without replacing the current spawn.");
            return;
        }

        var layout = TinyRiders.ThemedCollectionSession.Instance?.roomLayout;
        if (layout != null && (data.roomLayoutRevision < layout.revision || !layout.Contains(data.position, .4f)))
        {
            data.position = layout.spawn;
            data.yaw = 0;
            data.pitch = layout.spawnPitch;
        }
        player.Motor.SetPositionAndRotation(data.position, Quaternion.Euler(0f, data.yaw, 0f), true);
        player.Motor.BaseVelocity = Vector3.zero;
        if (player.Look != null) player.Look.SetLook(data.yaw, data.pitch);
    }
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private void LogSaveIssue(string message) => Debug.LogWarning("[Save] " + message, this);
}
