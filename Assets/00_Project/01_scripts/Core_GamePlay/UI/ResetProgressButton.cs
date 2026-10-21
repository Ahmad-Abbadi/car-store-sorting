using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary> Reset Progress button with a Yes/No confirm; wipes saved progress, then restarts the scene. </summary>
public class ResetProgressButton : MonoBehaviour
{
    // Default PlayerPrefs keys of every progress save; settings and analytics are kept.
    private static readonly string[] ProgressKeys =
    {
        "LevelSave", "Coins", "Coins_RewardWallet", "CollectorQuests", "PlayerPositionSave", TutorialProgress.StatusKey
    };

    [SerializeField] private Button resetButton;

    [Tooltip("Confirm popup shown after pressing Reset; hidden until then.")]
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    [Tooltip("Upgrades to reset; loaded upgrades are included automatically.")]
    [SerializeField] private List<UpgradeSO> upgrades = new List<UpgradeSO>();

    [Tooltip("Restart through GameFlowManager (pause menu) instead of a plain scene reload.")]
    [SerializeField] private bool restartThroughGameFlow;

    private void Awake()
    {
        if (resetButton) resetButton.onClick.AddListener(ShowConfirm);
        if (yesButton) yesButton.onClick.AddListener(ConfirmReset);
        if (noButton) noButton.onClick.AddListener(HideConfirm);
        HideConfirm();
    }

    private void OnDisable() => HideConfirm();

    private void OnDestroy()
    {
        if (resetButton) resetButton.onClick.RemoveListener(ShowConfirm);
        if (yesButton) yesButton.onClick.RemoveListener(ConfirmReset);
        if (noButton) noButton.onClick.RemoveListener(HideConfirm);
    }

    /// <summary> Opens the Yes/No popup. </summary>
    private void ShowConfirm()
    {
        if (confirmPanel) confirmPanel.SetActive(true);
        else ConfirmReset();
    }

    /// <summary> Closes the Yes/No popup. </summary>
    private void HideConfirm()
    {
        if (confirmPanel) confirmPanel.SetActive(false);
    }

    /// <summary> Wipes all progress, then restarts the current scene. </summary>
    private void ConfirmReset()
    {
        HideConfirm();
        WipeProgress();

        if (restartThroughGameFlow && GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.Restart();
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary> Deletes every progress save and resets in-memory progress. </summary>
    internal static void DeleteProgressKeys(System.Action<string> deleteKey)
    {
        foreach (string key in ProgressKeys) deleteKey(key);
    }

    private void WipeProgress()
    {
        TinyRiders.Quests.CollectorQuestManager.Instance?.PrepareFullReset();
        DeleteProgressKeys(PlayerPrefs.DeleteKey);

        HashSet<UpgradeSO> all = new HashSet<UpgradeSO>(upgrades);
        all.UnionWith(Resources.FindObjectsOfTypeAll<UpgradeSO>());
        if (UpgradeManager.Instance != null) all.UnionWith(UpgradeManager.Instance.Upgrades);
        foreach (UpgradeSO upgrade in all)
            if (upgrade != null) upgrade.ResetLevel();

        if (LevelsManager.Instance != null && LevelsManager.Instance.ProgressionData != null)
            LevelsManager.Instance.ProgressionData.ResetProgression();

        PlayerPrefs.Save();
    }
}
