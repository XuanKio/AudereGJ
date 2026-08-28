using System.Collections;
using System.Collections.Generic;
using Audere.Audio;
using Audere.Puzzle;
using Audere.Puzzle.Board;
using UnityEngine;
using UnityEngine.Serialization;

namespace Audere.Story.Steps
{
    /// <summary>
    /// Small scene-authored transition between StepTile boards. References stay
    /// visible in the hierarchy and no puzzle layout is generated at runtime.
    /// </summary>
    public sealed class BoardTileTransitionStep : StoryStep
    {
        [Header("Puzzle Root Coordination")]
        [SerializeField] private PuzzleRootCoordinator puzzleRootCoordinator;
        [SerializeField] private bool autoCollectHideTiles;
        [SerializeField] private bool autoCollectRevealTiles;

        [Header("Hide Current Board")]
        [SerializeField] private List<Transform> objectsToHide = new List<Transform>();
        [SerializeField] private GameObject rootToDisableAfterHide;
        [SerializeField] private PuzzleController sourcePuzzle;
        [SerializeField] private GoalTileBehaviour goalToBecomeAnchor;

        [Header("Reveal Next Board")]
        [SerializeField] private GameObject rootToEnableBeforeReveal;
        [SerializeField] private List<Transform> objectsToReveal = new List<Transform>();
        [SerializeField] private List<Transform> objectsToKeepHidden = new List<Transform>();
        [SerializeField] private PuzzleController revealPuzzle;
        [SerializeField] private Transform revealFromAnchor;

        [Header("Deterministic Cancellation")]
        [SerializeField] private PuzzleSequencePrepareStep normalizeOnCancel;

        [Header("Motion Style")]
        [FormerlySerializedAs("flipDuration")]
        [SerializeField, Min(0f)] private float transitionDuration = .22f;
        [SerializeField, Min(0f)] private float staggerDelay = .04f;
        [Tooltip("Total time from the first tile appearing until the whole board has settled.")]
        [SerializeField, Min(0f)] private float revealWaveDuration = .95f;
        [Tooltip("How far below its authored position a tile starts/ends.")]
        [SerializeField, Min(0f)] private float verticalOffset = .08f;
        [Tooltip("Subtle scale overshoot used when a tile appears. 0.015 = 1.5%.")]
        [SerializeField, Range(0f, .05f)] private float revealOvershoot = .015f;
        [SerializeField] private bool useUnscaledTime = true;

        [Header("Audio")]
        [SerializeField] private AudioId tileTransitionAudioId = AudioId.Tile_Pop;
        [SerializeField, Min(0f)] private float tileSoundInterval = .11f;

        private float nextTileSoundTime;

        private Transform activeTransform;
        private Vector3 activeAuthoredScale;
        private Vector3 activeAuthoredLocalPosition;
        private readonly List<SpriteRenderer> activeRenderers = new List<SpriteRenderer>();
        private readonly List<Color> activeRendererColors = new List<Color>();
        private readonly List<RevealVisualState> activeRevealStates = new List<RevealVisualState>();

        private sealed class RevealVisualState
        {
            public Transform Target;
            public Vector3 AuthoredScale;
            public Vector3 AuthoredLocalPosition;
            public readonly List<SpriteRenderer> Renderers = new List<SpriteRenderer>();
            public readonly List<Color> RendererColors = new List<Color>();
        }

