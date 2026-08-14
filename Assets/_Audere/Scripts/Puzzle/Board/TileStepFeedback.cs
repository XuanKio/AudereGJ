using System.Collections;
using UnityEngine;

namespace Audere.Puzzle.Board
{
    /// <summary>
    /// Prefab-owned landing feedback shared by walkable tile types.
    /// Gameplay only sends enter/exit events; each tile prefab owns its motion tuning.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TileStepFeedback : MonoBehaviour, IBoardTileBehaviour
    {
        [Header("Prefab References")]
        [SerializeField] private Transform visualRoot;

        [Header("Impact")]
        [SerializeField, Min(.01f)] private float pressDuration = .055f;
        [SerializeField, Min(0f)] private float pressDistance = .065f;
        [SerializeField, Range(0f, .2f)] private float pressWiden = .045f;
        [SerializeField, Range(0f, .25f)] private float pressSquash = .09f;

        [Header("Rebound")]
        [SerializeField, Min(.01f)] private float reboundDuration = .085f;
        [SerializeField, Min(0f)] private float reboundDistance = .018f;
        [SerializeField, Range(0f, .15f)] private float reboundNarrow = .018f;
        [SerializeField, Range(0f, .15f)] private float reboundStretch = .035f;
        [SerializeField, Min(.01f)] private float settleDuration = .14f;

        private Coroutine feedbackRoutine;
        private Vector3 homeLocalPosition;
        private Vector3 homeLocalScale;
        private bool initialized;

        public Transform VisualRoot => visualRoot;

        public void ConfigurePrefab(Transform root)
        {
            visualRoot = root;
            CaptureHomePose();
        }

        public void OnTileInitialized(BoardTile tile)
        {
            CaptureHomePose();
            RestorePose();
        }

        public void OnPlayerEntered(BoardTile tile, GridPlayer player)
        {
            if (!isActiveAndEnabled || visualRoot == null)
                return;

            if (feedbackRoutine != null)
                StopCoroutine(feedbackRoutine);
            feedbackRoutine = StartCoroutine(PlayStepResponse());
        }

        public void OnPlayerExited(BoardTile tile, GridPlayer player) { }

        private void Awake()
        {
            CaptureHomePose();
        }

        private void OnDisable()
        {
            feedbackRoutine = null;
            RestorePose();
        }

        private IEnumerator PlayStepResponse()
        {
            Vector3 startPosition = visualRoot.localPosition;
            Vector3 startScale = visualRoot.localScale;
            Vector3 pressedPosition = homeLocalPosition + Vector3.down * pressDistance;
            Vector3 pressedScale = Vector3.Scale(
                homeLocalScale,
                new Vector3(1f + pressWiden, 1f - pressSquash, 1f));

            yield return AnimatePose(
                startPosition,
                pressedPosition,
                startScale,
                pressedScale,
                pressDuration,
                EaseOutCubic);

            Vector3 reboundPosition = homeLocalPosition + Vector3.up * reboundDistance;
            Vector3 reboundScale = Vector3.Scale(
                homeLocalScale,
                new Vector3(1f - reboundNarrow, 1f + reboundStretch, 1f));

            yield return AnimatePose(
                pressedPosition,
                reboundPosition,
                pressedScale,
                reboundScale,
                reboundDuration,
                EaseOutBack);

            yield return AnimatePose(
                reboundPosition,
                homeLocalPosition,
                reboundScale,
                homeLocalScale,
                settleDuration,
                SmoothStep);

            RestorePose();
            feedbackRoutine = null;
        }

        private IEnumerator AnimatePose(
            Vector3 fromPosition,
            Vector3 toPosition,
            Vector3 fromScale,
            Vector3 toScale,
            float duration,
            System.Func<float, float> easing)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = duration <= Mathf.Epsilon
                    ? 1f
                    : Mathf.Clamp01(elapsed / duration);
                float eased = easing(progress);
                visualRoot.localPosition = Vector3.LerpUnclamped(fromPosition, toPosition, eased);
                visualRoot.localScale = Vector3.LerpUnclamped(fromScale, toScale, eased);
                yield return null;
            }

            visualRoot.localPosition = toPosition;
            visualRoot.localScale = toScale;
        }

        private void CaptureHomePose()
        {
            if (visualRoot == null)
                return;

            homeLocalPosition = visualRoot.localPosition;
            homeLocalScale = visualRoot.localScale;
            initialized = true;
        }

        private void RestorePose()
        {
            if (!initialized || visualRoot == null)
                return;

            visualRoot.localPosition = homeLocalPosition;
            visualRoot.localScale = homeLocalScale;
        }

        private static float EaseOutCubic(float value)
        {
            float inverse = 1f - Mathf.Clamp01(value);
            return 1f - inverse * inverse * inverse;
        }

        private static float EaseOutBack(float value)
        {
            float shifted = Mathf.Clamp01(value) - 1f;
            const float overshoot = 1.70158f;
            return 1f + (overshoot + 1f) * shifted * shifted * shifted +
                overshoot * shifted * shifted;
        }

        private static float SmoothStep(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }
    }
}
