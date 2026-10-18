using DG.Tweening;
using UnityEngine;

namespace ToyShop.UI
{
    /// <summary>Presentation only: activation, input and pause remain owned by the existing controllers.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(CanvasGroup))]
    public sealed class ToyShopPanelEntrance : MonoBehaviour
    {
        private RectTransform rect;
        private CanvasGroup group;
        private Vector2 baseline;
        private float baselineAlpha;
        private Sequence motion;

        private void Awake()
        {
            rect = (RectTransform)transform;
            group = GetComponent<CanvasGroup>();
            baseline = rect.anchoredPosition;
            baselineAlpha = group.alpha;
        }
        private void OnEnable() => Play();
        public void Play()
        {
            if (!rect) return;
            motion?.Kill();
            var theme = ToyShopTheme.Current;
            rect.anchoredPosition = baseline - Vector2.up * theme.panelTravel;
            group.alpha = 0;
            motion = DOTween.Sequence().SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .Join(rect.DOAnchorPos(baseline, theme.panelDuration).SetEase(Ease.OutCubic))
                .Join(group.DOFade(baselineAlpha, theme.panelDuration));
        }
        private void OnDisable()
        {
            motion?.Kill();
            if (rect) rect.anchoredPosition = baseline;
            if (group) group.alpha = baselineAlpha;
        }
    }
}
