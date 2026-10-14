using UnityEditor;
using UnityEngine;

namespace SceneDock.Drawers
{
    public class SettingsDrawer : Drawer
    {
        private float cachedSliderValue;

        public SettingsDrawer(float defaultWidth, float defaultHeight, PopupWindow window) : base(defaultWidth, defaultHeight, window) { }


        public override void DrawEntirely()
        {
            Header();

            GUILayout.Space(22);


            GUILayout.Space(3);
            Data.ShowSearchBar = GUILayout.Toggle(Data.ShowSearchBar, "Show Search Field");
            GUILayout.Space(5);
            Data.EnableFavorites = GUILayout.Toggle(Data.EnableFavorites, "Enable Favorites");
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Window Size:");
            float rawSliderValue = GUILayout.HorizontalSlider(Data.WindowSizeMultiplier, 1, 3, GUILayout.Width(80));
            //cachedSliderValue = rawSliderValue;
            cachedSliderValue = Mathf.Round(rawSliderValue / 0.25f) * 0.25f; // Snap to 0.5 increments

            if (Data.WindowSizeMultiplier != cachedSliderValue)
            {
                Data.WindowSizeMultiplier = cachedSliderValue;
            }

            GUILayout.Space(10);
            GUILayout.EndHorizontal();
        }

        private void Header()
        {
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.normal.background = Extensions.MakeTex(200, 26, new Color(0.2f, 0.2f, 0.2f));

            GUILayout.BeginArea(new Rect(0, 0, 200, 24), style);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Options", EditorStyles.boldLabel);
            ReturnButton();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void ReturnButton()
        {
            GUIStyle style = new GUIStyle(GUI.skin.button);
            style.normal.background = Extensions.MakeTex(1, 1, new Color(0, 0, 0, 0));

            Texture icon = EditorGUIUtility.IconContent("SettingsIcon").image;


            if (GUILayout.Button("⏎ Back", style, new GUILayoutOption[] { GUILayout.Width(55) }))
            {
                // Switch to main window
                window.SetCurrentDrawer(window.MainDrawer);
            }
        }
    }
}