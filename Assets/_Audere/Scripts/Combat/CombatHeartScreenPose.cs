using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat
{
    // Capture before encounter cleanup can reset the cursor or Battle Box transform.
    public readonly struct CombatHeartScreenPose
    {
        public readonly Sprite Sprite;
        public readonly Vector2 ViewportCenter;
        public readonly Vector2 ViewportSize;
        public readonly float Rotation;
        public bool IsValid => Sprite != null && ViewportSize.x > 0f && ViewportSize.y > 0f;

        public CombatHeartScreenPose(Sprite sprite, Vector2 center, Vector2 size, float rotation)
        {
            Sprite = sprite;
            ViewportCenter = center;
            ViewportSize = size;
            Rotation = rotation;
        }

        public static CombatHeartScreenPose Capture(Image image, Camera worldCamera)
        {
            if (image == null || image.sprite == null) return default;
            Canvas canvas = image.canvas;
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? (canvas.worldCamera != null ? canvas.worldCamera : worldCamera) : null;
            if (canvas != null && canvas.renderMode == RenderMode.WorldSpace && camera == null)
                return default;
            var corners = new Vector3[4];
            image.rectTransform.GetWorldCorners(corners);
            Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 topLeft = RectTransformUtility.WorldToScreenPoint(camera, corners[1]);
            Vector2 topRight = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            Vector2 bottomRight = RectTransformUtility.WorldToScreenPoint(camera, corners[3]);
            Vector2 screen = new Vector2(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
            Vector2 edge = bottomRight - bottomLeft;
            return new CombatHeartScreenPose(image.sprite,
                (bottomLeft + topRight) * .5f / screen,
                new Vector2(edge.magnitude, (topLeft - bottomLeft).magnitude) / screen,
                Mathf.Atan2(edge.y, edge.x) * Mathf.Rad2Deg);
        }
    }
}
