using TMPro;
using UnityEngine;

/// <summary>Factual provider disclosure; no account, consent gate, or invented developer policy.</summary>
public static class AnalyticsPrivacyNotice
{
    public static void Create(Transform owner)
    {
        var settings = Resources.Load<ByteBrewSettings>("ByteBrewSettings");
        if (!ByteBrewInitializer.HasWebCredentials(settings)) return;
        var root = new GameObject("AnalyticsPrivacyNotice", typeof(RectTransform), typeof(Canvas),
            typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, owner.gameObject.scene);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;
        var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var panel = new GameObject("ByteBrewPrivacyDetails", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        panel.transform.SetParent(root.transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0);
        rect.anchoredPosition = new Vector2(0, 8);
        rect.sizeDelta = new Vector2(1160, 76);
        panel.GetComponent<UnityEngine.UI.Image>().color = new Color(0.035f, 0.055f, 0.09f, 0.88f);
        panel.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => Application.OpenURL("https://docs.bytebrew.io/BBSettings/privacypolicy"));
        var label = new GameObject("Disclosure", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(panel.transform, false);
        var labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(14, 4);
        labelRect.offsetMax = new Vector2(-14, -4);
        var text = label.GetComponent<TextMeshProUGUI>();
        var data = Resources.Load<RuntimeLocalizationData>("RuntimeLocalizationData");
        if (data && data.latinFont) text.font = data.latinFont;
        text.fontSize = 24;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.text = "Gameplay analytics by ByteBrew: sessions, device/browser details and game progress events.\nSelect to read ByteBrew's privacy policy.";
    }
}
