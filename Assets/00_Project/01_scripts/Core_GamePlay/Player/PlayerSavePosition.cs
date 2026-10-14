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
    }

    [Tooltip("The player's component hub.")]
    [SerializeField] private Player player;

    [Tooltip("PlayerPrefs key the position is stored under.")]
    [SerializeField] private string saveKey = "PlayerPositionSave";

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
        if (!_ready || _levelFinished || player == null || player.Motor == null) return;

        PositionSave data = new PositionSave
        {
            scene = gameObject.scene.name,
            position = player.Motor.TransientPosition,
            yaw = player.Look != null ? player.Look.Yaw : player.Motor.TransientRotation.eulerAngles.y,
            pitch = player.Look != null ? player.Look.Pitch : 0f
        };

        PlayerPrefs.SetString(saveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    /// <summary> Deletes the saved position. </summary>
    [ContextMenu("Clear Save")]
    public void Clear()
    {
        PlayerPrefs.DeleteKey(saveKey);
        PlayerPrefs.Save();
    }

    /// <summary> Wipes the save once the level is won; replays start fresh. </summary>
    private void HandleLevelCompleted(int levelNumber)
    {
        _levelFinished = true;
        Clear();
    }

    /// <summary> Moves the player to the saved pose; ignores another scene's save. </summary>
    private void Load()
    {
        if (player == null || player.Motor == null) return;

        string json = PlayerPrefs.GetString(saveKey, string.Empty);
        if (string.IsNullOrEmpty(json)) return;

        PositionSave data;
        try
        {
            data = JsonUtility.FromJson<PositionSave>(json);
        }
        catch (ArgumentException e)
        {
            Debug.LogWarning($"{name}: corrupt position save discarded. {e.Message}", this);
            Clear();
            return;
        }

        if (data == null || data.scene != gameObject.scene.name)
        {
            Clear();
            return;
        }

        player.Motor.SetPositionAndRotation(data.position, Quaternion.Euler(0f, data.yaw, 0f), true);
        player.Motor.BaseVelocity = Vector3.zero;
        if (player.Look != null) player.Look.SetLook(data.yaw, data.pitch);
    }
}
