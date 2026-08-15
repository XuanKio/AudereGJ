using System.Collections;
using TMPro;
using UnityEngine;

namespace Audere.Dialogue
{
    [DisallowMultipleComponent]
    public sealed class DialogueBubbleView : MonoBehaviour
    {
        [SerializeField] private TMP_Text characterNameText;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform bubbleTransform;
        [SerializeField, Min(0.01f)] private float popDuration = 0.2f;
        [SerializeField, Range(0.5f, 1f)] private float popStartScale = 0.78f;
        [SerializeField, Range(1f, 1.2f)] private float popOvershootScale = 1.06f;
        [SerializeField, Min(0f)] private float popRiseDistance = 22f;
        [SerializeField, Min(0.01f)] private float popOutDuration = 0.09f;

        private Vector2 restingPosition;
        private bool hasRestingPosition;

        public TMP_Text DialogueText => dialogueText;

        private void Awake()
        {
            CacheRestingPosition();
        }

        public void SetContent(string characterName, string text)
        {
            if (characterNameText != null)
                characterNameText.text = characterName;

            if (dialogueText != null)
            {
                dialogueText.text = text;
                dialogueText.maxVisibleCharacters = 0;
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);

            if (visible)
                return;

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            if (bubbleTransform != null)
            {
                bubbleTransform.localScale = Vector3.one;
                CacheRestingPosition();
                bubbleTransform.anchoredPosition = restingPosition;
            }
        }

        public IEnumerator PopIn()
        {
            gameObject.SetActive(true);

            if (bubbleTransform == null)
                bubbleTransform = (RectTransform)transform;
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            CacheRestingPosition();

            bubbleTransform.localScale = Vector3.one * popStartScale;
            bubbleTransform.anchoredPosition = restingPosition + Vector2.down * popRiseDistance;
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            float elapsed = 0f;
            const float overshootPoint = 0.68f;

            while (elapsed < popDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalized = Mathf.Clamp01(elapsed / popDuration);
                float scale;

                if (normalized < overshootPoint)
                {
                    float phase = normalized / overshootPoint;
                    float eased = 1f - Mathf.Pow(1f - phase, 3f);
                    scale = Mathf.LerpUnclamped(popStartScale, popOvershootScale, eased);
                }
                else
                {
                    float phase = (normalized - overshootPoint) / (1f - overshootPoint);
                    float eased = phase * phase * (3f - 2f * phase);
                    scale = Mathf.LerpUnclamped(popOvershootScale, 1f, eased);
                }

                bubbleTransform.localScale = Vector3.one * scale;
                float riseProgress = 1f - Mathf.Pow(1f - normalized, 3f);
                bubbleTransform.anchoredPosition = Vector2.LerpUnclamped(
                    restingPosition + Vector2.down * popRiseDistance,
                    restingPosition,
                    riseProgress);
                if (canvasGroup != null)
                    canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(normalized * 1.35f));

                yield return null;
            }

            bubbleTransform.localScale = Vector3.one;
            bubbleTransform.anchoredPosition = restingPosition;
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
        }

        public IEnumerator PopOut()
        {
            if (!gameObject.activeSelf)
                yield break;

            if (bubbleTransform == null)
                bubbleTransform = (RectTransform)transform;
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            CacheRestingPosition();
            float elapsed = 0f;

            while (elapsed < popOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalized = Mathf.Clamp01(elapsed / popOutDuration);
                float eased = normalized * normalized * (3f - 2f * normalized);

                bubbleTransform.localScale = Vector3.one * Mathf.Lerp(1f, 0.94f, eased);
                bubbleTransform.anchoredPosition = Vector2.Lerp(
                    restingPosition,
                    restingPosition + Vector2.down * (popRiseDistance * 0.35f),
                    eased);
                if (canvasGroup != null)
                    canvasGroup.alpha = 1f - eased;

                yield return null;
            }

            SetVisible(false);
        }

        private void CacheRestingPosition()
        {
            if (hasRestingPosition || bubbleTransform == null)
                return;

            restingPosition = bubbleTransform.anchoredPosition;
            hasRestingPosition = true;
        }
    }
}
