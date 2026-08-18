using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CombatDashedRingGraphic : MaskableGraphic
    {
        [Header("Ring Shape")]
        [SerializeField, Range(3, 64), Tooltip("Number of visible ring segments. Higher values create a denser border.")]
        private int segmentCount = 8;
        [SerializeField, Min(1f), Tooltip("Thickness of each ring segment in UI pixels.")]
        private float strokeThickness = 8f;
        [SerializeField, Range(.05f, .95f), Tooltip("How much of each segment cell is filled. Higher values reduce the gaps.")]
        private float segmentFill = .68f;
        [SerializeField, Min(0f), Tooltip("Space between the outer edge of the ring and this RectTransform.")]
        private float outerPadding = 3f;
        [SerializeField, Range(-180f, 180f), Tooltip("Rotates the authored segment pattern around the ring.")]
        private float startAngle = 22.5f;
        [SerializeField, Range(1, 24), Tooltip("More subdivisions make each curved segment smoother.")]
        private int curveSubdivisions = 8;

        public int SegmentCount => segmentCount;
        public float StrokeThickness => strokeThickness;
        public float SegmentFill => segmentFill;

        public void Configure(
            int segments,
            float thickness,
            float fill,
            float padding,
            float angle,
            int subdivisions)
        {
            segmentCount = Mathf.Clamp(segments, 3, 64);
            strokeThickness = Mathf.Max(1f, thickness);
            segmentFill = Mathf.Clamp(fill, .05f, .95f);
            outerPadding = Mathf.Max(0f, padding);
            startAngle = Mathf.Repeat(angle + 180f, 360f) - 180f;
            curveSubdivisions = Mathf.Clamp(subdivisions, 1, 24);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();

            Rect rect = GetPixelAdjustedRect();
            float outerRadius = Mathf.Min(rect.width, rect.height) * .5f - outerPadding;
            if (outerRadius <= 0f)
                return;

            float innerRadius = Mathf.Max(0f, outerRadius - strokeThickness);
            float cellAngle = 360f / Mathf.Max(3, segmentCount);
            float arcAngle = cellAngle * Mathf.Clamp(segmentFill, .05f, .95f);
            int subdivisions = Mathf.Max(1, curveSubdivisions);
            Vector2 center = rect.center;
            Color32 vertexColor = color;

            for (int segment = 0; segment < segmentCount; segment++)
            {
                float centerAngle = startAngle + segment * cellAngle;
                float firstAngle = centerAngle - arcAngle * .5f;
                int firstVertex = vertexHelper.currentVertCount;

                for (int step = 0; step <= subdivisions; step++)
                {
                    float t = step / (float)subdivisions;
                    float radians = (firstAngle + arcAngle * t) * Mathf.Deg2Rad;
                    Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                    AddVertex(vertexHelper, center + direction * innerRadius, vertexColor, direction, innerRadius, outerRadius);
                    AddVertex(vertexHelper, center + direction * outerRadius, vertexColor, direction, outerRadius, outerRadius);
                }

                for (int step = 0; step < subdivisions; step++)
                {
                    int index = firstVertex + step * 2;
                    vertexHelper.AddTriangle(index, index + 1, index + 3);
                    vertexHelper.AddTriangle(index, index + 3, index + 2);
                }
            }
        }

        private static void AddVertex(
            VertexHelper vertexHelper,
            Vector2 position,
            Color32 color,
            Vector2 direction,
            float radius,
            float outerRadius)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = color;
            float normalizedRadius = outerRadius > 0f ? radius / outerRadius : 0f;
            vertex.uv0 = Vector2.one * .5f + direction * normalizedRadius * .5f;
            vertexHelper.AddVert(vertex);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            segmentCount = Mathf.Clamp(segmentCount, 3, 64);
            strokeThickness = Mathf.Max(1f, strokeThickness);
            segmentFill = Mathf.Clamp(segmentFill, .05f, .95f);
            outerPadding = Mathf.Max(0f, outerPadding);
            curveSubdivisions = Mathf.Clamp(curveSubdivisions, 1, 24);
            SetVerticesDirty();
        }
#endif
    }
}
