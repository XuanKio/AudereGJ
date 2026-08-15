using System;
using System.Collections;
using System.Collections.Generic;
using Audere.Puzzle.Tweening;
using UnityEngine;

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
        private GridSpace2D gridSpace;
        private Color prefabColor;
        public Vector2Int GridPosition { get; private set; }
        public bool FellDuringTraversal { get; private set; }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogError("[GridPlayer] Player prefab needs a SpriteRenderer.", this);
                enabled = false;
                return;
            }

            gridSpace = GetComponentInParent<GridSpace2D>();
            prefabColor = spriteRenderer.color;
            RestoreVisualState();
        }

        public void SetPosition(Vector2Int gridPosition, Vector3 worldPosition)
        {
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

            for (int index = 1; index < path.Count; index++)
            {
                Vector2Int previousGridPosition = GridPosition;
                Vector3 start = transform.position;
                Vector3 destination = board.GridSpace.CellToWorldCenter(path[index]) +
                    GetVisualOffset(board.GridSpace);
                bool destinationHasTile = board.HasTile(path[index]);
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
                    yield break;
                }

                while (elapsed < stepDuration)
                {
                    elapsed += Time.deltaTime;
                    float progress = Mathf.Clamp01(elapsed / stepDuration);
                    float easedProgress = SmootherStep(progress);
                    float travelPulse = Mathf.Sin(progress * Mathf.PI);
                    transform.position = Vector3.Lerp(start, destination, easedProgress) +
                        Vector3.up * (stepArcHeight * travelPulse);
                    transform.localScale = new Vector3(
                        visualScale * (1f - travelStretch * travelPulse * .35f),
                        visualScale * (1f + travelStretch * travelPulse),
                        visualScale);
                    yield return null;
                }

                transform.position = destination;
                transform.localScale = Vector3.one * visualScale;
                GridPosition = path[index];
                board.NotifyPlayerEntered(GridPosition, this);
                yield return PlayLandingResponse();
            }
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
                yield return null;
            }

            transform.localScale = Vector3.one * visualScale;
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

                    Color color = startColor;
                    color.a = Mathf.Lerp(startColor.a, 0f, fade);
                    spriteRenderer.color = color;
                });

            yield return tween.Play();

            spriteRenderer.enabled = false;
        }

        private FallProfile GetFallProfile(Vector3 direction)
        {
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
                return direction.x < 0f ? fallLeft : fallRight;

            return direction.y > 0f ? fallUp : fallDown;
        }

        private void RestoreVisualState()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
                spriteRenderer.color = prefabColor;
                // The source sprite faces left. Start facing right by mirroring it.
                spriteRenderer.flipX = true;
            }

            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one * visualScale;
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

        private static float SmootherStep(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * value * (value * (value * 6f - 15f) + 10f);
        }
    }

}
