using System.Collections.Generic;
using UnityEngine;

namespace Audere.Puzzle.PathPieces
{
    [DisallowMultipleComponent]
    public sealed class PathPreview : MonoBehaviour
    {
        public enum PresentationState
        {
            Valid,
            Invalid,
            Dangerous
        }

        [Header("World Visual References")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform connectorRoot;
        [SerializeField] private SpriteRenderer endpointA;
        [SerializeField] private SpriteRenderer endpointB;
        [SerializeField] private SpriteRenderer connectorTemplate;

        [Header("Sprites")]
        [SerializeField] private Sprite endpointSprite;
        [SerializeField] private Sprite connectorSprite;

        [Header("World Sizing")]
        [Tooltip("Endpoint size compared with one board cell.")]
        [SerializeField, Range(.5f, 1.2f)] private float endpointScaleToBoardTile = .86f;
        [Tooltip("Middle tile size compared with one board cell.")]
        [SerializeField, Range(.08f, .5f)] private float connectorScaleToBoardTile = .2f;
        [Tooltip("Preferred center-to-center gap between the small middle tiles, measured in cells.")]
        [SerializeField, Range(.12f, .75f)] private float connectorSpacingToBoardTile = .32f;
        [Tooltip("Gap between a cursor endpoint and the first middle tile, measured in cells.")]
        [SerializeField, Range(0f, .25f)] private float endpointClearanceToBoardTile = .06f;
        [SerializeField, Range(0, 64)] private int maximumConnectorCount = 32;
        [SerializeField] private int sortingOrder = 3;

        [Header("Motion")]
        [SerializeField, Min(1f)] private float positionSharpness = 20f;
        [SerializeField, Min(1f)] private float appearanceSharpness = 18f;
        [SerializeField, Range(.5f, 1f)] private float retargetStartScale = .88f;
        [SerializeField, Min(1f)] private float retargetScaleSharpness = 16f;

        [Header("State Palette")]
        [SerializeField] private Color validEndpointColor = Color.white;
        [SerializeField] private Color validConnectorColor = Color.white;
        [SerializeField] private Color invalidEndpointColor = Color.white;
        [SerializeField] private Color invalidConnectorColor = Color.white;
        [SerializeField] private Color dangerousEndpointColor = Color.white;
        [SerializeField] private Color dangerousConnectorColor = Color.white;
        [SerializeField, Range(.75f, 1.15f)] private float validVisualScale = 1f;
        [SerializeField, Range(.75f, 1.15f)] private float invalidVisualScale = .94f;
        [SerializeField, Range(.75f, 1.15f)] private float dangerousVisualScale = .98f;

        private readonly List<SpriteRenderer> connectorPool = new List<SpriteRenderer>();
        private readonly List<Vector3> targetConnectorPositions = new List<Vector3>();
        private readonly List<float> pathDistances = new List<float>();
        private readonly List<float> connectorDistances = new List<float>();
        private Vector3 targetEndpointA;
        private Vector3 targetEndpointB;
        private float targetEndpointSize;
        private float targetConnectorSize;
        private float targetAlpha;
        private float retargetScale = 1f;
        private float targetStateScale = 1f;
        private Color targetEndpointColor = Color.white;
        private Color targetConnectorColor = Color.white;
        private bool positionsInitialized;

        public PresentationState CurrentState { get; private set; }

        private void Awake()
        {
            CacheReferences();
            ApplySpritesAndSorting();
            SetVisible(false);
        }

        private void OnValidate()
        {
            CacheReferences();
            ApplySpritesAndSorting();
        }

        public void Setup()
        {
            CacheReferences();
            ApplySpritesAndSorting();
        }

        public void Show(IReadOnlyList<Vector3> worldPoints, float cellWorldSize)
        {
            if (worldPoints == null || worldPoints.Count < 2)
            {
                Clear();
                return;
            }

            CacheReferences();
            targetEndpointA = worldPoints[0];
            targetEndpointB = worldPoints[worldPoints.Count - 1];
            targetEndpointSize = Mathf.Max(.01f, cellWorldSize * endpointScaleToBoardTile);
            targetConnectorSize = Mathf.Max(.01f, cellWorldSize * connectorScaleToBoardTile);
            targetAlpha = 1f;
            retargetScale = positionsInitialized
                ? Mathf.Min(retargetScale, retargetStartScale)
                : 1f;

            targetConnectorPositions.Clear();
            BuildConnectorPositions(worldPoints, cellWorldSize);

            EnsureConnectorPool(targetConnectorPositions.Count);
            SetVisible(true);
            ApplySpritesAndSorting();
        }

        private void BuildConnectorPositions(
            IReadOnlyList<Vector3> worldPoints,
            float cellWorldSize)
        {
            pathDistances.Clear();
            float totalDistance = 0f;
            pathDistances.Add(0f);

            for (int index = 1; index < worldPoints.Count; index++)
            {
                totalDistance += Vector3.Distance(worldPoints[index - 1], worldPoints[index]);
                pathDistances.Add(totalDistance);
            }

            float endpointRadius = targetEndpointSize * .5f;
            float clearance = cellWorldSize * endpointClearanceToBoardTile;
            float usableStart = Mathf.Min(totalDistance * .5f, endpointRadius + clearance);
            float usableEnd = Mathf.Max(totalDistance * .5f, totalDistance - endpointRadius - clearance);
            float usableLength = Mathf.Max(0f, usableEnd - usableStart);
            float preferredSpacing = Mathf.Max(.01f, cellWorldSize * connectorSpacingToBoardTile);
            int connectorCount = Mathf.Clamp(
                Mathf.RoundToInt(usableLength / preferredSpacing),
                0,
                maximumConnectorCount);

            connectorDistances.Clear();
            for (int index = 0; index < connectorCount; index++)
            {
                float normalized = (index + 1f) / (connectorCount + 1f);
                float distance = Mathf.Lerp(usableStart, usableEnd, normalized);
                connectorDistances.Add(distance);
            }

            // A uniformly sampled polyline can miss an exact 90-degree vertex and make an L
            // piece look rounded or diagonal. Snap the nearest sample to every authored bend;
            // if there is room, add a dedicated corner sample instead.
            for (int index = 1; index < pathDistances.Count - 1; index++)
            {
                float cornerDistance = pathDistances[index];
                if (cornerDistance <= usableStart || cornerDistance >= usableEnd)
                    continue;

                int nearestIndex = FindNearestDistanceIndex(connectorDistances, cornerDistance);
                if (nearestIndex >= 0 &&
                    Mathf.Abs(connectorDistances[nearestIndex] - cornerDistance) <= preferredSpacing * .55f)
                {
                    connectorDistances[nearestIndex] = cornerDistance;
                }
                else if (connectorDistances.Count < maximumConnectorCount)
                {
                    connectorDistances.Add(cornerDistance);
                }
            }

            connectorDistances.Sort();
            foreach (float distance in connectorDistances)
                targetConnectorPositions.Add(SamplePolyline(worldPoints, distance));
        }

        private static int FindNearestDistanceIndex(IReadOnlyList<float> distances, float target)
        {
            int nearestIndex = -1;
            float nearestDelta = float.PositiveInfinity;
            for (int index = 0; index < distances.Count; index++)
            {
                float delta = Mathf.Abs(distances[index] - target);
                if (delta >= nearestDelta)
                    continue;

                nearestDelta = delta;
                nearestIndex = index;
            }

            return nearestIndex;
        }

        private Vector3 SamplePolyline(IReadOnlyList<Vector3> points, float distance)
        {
            for (int index = 1; index < pathDistances.Count; index++)
            {
                if (distance > pathDistances[index])
                    continue;

                float startDistance = pathDistances[index - 1];
                float segmentLength = pathDistances[index] - startDistance;
                float progress = segmentLength <= Mathf.Epsilon
                    ? 0f
                    : (distance - startDistance) / segmentLength;
                return Vector3.Lerp(points[index - 1], points[index], progress);
            }

            return points[points.Count - 1];
        }

        public void SetState(PresentationState state)
        {
            CurrentState = state;
            switch (state)
            {
                case PresentationState.Invalid:
                    targetEndpointColor = invalidEndpointColor;
                    targetConnectorColor = invalidConnectorColor;
                    targetStateScale = invalidVisualScale;
                    break;
                case PresentationState.Dangerous:
                    targetEndpointColor = dangerousEndpointColor;
                    targetConnectorColor = dangerousConnectorColor;
                    targetStateScale = dangerousVisualScale;
                    break;
                default:
                    targetEndpointColor = validEndpointColor;
                    targetConnectorColor = validConnectorColor;
                    targetStateScale = validVisualScale;
                    break;
            }
        }

        public void Clear()
        {
            targetAlpha = 0f;
            // Show and drop/cancel can happen before the first LateUpdate.
            // In that case there is no initialized fade to finish hiding the endpoints.
            if (!positionsInitialized) SetVisible(false);
        }

        private void LateUpdate()
        {
            float deltaTime = Time.unscaledDeltaTime;
            float positionBlend = 1f - Mathf.Exp(-positionSharpness * deltaTime);
            float appearanceBlend = 1f - Mathf.Exp(-appearanceSharpness * deltaTime);
            float scaleBlend = 1f - Mathf.Exp(-retargetScaleSharpness * deltaTime);
            retargetScale = Mathf.Lerp(retargetScale, 1f, scaleBlend);
            float presentationScale = retargetScale * targetStateScale;

            if (targetAlpha <= 0f && !positionsInitialized)
                return;

            if (endpointA != null)
                SmoothRenderer(
                    endpointA,
                    targetEndpointA,
                    targetEndpointSize * presentationScale,
                    targetEndpointColor,
                    positionBlend,
                    appearanceBlend);
            if (endpointB != null)
                SmoothRenderer(
                    endpointB,
                    targetEndpointB,
                    targetEndpointSize * presentationScale,
                    targetEndpointColor,
                    positionBlend,
                    appearanceBlend);

            for (int index = 0; index < connectorPool.Count; index++)
            {
                bool active = index < targetConnectorPositions.Count;
                connectorPool[index].gameObject.SetActive(active);
                if (active)
                    SmoothRenderer(
                        connectorPool[index],
                        targetConnectorPositions[index],
                        targetConnectorSize * presentationScale,
                        targetConnectorColor,
                        positionBlend,
                        appearanceBlend);
            }

            if (targetAlpha <= 0f &&
                endpointA != null &&
                endpointA.color.a < .01f)
            {
                SetVisible(false);
                positionsInitialized = false;
                return;
            }

            positionsInitialized = true;
        }

        private void SmoothRenderer(
            SpriteRenderer renderer,
            Vector3 targetPosition,
            float targetSize,
            Color targetColor,
            float positionBlend,
            float appearanceBlend)
        {
            if (renderer.sprite == null)
                return;

            Vector2 spriteSize = renderer.sprite.bounds.size;
            Vector3 parentScale = renderer.transform.parent != null
                ? renderer.transform.parent.lossyScale
                : Vector3.one;
            Vector3 desiredScale = new Vector3(
                targetSize /
                    (Mathf.Max(.0001f, spriteSize.x) * Mathf.Max(.0001f, Mathf.Abs(parentScale.x))),
                targetSize /
                    (Mathf.Max(.0001f, spriteSize.y) * Mathf.Max(.0001f, Mathf.Abs(parentScale.y))),
                1f);
            Vector3 localCenterOffset = Vector3.Scale(
                renderer.sprite.bounds.center,
                desiredScale);
            Vector3 worldCenterOffset = renderer.transform.parent != null
                ? renderer.transform.parent.TransformVector(localCenterOffset)
                : localCenterOffset;
            Vector3 centeredPosition = targetPosition - worldCenterOffset;
            renderer.transform.position = positionsInitialized
                ? Vector3.Lerp(renderer.transform.position, centeredPosition, positionBlend)
                : centeredPosition;
            renderer.transform.localScale = positionsInitialized
                ? Vector3.Lerp(renderer.transform.localScale, desiredScale, appearanceBlend)
                : desiredScale;

            Color desiredColor = targetColor;
            desiredColor.a *= targetAlpha;
            renderer.color = positionsInitialized
                ? Color.Lerp(renderer.color, desiredColor, appearanceBlend)
                : desiredColor;
        }

        private void EnsureConnectorPool(int count)
        {
            if (connectorTemplate == null)
                return;

            while (connectorPool.Count < count)
            {
                SpriteRenderer connector = Instantiate(
                    connectorTemplate,
                    connectorRoot != null ? connectorRoot : visualRoot);
                connector.name = $"Middle Tile {connectorPool.Count + 1:00}";
                connector.gameObject.SetActive(true);
                connectorPool.Add(connector);
            }
        }

        private void SetVisible(bool visible)
        {
            if (endpointA != null) endpointA.gameObject.SetActive(visible);
            if (endpointB != null) endpointB.gameObject.SetActive(visible);
            foreach (SpriteRenderer connector in connectorPool)
                if (connector != null)
                    connector.gameObject.SetActive(visible);
        }

        private void ApplySpritesAndSorting()
        {
            ApplyRenderer(endpointA, endpointSprite);
            ApplyRenderer(endpointB, endpointSprite);
            ApplyRenderer(connectorTemplate, connectorSprite);
            foreach (SpriteRenderer connector in connectorPool)
                ApplyRenderer(connector, connectorSprite);
        }

        private void ApplyRenderer(SpriteRenderer renderer, Sprite sprite)
        {
            if (renderer == null)
                return;

            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
        }

        private void CacheReferences()
        {
            if (visualRoot == null) visualRoot = transform;
            if (connectorRoot == null) connectorRoot = visualRoot;
            if (endpointA == null)
            {
                Transform endpoint = visualRoot.Find("Endpoint A");
                if (endpoint != null) endpointA = endpoint.GetComponent<SpriteRenderer>();
            }
            if (endpointB == null)
            {
                Transform endpoint = visualRoot.Find("Endpoint B");
                if (endpoint != null) endpointB = endpoint.GetComponent<SpriteRenderer>();
            }
            if (connectorTemplate == null)
            {
                Transform template = visualRoot.Find("Middle Tile Template");
                if (template != null)
                    connectorTemplate = template.GetComponent<SpriteRenderer>();
            }
        }
    }
}
