using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Audere.UI
{
    [DisallowMultipleComponent]
    public sealed class MainMenuBackgroundParallax : MonoBehaviour
    {
        [SerializeField] private RectTransform background;
        [SerializeField] private Vector2 maxPointerOffset = new Vector2(24f, 14f);
        [SerializeField, Min(0.01f)] private float followSharpness = 2.2f;
        [SerializeField] private Vector2 idleDriftAmount = new Vector2(5f, 3f);
        [SerializeField, Min(0.01f)] private float idleDriftSpeed = 0.1f;
        [SerializeField] private bool invertPointer = true;

        private Vector2 basePosition;
        private bool initialized;

        private void Awake()
        {
            InitializeIfNeeded();
        }

        private void OnEnable()
        {
            InitializeIfNeeded();
        }

        private void LateUpdate()
        {
            if (!initialized || background == null)
                return;

            Vector2 pointer = ReadNormalizedPointer();
            if (invertPointer)
                pointer = -pointer;

            float time = Time.unscaledTime * idleDriftSpeed * Mathf.PI * 2f;
            Vector2 idleDrift = new Vector2(
                Mathf.Sin(time) * idleDriftAmount.x,
                Mathf.Cos(time * 0.73f) * idleDriftAmount.y);

            Vector2 targetPosition = basePosition
                + Vector2.Scale(pointer, maxPointerOffset)
                + idleDrift;
            float blend = 1f - Mathf.Exp(-followSharpness * Time.unscaledDeltaTime);
            background.anchoredPosition = Vector2.Lerp(
                background.anchoredPosition,
                targetPosition,
                blend);
        }

        private void OnDisable()
        {
            if (initialized && background != null)
                background.anchoredPosition = basePosition;
        }

        private void InitializeIfNeeded()
        {
            if (initialized)
                return;

            if (background == null)
                background = transform as RectTransform;

            if (background == null)
                return;

            basePosition = background.anchoredPosition;
            initialized = true;
        }

        private static Vector2 ReadNormalizedPointer()
        {
            if (!Application.isFocused || Screen.width <= 0 || Screen.height <= 0)
                return Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current == null)
                return Vector2.zero;

            Vector2 pointerPosition = Mouse.current.position.ReadValue();
#else
            Vector2 pointerPosition = Input.mousePosition;
#endif

            return new Vector2(
                Mathf.Clamp(pointerPosition.x / Screen.width * 2f - 1f, -1f, 1f),
                Mathf.Clamp(pointerPosition.y / Screen.height * 2f - 1f, -1f, 1f));
        }
    }
}
