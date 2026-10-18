using TMPro;
using UnityEngine;

namespace ToyShop.UI
{
    [CreateAssetMenu(menuName = "Tiny Riders/UI Theme")]
    public sealed class ToyShopTheme : ScriptableObject
    {
        public Color cream = new Color32(255, 248, 236, 255);
        public Color navy = new Color32(34, 50, 74, 255);
        public Color blue = new Color32(52, 120, 246, 255);
        public Color coral = new Color32(255, 116, 104, 255);
        public Color gold = new Color32(255, 200, 87, 255);
        public Color mint = new Color32(72, 189, 153, 255);
        public Color muted = new Color32(220, 227, 235, 255);
        public TMP_FontAsset font;
        public Sprite rounded;
        public Sprite circle;
        [Min(0)] public float panelDuration = .2f;
        [Min(0)] public float panelTravel = 14f;
        [Min(0)] public float buttonDuration = .1f;
        [Range(0, .1f)] public float hoverScale = .025f;
        [Min(0)] public float spacing = 16f;
        public Vector2 shadowOffset = new Vector2(0, -4);

        private static ToyShopTheme cached;
        public static ToyShopTheme Current
        {
            get
            {
                if (!cached) cached = Resources.Load<ToyShopTheme>("ToyShopTheme");
                if (!cached) cached = CreateInstance<ToyShopTheme>();
                return cached;
            }
        }
    }
}
