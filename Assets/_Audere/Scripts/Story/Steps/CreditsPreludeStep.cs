using System.Collections;
using Audere.Audio;
using TMPro;
using UnityEngine;

namespace Audere.Story.Steps
{
    /// <summary>Scrolls every credit through the screen, then jolts the text before the dodge.</summary>
    public sealed class CreditsPreludeStep : StoryStep
    {
        [SerializeField] private RectTransform creditsCanvas;
        [SerializeField] private TextMeshProUGUI creditsText;
        [SerializeField, Min(.1f)] private float scrollDuration = 11f;
        [SerializeField, Range(1f, 2f)] private float shakeDuration = 1.5f;
        [SerializeField, Range(1f, 1.5f)] private float backgroundMusicBoost = 1.3f;

        private Vector2 authoredPosition;
        private Color authoredColor;
        private bool hasPresentation;

        public float ScrollDuration => scrollDuration;
        public float ShakeDuration => shakeDuration;

        protected override IEnumerator Execute()
        {
            if (creditsCanvas == null || creditsText == null ||
                !creditsCanvas.gameObject.activeInHierarchy)
            {
                Debug.LogError("[CreditsPreludeStep] Assign an active credits canvas and text.", this);
                FailStep();
                yield break;
            }

            RectTransform textRect = creditsText.rectTransform;
            authoredPosition = textRect.anchoredPosition;
            authoredColor = creditsText.color;
            hasPresentation = true;
            AudioService.Instance?.SetMusicBoost(this, backgroundMusicBoost);
            Canvas.ForceUpdateCanvases();
            creditsText.ForceMeshUpdate();

            float textHeight = Mathf.Max(textRect.rect.height, creditsText.preferredHeight);
            float travel = (creditsCanvas.rect.height + textHeight) * .5f + 40f;
            float elapsed = 0f;
            while (elapsed < scrollDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / scrollDuration);
                textRect.anchoredPosition = Vector2.up * Mathf.Lerp(travel, -travel, progress);
                yield return null;
            }

            // A brief visible jolt marks the shift from readable credits to incoming hazards.
            elapsed = 0f;
            while (elapsed < shakeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / shakeDuration);
                float envelope = Mathf.Sin(progress * Mathf.PI);
                textRect.anchoredPosition = new Vector2(
                    Mathf.Sin(elapsed * 82f) * 15f,
                    Mathf.Cos(elapsed * 97f) * 11f) * envelope;
                float flicker = .65f + .35f * Mathf.Abs(Mathf.Sin(elapsed * 35f));
                creditsText.color = new Color(authoredColor.r, authoredColor.g,
                    authoredColor.b, authoredColor.a * flicker);
                yield return null;
            }

            textRect.anchoredPosition = Vector2.zero;
            creditsText.color = authoredColor;
            hasPresentation = false;
            AudioService.Instance?.ReleaseMusicOwner(this);
            CompleteStep();
        }

        protected override void OnCancelled() => RestorePresentation();

        private void OnDestroy() => RestorePresentation();

        private void RestorePresentation()
        {
            if (!hasPresentation)
                return;

            hasPresentation = false;
            AudioService.Instance?.ReleaseMusicOwner(this);
            if (creditsText != null)
            {
                creditsText.rectTransform.anchoredPosition = authoredPosition;
                creditsText.color = authoredColor;
            }
        }
    }
}
