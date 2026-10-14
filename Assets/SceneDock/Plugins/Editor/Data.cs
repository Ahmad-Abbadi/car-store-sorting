using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace SceneDock
{
    internal static class Data
    {
        public static bool ShowSearchBar
        {
            get { return EditorPrefs.GetBool("SceneSelector_ShowSearchBar", true); }
            set { EditorPrefs.SetBool("SceneSelector_ShowSearchBar", value); }
        }

        public static bool EnableFavorites
        {
            get { return EditorPrefs.GetBool("SceneSelector_EnableFavorites", true); }
            set { EditorPrefs.SetBool("SceneSelector_EnableFavorites", value); }
        }

        public static float WindowSizeMultiplier
        {
            get { return EditorPrefs.GetFloat("SceneSelector_WindowSizeMultiplier", 1f); }
            set { EditorPrefs.SetFloat("SceneSelector_WindowSizeMultiplier", value); }
        }

        private static Categories SavedCategories
        {
            get
            {
                string json = EditorPrefs.GetString("SceneSelector_SavedCategories", "");
                if (string.IsNullOrEmpty(json))
                    return new Categories();

                Categories result = Categories.FromJson(json);
                return result ?? new Categories();
            }
            set { EditorPrefs.SetString("SceneSelector_SavedCategories", value.ToString()); }
        }

        public static int SelectedCategoryIndex
        {
            get { return EditorPrefs.GetInt("SceneSelector_SelectedCategoryIndex", 0); }
            set { EditorPrefs.SetInt("SceneSelector_SelectedCategoryIndex", value); }
        }


        internal static IReadOnlyList<Category> GetAllCategories() => SavedCategories.GetAll();

        internal static Category GetCategoryByIndex(int index) => SavedCategories.GetCategoryByIndex(index);

        public static bool AddCategory(string categoryName)
        {
            Categories categories = SavedCategories;
            bool result = categories.AddCategory(categoryName);

            if (result) SavedCategories = categories;

            return result;
        }

        public static bool RemoveSelectedCategory()
        {
            Categories categories = SavedCategories;
            bool result = categories.RemoveCategoryByIndex(SelectedCategoryIndex);

            if (result) SavedCategories = categories;

            return result;
        }

        public static bool AddSceneToActiveCategory(string sceneGUID)
        {
            string activeCategoryName = SavedCategories.GetCategoryByIndex(SelectedCategoryIndex)?.Name;
            Categories categories = SavedCategories;
            bool result = categories.AddSceneToCategory(activeCategoryName, sceneGUID);

            if(result) SavedCategories = categories;

            return result;
        }

        public static bool RemoveSceneFromActiveCategory(string sceneGUID)
        {
            string activeCategoryName = SavedCategories.GetCategoryByIndex(SelectedCategoryIndex)?.Name;
            Categories categories = SavedCategories;
            bool result = categories.RemoveSceneFromCategory(activeCategoryName, sceneGUID);

            if (result) SavedCategories = categories;

            return result;
        }

        public static List<string> GetFavoriteScenesGUIDs()
        {
            if(SelectedCategoryIndex == 0)
            {
                return EditorBuildSettings.scenes.Select(scene => scene.guid.ToString()).Where(guid => SavedCategories.FavoriteScenesGUIDs.Contains(guid)).ToList();
            }
            else
            {
                return GetCategoryByIndex(SelectedCategoryIndex)?.ScenesGUIDs.Where(guid => SavedCategories.FavoriteScenesGUIDs.Contains(guid)).ToList();
            }
        }

        public static void ToggleFavoriteScene(string sceneGUID)
        {
            Categories categories = SavedCategories;

            if (categories.FavoriteScenesGUIDs.Contains(sceneGUID))
                categories.FavoriteScenesGUIDs.Remove(sceneGUID);
            else
                categories.FavoriteScenesGUIDs.Add(sceneGUID);

            SavedCategories = categories;
        }
    }
}