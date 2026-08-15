using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Audere.Dialogue
{
    [DisallowMultipleComponent]
    public sealed class DialogueController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private DialogueCharacterCatalog characterCatalog;

        [Header("Views")]
        [SerializeField] private CanvasGroup dialogueGroup;
        [SerializeField] private DialogueCharacterSlotView leftSlot;
        [SerializeField] private DialogueCharacterSlotView rightSlot;

        [Header("Playback")]
        [SerializeField, Min(1f)] private float charactersPerSecond = 42f;
        [SerializeField, Min(0.01f)] private float characterEntranceDuration = 0.24f;
        [SerializeField, Min(0f)] private float bubbleDelay = 0.06f;

        private readonly HashSet<string> playedDialogueIds = new HashSet<string>();
        private Coroutine playbackRoutine;
        private float timeScaleBeforeDialogue = 1f;
        private bool ownsGameplayPause;

        public bool IsPlaying => playbackRoutine != null;

        private void Awake()
        {
            HideImmediately();
        }

        private void OnDisable()
        {
            StopPlayback(true);
        }

        public bool Play(DialogueData data, bool triggerOnce = true)
        {
            if (data == null || !data.HasLines)
            {
                Debug.LogWarning("[DialogueController] Dialogue data is missing or has no lines.", data);
                return false;
            }

            if (triggerOnce && playedDialogueIds.Contains(data.DialogueId))
                return false;

            if (!TryResolveCharacter(data.LeftCharacter, out DialogueCharacterCatalog.Entry leftCharacter) ||
                !TryResolveCharacter(data.RightCharacter, out DialogueCharacterCatalog.Entry rightCharacter))
                return false;

            StopPlayback(true);
            if (triggerOnce)
                playedDialogueIds.Add(data.DialogueId);

            playbackRoutine = StartCoroutine(PlayRoutine(data, leftCharacter, rightCharacter));
            return true;
        }

        public void ForceClose()
        {
            StopPlayback(true);
            HideImmediately();
        }

        public void HideImmediately()
        {
            if (dialogueGroup != null)
            {
                dialogueGroup.alpha = 0f;
                dialogueGroup.interactable = false;
                dialogueGroup.blocksRaycasts = false;
            }

            if (leftSlot != null) leftSlot.HideBubble();
            if (rightSlot != null) rightSlot.HideBubble();
        }

        private IEnumerator PlayRoutine(
            DialogueData data,
            DialogueCharacterCatalog.Entry leftCharacter,
            DialogueCharacterCatalog.Entry rightCharacter)
        {
            timeScaleBeforeDialogue = Time.timeScale;
            Time.timeScale = 0f;
            ownsGameplayPause = true;

            if (dialogueGroup != null)
            {
                dialogueGroup.alpha = 1f;
                dialogueGroup.interactable = true;
                dialogueGroup.blocksRaycasts = true;
            }

            leftSlot.PrepareForEntrance(leftCharacter);
            rightSlot.PrepareForEntrance(rightCharacter);

            yield return AnimateCharactersIn();

            DialogueBubbleView currentBubble = null;

            foreach (DialogueData.Line line in data.Lines)
            {
                if (SkipPressed())
                    break;

                if (currentBubble != null)
                    yield return currentBubble.PopOut();

                DialogueCharacterSlotView speakingSlot = line.Speaker == DialogueSpeakerSide.Left
                    ? leftSlot
                    : rightSlot;

                leftSlot.SetPresentation(leftCharacter, line.Speaker == DialogueSpeakerSide.Left, line.Text);
                rightSlot.SetPresentation(rightCharacter, line.Speaker == DialogueSpeakerSide.Right, line.Text);

                TMP_Text text = speakingSlot.Bubble != null ? speakingSlot.Bubble.DialogueText : null;
                if (bubbleDelay > 0f)
                    yield return WaitUnscaled(bubbleDelay);

                if (speakingSlot.Bubble != null)
                {
                    yield return speakingSlot.Bubble.PopIn();
                    currentBubble = speakingSlot.Bubble;
                }

                if (SkipPressed())
                    break;

                if (text != null)
                    yield return TypeLine(text);

                if (SkipPressed())
                    break;

                while (!AdvancePressed() && !SkipPressed())
                    yield return null;

                if (SkipPressed())
                    break;

                yield return null;
            }

            if (currentBubble != null)
                yield return currentBubble.PopOut();

            playbackRoutine = null;
            RestoreGameplayTime();
            HideImmediately();
        }

        private IEnumerator AnimateCharactersIn()
        {
            float elapsed = 0f;
            while (elapsed < characterEntranceDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = SmoothOut(elapsed / characterEntranceDuration);

                leftSlot.SetVisibility(progress);
                rightSlot.SetVisibility(progress);
                yield return null;
            }

            leftSlot.SetVisibility(1f);
            rightSlot.SetVisibility(1f);
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

        private static float SmoothOut(float value)
        {
            value = Mathf.Clamp01(value);
            return 1f - Mathf.Pow(1f - value, 3f);
        }

        private IEnumerator TypeLine(TMP_Text text)
        {
            text.maxVisibleCharacters = 0;
            text.ForceMeshUpdate();
            int characterCount = text.textInfo.characterCount;
            float visibleCharacters = 0f;

            while (text.maxVisibleCharacters < characterCount)
            {
                if (SkipPressed())
                    yield break;

                if (AdvancePressed())
                {
                    text.maxVisibleCharacters = characterCount;
                    yield break;
                }

                visibleCharacters += charactersPerSecond * Time.unscaledDeltaTime;
                text.maxVisibleCharacters = Mathf.Min(characterCount, Mathf.FloorToInt(visibleCharacters));
                yield return null;
            }
        }

        private bool TryResolveCharacter(
            DialogueCharacterId characterId,
            out DialogueCharacterCatalog.Entry character)
        {
            if (characterCatalog != null && characterCatalog.TryGet(characterId, out character))
            {
                if (character.Portrait == null)
                    Debug.LogWarning($"[DialogueController] {characterId} does not have a portrait yet.", characterCatalog);
                return true;
            }

            Debug.LogError($"[DialogueController] Character {characterId} is not configured in the catalog.", characterCatalog);
            character = default;
            return false;
        }

        private void StopPlayback(bool restoreTime)
        {
            if (playbackRoutine != null)
            {
                StopCoroutine(playbackRoutine);
                playbackRoutine = null;
            }

            if (restoreTime)
                RestoreGameplayTime();
        }

        private void RestoreGameplayTime()
        {
            if (!ownsGameplayPause)
                return;

            Time.timeScale = timeScaleBeforeDialogue;
            ownsGameplayPause = false;
        }

        private static bool AdvancePressed()
        {
            return Input.GetMouseButtonDown(0) ||
                   Input.GetKeyDown(KeyCode.Space) ||
                   Input.GetKeyDown(KeyCode.Return) ||
                   Input.GetKeyDown(KeyCode.KeypadEnter);
        }

        private static bool SkipPressed()
        {
            return Input.GetKeyDown(KeyCode.Escape);
        }
    }
}
