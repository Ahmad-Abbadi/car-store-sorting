using UnityEditor.Overlays;
using UnityEditor;

namespace SceneDock
{
    [Overlay(typeof(SceneView), "Scene Dock", defaultDisplay: true, defaultDockPosition = DockPosition.Top, defaultDockZone = DockZone.TopToolbar, defaultLayout = Layout.HorizontalToolbar)]
    public class ToolbarOverlay : UnityEditor.Overlays.ToolbarOverlay
    {
        public ToolbarOverlay() : base(ToolbarButton.id) { }
    }
}