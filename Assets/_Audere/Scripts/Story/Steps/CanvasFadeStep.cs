using System.Collections;
using UnityEngine;

namespace Audere.Story.Steps
{
    public sealed class CanvasFadeStep : StoryStep
    {
        [Header("Fade")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField, Range(0f, 1f)] private float targetAlpha = 1f;
        [SerializeField, Min(0f)] private float duration = .45f;
        [SerializeField] private bool useUnscaledTime = true;

        protected override IEnumerator Execute()
        {
            if (canvasGroup == null)
            {
                Debug.LogError("[CanvasFadeStep] Canvas Group reference is required.", this);
                FailStep();
                yield break;
            }

            float startAlpha = canvasGroup.alpha;
            canvasGroup.interactable = false;
            if (targetAlpha > startAlpha)
                canvasGroup.blocksRaycasts = true;

            if (duration <= Mathf.Epsilon)
            {
                ApplyFinalState();
                CompleteStep();
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = progress * progress * (3f - 2f * progress);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, eased);
                yield return null;
            }

            ApplyFinalState();
            CompleteStep();
        }

        protected override void OnCancelled()
        {
            if (canvasGroup == null)
                return;

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = canvasGroup.alpha >= .999f;
        }

        private void ApplyFinalState()
        {
            canvasGroup.alpha = targetAlpha;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = targetAlpha >= .999f;
        }
    }
}
