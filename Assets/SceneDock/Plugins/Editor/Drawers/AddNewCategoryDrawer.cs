using UnityEditor;
using UnityEngine;

namespace SceneDock.Drawers
{
    public class AddNewCategoryDrawer : Drawer
    {
        private string categoryName = "";
        public AddNewCategoryDrawer(float defaultWidth, float defaultHeight, PopupWindow window) : base(defaultWidth, defaultHeight, window) { }

        public override void DrawEntirely()
        {
            GUILayout.BeginVertical(GUILayout.Width(defaultWindowWidth));
            Header();


            GUILayout.Label("Category Name");
            categoryName = GUILayout.TextField(categoryName);
            GUILayout.Space(5);
            if (GUILayout.Button("Save"))
            {
                Data.AddCategory(categoryName);
                window.SetCurrentDrawer(window.MainDrawer);
            }

            GUILayout.EndVertical();
        }

        private void Header()
        {
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.normal.background = Extensions.MakeTex(200, 26, new Color(0.2f, 0.2f, 0.2f));

            GUILayout.BeginArea(new Rect(0, 0, 200, 24), style);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Add Category", EditorStyles.boldLabel);
            ReturnButton();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            GUILayout.Space(30);
        }

        private void ReturnButton()
        {
            GUIStyle style = new GUIStyle(GUI.skin.button);
            style.normal.background = Extensions.MakeTex(1, 1, new Color(0, 0, 0, 0));

            if (GUILayout.Button("⏎ Back", style, new GUILayoutOption[] { GUILayout.Width(55) }))
            {
                // Switch to main window
                window.SetCurrentDrawer(window.MainDrawer);
            }
        }
    }
}