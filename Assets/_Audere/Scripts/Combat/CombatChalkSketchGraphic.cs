using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat
{
    public sealed class CombatChalkSketchGraphic : MaskableGraphic
    {
        private Vector2[] vertices;
        private float progress, width = 7f;
        public void Draw(Vector2[] points, float edges, float strokeWidth)
        { vertices=points; progress=edges; width=strokeWidth; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (vertices == null) return;
            for (int i=0;i<vertices.Length && progress>i;i++)
            {
                Vector2 a=vertices[i], b=Vector2.Lerp(a,vertices[(i+1)%vertices.Length],Mathf.Clamp01(progress-i));
                Vector2 normal=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;
                int n=vh.currentVertCount;
                vh.AddVert(a-normal,color,Vector2.zero); vh.AddVert(a+normal,color,Vector2.up);
                vh.AddVert(b+normal,color,Vector2.one); vh.AddVert(b-normal,color,Vector2.right);
                vh.AddTriangle(n,n+1,n+2); vh.AddTriangle(n,n+2,n+3);
            }
        }
    }
}
