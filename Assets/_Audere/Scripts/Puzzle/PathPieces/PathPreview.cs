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
        [SerializeField, Range(.08f, .5f)] private float connectorScaleToBoardTile = .14f;
        [Tooltip("Preferred center-to-center gap between the small middle tiles, measured in cells.")]
        [SerializeField, Range(.12f, .75f)] private float connectorSpacingToBoardTile = .25f;
        [Tooltip("Gap between a cursor endpoint and the first middle tile, measured in cells.")]
        [SerializeField, Range(0f, .25f)] private float endpointClearanceToBoardTile = .06f;
        [SerializeField, Range(0, 64)] private int maximumConnectorCount = 32;
        [SerializeField] private int sortingOrder = 3;

        [Header("Motion")]
        [SerializeField, Min(1f)] private float positionSharpness = 20f;
        [Tooltip("How quickly a rotated path settles into its next 90-degree orientation.")]
        [SerializeField, Min(1f)] private float rotationSharpness = 14f;
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
        private readonly List<Vector3> canonicalShapeOffsets = new List<Vector3>();
        private readonly HashSet<SpriteRenderer> initializedConnectors = new HashSet<SpriteRenderer>();
        private Vector3 targetEndpointA;
        private Vector3 targetEndpointB;
        private float targetEndpointSize;
        private float targetConnectorSize;
        private float targetAlpha;
        private float retargetScale = 1f;
        private float targetStateScale = 1f;
        private Color targetEndpointColor = Color.white;
        private Color targetConnectorColor = Color.white;
        private float targetRotationDegrees;
        private float displayedRotationDegrees;
        private bool rotationInitialized;
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
            Show(worldPoints, cellWorldSize, GridRotation.Degrees0, true);
        }

        public void Show(
            IReadOnlyList<Vector3> worldPoints,
            float cellWorldSize,
            GridRotation rotation,
            bool animateRotation = true)
        {
            if (worldPoints == null || worldPoints.Count < 2)
            {
                Clear();
                return;
            }

            CacheReferences();
            bool wasVisible = positionsInitialized;
            float nextRotationDegrees = (int)rotation * 90f;
            bool shapeChanged = ShapeChanged(worldPoints, nextRotationDegrees);
            if (shapeChanged)
            {
                // A genuinely different piece must not morph old connector indices through
                // unrelated corners. Quarter-turns are normalized out by ShapeChanged and
                // remain eligible for the coherent rotation tween below.
                positionsInitialized = false;
                initializedConnectors.Clear();
            }
            CacheCanonicalShape(worldPoints, nextRotationDegrees);
            if (!rotationInitialized || !wasVisible || shapeChanged || !animateRotation)
            {
                displayedRotationDegrees = nextRotationDegrees;
                rotationInitialized = true;
            }
            targetRotationDegrees = nextRotationDegrees;
            targetEndpointA = worldPoints[0];
            targetEndpointB = worldPoints[worldPoints.Count - 1];
            targetEndpointSize = Mathf.Max(.01f, cellWorldSize * endpointScaleToBoardTile);
            targetConnectorSize = Mathf.Max(.01f, cellWorldSize * connectorScaleToBoardTile);
            targetAlpha = 1f;
            retargetScale = wasVisible
                ? Mathf.Min(retargetScale, retargetStartScale)
                : 1f;

            targetConnectorPositions.Clear();
            BuildConnectorPositions(worldPoints, cellWorldSize);

            EnsureConnectorPool(targetConnectorPositions.Count);
            SetVisible(true);
            ApplySpritesAndSorting();
        }

        private bool ShapeChanged(IReadOnlyList<Vector3> points, float rotationDegrees)
        {
            if (canonicalShapeOffsets.Count != points.Count) return true;
            Quaternion inverseRotation = Quaternion.Euler(0f, 0f, -rotationDegrees);
            for (int i = 1; i < points.Count; i++)
            {
                Vector3 canonicalOffset = inverseRotation * (points[i] - points[0]);
                if ((canonicalOffset - canonicalShapeOffsets[i]).sqrMagnitude > .00000001f)
                    return true;
            }
            return false;
        }

        private void CacheCanonicalShape(IReadOnlyList<Vector3> points, float rotationDegrees)
        {
            canonicalShapeOffsets.Clear();
            Quaternion inverseRotation = Quaternion.Euler(0f, 0f, -rotationDegrees);
            for (int i = 0; i < points.Count; i++)
                canonicalShapeOffsets.Add(inverseRotation * (points[i] - points[0]));
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

            if (totalDistance <= Mathf.Epsilon || maximumConnectorCount <= 0) return;
            // Include each square's half width so its edge never crowds endpoint A/B.
            float clearance = cellWorldSize * endpointClearanceToBoardTile;
            float usableStart = targetEndpointSize * .5f + targetConnectorSize * .5f + clearance;
            float usableEnd = totalDistance - usableStart;
            if (usableEnd < usableStart) return;

            // PathPieceData is an adjacent-cell polyline. Integer subdivisions keep
            // straight runs and every 90-degree corner on the SAME distance lattice.
            // Moving individual samples onto corners used to create oversized clumps.
            float cellSize = Mathf.Max(.01f, cellWorldSize);
            float preferredSpacing = Mathf.Max(.01f, cellSize * connectorSpacingToBoardTile);
            int subdivisions = Mathf.Clamp(Mathf.RoundToInt(cellSize / preferredSpacing), 1, 8);
            float spacing = cellSize / subdivisions;
            while (subdivisions > 1 && CountSamples(usableStart, usableEnd, spacing) > maximumConnectorCount)
                spacing = cellSize / --subdivisions;
            if (CountSamples(usableStart, usableEnd, spacing) > maximumConnectorCount)
                spacing *= Mathf.Ceil(CountSamples(usableStart, usableEnd, spacing) / (float)maximumConnectorCount);

            int first = Mathf.CeilToInt(usableStart / spacing);
            int last = Mathf.FloorToInt(usableEnd / spacing);
            for (int sample = first; sample <= last && targetConnectorPositions.Count < maximumConnectorCount; sample++)
                targetConnectorPositions.Add(SamplePolyline(worldPoints, sample * spacing));
        }

        private static int CountSamples(float start, float end, float spacing) =>
            Mathf.Max(0, Mathf.FloorToInt(end / spacing) - Mathf.CeilToInt(start / spacing) + 1);

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

        private void LateUpdate() => TickPresentation(Time.unscaledDeltaTime);

        private void TickPresentation(float deltaTime)
        {
            float positionBlend = 1f - Mathf.Exp(-positionSharpness * deltaTime);
            float rotationBlend = 1f - Mathf.Exp(-rotationSharpness * deltaTime);
            float appearanceBlend = 1f - Mathf.Exp(-appearanceSharpness * deltaTime);
            float scaleBlend = 1f - Mathf.Exp(-retargetScaleSharpness * deltaTime);
            if (rotationInitialized)
                displayedRotationDegrees = Mathf.LerpAngle(
                    displayedRotationDegrees,
                    targetRotationDegrees,
                    rotationBlend);
            retargetScale = Mathf.Lerp(retargetScale, 1f, scaleBlend);
            float presentationScale = retargetScale * targetStateScale;
            // Ordered paths rotate around their first authored point. Keeping that
            // anchor fixed prevents the preview from orbiting around its midpoint.
            Vector3 rotationPivot = targetEndpointA;

            if (targetAlpha <= 0f && !positionsInitialized)
                return;

            if (endpointA != null)
                SmoothRenderer(
                    endpointA,
                    GetAnimatedPosition(targetEndpointA, rotationPivot),
                    targetEndpointSize * presentationScale,
                    targetEndpointColor,
                    positionBlend,
                    appearanceBlend);
            if (endpointB != null)
                SmoothRenderer(
                    endpointB,
                    GetAnimatedPosition(targetEndpointB, rotationPivot),
                    targetEndpointSize * presentationScale,
                    targetEndpointColor,
                    positionBlend,
                    appearanceBlend);

            for (int index = 0; index < connectorPool.Count; index++)
            {
                bool active = index < targetConnectorPositions.Count;
                SpriteRenderer connector = connectorPool[index];
                if (active)
                {
                    bool firstFrame = initializedConnectors.Add(connector);
                    SmoothRenderer(
                        connector,
                        GetAnimatedPosition(targetConnectorPositions[index], rotationPivot),
                        targetConnectorSize * presentationScale,
                        targetConnectorColor,
                        positionBlend,
                        appearanceBlend,
                        firstFrame,
                        true);
                }
                else initializedConnectors.Remove(connector);
                connector.gameObject.SetActive(active);
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

        private Vector3 GetAnimatedPosition(Vector3 targetPosition, Vector3 pivot)
        {
            float remainingAngle = Mathf.DeltaAngle(targetRotationDegrees, displayedRotationDegrees);
            return pivot + Quaternion.Euler(0f, 0f, remainingAngle) * (targetPosition - pivot);
        }

        private void SmoothRenderer(
            SpriteRenderer renderer,
            Vector3 targetPosition,
            float targetSize,
            Color targetColor,
            float positionBlend,
            float appearanceBlend,
            bool snap = false,
            bool uniformSize = false)
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
            renderer.transform.position = positionsInitialized && !snap
                ? Vector3.Lerp(renderer.transform.position, centeredPosition, positionBlend)
                : centeredPosition;
            renderer.transform.localScale = positionsInitialized && !snap && !uniformSize
                ? Vector3.Lerp(renderer.transform.localScale, desiredScale, appearanceBlend)
                : desiredScale;

            Color desiredColor = targetColor;
            desiredColor.a *= targetAlpha;
            renderer.color = positionsInitialized && !snap
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
            if (!visible)
            {
                initializedConnectors.Clear();
                rotationInitialized = false;
            }
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
