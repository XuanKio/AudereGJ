using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat
{
    // Two complete, displaced board halves. There is no blade or beam in the gap.
    public sealed class CombatSplitBoardGraphic : MaskableGraphic
    {
        private Rect frame, field;
        private float cut, separation, glow;
        private Color border, fill;

        public void SetGeometry(Rect frameRect, Rect fieldRect, float cutX, float distance,
            Color borderColor, Color fieldColor, float impactGlow)
        {
            frame = frameRect; field = fieldRect; cut = cutX; separation = distance;
            border = borderColor; fill = fieldColor; glow = impactGlow;
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (separation <= .01f) return;
            Color edge = Color.Lerp(border, new Color(.9f, .78f, 1f), glow * .65f);
            Quad(vh, new Rect(frame.xMin - separation, frame.yMin, cut - frame.xMin, frame.height), edge);
            Quad(vh, new Rect(cut + separation, frame.yMin, frame.xMax - cut, frame.height), edge);
            Quad(vh, new Rect(field.xMin - separation, field.yMin, cut - field.xMin - 5f, field.height), fill);
            Quad(vh, new Rect(cut + separation + 5f, field.yMin, field.xMax - cut - 5f, field.height), fill);
            // A ragged inner rim reads as the board being pulled apart by the body.
            const int teeth = 18;
            for (int i = 0; i < teeth; i++)
            {
                float y0 = Mathf.Lerp(frame.yMin, frame.yMax, i / (float)teeth);
                float y1 = Mathf.Lerp(frame.yMin, frame.yMax, (i + 1f) / teeth);
                float tooth = i % 3 == 0 ? 9f : 4f;
                Triangle(vh, new Vector2(cut - separation, y0), new Vector2(cut - separation + tooth, (y0+y1)*.5f), new Vector2(cut-separation,y1), edge);
                Triangle(vh, new Vector2(cut + separation, y0), new Vector2(cut + separation - tooth, (y0+y1)*.5f), new Vector2(cut+separation,y1), edge);
            }
        }

        private static void Quad(VertexHelper vh, Rect r, Color c)
        {
            int n = vh.currentVertCount;
            vh.AddVert(new Vector3(r.xMin,r.yMin),c,Vector2.zero);
            vh.AddVert(new Vector3(r.xMin,r.yMax),c,Vector2.zero);
            vh.AddVert(new Vector3(r.xMax,r.yMax),c,Vector2.zero);
            vh.AddVert(new Vector3(r.xMax,r.yMin),c,Vector2.zero);
            vh.AddTriangle(n,n+1,n+2); vh.AddTriangle(n,n+2,n+3);
        }
        private static void Triangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            int n=vh.currentVertCount;
            vh.AddVert(a,color,Vector2.zero);vh.AddVert(b,color,Vector2.zero);vh.AddVert(c,color,Vector2.zero);
            vh.AddTriangle(n,n+1,n+2);
        }
    }
}
