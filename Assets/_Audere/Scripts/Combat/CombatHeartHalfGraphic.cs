using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace Audere.Combat
{
    // Both halves sample the existing Heart Visual sprite along the same jagged seam.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CombatHeartHalfGraphic : MaskableGraphic
    {
        [SerializeField] private Sprite heartSprite;
        [SerializeField] private bool rightHalf;
        private static readonly float[] Seam = { .5f, .44f, .56f, .45f, .54f, .5f };

        public override Texture mainTexture => heartSprite != null ? heartSprite.texture : Texture2D.whiteTexture;

        public void Configure(Sprite sprite, bool isRightHalf)
        {
            heartSprite = sprite;
            rightHalf = isRightHalf;
            raycastTarget = false;
            color = Color.white;
            SetAllDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (heartSprite == null) return;
            Rect rect = GetPixelAdjustedRect();
            float aspect = heartSprite.rect.width / heartSprite.rect.height;
            Vector2 size = rect.size;
            if (size.x <= 0f || size.y <= 0f) return;
            if (size.x / size.y > aspect) size.x = size.y * aspect;
            else size.y = size.x / aspect;
            Rect fitted = new Rect(rect.center - size * .5f, size);
            Vector4 uv = DataUtility.GetOuterUV(heartSprite);
            for (int i = 0; i < Seam.Length - 1; i++)
            {
                float y0 = i / (float)(Seam.Length - 1);
                float y1 = (i + 1) / (float)(Seam.Length - 1);
                float left0 = rightHalf ? Seam[i] : 0f;
                float left1 = rightHalf ? Seam[i + 1] : 0f;
                float right0 = rightHalf ? 1f : Seam[i];
                float right1 = rightHalf ? 1f : Seam[i + 1];
                int start = vh.currentVertCount;
                AddVertex(vh, fitted, uv, left0, y0);
                AddVertex(vh, fitted, uv, left1, y1);
                AddVertex(vh, fitted, uv, right1, y1);
                AddVertex(vh, fitted, uv, right0, y0);
                vh.AddTriangle(start, start + 1, start + 2);
                vh.AddTriangle(start, start + 2, start + 3);
            }
        }

        private void AddVertex(VertexHelper vh, Rect rect, Vector4 uv, float x, float y)
        {
            vh.AddVert(new Vector3(rect.x + rect.width * x, rect.y + rect.height * y), color,
                new Vector2(Mathf.Lerp(uv.x, uv.z, x), Mathf.Lerp(uv.y, uv.w, y)));
        }
    }
}
