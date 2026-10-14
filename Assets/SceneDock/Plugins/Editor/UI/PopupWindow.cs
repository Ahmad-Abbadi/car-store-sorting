using SceneDock.Drawers;
using UnityEditor;
using UnityEngine;

namespace SceneDock
{
    public class PopupWindow : PopupWindowContent
    {
        private float defaultWindowWidth = 200f;
        private float defaultWindowHeight = 200f;
        private MainDrawer mainDrawer;
        private SettingsDrawer settingsDrawer;
        private AddNewCategoryDrawer addNewCategoryDrawer;
        private Drawer currentDrawer;

        public MainDrawer MainDrawer => mainDrawer;
        public SettingsDrawer SettingsDrawer => settingsDrawer;
        public AddNewCategoryDrawer AddNewCategoryDrawer => addNewCategoryDrawer;

        public PopupWindow()
        {
            mainDrawer = new MainDrawer(defaultWindowWidth, defaultWindowHeight, this);
            settingsDrawer = new SettingsDrawer(defaultWindowWidth, defaultWindowHeight, this);
            addNewCategoryDrawer = new AddNewCategoryDrawer(defaultWindowWidth, defaultWindowHeight, this);
            currentDrawer = mainDrawer;
        }


        public override Vector2 GetWindowSize()
        {
            return new Vector2(defaultWindowWidth, defaultWindowHeight * Data.WindowSizeMultiplier);
        }

        public override void OnGUI(Rect rect) => currentDrawer.DrawEntirely();

        public void SetCurrentDrawer(Drawer newDrawer) => currentDrawer = newDrawer;
    }
}
