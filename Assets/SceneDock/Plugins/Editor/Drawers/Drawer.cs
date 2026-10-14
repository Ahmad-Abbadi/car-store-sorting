
namespace SceneDock.Drawers
{
    public abstract class Drawer
    {
        protected float defaultWindowWidth = 200f;
        protected float defaultWindowHeight = 200f;
        protected PopupWindow window;

        public Drawer(float defaultWidth, float defaultHeight, PopupWindow window) 
        {
            defaultWindowWidth = defaultWidth;
            defaultWindowHeight = defaultHeight;
            this.window = window;
        }

        public abstract void DrawEntirely();
    }
}