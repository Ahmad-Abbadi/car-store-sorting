using UnityEngine;

namespace SceneDock
{
    public static class Extensions
    {
        // Helper function to make a solid color texture
        internal static Texture2D MakeTex(int width, int height, Color col)
        {
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);


            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}
