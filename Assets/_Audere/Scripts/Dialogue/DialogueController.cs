using System;
using System.Collections;
using System.Collections.Generic;
using Audere.Audio;
using Audere.GameplayInput;
using TMPro;
using UnityEngine;

namespace Audere.Dialogue
{
    public enum DialogueResult
    {
        Completed,
        Cancelled
    }

    public enum DialoguePlaybackMode
    {
        GlobalTimePause = 0,
        CallerOwnedPause = 1,
        AutoAdvanceNoInput = 2,
    }

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

        [Header("Audio")]
        [SerializeField] private AudioId typewriterAudioId = AudioId.Dialogue_Text;
        [SerializeField] private AudioSource typewriterAudioSource;

        private readonly HashSet<string> playedDialogueIds = new HashSet<string>();
        private Coroutine playbackRoutine;
        private Action<DialogueResult> activeCompletion;
        private float timeScaleBeforeDialogue = 1f;
        private bool ownsGameplayPause;
        private bool cancellationRequested;
        private int playRequestVersion;
        private GameplayInputGate inputGate;
        private GameplayInputToken inputToken;
        private DialoguePlaybackMode playbackMode;
        private float autoMinimumLineDuration;
        private float autoCharactersPerSecond;
        private float autoInterLineGap;

        public bool IsPlaying => playbackRoutine != null;

        private void Awake()
        {
            EnsureTypewriterAudioSource();
            HideImmediately();
        }

        private void Update()
        {
            if (IsPlaying && SkipPressed())
                cancellationRequested = true;
        }

        private void OnDisable()
        {
            CancelPlayback();
        }

        public bool Play(DialogueData data, bool triggerOnce = true)
        {
            return Play(data, null, DialoguePlaybackMode.GlobalTimePause, triggerOnce);
        }

        public bool Play(
            DialogueData data,
            Action<DialogueResult> onEnded,
            bool triggerOnce = true)
        {
            return Play(data, onEnded, DialoguePlaybackMode.GlobalTimePause, triggerOnce);
        }

        public bool Play(
            DialogueData data,
            Action<DialogueResult> onEnded,
            DialoguePlaybackMode mode,
            bool triggerOnce = true)
        {
            return PlayInternal(data, onEnded, mode, triggerOnce, 0f, 0f, 0f);
        }

        public bool PlayAuto(
            DialogueData data,
            Action<DialogueResult> onEnded,
            float minimumLineDuration,
            float autoAdvanceCharactersPerSecond,
            float interLineGap,
            bool triggerOnce = false)
        {
            return PlayInternal(
                data,
                onEnded,
                DialoguePlaybackMode.AutoAdvanceNoInput,
                triggerOnce,
                Mathf.Max(.1f, minimumLineDuration),
                Mathf.Max(1f, autoAdvanceCharactersPerSecond),
                Mathf.Max(0f, interLineGap));
        }

        private bool PlayInternal(
            DialogueData data,
            Action<DialogueResult> onEnded,
            DialoguePlaybackMode mode,
            bool triggerOnce,
            float minimumLineDuration,
            float autoAdvanceCharactersPerSecond,
            float interLineGap)
        {
            if (!isActiveAndEnabled)
            {
                Debug.LogError("[DialogueController] Enable the controller before calling Play.", this);
                return false;
            }

            if (data == null || !data.HasLines)
            {
                Debug.LogWarning("[DialogueController] Dialogue data is missing or has no lines.", data);
                return false;
            }

            if (triggerOnce && playedDialogueIds.Contains(data.DialogueId))
                return false;

            bool mirrorForAudere = ShouldMirrorForAudere(data);
            DialogueCharacterId displayLeft = mirrorForAudere ? data.RightCharacter : data.LeftCharacter;
            DialogueCharacterId displayRight = mirrorForAudere ? data.LeftCharacter : data.RightCharacter;
            if (!TryResolveCharacter(displayLeft, out DialogueCharacterCatalog.Entry leftCharacter) ||
                !TryResolveCharacter(displayRight, out DialogueCharacterCatalog.Entry rightCharacter))
                return false;

            int requestVersion = ++playRequestVersion;
            CancelPlayback();

            // A cancellation callback is allowed to start another dialogue. In that case,
            // let that newer request win instead of overwriting its coroutine and callback.
            if (requestVersion != playRequestVersion)
                return false;

            if (mode != DialoguePlaybackMode.AutoAdvanceNoInput)
            {
                GameplayInputGate gate = ResolveInputGate();
                if (gate == null)
                {
                    Debug.LogError("[DialogueController] GameplayInputGate is not available.", this);
                    return false;
                }

                GameplayInputToken token = gate.PushMode(this, GameplayInputMode.Dialogue);
                if (!token.IsValid)
                    return false;

                inputGate = gate;
                inputToken = token;
            }

            if (triggerOnce)
                playedDialogueIds.Add(data.DialogueId);

            activeCompletion = onEnded;
            playbackMode = mode;
            autoMinimumLineDuration = minimumLineDuration;
            autoCharactersPerSecond = autoAdvanceCharactersPerSecond;
            autoInterLineGap = interLineGap;
            cancellationRequested = false;
            playbackRoutine = StartCoroutine(PlayRoutine(data, leftCharacter, rightCharacter, mirrorForAudere));
            return true;
        }