        protected override IEnumerator Execute()
        {
            nextTileSoundTime = float.NegativeInfinity;
            List<Transform> hideTargets = ResolveTargets(
                sourcePuzzle,
                objectsToHide,
                autoCollectHideTiles,
                true,
                goalToBecomeAnchor != null ? goalToBecomeAnchor.transform : null);
            if (hideTargets == null)
            {
                FailStep();
                yield break;
            }

            if (sourcePuzzle != null)
            {
                if (!sourcePuzzle.BeginCollapse())
                {
                    Debug.LogError("[BoardTileTransitionStep] Source puzzle cannot collapse while it is playing.", this);
                    FailStep();
                    yield break;
                }
                sourcePuzzle.Exit(false);
            }

            foreach (Transform target in hideTargets)
            {
                if (target == null || !target.gameObject.activeSelf)
                    continue;

                Vector3 authoredScale = target.localScale;
                Vector3 authoredLocalPosition = target.localPosition;
                yield return AnimateHide(target, authoredScale, authoredLocalPosition);
                target.gameObject.SetActive(false);
                target.localScale = authoredScale;
                target.localPosition = authoredLocalPosition;
                yield return WaitForStagger();
            }

            if (goalToBecomeAnchor != null)
            {
                goalToBecomeAnchor.BecomeTransitionAnchor();
                puzzleRootCoordinator?.CaptureTransitionAnchor(
                    sourcePuzzle,
                    goalToBecomeAnchor.transform);
            }

            if (rootToDisableAfterHide != null)
                rootToDisableAfterHide.SetActive(false);

            if (revealPuzzle != null)
            {
                bool reset = puzzleRootCoordinator != null
                    ? puzzleRootCoordinator.ActivateForReveal(revealPuzzle, false)
                    : revealPuzzle.ResetToInitialState(true, false);
                if (!reset)
                {
                    FailStep();
                    yield break;
                }

                if (revealFromAnchor != null &&
                    !revealPuzzle.AlignPlayerStartToAnchor(revealFromAnchor))
                {
                    Debug.LogError("[BoardTileTransitionStep] Could not align reveal puzzle to anchor.", this);
                    FailStep();
                    yield break;
                }

                if (!revealPuzzle.BeginReveal())
                {
                    Debug.LogError("[BoardTileTransitionStep] Reveal puzzle is not ready to reveal.", this);
                    FailStep();
                    yield break;
                }
            }

            List<Transform> revealTargets = ResolveTargets(
                revealPuzzle,
                objectsToReveal,
                autoCollectRevealTiles,
                false,
                null);
            if (revealTargets == null)
            {
                FailStep();
                yield break;
            }

            List<Vector3> revealScales = new List<Vector3>(revealTargets.Count);
            foreach (Transform target in revealTargets)
            {
                revealScales.Add(target != null ? target.localScale : Vector3.one);
                if (target != null)
                    target.gameObject.SetActive(false);
            }

            if (rootToEnableBeforeReveal != null)
                rootToEnableBeforeReveal.SetActive(true);

            foreach (Transform target in objectsToKeepHidden)
                if (target != null)
                    target.gameObject.SetActive(false);

            yield return AnimateRevealWave(revealTargets, revealScales);

            sourcePuzzle?.CompleteCollapse();
        }

        private List<Transform> ResolveTargets(
            PuzzleController puzzle,
            List<Transform> authoredTargets,
            bool autoCollect,
            bool reverse,
            Transform excluded)
        {
            if (!autoCollect)
                return authoredTargets;

            if (puzzleRootCoordinator == null)
            {
                Debug.LogError(
                    $"[BoardTileTransitionStep] '{name}' enables automatic tile order but has no PuzzleRootCoordinator.",
                    this);
                return null;
            }

            List<Transform> resolved = new List<Transform>();
            if (!puzzleRootCoordinator.TryBuildTileOrder(
                    puzzle,
                    reverse,
                    excluded,
                    resolved))
            {
                Debug.LogError($"[BoardTileTransitionStep] '{name}' could not collect puzzle tiles.", this);
                return null;
            }

            return resolved;
        }

        protected override void OnCancelled()
        {
            if (activeTransform != null)
            {
                activeTransform.localScale = activeAuthoredScale;
                activeTransform.localPosition = activeAuthoredLocalPosition;
                RestoreActiveRendererColors();
            }

            ClearActiveVisualState();
            RestoreRevealVisualStates();
            if (isActiveAndEnabled && normalizeOnCancel != null)
                normalizeOnCancel.NormalizeAfterCancel();
        }

