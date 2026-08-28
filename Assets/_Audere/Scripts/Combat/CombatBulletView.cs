using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class CombatBulletView : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Vector2 velocity;
        private Vector2 pendingVelocity;
        private float activationDelay;
        private bool returningOrbit;
        private bool horizontalReturn;
        private Rect orbitBounds;
        private float orbitDuration, orbitElapsed, orbitStartAngle, orbitDirection;
        private bool collisionActive;
        private ICombatProjectileMotion pathMotion;
        private Graphic[] graphics;
        private Color[] authoredColors;
        private Color[] fadeStartColors;

        public RectTransform RectTransform => rectTransform;
        public CombatBulletView SourcePrefab { get; private set; }
        public int OwnerSessionVersion { get; private set; }
        public int OwnerPhaseVersion { get; private set; }
        public bool CollisionActive => collisionActive;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            CapturePresentation();
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
            Setup(sourcePrefab, startPosition, moveVelocity, sessionVersion, phaseVersion, 0f);
        }

        public void Setup(
            CombatBulletView sourcePrefab,
            Vector2 startPosition,
            Vector2 moveVelocity,
            int sessionVersion,
            int phaseVersion,
            float telegraphDelay)
        {
            if (rectTransform == null) Awake();
            ClearPathMotion();
            SourcePrefab = sourcePrefab;
            OwnerSessionVersion = sessionVersion;
            OwnerPhaseVersion = phaseVersion;
            rectTransform.anchoredPosition = startPosition;
            rectTransform.localRotation = Quaternion.identity;
            returningOrbit = false;
            horizontalReturn = false;
            orbitElapsed = 0f;
            activationDelay = Mathf.Max(0f, telegraphDelay);
            pendingVelocity = moveVelocity;
            velocity = activationDelay > 0f ? Vector2.zero : moveVelocity;
            collisionActive = activationDelay <= 0f;
            RestorePresentation();
            gameObject.SetActive(true);
        }

        public void ConfigureReturningOrbit(Rect bounds, float duration, float startAngle, float direction)
        {
            returningOrbit = true;
            horizontalReturn = false;
            orbitBounds = bounds;
            orbitDuration = Mathf.Max(.1f, duration);
            orbitElapsed = 0f;
            orbitStartAngle = startAngle;
            orbitDirection = direction < 0f ? -1f : 1f;
            rectTransform.anchoredPosition = ReturningOrbitMove.EvaluatePosition(bounds, 0f, startAngle, orbitDirection);
        }

        public void ConfigureHorizontalReturn(Rect bounds, float duration, float lane, float direction)
        {
            ConfigureReturningOrbit(bounds, duration, Mathf.Clamp01(lane), direction);
            horizontalReturn = true;
            rectTransform.anchoredPosition = ReturningOrbitMove.EvaluateHorizontalPosition(bounds, 0f, orbitStartAngle, orbitDirection);
        }

        public void BeginPresentationFade()
        {
            ClearPathMotion();
            EnsurePresentation();
            fadeStartColors = new Color[graphics.Length];
            for (int index = 0; index < graphics.Length; index++)
                fadeStartColors[index] = graphics[index] != null ? graphics[index].color : Color.clear;
            collisionActive = false;
            returningOrbit = false;
            horizontalReturn = false;
            velocity = Vector2.zero;
            pendingVelocity = Vector2.zero;
            activationDelay = 0f;
        }

        public void SetPresentationFade(float visibility)
        {
            EnsurePresentation();
            visibility = Mathf.Clamp01(visibility);
            for (int index = 0; index < graphics.Length; index++)
            {
                Graphic graphic = graphics[index];
                if (graphic == null)
                    continue;
                Color start = fadeStartColors != null && index < fadeStartColors.Length
                    ? fadeStartColors[index]
                    : graphic.color;
                graphic.color = new Color(start.r, start.g, start.b, start.a * visibility);
            }
        }

        public bool TickMovement(Rect playRect, float deltaTime, float despawnMargin = 36f)
        {
            if (rectTransform == null)
                return false;

            if (activationDelay > 0f)
            {
                float heldTime = Mathf.Min(activationDelay, Mathf.Max(0f, deltaTime));
                activationDelay -= heldTime;
                deltaTime -= heldTime;
                if (activationDelay > 0f)
                    return true;
                activationDelay = 0f;
                velocity = pendingVelocity;
                collisionActive = true;
            }

            if (returningOrbit)
            {
                orbitElapsed += Mathf.Max(0f, deltaTime);
                float progress = orbitElapsed / orbitDuration;
                rectTransform.anchoredPosition = horizontalReturn
                    ? ReturningOrbitMove.EvaluateHorizontalPosition(orbitBounds, progress, orbitStartAngle, orbitDirection)
                    : ReturningOrbitMove.EvaluatePosition(orbitBounds, progress, orbitStartAngle, orbitDirection);
                rectTransform.localRotation = Quaternion.Euler(0f, 0f, orbitElapsed * 540f * orbitDirection);
                return progress < 1f;
            }
            if (pathMotion != null)
                return pathMotion.Tick(rectTransform, deltaTime);
            rectTransform.anchoredPosition += velocity * deltaTime;
            Vector2 position = rectTransform.anchoredPosition;
            return position.x >= playRect.xMin - despawnMargin &&
                   position.x <= playRect.xMax + despawnMargin &&
                   position.y >= playRect.yMin - despawnMargin &&
                   position.y <= playRect.yMax + despawnMargin;
        }

        public void ReturnToPool()
        {
            ClearPathMotion();
            returningOrbit = false;
            horizontalReturn = false;
            orbitElapsed = 0f;
            collisionActive = false;
            velocity = Vector2.zero;
            pendingVelocity = Vector2.zero;
            activationDelay = 0f;
            OwnerSessionVersion = 0;
            OwnerPhaseVersion = 0;
            RestorePresentation();
            gameObject.SetActive(false);
        }

        public void ConfigurePathMotion(ICombatProjectileMotion motion)
        {
            ClearPathMotion();
            returningOrbit = false; horizontalReturn = false;
            pathMotion = motion;
            pathMotion?.Tick(rectTransform, 0f);
        }

        private void ClearPathMotion() { pathMotion?.Cancel(); pathMotion = null; }
        private void OnDestroy() => ClearPathMotion();

        private void CapturePresentation()
        {
            graphics = GetComponentsInChildren<Graphic>(true);
            authoredColors = new Color[graphics.Length];
            for (int index = 0; index < graphics.Length; index++)
                authoredColors[index] = graphics[index] != null ? graphics[index].color : Color.white;
            fadeStartColors = null;
        }

        private void EnsurePresentation()
        {
            if (graphics == null || authoredColors == null || graphics.Length != authoredColors.Length)
                CapturePresentation();
        }

        private void RestorePresentation()
        {
            EnsurePresentation();
            for (int index = 0; index < graphics.Length; index++)
                if (graphics[index] != null)
                    graphics[index].color = authoredColors[index];
            fadeStartColors = null;
        }
    }
}
