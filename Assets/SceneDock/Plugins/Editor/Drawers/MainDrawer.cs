using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SceneDock.Drawers
{
    public class MainDrawer : Drawer
    {
        private Vector2 scrollPos;
        private string searchString = "";
        private bool isDeleteActive;

        public MainDrawer(float defaultWidth, float defaultHeight, PopupWindow window) : base(defaultWidth, defaultHeight, window) { }


        public override void DrawEntirely()
        {
            GUILayout.BeginVertical(GUILayout.Width(defaultWindowWidth));
            Header();

            GUILayout.Space(26);

            SearchField();

            scrollPos = GUILayout.BeginScrollView(scrollPos);
            ScenesButtonsList(Data.SelectedCategoryIndex);
            GUILayout.EndScrollView();


            Footer();
            GUILayout.EndVertical();
        }

        private void Header()
        {
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.normal.background = Extensions.MakeTex(200, 26, new Color(0.2f, 0.2f, 0.2f));

            GUILayout.BeginArea(new Rect(0, 0, 200, 26), style);
            GUILayout.BeginHorizontal();
            ProfileSelector();
            GUILayout.FlexibleSpace();
            DeleteButton();
            ConfigButton();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void ProfileSelector()
        {
            List<string> options = new List<string>() { "Build Settings Scenes", "Add a new category" };

            IReadOnlyList<Category> savedCategories = Data.GetAllCategories();
            if (Data.SelectedCategoryIndex != 0)
            {
                options.Add("Delete selected category");
            }
            for (int i = 0; i < savedCategories.Count; i++)
            {
                options.Insert(options.Count - ((Data.SelectedCategoryIndex == 0) ? 1 : 2), savedCategories[i].Name);
            }

            if (GUILayout.Button(options[Data.SelectedCategoryIndex], "toolbarDropDown", GUILayout.MaxWidth(140)))
            {
                GenericMenu menu = new GenericMenu();

                for (int i = 0; i < options.Count; i++)
                {
                    // Add a separator above last item
                    if (i == options.Count - ((Data.SelectedCategoryIndex == 0) ? 1 : 2))
                        menu.AddSeparator("");

                    int index = i;
                    menu.AddItem(new GUIContent(options[i]), (index == Data.SelectedCategoryIndex), () =>
                    {

                        if (index < options.Count - ((Data.SelectedCategoryIndex == 0) ? 1 : 2))
                            Data.SelectedCategoryIndex = index;

                        // Add new category
                        else if (index == options.Count - ((Data.SelectedCategoryIndex == 0) ? 1 : 2))
                            window.SetCurrentDrawer(window.AddNewCategoryDrawer);

                        // Delete category
                        else if (index == options.Count - 1)
                        {
                            Data.SelectedCategoryIndex = 0;
                            Data.RemoveSelectedCategory();
                        }
                    });
                }

                menu.ShowAsContext();
            }
        }

        private void Divider(int y)
        {
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.normal.background = Extensions.MakeTex(200, 2, new Color(0.3f, 0.3f, 0.3f));

            GUILayout.BeginArea(new Rect(0, y, 200, 2), style);
            GUILayout.EndArea();

            GUILayout.Space(5);
        }

        private void ConfigButton()
        {
            GUIStyle style = new GUIStyle(GUI.skin.button);
            style.normal.background = Extensions.MakeTex(1, 1, new Color(0, 0, 0, 0));
            style.padding = new RectOffset(0, 0, 0, 0);
            style.margin = new RectOffset(0, 0, 1, 0);
            style.alignment = TextAnchor.MiddleCenter;
            style.imagePosition = ImagePosition.ImageOnly;

            GUIContent icon = EditorGUIUtility.IconContent("SettingsIcon");
            icon.tooltip = "Open Scene Selector settings.";


            if (GUILayout.Button(icon, style, new GUILayoutOption[] { GUILayout.Width(18), GUILayout.Height(18) }))
            {
                // Switch to settings window
                window.SetCurrentDrawer(window.SettingsDrawer);
            }
        }

        private void DeleteButton()
        {
            if (Data.SelectedCategoryIndex <= 0) return;

            GUIStyle style = new GUIStyle(GUI.skin.button);
            Color activeColor = isDeleteActive ? new Color(0.4f, 0.4f, 0.4f, 1f) : new Color(0,0,0,0);
            style.normal.background = Extensions.MakeTex(1, 1, activeColor);
            style.padding = new RectOffset(0, 0, 2, 0);
            style.margin = new RectOffset(0, 5, 0, 0);
            style.alignment = TextAnchor.MiddleCenter;
            style.imagePosition = ImagePosition.ImageOnly;

            GUIContent icon = EditorGUIUtility.IconContent("d_TreeEditor.Trash");
            icon.tooltip = "Toggle delete button on scenes list.";


            if (GUILayout.Button(icon, style, new GUILayoutOption[] { GUILayout.Width(20), GUILayout.Height(20) }))
            {
                isDeleteActive = !isDeleteActive;
            }
        }

        private void SearchField()
        {
            if (Data.ShowSearchBar == false) return;

            string newSearch = GUILayout.TextField(searchString);
            if (newSearch != searchString)
            {
                searchString = newSearch;
            }

            // Divider
            Divider(45);
        }

        private void ScenesButtonsList(int index = 0)
        {
            GUIStyle style = new GUIStyle(GUI.skin.button);
            style.alignment = TextAnchor.UpperLeft;

            Dictionary<string, string> sceneNameGuidDictionary = new();
            if (index == 0)
            {
                foreach (var scene in EditorBuildSettings.scenes)
                {
                    sceneNameGuidDictionary.Add(Path.GetFileNameWithoutExtension(scene.path), scene.guid.ToString());
                }
            }
            else
            {
                if (index - 1 < Data.GetAllCategories().Count)
                {
                    foreach (var scene in Data.GetAllCategories()[index - 1].ScenesGUIDs)
                    {
                        string scenePath = AssetDatabase.GUIDToAssetPath(scene);
                        sceneNameGuidDictionary.Add(Path.GetFileNameWithoutExtension(scenePath), scene);
                    }
                }
            }

            var favoritesSet = Data.GetFavoriteScenesGUIDs();
            var sortedScenesByFavorite = sceneNameGuidDictionary
                .OrderByDescending(kpv => favoritesSet.Contains(kpv.Value))
                .ToDictionary(kpv => kpv.Key, kpv => kpv.Value);

            int scenesCount = 0;

            foreach (var scene in sortedScenesByFavorite)
            {
                string sceneName = Path.GetFileNameWithoutExtension(scene.Key);
                if (!sceneName.ToLower().Contains(searchString.ToLower()))
                    continue;
                scenesCount++;

                string scenePath = AssetDatabase.GUIDToAssetPath(scene.Value);

                GUILayout.BeginHorizontal();

                // Open the scene
                if (GUILayout.Button(sceneName, style, GUILayout.ExpandWidth(true), GUILayout.Height(19)))
                {
                    EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                    EditorSceneManager.OpenScene(scenePath);
                }

                // Favorite toggle
                if (Data.EnableFavorites)
                {
                    Color originalColor = GUI.color;
                    if (Data.GetFavoriteScenesGUIDs().Contains(scene.Value)) GUI.color = Color.yellow;
                    if (GUILayout.Button(EditorGUIUtility.IconContent("Favorite"), style, GUILayout.Width(25), GUILayout.Height(19)))
                    {
                        var sceneObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);

                        if (sceneObject != null)
                        {
                            Data.ToggleFavoriteScene(scene.Value);
                        }
                    }
                    GUI.color = originalColor;
                }

                // Select scene in project window
                if (GUILayout.Button(EditorGUIUtility.IconContent("d_scenepicking_pickable_hover"), style, GUILayout.Width(25), GUILayout.Height(19)))
                {
                    var sceneObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
                    if (sceneObject != null)
                    {
                        EditorGUIUtility.PingObject(sceneObject);
                        Selection.activeObject = sceneObject;
                    }
                }

                // Delete scene from category

                GUIContent icon = EditorGUIUtility.IconContent("d_TreeEditor.Trash");
                if (isDeleteActive && Data.SelectedCategoryIndex > 0)
                {
                    if (GUILayout.Button(icon, style, GUILayout.Width(25), GUILayout.Height(19)))
                    {
                        var sceneObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
                        if (sceneObject != null)
                        {
                            EditorGUIUtility.PingObject(sceneObject);
                            Selection.activeObject = sceneObject;
                            Data.RemoveSceneFromActiveCategory(scene.Value);
                        }
                    }
                }

                GUILayout.EndHorizontal();
                
            }

            if (scenesCount == 0)
            {
                GUILayout.Label("No scenes found.");
            }
        }

        private void Footer()
        {
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.normal.background = Extensions.MakeTex(200, 26, new Color(0.2f, 0.2f, 0.2f));

            GUILayout.FlexibleSpace();
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            GUILayout.BeginHorizontal(style, GUILayout.ExpandWidth(true));
            if (Data.SelectedCategoryIndex > 0 && GUILayout.Button("Add current scene", buttonStyle))
            {
                string activeScenePath = EditorSceneManager.GetActiveScene().path;
                string activeSceneGUID = AssetDatabase.AssetPathToGUID(activeScenePath);
                Data.AddSceneToActiveCategory(activeSceneGUID);
            }
            GUILayout.EndHorizontal();
        }
    }

}