using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ByteBrewSDK;
using TinyRiders.Quests;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Maps gameplay notifications to ByteBrew. No saved-progress replay or offline queue.</summary>
[DisallowMultipleComponent]
public sealed class GameAnalytics : MonoBehaviour
{
    [Tooltip("Print analytics events in the Editor without initializing or contacting ByteBrew.")]
    public bool logEventsInEditor;
    private static GameAnalytics instance;
    private CollectorQuestManager quests;
    private bool subscribed;
    private string pendingLevelScene;
    private int activeLevel = 1;
    private bool levelCompletionReported;

    private void OnEnable()
    {
        if (instance && instance != this) return;
        if (subscribed) return;
        instance = this;
        subscribed = true;
        LevelsManager.OnLevelEntered += LevelStarted;
        LevelsManager.OnLevelCompleted += LevelCompleted;
        LevelSaveManager.OnStoreCompleted += StoreCompleted;
        TutorialController.Started += TutorialStarted;
        TutorialController.StepReached += TutorialStep;
        TutorialController.Finished += TutorialFinished;
        SceneManager.sceneLoaded += SceneLoaded;
        BindQuests();
    }

    private void OnDisable()
    {
        if (!subscribed) return;
        subscribed = false;
        LevelsManager.OnLevelEntered -= LevelStarted;
        LevelsManager.OnLevelCompleted -= LevelCompleted;
        LevelSaveManager.OnStoreCompleted -= StoreCompleted;
        TutorialController.Started -= TutorialStarted;
        TutorialController.StepReached -= TutorialStep;
        TutorialController.Finished -= TutorialFinished;
        SceneManager.sceneLoaded -= SceneLoaded;
        UnbindQuests();
        if (instance == this) instance = null;
    }

    private void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindQuests();
        if (scene.name != "Level" && scene.name != "ThemedSorting") return;
        // The store is also entered directly from the menu/tutorial/replay, without LevelsManager.
        if (pendingLevelScene != scene.name)
            BeginLevel(LevelsManager.Instance ? LevelsManager.Instance.CurrentLevelNumber : 1, scene.name);
        pendingLevelScene = null;
    }
    // Also handles managers created after sceneLoaded and managers destroyed during a transition.
    private void LateUpdate() { if (subscribed && quests != CollectorQuestManager.Instance) BindQuests(); }

    private void BindQuests()
    {
        if (quests == CollectorQuestManager.Instance) return;
        UnbindQuests();
        quests = CollectorQuestManager.Instance;
        if (!quests) return;
        quests.Completed += QuestCompleted;
        quests.RankedUp += RankUp;
    }

    private void UnbindQuests()
    {
        if (!ReferenceEquals(quests, null))
        {
            quests.Completed -= QuestCompleted;
            quests.RankedUp -= RankUp;
        }
        quests = null;
    }

    private void LevelStarted(int level)
    {
        pendingLevelScene = LevelsManager.EnteredSceneName;
        BeginLevel(level, pendingLevelScene);
    }
    private void BeginLevel(int level, string scene)
    {
        activeLevel = level;
        levelCompletionReported = false;
        Track("level_started", "level", Number(level), scene);
    }
    private void LevelCompleted(int level) => CompleteLevel(level, SceneManager.GetActiveScene().name);
    private void StoreCompleted(string scene) => CompleteLevel(activeLevel, scene);
    private void CompleteLevel(int level, string scene)
    {
        if (levelCompletionReported && activeLevel == level) return;
        activeLevel = level;
        levelCompletionReported = true;
        Track("level_completed", "level", Number(level), scene);
    }
    private void TutorialStarted(TutorialController tutorial) => Track("tutorial_started", null, null, tutorial.gameObject.scene.name);
    private void TutorialStep(TutorialController tutorial, TutorialController.Lesson lesson) =>
        Track("tutorial_step", "step", lesson.ToString().ToLowerInvariant(), tutorial.gameObject.scene.name);
    private void TutorialFinished(TutorialController tutorial, bool skipped) =>
        Track(skipped ? "tutorial_skipped" : "tutorial_completed", null, null, tutorial.gameObject.scene.name);
    private void QuestCompleted(CollectorQuestView quest) => Track("quest_completed", "quest_id", quest.Id, quests.gameObject.scene.name);
    private void RankUp(int rank) => Track("collector_rank_up", "rank", Number(rank), quests.gameObject.scene.name);
    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    // The SDK serializes dictionary entries as key=value; so delimiters and punctuation must be removed.
    public static string ParameterToken(string value)
    {
        var token = new StringBuilder();
        foreach (char c in value ?? string.Empty)
            token.Append(c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '_' ? c : '_');
        return token.ToString();
    }

    private void Track(string eventName, string key, string value, string scene)
    {
        var parameters = new Dictionary<string, string> { { "scene", ParameterToken(scene) } };
        if (key != null) parameters.Add(key, ParameterToken(value));
#if UNITY_EDITOR
        if (logEventsInEditor)
        {
            var text = new StringBuilder("[GameAnalytics] " + eventName + " ");
            foreach (var pair in parameters) text.Append(pair.Key).Append('=').Append(pair.Value).Append(';');
            Debug.Log(text.ToString());
        }
#elif UNITY_WEBGL
        if (!ByteBrewInitializer.ReportingEnabled) return;
        try
        {
            if (ByteBrew.IsByteBrewInitialized()) ByteBrew.NewCustomEvent(eventName, parameters);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[ByteBrew] Event unavailable (" + e.GetType().Name + ").");
        }
#endif
    }
}
