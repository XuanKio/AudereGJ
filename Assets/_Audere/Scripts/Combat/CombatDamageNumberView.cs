using System.Collections;
using TMPro;
using UnityEngine;

namespace Audere.Combat
{
    [DisallowMultipleComponent]
    public sealed class CombatDamageNumberView : MonoBehaviour
    {
        private RectTransform rectTransform;
        private TMP_Text label;
        private CanvasGroup canvasGroup;
        private Coroutine animationRoutine;

        public void Configure(TMP_Text damageLabel, CanvasGroup group)
        {
            rectTransform = transform as RectTransform;
            label = damageLabel;
            canvasGroup = group;
        }

        public void Play(
            int damage,
            Vector2 startPosition,
            Color color,
            float fontSize,
            float duration,
            float riseDistance,
            float horizontalDrift)
        {
            if (rectTransform == null) rectTransform = transform as RectTransform;
            if (label == null) label = GetComponent<TMP_Text>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (rectTransform == null || label == null || canvasGroup == null) return;

            if (animationRoutine != null) StopCoroutine(animationRoutine);

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            label.SetText("{0}", damage);
            label.color = color;
            label.fontSize = fontSize;
            canvasGroup.alpha = 1f;
            rectTransform.anchoredPosition = startPosition;
            rectTransform.localScale = Vector3.one * .42f;
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-4f, 4f));
            animationRoutine = StartCoroutine(Animate(
                startPosition,
                Mathf.Max(.1f, duration),
                Mathf.Max(0f, riseDistance),
                horizontalDrift));
        }

        public void StopImmediately()
        {
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            animationRoutine = null;
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }

        private IEnumerator Animate(Vector2 startPosition, float duration, float riseDistance, float horizontalDrift)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float riseT = 1f - Mathf.Pow(1f - t, 3f);
                rectTransform.anchoredPosition = startPosition + new Vector2(horizontalDrift * t, riseDistance * riseT);

                const float popEnd = .24f;
                if (t < popEnd)
                {
                    float popT = Mathf.Clamp01(t / popEnd);
                    float popScale = Mathf.LerpUnclamped(.42f, 1f, EaseOutBack(popT));
                    rectTransform.localScale = Vector3.one * popScale;
                }
                else
                {
                    float settleT = Mathf.Clamp01((t - popEnd) / .22f);
                    float settleScale = Mathf.Lerp(1.08f, 1f, SmoothStep(settleT));
                    rectTransform.localScale = Vector3.one * settleScale;
                }

                canvasGroup.alpha = 1f - SmoothStep(Mathf.InverseLerp(.62f, 1f, t));
                rectTransform.localRotation = Quaternion.Lerp(rectTransform.localRotation, Quaternion.identity, t);
                yield return null;
            }

            animationRoutine = null;
            gameObject.SetActive(false);
        }

        private static float EaseOutBack(float t)
        {
            const float overshoot = 1.70158f;
            float shifted = t - 1f;
            return 1f + (overshoot + 1f) * shifted * shifted * shifted + overshoot * shifted * shifted;
        }

        private static float SmoothStep(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private void OnDisable()
        {
            animationRoutine = null;
        }
    }
}
