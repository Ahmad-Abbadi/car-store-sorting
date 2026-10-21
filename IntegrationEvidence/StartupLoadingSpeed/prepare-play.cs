var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) =>
{
    if (scene.name != "Level") return;
    var save = UnityEngine.Object.FindFirstObjectByType<LevelSaveManager>();
    var key = save.StorageKey;
    UnityEditor.SessionState.SetString("StartupSpeed.UserLevelKey", key);
    UnityEditor.SessionState.SetString("StartupSpeed.UserLevel", UnityEngine.PlayerPrefs.GetString(key, "<absent>"));
    typeof(LevelSaveManager).GetField("saveKey", flags).SetValue(save, "StartupSpeed.QA.20261021");
    var position = UnityEngine.Object.FindFirstObjectByType<PlayerSavePosition>();
    typeof(PlayerSavePosition).GetField("saveKey", flags).SetValue(position, "StartupSpeed.QA.20261021.position");
    var coins = UnityEngine.Object.FindFirstObjectByType<CoinsManager>();
    if (coins) typeof(CoinsManager).GetField("saveKey", flags).SetValue(coins, "StartupSpeed.QA.20261021.coins");
    foreach (var probe in UnityEngine.Object.FindObjectsByType<TinyRiders.ThemedPerformanceProbe>(UnityEngine.FindObjectsSortMode.None)) probe.enabled = false;
};
SceneLoader.LoadWithLoadingScreen("Level");
return "Loading the existing Level through its normal loader, with isolated persistence keys.";
