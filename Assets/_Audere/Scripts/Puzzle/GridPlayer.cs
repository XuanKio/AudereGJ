using System;
using System.Collections;
using System.Collections.Generic;
using Audere.Audio;
using Audere.Puzzle.Tweening;
using UnityEngine;
using UnityEngine.Rendering;

namespace Audere.Puzzle
{
    [DisallowMultipleComponent]
    public sealed class GridPlayer : MonoBehaviour
    {
        [Serializable]
        private struct FallProfile
        {
            [Min(.1f)] public float outwardCells;
            [Min(.05f)] public float dropCells;

            public FallProfile(float outward, float drop)
            {
                outwardCells = outward;
                dropCells = drop;
            }
        }

        [SerializeField, Min(0.01f)] private float stepDuration = 0.2f;
        [SerializeField, Range(.2f, 2f)] private float visualScale = 1.5f;

        [Header("Step Feel")]
        [SerializeField, Min(0f)] private float stepArcHeight = .075f;
        [SerializeField, Range(0f, .2f)] private float travelStretch = .065f;
        [SerializeField, Min(0f)] private float landingDuration = .085f;
        [SerializeField, Range(0f, .25f)] private float landingSquash = .105f;
        [SerializeField, Range(0f, .2f)] private float landingWiden = .075f;

        [Header("Falling")]
        [SerializeField, Min(.1f)] private float fallDuration = .62f;
        [Tooltip("Independent travel/drop tuning for each exit direction, measured in grid cells.")]
        [SerializeField] private FallProfile fallLeft = new FallProfile(1.2f, .85f);
        [SerializeField] private FallProfile fallRight = new FallProfile(1.2f, .85f);
        [SerializeField] private FallProfile fallUp = new FallProfile(1.05f, .75f);
        [SerializeField] private FallProfile fallDown = new FallProfile(1.25f, 1.5f);
        [SerializeField, Range(.02f, .5f)] private float fallEndScale = .08f;

        private SpriteRenderer spriteRenderer;
        private Transform groundedShadow;
        private GridSpace2D gridSpace;
        private Color prefabColor;
        private Vector3 shadowRestLocalPosition;
        private Quaternion shadowRestLocalRotation;
        private Vector3 shadowRestLocalScale;
        private Vector3 shadowRestWorldOffset;
        private Quaternion shadowRestWorldRotation;
        private Vector3 shadowRestWorldScale;
        private bool shadowStartsActive;
        private bool motionActive;
        private Vector3 motionGroundPosition;
        private SortingGroup depthGroup;
        private int authoredDepthOrder;
        public bool IsMoving => motionActive;
        public float GroundSortY => (motionActive ? motionGroundPosition.y : transform.position.y) - GetVisualOffset().y;
        public Vector2Int MotionTargetCell { get; private set; }

        public void SetStandingPresentation(Vector3 offset, int sortingOrder)
        {
            if (depthGroup != null) depthGroup.sortingOrder = sortingOrder;
            if (motionActive || gridSpace == null) return;
            Vector3 destination = gridSpace.CellToWorldCenter(GridPosition) + GetVisualOffset() + offset;
            transform.position = Vector3.MoveTowards(transform.position, destination, .7f * Time.unscaledDeltaTime);
            KeepShadowGrounded(transform.position);
        }

        public void ResetDepthOrder()
        {
            if (depthGroup != null) depthGroup.sortingOrder = authoredDepthOrder;
        }

        public void CancelMotion()
        {
            if (!motionActive) return;
            bool facing = spriteRenderer != null && spriteRenderer.flipX;
            transform.position = motionGroundPosition;
            RestoreVisualState();
            if (spriteRenderer != null) spriteRenderer.flipX = facing;
            KeepShadowGrounded(motionGroundPosition);
            motionActive = false;
        }
        public Vector2Int GridPosition { get; private set; }
        public bool FellDuringTraversal { get; private set; }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            depthGroup = GetComponent<SortingGroup>();
            if (depthGroup != null) authoredDepthOrder = depthGroup.sortingOrder;
            if (spriteRenderer == null)
            {
                Debug.LogError("[GridPlayer] Player prefab needs a SpriteRenderer.", this);
                enabled = false;
                return;
            }

            gridSpace = GetComponentInParent<GridSpace2D>();
            prefabColor = spriteRenderer.color;
            groundedShadow = FindGroundedShadow();
            CacheShadowLocalPose();
            RestoreVisualState();
        }

        public void SetPosition(Vector2Int gridPosition, Vector3 worldPosition)
        {
            motionActive = false;
            GridPosition = gridPosition;
            FellDuringTraversal = false;
            transform.position = worldPosition + GetVisualOffset();
            RestoreVisualState();
        }

