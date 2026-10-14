using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SceneDock
{
    /// <summary>
    /// Scene Dock ships as a SceneView Overlay. The Game view (UnityEditor.GameView) is an
    /// internal, sealed EditorWindow that does not support the Overlay system, so the overlay
    /// cannot simply be re-targeted at it. Instead this injects an equivalent floating
    /// "Select Scene" button into every open Game view's rootVisualElement, opening the exact
    /// same scene-switch popup the Scene view dock uses.
    /// </summary>
    [InitializeOnLoad]
    public static class GameViewDock
    {
        private const string ButtonName = "scene-dock-gameview-button";
        private static readonly Type GameViewType =
            typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");

        private static double _nextCheck;

        static GameViewDock()
        {
            EditorApplication.update += Update;
        }

        private static void Update()
        {
            // Poll roughly once per second so the button survives Game view rebuilds
            // (docking, layout changes, entering/exiting play mode) without per-frame cost.
            if (EditorApplication.timeSinceStartup < _nextCheck) return;
            _nextCheck = EditorApplication.timeSinceStartup + 1.0;

            if (GameViewType == null) return;

            foreach (var obj in Resources.FindObjectsOfTypeAll(GameViewType))
            {
                if (obj is EditorWindow window)
                    InjectButton(window);
            }
        }

        private static void InjectButton(EditorWindow gameView)
        {
            var root = gameView.rootVisualElement;
            if (root == null) return;
            if (root.Q<Button>(ButtonName) != null) return; // already injected

            Button button = null;
            button = new Button(() =>
            {
                // Anchor the popup to the button; `new PopupWindow()` resolves to
                // SceneDock.PopupWindow, the same IMGUI content the Scene view dock shows.
                ToolbarButton.WorldBound = button.worldBound;
                UnityEditor.PopupWindow.Show(button.worldBound, new PopupWindow());
            })
            {
                name = ButtonName,
                text = "Select Scene",
                tooltip = "Changes currently open scene",
            };

            var s = button.style;
            s.position = Position.Absolute;
            s.top = 6;
            s.left = 6;
            s.marginTop = 0;
            s.marginLeft = 0;
            s.marginRight = 0;
            s.marginBottom = 0;
            s.paddingLeft = 8;
            s.paddingRight = 8;
            s.paddingTop = 3;
            s.paddingBottom = 3;
            s.backgroundColor = new Color(0.22f, 0.22f, 0.22f, 0.92f);
            s.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            s.borderTopLeftRadius = 4;
            s.borderTopRightRadius = 4;
            s.borderBottomLeftRadius = 4;
            s.borderBottomRightRadius = 4;

            root.Add(button);
        }
    }
}
