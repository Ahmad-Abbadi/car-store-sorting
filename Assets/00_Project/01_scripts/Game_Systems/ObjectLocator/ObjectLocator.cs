using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary> Screen pointers toward world objects or UI; world ones clamp to the screen edge. </summary>
[RequireComponent(typeof(RectTransform))]
public class ObjectLocator : MonoBehaviour
{
    /// <summary> One pooled pointer and the object it tracks. </summary>
    private sealed class Pointer
    {
        public RectTransform Rect;
        public RectTransform Arrow;
        public CanvasGroup Group;
        public Transform Target;
        public RectTransform UiTarget; // set when pointing at UI instead of the world
        public Renderer[] Renderers;
        public Vector3 TopOffset; // target pivot to bounds top
        public float NextBoundsRefresh;
        public Tween Tween;
    }

    private static ObjectLocator _instance;

    [Header("Look")]
    [Tooltip("Fill color baked into the generated arrow.")]
    [SerializeField] private Color color = new Color(0.608f, 0.361f, 0.965f);
    [Tooltip("Rim and outline color of the generated arrow.")]
    [SerializeField] private Color outlineColor = new Color(0.165f, 0.075f, 0.322f);
    [SerializeField] private Vector2 size = new Vector2(220f, 220f);
    [Tooltip("Optional arrow sprite pointing up; generated when empty.")]
    [SerializeField] private Sprite arrowSprite;

    [Header("Placement")]
    [Tooltip("Screen padding for edge pointers, in canvas units.")]
    [SerializeField] private float edgePadding = 60f;
    [Tooltip("Gap above the object's top, in canvas units.")]
    [SerializeField] private float onScreenGap = 30f;
    [Tooltip("Height used when the object has no renderers.")]
    [SerializeField] private float fallbackHeight = 0.5f;
    [SerializeField] private float boundsRefreshInterval = 0.25f;

    [Header("Bob")]
    [SerializeField] private float bobAmplitude = 14f;
    [SerializeField] private float bobPeriod = 0.8f;
    [Tooltip("Squash and stretch strength while bobbing.")]
    [SerializeField, Range(0f, 0.3f)] private float squash = 0.1f;
    [Tooltip("Scale pulse strength while on the screen edge.")]
    [SerializeField, Range(0f, 0.3f)] private float edgePulse = 0.1f;

    [Header("Show / Hide")]
    [SerializeField] private float popDuration = 0.3f;
    [SerializeField] private float fadeDuration = 0.25f;

    private readonly List<Pointer> _active = new List<Pointer>();
    private readonly Stack<Pointer> _pool = new Stack<Pointer>();
    private RectTransform _rect;
    private Camera _camera;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _instance = null;

