using System;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>Shared branding authoring used by the menu and loading-screen builders.</summary>
public static class ToyCarSortBranding
{
    public const string LogoPath = "Assets/00_Project/04_ِArt/UI/Branding/ToyCarSortLogo.png";

    public static void ConfigureLogo()
    {
        var importer = AssetImporter.GetAtPath(LogoPath) as TextureImporter;
        if (!importer) throw new InvalidOperationException("Import the Toy Car Sort logo first.");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.isReadable = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    public static void ApplyMenu(GameObject canvas)
    {
        var card = canvas.transform.Find("Menu Card") as RectTransform;
        if (!card) throw new InvalidOperationException("Menu Card is missing.");
        // Keep every control and its dimensions; reserve the space above the card for the logo.
        card.anchorMin = card.anchorMax = new Vector2(0, .4f);
        var title = canvas.transform.Find("Title");
        if (title)
        {
            var text = title.GetComponent<TMP_Text>();
            if (text) text.text = "Toy Car Sort";
            title.gameObject.SetActive(false);
        }
        var logo = Logo(canvas.transform);
        logo.anchorMin = new Vector2(0, .4f);
        logo.anchorMax = new Vector2(0, 1);
        logo.pivot = new Vector2(.5f, .5f);
        logo.offsetMin = new Vector2(card.anchoredPosition.x, card.anchoredPosition.y + card.rect.height * (1 - card.pivot.y) + 12);
        logo.offsetMax = new Vector2(card.anchoredPosition.x + card.rect.width, -24);
        var reset = canvas.transform.Find("Reset_Group");
        if (reset) reset.SetAsLastSibling();
    }

    public static void ApplyLoading(GameObject root)
    {
        var logo = Logo(root.transform);
        logo.anchorMin = logo.anchorMax = new Vector2(.5f, 1);
        logo.pivot = new Vector2(.5f, 1);
        logo.anchoredPosition = new Vector2(0, -24);
        logo.sizeDelta = new Vector2(450, 300);
    }

    private static RectTransform Logo(Transform parent)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(LogoPath);
        if (!sprite) throw new InvalidOperationException("Toy Car Sort logo must be imported as a Sprite.");
        var existing = parent.Find("Toy Car Sort Logo");
        var rect = existing as RectTransform;
        if (!rect)
        {
            rect = new GameObject("Toy Car Sort Logo", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
        }
        var image = rect.GetComponent<UnityEngine.UI.Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return rect;
    }
}
