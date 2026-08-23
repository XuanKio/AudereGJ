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

        public DialogueBubbleView Bubble => bubble;

        public void SetCharacter(DialogueCharacterCatalog.Entry character)
        {
            if (characterImage == null)
                return;

            characterImage.sprite = character.Portrait;
            characterImage.enabled = character.Portrait != null;
        }

        public void PrepareForEntrance(
            DialogueCharacterCatalog.Entry character,
            bool isPreparingToSpeak = false)
        {
            SetCharacter(character);
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
            string lineText)
        {
            SetCharacter(character);

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
