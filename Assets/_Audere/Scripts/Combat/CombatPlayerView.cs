using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class CombatPlayerView : MonoBehaviour
    {
        [SerializeField] private Image visual;
        [SerializeField] private Color normalColor = new Color(.72f, .95f, .92f, 1f);
        [SerializeField] private Color hitColor = new Color(1f, .38f, .44f, 1f);

        private RectTransform rectTransform;
        private Image[] visualParts;
        private float invulnerabilityRemaining;
        private Coroutine loseRhythmRoutine;

        public RectTransform RectTransform => rectTransform;
        public Vector2 Position => rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (visual == null)
                visual = GetComponentInChildren<Image>(true);
            visualParts = GetComponentsInChildren<Image>(true);
        }

        public void Configure(Image playerVisual)
        {
            visual = playerVisual;
            visualParts = GetComponentsInChildren<Image>(true);
        }

        public void ResetPlayer()
        {
            if (rectTransform == null) Awake();
            StopLoseRhythm();
            // The Heart is a child of the Catch Cursor, so local zero is always
            // the exact center of the mouse-controlled catcher.
            rectTransform.anchoredPosition = Vector2.zero;
            invulnerabilityRemaining = 0f;
            SetVisualColor(normalColor);
            gameObject.SetActive(true);
        }

        public void TickVisual(float deltaTime)
        {
            if (rectTransform == null)
                return;

            invulnerabilityRemaining = Mathf.Max(0f, invulnerabilityRemaining - deltaTime);
            if (invulnerabilityRemaining > 0f)
            {
                bool bright = Mathf.FloorToInt(invulnerabilityRemaining * 18f) % 2 == 0;
                SetVisualColor(bright ? hitColor : normalColor);
            }
            else
                SetVisualColor(normalColor);
        }

        public bool TryRegisterHit(float invulnerabilityDuration)
        {
            if (invulnerabilityRemaining > 0f)
                return false;

            invulnerabilityRemaining = Mathf.Max(.05f, invulnerabilityDuration);
            SetVisualColor(hitColor);
            return true;
        }

        public void PlayLoseRhythm(float duration)
        {
            if (!isActiveAndEnabled)
                return;
            StopLoseRhythm();
            loseRhythmRoutine = StartCoroutine(LoseRhythmRoutine(Mathf.Max(.1f, duration)));
        }

        private IEnumerator LoseRhythmRoutine(float duration)
        {
            Vector2 origin = rectTransform.anchoredPosition;
            Vector3 baseScale = rectTransform.localScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float envelope = Mathf.Sin(t * Mathf.PI);
                rectTransform.anchoredPosition = origin + new Vector2(
                    Mathf.Sin(t * Mathf.PI * 8f) * 4f,
                    Mathf.Sin(t * Mathf.PI * 5f) * 2f) * envelope;
                rectTransform.localScale = baseScale * (1f + Mathf.Sin(t * Mathf.PI * 6f) * .07f * envelope);
                yield return null;
            }
            rectTransform.anchoredPosition = origin;
            rectTransform.localScale = baseScale;
            loseRhythmRoutine = null;
        }

        private void StopLoseRhythm()
        {
            if (loseRhythmRoutine != null)
                StopCoroutine(loseRhythmRoutine);
            loseRhythmRoutine = null;
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.localScale = Vector3.one;
            }
        }

        private void OnDisable()
        {
            StopLoseRhythm();
        }

        private void SetVisualColor(Color color)
        {
            if (visualParts == null || visualParts.Length == 0)
                visualParts = GetComponentsInChildren<Image>(true);

            for (int i = 0; i < visualParts.Length; i++)
            {
                if (visualParts[i] != null)
                    visualParts[i].color = color;
            }
        }
    }
}
