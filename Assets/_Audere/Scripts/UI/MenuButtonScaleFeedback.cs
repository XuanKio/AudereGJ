using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Audere.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Selectable))]
    public sealed class MenuButtonScaleFeedback : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        ISelectHandler,
        IDeselectHandler
    {
        [SerializeField, Min(1f)] private float hoverScale = 1.08f;
        [SerializeField, Range(0.8f, 1f)] private float pressedMultiplier = 0.96f;
        [SerializeField, Min(0.1f)] private float responseSpeed = 14f;

        private Vector3 restingScale;
        private bool hovered;
        private bool selected;
        private bool pressed;

        private void Awake()
        {
            restingScale = transform.localScale;
        }

        private void OnEnable()
        {
            restingScale = transform.localScale;
            hovered = false;
            selected = false;
            pressed = false;
        }

        private void OnDisable()
        {
            transform.localScale = restingScale;
        }

        private void Update()
        {
            bool emphasized = hovered || selected;
            float scale = emphasized ? hoverScale : 1f;

            if (pressed)
                scale *= pressedMultiplier;

            Vector3 targetScale = restingScale * scale;
            float blend = 1f - Mathf.Exp(-responseSpeed * Time.unscaledDeltaTime);
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, blend);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            pressed = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pressed = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pressed = false;
        }

        public void OnSelect(BaseEventData eventData)
        {
            selected = true;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            selected = false;
            pressed = false;
        }
    }
}
