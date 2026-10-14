using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Irahimz.EditorTools
{
    /// <summary>
    /// Drop-in editor cleanup tool: strips the "missing script" component slots that get left on
    /// prefabs and scene objects after their MonoBehaviour scripts are deleted. Uses Unity's own safe
    /// API (<see cref="GameObjectUtility.RemoveMonoBehavioursWithMissingScript"/>) so prefab-instance
    /// and stripped-component structures are handled correctly (no hand-editing of YAML).
    ///
    /// Self-contained — copy the whole "RemoveMissingScripts" folder into any project's Assets/.
    /// The Editor-only asmdef keeps it out of your runtime build.
    ///
    /// Menu:
    ///   IrahimzTools > Cleanup > Remove Missing Scripts In All Prefabs
    ///   IrahimzTools > Cleanup > Remove Missing Scripts In Open Scene(s)
    ///
    /// Note: this removes the empty component slots only. It does NOT delete the now-purposeless
    /// GameObjects that held them, nor tidy leftover null entries in serialized lists that referenced
    /// the removed components — do that by hand afterwards if you want a spotless hierarchy.
    /// </summary>
    public static class RemoveMissingScriptsUtility
    {
        [MenuItem("IrahimzTools/Cleanup/Remove Missing Scripts In All Prefabs")]
        public static void CleanAllPrefabs()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            int prefabsChanged = 0;
            int componentsRemoved = 0;

            try
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    EditorUtility.DisplayProgressBar("Removing missing scripts", path, (float)i / guids.Length);

                    GameObject root = PrefabUtility.LoadPrefabContents(path);
                    int removedHere = RemoveInHierarchy(root);
                    if (removedHere > 0)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        prefabsChanged++;
                        componentsRemoved += removedHere;
                        Debug.Log($"[Cleanup] {removedHere} missing script(s) removed from {path}");
                    }
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
            }

            Debug.Log($"[Cleanup] Done. {componentsRemoved} missing script(s) removed across {prefabsChanged} prefab(s).");
        }

        [MenuItem("IrahimzTools/Cleanup/Remove Missing Scripts In Open Scene(s)")]
        public static void CleanOpenScenes()
        {
            int componentsRemoved = 0;

            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                Scene scene = SceneManager.GetSceneAt(s);
                if (!scene.isLoaded) continue;

                foreach (GameObject root in scene.GetRootGameObjects())
                    componentsRemoved += RemoveInHierarchy(root);

                EditorSceneManager.MarkSceneDirty(scene);
            }

            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[Cleanup] Done. {componentsRemoved} missing script(s) removed from open scene(s).");
        }

        // Walk every transform (self + children, inactive included) and strip missing MonoBehaviours.
        private static int RemoveInHierarchy(GameObject root)
        {
            int removed = 0;
            List<Transform> all = new List<Transform>(root.GetComponentsInChildren<Transform>(true));
            foreach (Transform t in all)
            {
                int before = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                if (before > 0)
                    removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            }
            return removed;
        }
    }
}
