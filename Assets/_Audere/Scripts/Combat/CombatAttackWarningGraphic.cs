using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat
{
    // Font-independent pixel exclamation, readable over both the border and enemy art.
    public sealed class CombatAttackWarningGraphic : MaskableGraphic
    {
        private Vector2[] positions;
        public void SetPositions(Vector2[] value) { positions = value; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (positions != null)
            {
                foreach (Vector2 p in positions)
                {
                    Quad(vh,new Rect(p.x-5,p.y-8,10,34),new Color(.06f,.015f,.025f,color.a));
                    Quad(vh,new Rect(p.x-3,p.y+4,6,20),color);
                    Quad(vh,new Rect(p.x-3,p.y-6,6,6),color);
                }
                return;
            }
            Color shadow=new Color(.04f,.01f,.03f,color.a);
            Quad(vh,new Rect(-8,0,16,30),shadow);Quad(vh,new Rect(-8,-16,16,16),shadow);
            Quad(vh,new Rect(-4,4,8,22),color);Quad(vh,new Rect(-4,-12,8,8),color);
        }
        private static void Quad(VertexHelper vh,Rect r,Color c)
        {
            int n=vh.currentVertCount;
            vh.AddVert(new Vector3(r.xMin,r.yMin),c,Vector2.zero);vh.AddVert(new Vector3(r.xMin,r.yMax),c,Vector2.zero);
            vh.AddVert(new Vector3(r.xMax,r.yMax),c,Vector2.zero);vh.AddVert(new Vector3(r.xMax,r.yMin),c,Vector2.zero);
            vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
        }
    }
}