        private IEnumerator AnimateHide(
            Transform target,
            Vector3 authoredScale,
            Vector3 authoredLocalPosition)
        {
            TryPlayTileTransitionSound();
            CaptureVisualState(target, authoredScale, authoredLocalPosition);
            Vector3 hiddenLocalPosition = authoredLocalPosition + Vector3.down * verticalOffset;

            if (transitionDuration <= 0f)
            {
                SetActiveRenderersAlpha(0f);
                target.localScale = authoredScale * .97f;
                target.localPosition = hiddenLocalPosition;
                RestoreActiveRendererColors();
                ClearActiveVisualState();
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / transitionDuration);
                float eased = SmoothStep(progress);
                float scaleMultiplier = progress < .22f
                    ? Mathf.LerpUnclamped(1f, 1f + revealOvershoot * .45f, SmoothStep(progress / .22f))
                    : Mathf.LerpUnclamped(1f + revealOvershoot * .45f, .97f, SmoothStep((progress - .22f) / .78f));

                SetActiveRenderersAlpha(1f - eased);
                target.localScale = authoredScale * scaleMultiplier;
                target.localPosition = Vector3.LerpUnclamped(
                    authoredLocalPosition,
                    hiddenLocalPosition,
                    eased);
                yield return null;
            }

            SetActiveRenderersAlpha(0f);
            target.localScale = authoredScale * .97f;
            target.localPosition = hiddenLocalPosition;
            RestoreActiveRendererColors();
            ClearActiveVisualState();
        }

        private IEnumerator AnimateRevealWave(
            IReadOnlyList<Transform> targets,
            IReadOnlyList<Vector3> authoredScales)
        {
            activeRevealStates.Clear();
            for (int index = 0; index < targets.Count; index++)
            {
                Transform target = targets[index];
                if (target == null)
                    continue;

                RevealVisualState state = new RevealVisualState
                {
                    Target = target,
                    AuthoredScale = authoredScales[index],
                    AuthoredLocalPosition = target.localPosition,
                };
                foreach (SpriteRenderer renderer in target.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    state.Renderers.Add(renderer);
                    state.RendererColors.Add(renderer.color);
                }

                target.localPosition = state.AuthoredLocalPosition + Vector3.down * verticalOffset;
                target.localScale = state.AuthoredScale * .96f;
                SetRenderersAlpha(state, 0f);
                activeRevealStates.Add(state);
            }

            if (activeRevealStates.Count == 0)
                yield break;

            if (transitionDuration <= 0f || revealWaveDuration <= 0f)
            {
                TryPlayTileTransitionSound();
                RestoreRevealVisualStates();
                yield break;
            }

            float tileDuration = Mathf.Min(transitionDuration, revealWaveDuration);
            float startWindow = Mathf.Max(0f, revealWaveDuration - tileDuration);
            float elapsed = 0f;
            while (elapsed < revealWaveDuration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                for (int index = 0; index < activeRevealStates.Count; index++)
                {
                    RevealVisualState state = activeRevealStates[index];
                    if (state.Target == null)
                        continue;

                    float startTime = activeRevealStates.Count > 1
                        ? startWindow * index / (activeRevealStates.Count - 1f)
                        : 0f;
                    float progress = Mathf.Clamp01((elapsed - startTime) / tileDuration);
                    if (progress <= 0f)
                        continue;

                    if (!state.Target.gameObject.activeSelf)
                    {
                        state.Target.gameObject.SetActive(true);
                        TryPlayTileTransitionSound();
                    }
                    ApplyRevealProgress(state, progress);
                }
                yield return null;
            }

            RestoreRevealVisualStates();
        }

