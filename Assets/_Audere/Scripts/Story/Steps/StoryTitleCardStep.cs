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
        [Tooltip("Keep the title visible until the player clicks, taps or confirms.")]
        [SerializeField] private bool waitForConfirm;
        [Tooltip("Allows click/tap/confirm to skip the normal hold without making confirmation required.")]
        [SerializeField] private bool allowConfirmSkip;
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

            bool waitForRelease = IsPrimaryPointerHeld();
            elapsed = 0f;
            while (waitForConfirm || elapsed < holdDuration)
            {
                if (waitForRelease)
                {
                    waitForRelease = IsPrimaryPointerHeld();
                }
                else if (ConfirmPressed() && (waitForConfirm || allowConfirmSkip))
                {
                    break;
                }

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

        private static bool ConfirmPressed()
        {
            if (Input.GetMouseButtonDown(0) ||
                Input.GetKeyDown(KeyCode.Space) ||
                Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter))
                return true;

            return Input.touchCount > 0 &&
                   Input.GetTouch(0).phase == TouchPhase.Began;
        }

        private static bool IsPrimaryPointerHeld()
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                return touch.phase != TouchPhase.Ended &&
                       touch.phase != TouchPhase.Canceled;
            }

            return Input.GetMouseButton(0);
        }
    }
}