        public void ForceClose()
        {
            CancelPlayback();
            HideImmediately();
        }

        public void HideImmediately()
        {
            StopTypewriterSound();

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
            DialogueCharacterCatalog.Entry rightCharacter,
            bool mirrorForAudere)
        {
            if (playbackMode == DialoguePlaybackMode.GlobalTimePause)
            {
                timeScaleBeforeDialogue = Time.timeScale;
                Time.timeScale = 0f;
                ownsGameplayPause = true;
            }

            if (dialogueGroup != null)
            {
                dialogueGroup.alpha = 1f;
                bool takesInput = playbackMode != DialoguePlaybackMode.AutoAdvanceNoInput;
                dialogueGroup.interactable = takesInput;
                dialogueGroup.blocksRaycasts = takesInput;
            }

            DialogueSpeakerSide firstSpeaker = DisplaySide(data.Lines[0].Speaker, mirrorForAudere);
            Sprite leftPortrait = mirrorForAudere
                ? data.RightPortraitOverride
                : data.LeftPortraitOverride;
            Sprite rightPortrait = mirrorForAudere
                ? data.LeftPortraitOverride
                : data.RightPortraitOverride;
            leftSlot.PrepareForEntrance(
                leftCharacter,
                firstSpeaker == DialogueSpeakerSide.Left,
                leftPortrait);
            rightSlot.PrepareForEntrance(
                rightCharacter,
                firstSpeaker == DialogueSpeakerSide.Right,
                rightPortrait);

            yield return AnimateCharactersIn();

            DialogueBubbleView currentBubble = null;

            foreach (DialogueData.Line line in data.Lines)
            {
                if (CancelPressed())
                    break;

                if (currentBubble != null)
                    yield return currentBubble.PopOut();

                DialogueSpeakerSide displaySpeaker = DisplaySide(line.Speaker, mirrorForAudere);
                DialogueCharacterSlotView speakingSlot = displaySpeaker == DialogueSpeakerSide.Left
                    ? leftSlot
                    : rightSlot;

                if (line.PortraitOverride != null)
                {
                    if (displaySpeaker == DialogueSpeakerSide.Left)
                        leftPortrait = line.PortraitOverride;
                    else
                        rightPortrait = line.PortraitOverride;
                }

                leftSlot.SetPresentation(
                    leftCharacter,
                    displaySpeaker == DialogueSpeakerSide.Left,
                    line.Text,
                    leftPortrait,
                    displaySpeaker == DialogueSpeakerSide.Left && line.GlitchPortraitTransition);
                rightSlot.SetPresentation(
                    rightCharacter,
                    displaySpeaker == DialogueSpeakerSide.Right,
                    line.Text,
                    rightPortrait,
                    displaySpeaker == DialogueSpeakerSide.Right && line.GlitchPortraitTransition);

                TMP_Text text = speakingSlot.Bubble != null ? speakingSlot.Bubble.DialogueText : null;
                if (bubbleDelay > 0f)
                    yield return WaitUnscaled(bubbleDelay);

                if (speakingSlot.Bubble != null)
                {
                    yield return speakingSlot.Bubble.PopIn();
                    currentBubble = speakingSlot.Bubble;
                }

                if (CancelPressed())
                    break;

                if (text != null)
                    yield return TypeLine(
                        text,
                        playbackMode == DialoguePlaybackMode.AutoAdvanceNoInput
                            ? autoCharactersPerSecond
                            : charactersPerSecond);

                if (CancelPressed())
                    break;

                if (playbackMode == DialoguePlaybackMode.AutoAdvanceNoInput)
                {
                    float typedDuration = text != null
                        ? text.textInfo.characterCount / Mathf.Max(1f, autoCharactersPerSecond)
                        : 0f;
                    float totalDuration = Mathf.Max(autoMinimumLineDuration, typedDuration + .55f);
                    yield return WaitUnscaled(Mathf.Max(0f, totalDuration - typedDuration));
                    if (autoInterLineGap > 0f)
                        yield return WaitUnscaled(autoInterLineGap);
                }
                else
                {
                    while (!AdvancePressed() && !CancelPressed())
                        yield return null;
                }

                if (CancelPressed())
                    break;

                yield return null;
            }

            if (currentBubble != null)
                yield return currentBubble.PopOut();

            DialogueResult result = cancellationRequested
                ? DialogueResult.Cancelled
                : DialogueResult.Completed;

            EndPlayback(result, false);
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

        private IEnumerator TypeLine(TMP_Text text, float speed)
        {
            text.maxVisibleCharacters = 0;
            text.ForceMeshUpdate();
            int characterCount = text.textInfo.characterCount;
            float visibleCharacters = 0f;

            StartTypewriterSound();

            while (text.maxVisibleCharacters < characterCount)
            {
                if (CancelPressed())
                {
                    StopTypewriterSound();
                    yield break;
                }

                if (AdvancePressed())
                {
                    text.maxVisibleCharacters = characterCount;
                    StopTypewriterSound();
                    yield break;
                }

                visibleCharacters += Mathf.Max(1f, speed) * Time.unscaledDeltaTime;
                text.maxVisibleCharacters = Mathf.Min(characterCount, Mathf.FloorToInt(visibleCharacters));
                yield return null;
            }

            StopTypewriterSound();
        }

        private void EnsureTypewriterAudioSource()
        {
            if (typewriterAudioSource == null)
                typewriterAudioSource = gameObject.AddComponent<AudioSource>();

            typewriterAudioSource.playOnAwake = false;
            typewriterAudioSource.loop = true;
            typewriterAudioSource.spatialBlend = 0f;
        }

        private void StartTypewriterSound()
        {
            EnsureTypewriterAudioSource();

            AudioService audioService = AudioService.Instance;
            if (audioService == null ||
                !audioService.TryResolveSfx(typewriterAudioId, out AudioClip clip, out float volume))
            {
                return;
            }

            typewriterAudioSource.Stop();
            typewriterAudioSource.clip = clip;
            typewriterAudioSource.volume = volume;
            typewriterAudioSource.Play();
        }

        private void StopTypewriterSound()
        {
            if (typewriterAudioSource == null)
                return;

            typewriterAudioSource.Stop();
            typewriterAudioSource.clip = null;
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

        private void CancelPlayback()
        {
            if (playbackRoutine == null)
                return;

            EndPlayback(DialogueResult.Cancelled, true);
        }

        private void EndPlayback(DialogueResult result, bool stopCoroutine)
        {
            Coroutine routine = playbackRoutine;
            if (routine == null)
                return;

            playbackRoutine = null;
            if (stopCoroutine)
                StopCoroutine(routine);

            RestoreGameplayTime();
            HideImmediately();

            Action<DialogueResult> completion = activeCompletion;
            activeCompletion = null;
            cancellationRequested = false;
            ReleaseInputClaim();
            completion?.Invoke(result);
        }

        private void RestoreGameplayTime()
        {
            if (!ownsGameplayPause)
                return;

            Time.timeScale = timeScaleBeforeDialogue;
            ownsGameplayPause = false;
        }

        private GameplayInputGate ResolveInputGate()
        {
            GameplayUIRoot root = GetComponentInParent<GameplayUIRoot>(true);
            if (root == null)
                root = GameplayUIRoot.Instance;

            return root != null ? root.InputGate : null;
        }

        private void ReleaseInputClaim()
        {
            GameplayInputGate gate = inputGate;
            GameplayInputToken token = inputToken;
            inputGate = null;
            inputToken = default;

            if (gate != null && token.IsValid)
                gate.Release(token);
        }

        private bool HasDialogueInput()
        {
            return inputGate != null &&
                   inputGate.IsActive(inputToken) &&
                   inputGate.Allows(GameplayInputMode.Dialogue);
        }

        private bool AdvancePressed()
        {
            return HasDialogueInput() &&
                   (Input.GetMouseButtonDown(0) ||
                   Input.GetKeyDown(KeyCode.Space) ||
                   Input.GetKeyDown(KeyCode.Return) ||
                   Input.GetKeyDown(KeyCode.KeypadEnter));
        }

        private bool SkipPressed()
        {
            return HasDialogueInput() && Input.GetKeyDown(KeyCode.Escape);
        }

        private bool CancelPressed()
        {
            if (cancellationRequested || SkipPressed())
                cancellationRequested = true;

            return cancellationRequested;
        }

        private static bool ShouldMirrorForAudere(DialogueData data)
        {
            return data != null &&
                   data.RightCharacter == DialogueCharacterId.Audere &&
                   data.LeftCharacter != DialogueCharacterId.Audere;
        }

        private static DialogueSpeakerSide DisplaySide(DialogueSpeakerSide authoredSide, bool mirror)
        {
            if (!mirror)
                return authoredSide;
            return authoredSide == DialogueSpeakerSide.Left
                ? DialogueSpeakerSide.Right
                : DialogueSpeakerSide.Left;
        }
    }
}
