using UnityEngine;

namespace Audere.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class CombatBulletView : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Vector2 velocity;
        private bool collisionActive;

        public RectTransform RectTransform => rectTransform;
        public CombatBulletView SourcePrefab { get; private set; }
        public int OwnerSessionVersion { get; private set; }
        public int OwnerPhaseVersion { get; private set; }
        public bool CollisionActive => collisionActive;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        public void Setup(Vector2 startPosition, Vector2 moveVelocity)
        {
            Setup(null, startPosition, moveVelocity, 0, 0);
        }

        public void Setup(
            CombatBulletView sourcePrefab,
            Vector2 startPosition,
            Vector2 moveVelocity,
            int sessionVersion,
            int phaseVersion)
        {
            if (rectTransform == null) Awake();
            SourcePrefab = sourcePrefab;
            OwnerSessionVersion = sessionVersion;
            OwnerPhaseVersion = phaseVersion;
            rectTransform.anchoredPosition = startPosition;
            rectTransform.localRotation = Quaternion.identity;
            velocity = moveVelocity;
            collisionActive = true;
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
            collisionActive = false;
            velocity = Vector2.zero;
            OwnerSessionVersion = 0;
            OwnerPhaseVersion = 0;
            gameObject.SetActive(false);
        }
    }
}