        public IEnumerator Traverse(
            IReadOnlyList<Vector2Int> path,
            Board.BoardManager board,
            Action onFallStarted = null)
        {
            FellDuringTraversal = false;
            motionActive = true;
            motionGroundPosition = transform.position;

            for (int index = 1; index < path.Count; index++)
            {
                Vector2Int previousGridPosition = GridPosition;
                Vector3 start = transform.position;
                MotionTargetCell = path[index];
                var pair = board.GetComponent<CooperativePuzzleSession>();
                Vector3 destination = board.GridSpace.CellToWorldCenter(path[index]) + GetVisualOffset(board.GridSpace) +
                    (pair != null ? pair.ArrivalOffset(this, path[index]) : Vector3.zero);
                bool destinationHasTile = board.CanPlayerEnter(path[index], this);
                float elapsed = 0f;

                UpdateFacing(start, destination);
                board.NotifyPlayerExited(previousGridPosition, this);

                // Start falling from the last safe tile. Finishing a normal step
                // into empty space first would ease velocity to zero and hitch.
                if (!destinationHasTile)
                {
                    FellDuringTraversal = true;
                    GridPosition = path[index];
                    onFallStarted?.Invoke();
                    yield return PlayFall(start, destination);
                    motionActive = false;
                    yield break;
                }

                while (elapsed < stepDuration)
                {
                    elapsed += Time.deltaTime;
                    float progress = Mathf.Clamp01(elapsed / stepDuration);
                    float easedProgress = SmootherStep(progress);
                    float travelPulse = Mathf.Sin(progress * Mathf.PI);
                    Vector3 groundedPosition = Vector3.Lerp(start, destination, easedProgress);
                    transform.position = groundedPosition +
                        Vector3.up * (stepArcHeight * travelPulse);
                    transform.localScale = new Vector3(
                        visualScale * (1f - travelStretch * travelPulse * .35f),
                        visualScale * (1f + travelStretch * travelPulse),
                        visualScale);
                    KeepShadowGrounded(groundedPosition);
                    yield return null;
                }

                transform.position = destination;
                transform.localScale = Vector3.one * visualScale;
                KeepShadowGrounded(destination);
                GridPosition = path[index];
                board.NotifyPlayerEntered(GridPosition, this);
                AudioService.Instance?.Play(AudioId.Actor_Step);
                yield return PlayLandingResponse();
            }
            motionActive = false;
        }

        private IEnumerator PlayLandingResponse()
        {
            if (landingDuration <= Mathf.Epsilon)
                yield break;

            float elapsed = 0f;
            while (elapsed < landingDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / landingDuration);
                float impact = Mathf.Sin(progress * Mathf.PI);
                float rebound = Mathf.Sin(progress * Mathf.PI * 2f) *
                    (1f - progress) * .35f;

                transform.localScale = new Vector3(
                    visualScale * (1f + landingWiden * impact - rebound * .02f),
                    visualScale * (1f - landingSquash * impact + rebound * .04f),
                    visualScale);
                KeepShadowGrounded(transform.position);
                yield return null;
            }

