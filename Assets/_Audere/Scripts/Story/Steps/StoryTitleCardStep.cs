using System.Collections;
using TMPro;
using UnityEngine;

namespace Audere.Story.Steps
{
    public sealed class StoryTitleCardStep : StoryStep
    {
        [SerializeField] private CanvasGroup overlay;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private string title = "Ngày 1 - Kết thúc";
        [SerializeField, Min(0f)] private float fadeDuration = .65f;
        [SerializeField, Min(0f)] private float holdDuration = 1.8f;
        [SerializeField] private bool leaveVisible = true;

        protected override IEnumerator Execute()
        {
            if (overlay == null || titleText == null)
            {
                Debug.LogError("[StoryTitleCardStep] Overlay and title text are required.", this);
                FailStep();
                yield break;
            }

            titleText.text = title;
            overlay.interactable = false;
            overlay.blocksRaycasts = true;
            float start = overlay.alpha;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / Mathf.Max(.01f, fadeDuration)));
                overlay.alpha = Mathf.Lerp(start, 1f, t);
                yield return null;
            }
            overlay.alpha = 1f;

            elapsed = 0f;
            while (elapsed < holdDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!leaveVisible)
                overlay.alpha = 0f;
            CompleteStep();
        }

        protected override void OnCancelled()
        {
            if (overlay != null && !leaveVisible)
                overlay.alpha = 0f;
        }
    }
}
