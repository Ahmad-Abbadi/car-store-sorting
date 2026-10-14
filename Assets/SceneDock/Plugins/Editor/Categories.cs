using System.Collections.Generic;
using UnityEngine;
using System;

namespace SceneDock
{
    [Serializable]
    internal class Categories
    {
        [SerializeField] private List<Category> categories = new List<Category>();
        public List<string> FavoriteScenesGUIDs = new List<string>();


        public IReadOnlyList<Category> GetAll() => categories.AsReadOnly();

        public Category GetCategoryByIndex(int index) => (index > 0 && index <= categories.Count) ? categories[index - 1] : null;

        public bool AddCategory(string categoryName)
        {
            // Handle invalid names
            if (string.IsNullOrEmpty(categoryName.Trim()))
            {
                return false;
            }

            // Handle duplicate names
            if (categories.Exists(c => c.Name == categoryName))
            {
                return false;
            }

            categories.Add(new Category(categoryName));
            return true;
        }

        public bool RemoveCategoryByIndex(int index)
        {
            if (index < 0 || index > categories.Count)
            {
                return false;
            }

            categories.RemoveAt(index);
            return true;
        }

        public bool AddSceneToCategory(string categoryName, string sceneGUID)
        {
            Category category = categories.Find(c => c.Name == categoryName);
            if (category == null)
            {
                return false;
            }

            if (category.ScenesGUIDs.Contains(sceneGUID))
            {
                return false;
            }

            category.ScenesGUIDs.Add(sceneGUID);
            return true;
        }

        public bool RemoveSceneFromCategory(string categoryName, string sceneGUID)
        {
            Category category = categories.Find(c => c.Name == categoryName);
            if (category == null)
            {
                return false;
            }

            if (!category.ScenesGUIDs.Contains(sceneGUID))
            {
                return false;
            }

            category.ScenesGUIDs.Remove(sceneGUID);
            return true;
        }

        #region JSON Serialization Methods
        public static Categories FromJson(string json) => JsonUtility.FromJson<Categories>(json);

        public string ToJson() => JsonUtility.ToJson(this);

        public override string ToString() => ToJson();
        #endregion
    }
}