        private void ApplyRevealProgress(RevealVisualState state, float progress)
        {
            Vector3 startLocalPosition =
                state.AuthoredLocalPosition + Vector3.down * verticalOffset;
            Vector3 overshootLocalPosition =
                state.AuthoredLocalPosition + Vector3.up * (verticalOffset * .08f);
            float alphaProgress = 1f - Mathf.Pow(1f - progress, 2f);

            if (progress < .78f)
            {
                float riseProgress = EaseOutCubic(progress / .78f);
                state.Target.localPosition = Vector3.LerpUnclamped(
                    startLocalPosition,
                    overshootLocalPosition,
                    riseProgress);
                state.Target.localScale = state.AuthoredScale * Mathf.LerpUnclamped(
                    .96f,
                    1f + revealOvershoot,
                    riseProgress);
            }
            else
            {
                float settleProgress = SmoothStep((progress - .78f) / .22f);
                state.Target.localPosition = Vector3.LerpUnclamped(
                    overshootLocalPosition,
                    state.AuthoredLocalPosition,
                    settleProgress);
                state.Target.localScale = state.AuthoredScale * Mathf.LerpUnclamped(
                    1f + revealOvershoot,
                    1f,
                    settleProgress);
            }

            SetRenderersAlpha(state, alphaProgress);
        }

        private static void SetRenderersAlpha(RevealVisualState state, float multiplier)
        {
            multiplier = Mathf.Clamp01(multiplier);
            for (int index = 0; index < state.Renderers.Count; index++)
            {
                SpriteRenderer renderer = state.Renderers[index];
                if (renderer == null)
                    continue;

                Color color = state.RendererColors[index];
                color.a *= multiplier;
                renderer.color = color;
            }
        }

        private void RestoreRevealVisualStates()
        {
            foreach (RevealVisualState state in activeRevealStates)
            {
                if (state.Target != null)
                {
                    state.Target.localScale = state.AuthoredScale;
                    state.Target.localPosition = state.AuthoredLocalPosition;
                    state.Target.gameObject.SetActive(true);
                }

                for (int index = 0; index < state.Renderers.Count; index++)
                    if (state.Renderers[index] != null)
                        state.Renderers[index].color = state.RendererColors[index];
            }

            activeRevealStates.Clear();
        }

        private void CaptureVisualState(
            Transform target,
            Vector3 authoredScale,
            Vector3 authoredLocalPosition)
        {
            activeTransform = target;
            activeAuthoredScale = authoredScale;
            activeAuthoredLocalPosition = authoredLocalPosition;
            activeRenderers.Clear();
            activeRendererColors.Clear();

            foreach (SpriteRenderer renderer in target.GetComponentsInChildren<SpriteRenderer>(true))
            {
                activeRenderers.Add(renderer);
                activeRendererColors.Add(renderer.color);
            }
        }

        private void SetActiveRenderersAlpha(float multiplier)
        {
            multiplier = Mathf.Clamp01(multiplier);
            for (int index = 0; index < activeRenderers.Count; index++)
            {
                SpriteRenderer renderer = activeRenderers[index];
                if (renderer == null)
                    continue;

                Color color = activeRendererColors[index];
                color.a *= multiplier;
                renderer.color = color;
            }
        }

        private void RestoreActiveRendererColors()
        {
            for (int index = 0; index < activeRenderers.Count; index++)
                if (activeRenderers[index] != null)
                    activeRenderers[index].color = activeRendererColors[index];
        }

        private void ClearActiveVisualState()
        {
            activeTransform = null;
            activeRenderers.Clear();
            activeRendererColors.Clear();
        }

        private void TryPlayTileTransitionSound()
        {
            if (tileTransitionAudioId == AudioId.None)
                return;

            float now = Time.unscaledTime;
            if (now < nextTileSoundTime)
                return;

            AudioService.Instance?.Play(tileTransitionAudioId);
            nextTileSoundTime = now + tileSoundInterval;
        }

        private static float SmoothStep(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private static float EaseOutCubic(float value)
        {
            float inverse = 1f - Mathf.Clamp01(value);
            return 1f - inverse * inverse * inverse;
        }

        private IEnumerator WaitForStagger()
        {
            if (staggerDelay <= 0f)
                yield break;

            float elapsed = 0f;
            while (elapsed < staggerDelay)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }
        }
    }
}
