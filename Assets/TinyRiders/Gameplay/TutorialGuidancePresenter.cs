using TMPro;
using UnityEngine;

public enum TutorialGuidanceAction { None, Approach, Pickup, Place, TakeBack }

/// <summary>Projects a non-blocking arrow onto the target, or toward it at the screen edge.</summary>
public sealed class TutorialGuidancePresenter : MonoBehaviour
{
    public TutorialController tutorial;
    public Canvas canvas;
    public RectTransform instructionCard;
    public UnityEngine.UI.Image targetArrow, edgeArrow;
    public TMP_SpriteAsset mouseIcons, touchIcons;
    private Camera view;
    private Transform lastTarget;
    private TutorialGuidanceAction lastAction;
    private float changedAt;
    private readonly Vector3[] corners = new Vector3[4];

    public struct ArrowLayout
    {
        public Vector2 position;
        public float rotation;
        public bool atEdge;
    }

    public void Hide()
    {
        if (targetArrow) targetArrow.gameObject.SetActive(false);
        if (edgeArrow) edgeArrow.gameObject.SetActive(false);
    }

    private void OnDisable() => Hide();

    public string ControlHint(TutorialGuidanceAction action)
    {
        if (action == TutorialGuidanceAction.None) return "";
        bool touch = InputIcons.IsTouch;
        if (action == TutorialGuidanceAction.Approach)
            return Localizer.Get(touch ? "tutorial.touch_move" : "tutorial.desktop_move");
        string verb = action == TutorialGuidanceAction.Place ? "place" : action == TutorialGuidanceAction.TakeBack ? "take_back" : "pickup";
        string text = Localizer.Get("tutorial.hint." + (touch ? "touch." : "mouse.") + verb);
        var asset = touch ? touchIcons : mouseIcons;
        tutorial.controlHint.spriteAsset = asset;
        tutorial.controlHint.richText = true;
        string icon = touch ? (action == TutorialGuidanceAction.Place ? "Place" : "Interact")
            : action == TutorialGuidanceAction.Place ? "RMB" : "LMB";
        return ComposeHint(text, asset, icon);
    }

    public static string ComposeHint(string text, TMP_SpriteAsset asset, string icon)
    {
        if (asset != null)
            foreach (var character in asset.spriteCharacterTable)
                if (character.name == icon) return "<sprite name=\"" + icon + "\"> " + text;
        return text;
    }

    private void LateUpdate()
    {
        if (!tutorial || !tutorial.player || !canvas || !tutorial.GuidanceTarget ||
            tutorial.Step == TutorialController.Lesson.Complete || SceneLoader.IsLoading ||
            Time.timeScale <= 0 || !tutorial.player.Input.IsEnabled ||
            (tutorial.pauseMenu && tutorial.pauseMenu.Panel && tutorial.pauseMenu.Panel.activeSelf))
        { Hide(); return; }
        if (!view) view = tutorial.player.GetComponent<InteractionHandler>()?.SourceCamera ?? Camera.main;
        if (!view) { Hide(); return; }

        var target = tutorial.GuidanceTarget;
        var action = tutorial.GuidanceAction;
        if (lastTarget != target || lastAction != action)
        { lastTarget = target; lastAction = action; changedAt = Time.unscaledTime; }

        float scale = canvas.scaleFactor;
        Rect safe = Screen.safeArea;
        safe.xMin += 64 * scale; safe.xMax -= 64 * scale;
        safe.yMin += (InputIcons.IsTouch ? 240 : 80) * scale;
        safe.yMax -= 64 * scale;
        if (instructionCard)
        {
            instructionCard.GetWorldCorners(corners);
            float bottom = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[0]).y;
            safe.yMax = Mathf.Min(safe.yMax, bottom - 45 * scale);
        }
        if (safe.width < 80 * scale || safe.height < 80 * scale) { Hide(); return; }

        float offset = action == TutorialGuidanceAction.Place ? .08f : .25f;
        var projected = view.WorldToScreenPoint(target.position + Vector3.up * offset);
        var layout = CalculateLayout(projected, safe);
        var arrow = layout.atEdge ? edgeArrow : targetArrow;
        targetArrow.gameObject.SetActive(!layout.atEdge);
        edgeArrow.gameObject.SetActive(layout.atEdge);
        float time = Time.unscaledTime;
        if (!layout.atEdge)
        {
            layout.position.y += (tutorial.InStore ? 28 : 48 + Mathf.Sin(time * 3.5f) * 8) * scale;
            layout.position.y = Mathf.Min(layout.position.y, safe.yMax);
        }
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform,
            layout.position, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out var local);
        arrow.rectTransform.anchoredPosition = local;
        arrow.rectTransform.localRotation = Quaternion.Euler(0, 0, layout.rotation);
        float pulse = tutorial.InStore ? 1 : tutorial.IdleSeconds >= 8 ? 1 + .12f * Mathf.Sin(time * 6) : 1 + .04f * Mathf.Sin(time * 3.5f);
        if (!tutorial.InStore) pulse += .15f * Mathf.Clamp01(1 - (time - changedAt) / .35f);
        arrow.rectTransform.localScale = Vector3.one * pulse;
    }

    /// <summary>All positions are screen pixels. Inverting a behind-camera projection avoids misleading arrows.</summary>
    public static ArrowLayout CalculateLayout(Vector3 projected, Rect safe)
    {
        Vector2 point = projected;
        if (projected.z > 0 && safe.Contains(point))
            return new ArrowLayout { position = point, rotation = 180, atEdge = false };
        Vector2 direction = point - safe.center;
        if (projected.z <= 0) direction = -direction;
        if (direction.sqrMagnitude < .001f) direction = Vector2.right;
        direction.Normalize();
        float x = Mathf.Abs(direction.x) > .0001f ? safe.width * .5f / Mathf.Abs(direction.x) : float.PositiveInfinity;
        float y = Mathf.Abs(direction.y) > .0001f ? safe.height * .5f / Mathf.Abs(direction.y) : float.PositiveInfinity;
        return new ArrowLayout {
            position = safe.center + direction * Mathf.Min(x, y),
            rotation = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90,
            atEdge = true
        };
    }
}
