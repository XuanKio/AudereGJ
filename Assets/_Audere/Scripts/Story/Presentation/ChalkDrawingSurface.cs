using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Audere.Story.Presentation
{
    /// <summary>Bounded, session-local strokes. The material supplies chalk grain; no texture readback.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ChalkDrawingSurface : MaskableGraphic, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField, Min(1f)] private float brushWidth = 7f;
        [SerializeField, Range(100, 12000)] private int maximumSegments = 9000;
        private readonly List<Vector4> segments = new List<Vector4>();
        private Vector2 previous;
        private bool drawing;
        private int pointerId;
        public bool AcceptsDrawing { get; set; }
        public bool HasDrawing => segments.Count > 0;
        public int SegmentCount => segments.Count;
        public event Action DrawingChanged;

        public void ResetDrawing()
        {
            drawing = false;
            segments.Clear();
            SetVerticesDirty();
            DrawingChanged?.Invoke();
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (!AcceptsDrawing || e.button != PointerEventData.InputButton.Left ||
                !LocalPoint(e, out var p) || !rectTransform.rect.Contains(p)) return;
            pointerId = e.pointerId;
            drawing = true;
            previous = p;
            AddSegment(p, p + Vector2.right * .05f);
        }

        public void OnDrag(PointerEventData e)
        {
            if (!AcceptsDrawing || !drawing || e.pointerId != pointerId || !LocalPoint(e, out var p)) return;
            // Leaving the board lifts the chalk. Re-entry starts a new stroke, never a line across the frame.
            if (!rectTransform.rect.Contains(p)) { drawing = false; return; }
            if ((p - previous).sqrMagnitude < .5f) return;
            AddSegment(previous, p);
            previous = p;
        }

        public void OnPointerUp(PointerEventData e) { if (e.pointerId == pointerId) drawing = false; }
        protected override void OnDisable() { drawing = false; base.OnDisable(); }
        private bool LocalPoint(PointerEventData e, out Vector2 p) =>
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, e.position, e.pressEventCamera, out p);

        private void AddSegment(Vector2 a, Vector2 b)
        {
            if (segments.Count >= maximumSegments) return;
            segments.Add(new Vector4(a.x, a.y, b.x, b.y));
            SetVerticesDirty();
            DrawingChanged?.Invoke();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            // A transparent hit surface keeps GraphicRaycaster working before the first stroke.
            Rect r = rectTransform.rect;
            vh.AddVert(new Vector3(r.xMin, r.yMin), Color.clear, Vector2.zero);
            vh.AddVert(new Vector3(r.xMin, r.yMax), Color.clear, Vector2.up);
            vh.AddVert(new Vector3(r.xMax, r.yMax), Color.clear, Vector2.one);
            vh.AddVert(new Vector3(r.xMax, r.yMin), Color.clear, Vector2.right);
            vh.AddTriangle(0, 1, 2); vh.AddTriangle(0, 2, 3);
            foreach (var s in segments)
            {
                Vector2 a = new Vector2(s.x, s.y), b = new Vector2(s.z, s.w);
                Vector2 tangent = (b - a).normalized * (brushWidth * .5f);
                Vector2 normal = new Vector2(-tangent.y, tangent.x);
                // Overlapping caps and soft shader edges keep joins smooth; grain stays in board-local space.
                Vector2[] points = { a - tangent - normal, a - tangent + normal, b + tangent + normal, b + tangent - normal };
                Vector2[] uv = { new Vector2(0,0), new Vector2(0,1), new Vector2(1,1), new Vector2(1,0) };
                int start = vh.currentVertCount;
                for (int i = 0; i < 4; i++)
                {
                    var v = UIVertex.simpleVert;
                    v.position = points[i]; v.color = color; v.uv0 = uv[i];
                    vh.AddVert(v);
                }
                vh.AddTriangle(start, start + 1, start + 2);
                vh.AddTriangle(start, start + 2, start + 3);
            }
        }
    }
}
