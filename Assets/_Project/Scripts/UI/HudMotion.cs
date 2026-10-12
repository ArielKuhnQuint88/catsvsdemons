using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CatsVsDemons.UI
{
    /// <summary>One scale owner per control, including touch, keyboard and selection feedback.</summary>
    public sealed class HudMotion : MonoBehaviour, IPointerDownHandler,
        IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler, ISubmitHandler
    {
        private Selectable selectable;
        private Vector3 restScale;
        private bool pressed;
        private bool hovered;
        private bool selected;
        private bool ready;
        private float impulse;

        private void Awake()
        {
            selectable = GetComponent<Selectable>();
            restScale = transform.localScale;
        }

        public void SetSelected(bool value) => selected = value;
        public void SetReady(bool value) => ready = value;
        public void Punch() => impulse = 1f;

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            bool active = selectable == null || selectable.IsInteractable();
            float scale = active && pressed ? 0.94f : selected ? 1.065f :
                active && hovered ? 1.025f : 1f;
            if (active && ready) scale += Mathf.Sin(Time.unscaledTime * 4f) * 0.035f;
            scale += Mathf.Sin(impulse * Mathf.PI) * impulse * 0.12f;
            impulse = Mathf.Max(0f, impulse - dt * 3.5f);
            transform.localScale = Vector3.Lerp(transform.localScale,
                restScale * scale, 1f - Mathf.Exp(-20f * dt));
        }

        private void OnDisable()
        {
            pressed = hovered = false;
            impulse = 0f;
            transform.localScale = restScale;
        }

        public void OnPointerDown(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left) pressed = true;
        }
        public void OnPointerUp(PointerEventData data) => pressed = false;
        public void OnPointerEnter(PointerEventData data) => hovered = true;
        public void OnPointerExit(PointerEventData data) => hovered = pressed = false;
        public void OnSubmit(BaseEventData data) => Punch();
    }
}