    private void Awake()
    {
        _instance = this;
        _rect = (RectTransform)transform;
        if (arrowSprite == null) arrowSprite = CreateArrowSprite(color, outlineColor);
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    /// <summary> Starts pointing at the target; no-op if already tracked. </summary>
    public static void Show(Transform target)
    {
        if (target == null) return;
        if (_instance == null)
        {
            Debug.LogWarning("ObjectLocator: no locator in the scene.");
            return;
        }
        _instance.ShowPointer(target);
    }

    /// <summary> Stops pointing at the target, fading its pointer out. </summary>
    public static void Hide(Transform target)
    {
        if (_instance != null) _instance.HidePointer(target);
    }

    private void ShowPointer(Transform target)
    {
        if (Find(target) != null) return;

        Pointer p = _pool.Count > 0 ? _pool.Pop() : CreatePointer();
        p.Target = target;
        p.UiTarget = IsScreenUi(target) ? (RectTransform)target : null;
        p.Renderers = p.UiTarget != null ? null : target.GetComponentsInChildren<Renderer>();
        p.NextBoundsRefresh = 0f;
        p.Rect.gameObject.SetActive(true);
        _active.Add(p);

        p.Tween?.Kill();
        p.Group.alpha = 1f;
        p.Rect.localScale = Vector3.zero;
        p.Tween = p.Rect.DOScale(1f, popDuration).SetEase(Ease.OutBack).SetUpdate(true).SetLink(p.Rect.gameObject);
        Place(p);
    }

    private void HidePointer(Transform target)
    {
        Pointer p = Find(target);
        if (p != null) Release(p, fade: true);
    }

    private Pointer Find(Transform target)
    {
        foreach (Pointer p in _active)
            if (p.Target == target) return p;
        return null;
    }

    /// <summary> Removes from tracking; fades out first when asked. </summary>
    private void Release(Pointer p, bool fade)
    {
        _active.Remove(p);
        p.Target = null;
        p.UiTarget = null;
        p.Renderers = null;
        p.Tween?.Kill();

        if (!fade)
        {
            Pool(p);
            return;
        }
        p.Tween = p.Group.DOFade(0f, fadeDuration).SetUpdate(true).SetLink(p.Rect.gameObject).OnComplete(() => Pool(p));
    }

    private void Pool(Pointer p)
    {
        p.Rect.gameObject.SetActive(false);
        _pool.Push(p);
    }

    private void LateUpdate()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            Pointer p = _active[i];
            if (p.Target == null) Release(p, fade: false); // destroyed or picked up
            else Place(p);
        }
    }

    /// <summary> Over the object when visible, else on the screen edge toward it. </summary>
    private void Place(Pointer p)
    {
        // Hidden while the target is inactive, and world pointers while a panel pauses the game.
        bool visible = p.Target.gameObject.activeInHierarchy && (p.UiTarget != null || Time.timeScale > 0f);
        if (p.Arrow.gameObject.activeSelf != visible) p.Arrow.gameObject.SetActive(visible);
        if (!visible) return;

        float wave = Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / bobPeriod);
        if (p.UiTarget != null)
        {
            PlaceAbove(p, UiTopScreenPoint(p.UiTarget), wave);
            return;
        }

        if (_camera == null) _camera = Camera.main;
        if (_camera == null) return;

        if (Time.unscaledTime >= p.NextBoundsRefresh)
        {
            p.NextBoundsRefresh = Time.unscaledTime + boundsRefreshInterval;
            p.TopOffset = TopOf(p) - p.Target.position;
        }

        Vector3 screen = _camera.WorldToScreenPoint(p.Target.position + p.TopOffset);
        bool behind = screen.z < 0f;
        if (behind) screen = new Vector3(Screen.width - screen.x, Screen.height - screen.y, 0f); // mirror to the correct side

        RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, screen, null, out Vector2 local);
        Vector2 half = _rect.rect.size * 0.5f;
        Vector2 inner = half - Vector2.one * edgePadding;
        bool onScreen = !behind && Mathf.Abs(local.x) <= inner.x && Mathf.Abs(local.y) <= inner.y;

        if (onScreen)
        {
            PlaceAbove(p, screen, wave);
            return;
        }

        p.Arrow.localScale = Vector3.one * (1f + Mathf.Abs(wave) * edgePulse);

        // Clamp along the center-to-target ray onto the padded edge.
        Vector2 dir = local.sqrMagnitude > 0.0001f ? local : Vector2.down;
        float scale = Mathf.Min(
            Mathf.Abs(dir.x) > 0.0001f ? inner.x / Mathf.Abs(dir.x) : float.MaxValue,
            Mathf.Abs(dir.y) > 0.0001f ? inner.y / Mathf.Abs(dir.y) : float.MaxValue);
        p.Rect.anchoredPosition = dir * scale;
        p.Arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
    }

    /// <summary> Bobs above a screen point, pointing down, with squash and stretch. </summary>
    private void PlaceAbove(Pointer p, Vector3 screen, float wave)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, screen, null, out Vector2 local);
        p.Rect.anchoredPosition = local + Vector2.up * (onScreenGap + size.y * 0.5f + wave * bobAmplitude);
        p.Arrow.localRotation = Quaternion.Euler(0f, 0f, 180f); // point down at it

        // Stretch while rising, squash at the bottom of the bob.
        float s = wave * squash;
        p.Arrow.localScale = new Vector3(1f - s, 1f + s, 1f);
    }

    /// <summary> True for rects on a screen-space canvas; world-space UI counts as world. </summary>
    private static bool IsScreenUi(Transform target)
    {
        if (!(target is RectTransform)) return false;
        Canvas canvas = target.GetComponentInParent<Canvas>();
        return canvas != null && canvas.rootCanvas.renderMode != RenderMode.WorldSpace;
    }

    /// <summary> Screen point at the top-center of a UI rect. </summary>
    private static Vector3 UiTopScreenPoint(RectTransform target)
    {
        Canvas canvas = target.GetComponentInParent<Canvas>().rootCanvas;
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners); // 1 = top-left, 2 = top-right
        return RectTransformUtility.WorldToScreenPoint(cam, (corners[1] + corners[2]) * 0.5f);
    }

    /// <summary> Top-center of the target's renderers, or its pivot plus a fallback height. </summary>
    private Vector3 TopOf(Pointer p)
    {
        bool any = false;
        Bounds bounds = default;
        foreach (Renderer r in p.Renderers)
        {
            if (r == null || r.bounds.size == Vector3.zero) continue;
            if (!any) bounds = r.bounds;
            else bounds.Encapsulate(r.bounds);
            any = true;
        }

        if (!any) return p.Target.position + Vector3.up * fallbackHeight;
        return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
    }

    private Pointer CreatePointer()
    {
        GameObject go = new GameObject("Pointer", typeof(RectTransform), typeof(CanvasGroup));
        go.layer = gameObject.layer;
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(_rect, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        CanvasGroup group = go.GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        GameObject arrowGo = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
        arrowGo.layer = gameObject.layer;
        RectTransform arrow = (RectTransform)arrowGo.transform;
        arrow.SetParent(rect, false);
        arrow.anchorMin = Vector2.zero;
        arrow.anchorMax = Vector2.one;
        arrow.offsetMin = arrow.offsetMax = Vector2.zero;

        Image image = arrowGo.GetComponent<Image>();
        image.sprite = arrowSprite;
        image.color = Color.white; // colors are baked into the sprite
        image.raycastTarget = false;
        image.preserveAspect = true;

        return new Pointer { Rect = rect, Arrow = arrow, Group = group };
    }

    /// <summary> Bakes a chunky cartoon arrow: shadow, thick rim, gradient fill, shine. </summary>
    private static Sprite CreateArrowSprite(Color fill, Color rim)
    {
        const int n = 128;
        const float rimWidth = 9f;
        const float shadowSpread = 5f; // even halo, so it reads the same at any rotation
        Color shadow = new Color(0f, 0f, 0f, 0.35f);
        Color dark = Color.Lerp(fill, rim, 0.35f);
        Color light = Color.Lerp(fill, Color.white, 0.3f);

        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "LocatorArrow", wrapMode = TextureWrapMode.Clamp };
        Color[] px = new Color[n * n];

        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
            float d = ArrowDistance(p);
            Color c = Color.clear;

            c = Over(c, shadow, Coverage(d - shadowSpread));
            c = Over(c, rim, Coverage(d));

            Color body = Color.Lerp(dark, light, p.y / n); // lighter toward the tip
            c = Over(c, body, Coverage(d + rimWidth));

            // Shine band just inside the top-left edges.
            float shine = Coverage(d + rimWidth) * (1f - Coverage(ArrowDistance(p + new Vector2(-6f, 6f)) + rimWidth));
            c = Over(c, new Color(1f, 1f, 1f, 0.55f), shine);

            // Glossy dot on the head.
            float dot = Coverage(Vector2.Distance(p, new Vector2(n * 0.4f, n * 0.66f)) - 6f);
            c = Over(c, new Color(1f, 1f, 1f, 0.85f), dot);

            px[y * n + x] = c;
        }

        tex.SetPixels(px);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }

    /// <summary> Antialiased coverage from a signed distance (negative inside). </summary>
    private static float Coverage(float d) => Mathf.Clamp01(0.5f - d);

    /// <summary> Alpha-composites top over bottom with extra coverage. </summary>
    private static Color Over(Color bottom, Color top, float coverage)
    {
        float a = top.a * coverage;
        float outA = a + bottom.a * (1f - a);
        if (outA <= 0f) return Color.clear;

        Color rgb = (top * a + bottom * bottom.a * (1f - a)) / outA;
        rgb.a = outA;
        return rgb;
    }

    // Up-pointing arrow on a 128 grid: wide head over a stubby stem.
    private static readonly Vector2[] ArrowShape =
    {
        new Vector2(64f, 112f), new Vector2(110f, 64f), new Vector2(84f, 64f), new Vector2(84f, 22f),
        new Vector2(44f, 22f), new Vector2(44f, 64f), new Vector2(18f, 64f),
    };

    private const float ArrowRounding = 7f;

    /// <summary> Rounded signed distance to the arrow; negative inside. </summary>
    private static float ArrowDistance(Vector2 p) => PolygonDistance(p, ArrowShape) - ArrowRounding;

    /// <summary> Signed distance to a closed polygon; negative inside. </summary>
    private static float PolygonDistance(Vector2 p, Vector2[] v)
    {
        float d = float.MaxValue;
        bool inside = false;
        for (int i = 0, j = v.Length - 1; i < v.Length; j = i++)
        {
            Vector2 e = v[i] - v[j];
            Vector2 w = p - v[j];
            Vector2 b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
            d = Mathf.Min(d, b.sqrMagnitude);

            if ((v[j].y > p.y) != (v[i].y > p.y) && p.x < (v[i].x - v[j].x) * (p.y - v[j].y) / (v[i].y - v[j].y) + v[j].x)
                inside = !inside;
        }
        return inside ? -Mathf.Sqrt(d) : Mathf.Sqrt(d);
    }
}