            transform.localScale = Vector3.one * visualScale;
            KeepShadowGrounded(transform.position);
        }

        private IEnumerator PlayFall(Vector3 previousPosition, Vector3 edgePosition)
        {
            Vector3 movement = edgePosition - previousPosition;
            Vector3 direction = movement.sqrMagnitude > Mathf.Epsilon
                ? movement.normalized
                : Vector3.right;
            ApplyHorizontalFacing(direction.x);
            Vector3 fallStart = previousPosition;
            float cellWorldDistance = Mathf.Max(.01f, movement.magnitude);
            FallProfile profile = GetFallProfile(direction);
            Vector3 outwardTarget = fallStart +
                direction * (cellWorldDistance * profile.outwardCells);
            float dropDistance = cellWorldDistance * profile.dropCells;
            Vector3 startScale = transform.localScale;
            Vector3 endScale = Vector3.one * (visualScale * fallEndScale);
            Color startColor = spriteRenderer.color;

            GameplayTween tween = new GameplayTween(fallDuration)
                .OnUpdate(progress =>
                {
                    // Jump into the empty grid, then continue vertically down.
                    // The phases overlap so movement never stops between them.
                    float outwardPhase = Mathf.Clamp01(progress / .55f);
                    float dropPhase = Mathf.InverseLerp(.28f, 1f, progress);
                    float travel = GameplayTween.EaseOutCubic(outwardPhase);
                    float drop = GameplayTween.EaseInOutCubic(dropPhase);
                    float dissolve = GameplayTween.EaseOutQuadratic(progress);
                    float fade = progress;
                    float hopLift = Mathf.Sin(outwardPhase * Mathf.PI) *
                        stepArcHeight * .5f;

                    transform.position = Vector3.LerpUnclamped(fallStart, outwardTarget, travel) +
                        Vector3.up * hopLift +
                        Vector3.down * (dropDistance * drop);
                    transform.localScale = Vector3.LerpUnclamped(startScale, endScale, dissolve);
                    KeepShadowGrounded(fallStart);

                    Color color = startColor;
                    color.a = Mathf.Lerp(startColor.a, 0f, fade);
                    spriteRenderer.color = color;
                });

            yield return tween.Play();

            spriteRenderer.enabled = false;
            if (groundedShadow != null)
                groundedShadow.gameObject.SetActive(false);
        }

        private FallProfile GetFallProfile(Vector3 direction)
        {
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
                return direction.x < 0f ? fallLeft : fallRight;

            return direction.y > 0f ? fallUp : fallDown;
        }

        private void RestoreVisualState()
        {
            ResetDepthOrder();
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
                spriteRenderer.color = prefabColor;
                spriteRenderer.sortingOrder = 5;
                // The source sprite faces left. Start facing right by mirroring it.
                spriteRenderer.flipX = true;
            }

            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one * visualScale;

            if (groundedShadow != null)
            {
                groundedShadow.localPosition = shadowRestLocalPosition;
                groundedShadow.localRotation = shadowRestLocalRotation;
                groundedShadow.localScale = shadowRestLocalScale;
                groundedShadow.gameObject.SetActive(shadowStartsActive);
                CacheShadowWorldPose();
            }
        }

        private void UpdateFacing(Vector3 start, Vector3 destination)
        {
            float horizontalMovement = destination.x - start.x;
            ApplyHorizontalFacing(horizontalMovement);
        }

        private void ApplyHorizontalFacing(float horizontalDirection)
        {
            if (spriteRenderer != null && Mathf.Abs(horizontalDirection) > Mathf.Epsilon)
                spriteRenderer.flipX = horizontalDirection > 0f;
        }

        private Vector3 GetVisualOffset(GridSpace2D referenceGridSpace = null)
        {
            GridSpace2D activeGridSpace = referenceGridSpace != null
                ? referenceGridSpace
                : gridSpace;
            // Place the sprite's lowest point at the grid-cell centre. This remains
            // correct whether the sprite pivot is centred or already at its feet.
            float spriteBottom = spriteRenderer != null && spriteRenderer.sprite != null
                ? spriteRenderer.sprite.bounds.min.y * visualScale
                : 0f;
            Vector3 localOffset = Vector3.down * spriteBottom;

            return activeGridSpace != null
                ? activeGridSpace.transform.TransformVector(localOffset)
                : localOffset;
        }

        private Transform FindGroundedShadow()
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child == transform)
                    continue;

                if (child.name.StartsWith("shadow", StringComparison.OrdinalIgnoreCase))
                    return child;
            }

            return null;
        }

        private void CacheShadowLocalPose()
        {
            if (groundedShadow == null)
                return;

            shadowRestLocalPosition = groundedShadow.localPosition;
            shadowRestLocalRotation = groundedShadow.localRotation;
            shadowRestLocalScale = groundedShadow.localScale;
            shadowStartsActive = groundedShadow.gameObject.activeSelf;
        }

        private void CacheShadowWorldPose()
        {
            shadowRestWorldOffset = groundedShadow.position - transform.position;
            shadowRestWorldRotation = groundedShadow.rotation;
            shadowRestWorldScale = groundedShadow.lossyScale;
        }

        private void KeepShadowGrounded(Vector3 playerGroundPosition)
        {
            motionGroundPosition = playerGroundPosition;
            if (groundedShadow == null || !groundedShadow.gameObject.activeSelf)
                return;

            groundedShadow.position = playerGroundPosition + shadowRestWorldOffset;
            groundedShadow.rotation = shadowRestWorldRotation;

            Transform shadowParent = groundedShadow.parent;
            Vector3 parentScale = shadowParent != null
                ? shadowParent.lossyScale
                : Vector3.one;
            groundedShadow.localScale = new Vector3(
                DivideScale(shadowRestWorldScale.x, parentScale.x),
                DivideScale(shadowRestWorldScale.y, parentScale.y),
                DivideScale(shadowRestWorldScale.z, parentScale.z));
        }

        private static float DivideScale(float value, float divisor)
        {
            return Mathf.Abs(divisor) > Mathf.Epsilon ? value / divisor : value;
        }

        private static float SmootherStep(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * value * (value * (value * 6f - 15f) + 10f);
        }
    }








}
