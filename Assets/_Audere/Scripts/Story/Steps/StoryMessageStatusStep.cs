using System.Collections;
using TMPro;
using UnityEngine;

namespace Audere.Story.Steps
{
    public sealed class StoryMessageStatusStep : StoryStep
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private string message = "Đã gửi";
        [SerializeField, Min(0f)] private float fadeDuration = .18f;
        [SerializeField, Min(0f)] private float holdDuration = .72f;

        protected override IEnumerator Execute()
        {
            if (canvasGroup == null || statusText == null)
            {
                Debug.LogError("[StoryMessageStatusStep] CanvasGroup and status text are required.", this);
                FailStep();
                yield break;
            }

            statusText.text = message;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            yield return FadeTo(1f);
            yield return WaitUnscaled(holdDuration);
            yield return FadeTo(0f);
            CompleteStep();
        }

        protected override void OnCancelled()
        {
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        }

        private IEnumerator FadeTo(float target)
        {
            float start = canvasGroup.alpha;
            if (fadeDuration <= Mathf.Epsilon)
            {
                canvasGroup.alpha = target;
                yield break;
            }
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeDuration));
                canvasGroup.alpha = Mathf.Lerp(start, target, t);
                yield return null;
            }
            canvasGroup.alpha = target;
        }

        private static IEnumerator WaitUnscaled(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }
}
