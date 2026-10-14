using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;


namespace SceneDock
{
    [EditorToolbarElement(id: id, typeof(SceneView))]
    public class ToolbarButton : EditorToolbarButton
    {
        public const string id = "scene-dock-toolbar-button";
        public static Rect WorldBound;

        public ToolbarButton()
        {
            text = "Select Scene";
            tooltip = "Changes currently open scene";
            clicked += CreateCube_onClick;
        }

        private void CreateCube_onClick()
        {
            WorldBound = worldBound;
            UnityEditor.PopupWindow.Show(worldBound, new PopupWindow());
        }
    }
}

