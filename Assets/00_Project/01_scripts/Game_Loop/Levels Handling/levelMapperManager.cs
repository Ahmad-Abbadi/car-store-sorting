using System.Collections;
using UnityEngine;

public class levelMapperManager : MonoBehaviour
{

    [SerializeField] LevelMapperSO data;

    private void Start()
    {
        StartCoroutine(rotin());
    }

    IEnumerator rotin()
    {
        yield return new WaitForSecondsRealtime(0.3f);
        LoadLevel();
    }


    [ContextMenu("Load Level")]
    public void LoadLevel()
    {
        Debug.Log("<color=green> Load level from levemaper has been called </color>");

        // Preferred path: the persistent LevelsManager owns level loading now. At boot it jumps to the
        // furthest level the player has unlocked (the "last open level").
        if (LevelsManager.Instance != null)
        {
            LevelsManager.Instance.LoadLastOpenLevel();
            return;
        }

        // Fallback (no persistent LevelsManager, e.g. opening this scene directly): the original
        // inline resolution, kept so the mapper scene still works standalone.
        var worlds = data.worlds;
        if (worlds == null || worlds.Count == 0)
        {
            Debug.LogError("levelMapperManager: no worlds configured on the LevelMapperSO.", this);
            return;
        }

        // Boot resumes at the FURTHEST level ever reached ("last open level"), NOT the last level
        // opened. Seeding from Current would send a player who replayed an earlier level from the
        // level-select panel back to that replay instead of their frontier. This mirrors
        // LevelsManager.LoadLastOpenLevel — the path used when the persistent manager is present.
        int worldIndex = data.progressionData.HighestWorldIndex;
        int levelIndex = data.progressionData.HighestLevelIndex;

        // Highest is a valid reached coord, but roll any overflow forward into subsequent worlds
        // anyway (empty worlds are stepped over safely) so the resolution stays robust.
        while (worldIndex < worlds.Count - 1 && levelIndex >= WorldLevelCount(worldIndex))
        {
            levelIndex -= WorldLevelCount(worldIndex);
            worldIndex++;
        }

        // Clamp inside the final world — its last level is the progression ceiling.
        worldIndex = Mathf.Clamp(worldIndex, 0, worlds.Count - 1);
        levelIndex = Mathf.Clamp(levelIndex, 0, Mathf.Max(0, WorldLevelCount(worldIndex) - 1));

        if (WorldLevelCount(worldIndex) == 0)
        {
            Debug.LogError($"levelMapperManager: world {worldIndex} " +
                           $"('{worlds[worldIndex]?.WorldName}') has no scenes.", this);
            return;
        }

        // Re-sync Current onto the resolved frontier coord (matches LoadCoord, which sets Current to
        // the level it loads). This both makes a world roll-over stick and snaps a leftover replay
        // Current back up to the level we're actually booting into.
        if (worldIndex != data.progressionData.CurrentWorldIndex ||
            levelIndex != data.progressionData.CurrentLevelIndex)
        {
            data.progressionData.CurrentWorldIndex = worldIndex;
            data.progressionData.CurrentLevelIndex = levelIndex;
        }

        // Keep the furthest-reached ("last open") record in step with where we actually landed.
        data.progressionData.MarkHighest(worldIndex, levelIndex);

        string sceneName = worlds[worldIndex].SceneNames[levelIndex];
        Debug.Log($"<color=green>LevelMapper → world {worldIndex} " +
                  $"('{worlds[worldIndex].WorldName}'), level {levelIndex}: {sceneName}</color>");

        // This fallback bypasses LevelsManager.LoadCoord, which is what normally fires OnLevelEntered. Fire
        // it here too so the boot level (level 1 for a new player) still emits a "started" event and gets
        // tracked — otherwise analytics never sees the first level of a session. Mirrors LoadCoord: fire
        // BEFORE the scene load.
        LevelsManager.RaiseLevelEntered(FlatLevelNumber(worldIndex, levelIndex), sceneName);

        GameFlowManager.Instance.LoadScene(sceneName);
    }

    // Flat, 1-based level number for a (world, level) coordinate — matches LevelsManager's numbering so the
    // boot level reports the same number the rest of the flow uses.
    int FlatLevelNumber(int worldIndex, int levelIndex)
    {
        int number = 0;
        for (int w = 0; w < worldIndex; w++) number += WorldLevelCount(w);
        return number + levelIndex + 1;
    }

    int WorldLevelCount(int worldIndex)
    {
        WorldData world = data.worlds[worldIndex];
        return (world != null && world.SceneNames != null) ? world.SceneNames.Count : 0;
    }

}
