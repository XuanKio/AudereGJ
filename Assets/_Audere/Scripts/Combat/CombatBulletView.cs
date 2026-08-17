using UnityEngine;

namespace Audere.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class CombatBulletView : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Vector2 velocity;

        public RectTransform RectTransform => rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        public void Setup(Vector2 startPosition, Vector2 moveVelocity)
        {
            if (rectTransform == null) Awake();
            rectTransform.anchoredPosition = startPosition;
            velocity = moveVelocity;
            gameObject.SetActive(true);
        }

        public bool TickMovement(Rect playRect, float deltaTime, float despawnMargin = 36f)
        {
            if (rectTransform == null)
                return false;

            rectTransform.anchoredPosition += velocity * deltaTime;
            Vector2 position = rectTransform.anchoredPosition;
            return position.x >= playRect.xMin - despawnMargin &&
                   position.x <= playRect.xMax + despawnMargin &&
                   position.y >= playRect.yMin - despawnMargin &&
                   position.y <= playRect.yMax + despawnMargin;
        }

        public void ReturnToPool()
        {
            gameObject.SetActive(false);
        }
    }
}
