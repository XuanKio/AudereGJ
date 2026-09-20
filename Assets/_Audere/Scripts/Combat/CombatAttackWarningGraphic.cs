using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat
{
    // Font-independent pixel exclamation, readable over both the border and enemy art.
    public sealed class CombatAttackWarningGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
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
