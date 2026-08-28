using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Audere.Dialogue
{
    [DisallowMultipleComponent]
    public sealed class DialogueCharacterSlotView : MonoBehaviour
    {
        [SerializeField] private Image characterImage;
        [SerializeField] private DialogueBubbleView bubble;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField, Range(0.1f, 0.8f)] private float inactiveBrightness = 0.34f;

        private Coroutine portraitGlitch;
        private Sprite settledPortrait;
        private Vector3 settledPortraitPosition;
        public DialogueBubbleView Bubble => bubble;

        private void StopPortraitGlitch()
        {
            if (portraitGlitch == null) return;
            StopCoroutine(portraitGlitch);
            portraitGlitch = null;
            if (characterImage != null)
            {
                characterImage.sprite = settledPortrait;
                characterImage.rectTransform.localPosition = settledPortraitPosition;
            }
        }

        private void OnDisable() => StopPortraitGlitch();

        private IEnumerator GlitchPortrait(Sprite previous)
        {
            for (int i = 0; i < 4; i++)
            {
                characterImage.sprite = i % 2 == 0 ? previous : settledPortrait;
                characterImage.rectTransform.localPosition = settledPortraitPosition + Vector3.right * (i % 2 == 0 ? 3f : -2f);
                yield return new WaitForSecondsRealtime(.065f);
            }
            characterImage.sprite = settledPortrait;
            characterImage.rectTransform.localPosition = settledPortraitPosition;
            portraitGlitch = null;
        }

        public void SetCharacter(DialogueCharacterCatalog.Entry character, Sprite portraitOverride = null)
        {
            if (characterImage == null)
                return;

            StopPortraitGlitch();
            Sprite portrait = portraitOverride != null ? portraitOverride : character.Portrait;
            characterImage.sprite = portrait;
            characterImage.enabled = portrait != null;
        }

        public void PrepareForEntrance(
            DialogueCharacterCatalog.Entry character,
            bool isPreparingToSpeak = false,
            Sprite portraitOverride = null)
        {
            SetCharacter(character, portraitOverride);
            SetCharacterBrightness(isPreparingToSpeak ? 1f : inactiveBrightness);
            SetVisibility(0f);
            HideBubble();
            transform.localScale = Vector3.one;
        }

        public void SetVisibility(float alpha)
        {
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Clamp01(alpha);
        }

        public void SetPresentation(
            DialogueCharacterCatalog.Entry character,
            bool isSpeaking,
            string lineText,
            Sprite portraitOverride = null,
            bool glitchTransition = false)
        {
            StopPortraitGlitch();
            Sprite previous = characterImage != null ? characterImage.sprite : null;
            SetCharacter(character, portraitOverride);
            if (glitchTransition && previous != null && characterImage != null && previous != characterImage.sprite && isActiveAndEnabled)
            {
                settledPortrait = characterImage.sprite;
                settledPortraitPosition = characterImage.rectTransform.localPosition;
                portraitGlitch = StartCoroutine(GlitchPortrait(previous));
            }

            SetVisibility(1f);
            transform.localScale = Vector3.one;
            SetCharacterBrightness(isSpeaking ? 1f : inactiveBrightness);

            if (bubble == null)
                return;

            bubble.SetContent(character.DisplayName, lineText);
            bubble.SetVisible(false);
        }

        public void HideBubble()
        {
            StopPortraitGlitch();
            if (bubble != null)
                bubble.SetVisible(false);
        }

        private void SetCharacterBrightness(float brightness)
        {
            if (characterImage == null)
                return;

            characterImage.color = new Color(brightness, brightness, brightness, 1f);
        }
    }
}
