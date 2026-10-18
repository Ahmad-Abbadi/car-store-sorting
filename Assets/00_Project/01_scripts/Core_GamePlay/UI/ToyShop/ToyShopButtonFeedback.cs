using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ToyShop.UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(UnityEngine.UI.Button))]
    public sealed class ToyShopButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        private UnityEngine.UI.Button button;
        private Vector3 baseline;
        private Tween motion;
        private bool hovering, selected, pressed;

        private void Awake() { button = GetComponent<UnityEngine.UI.Button>(); baseline = transform.localScale; }
        private void OnEnable() { if (!button) button = GetComponent<UnityEngine.UI.Button>(); }
        private void LateUpdate()
        {
            if (button.IsInteractable() || (!hovering && !selected && !pressed)) return;
            motion?.Kill();
            transform.localScale = baseline;
            hovering = selected = pressed = false;
        }
        private void OnDisable()
        {
            motion?.Kill();
            transform.localScale = baseline;
            hovering = selected = pressed = false;
        }

        private void Animate()
        {
            motion?.Kill();
            var theme = ToyShopTheme.Current;
            float scale = !button.IsInteractable() ? 1 : pressed ? .97f : hovering || selected ? 1 + theme.hoverScale : 1;
            motion = transform.DOScale(baseline * scale, theme.buttonDuration).SetEase(Ease.OutQuad)
                .SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }
        public void OnPointerEnter(PointerEventData e) { hovering = true; Animate(); }
        public void OnPointerExit(PointerEventData e) { hovering = pressed = false; Animate(); }
        public void OnPointerDown(PointerEventData e) { if (e.button != PointerEventData.InputButton.Left) return; pressed = true; Animate(); }
        public void OnPointerUp(PointerEventData e) { pressed = false; Animate(); }
        public void OnSelect(BaseEventData e) { selected = true; Animate(); }
        public void OnDeselect(BaseEventData e) { selected = pressed = false; Animate(); }
        public void OnSubmit(BaseEventData e)
        {
            if (!button.IsInteractable()) return;
            motion?.Kill();
            transform.localScale = baseline * .97f;
            Animate();
        }
    }
}
