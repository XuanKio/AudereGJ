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
        private float telegraphFadeDuration;
        private bool returningOrbit;
        private bool horizontalReturn;
        private Rect orbitBounds;
        private float orbitDuration, orbitElapsed, orbitStartAngle, orbitDirection;
        private bool collisionActive;
        private System.Func<Vector2> avoidanceTarget;
        private System.Func<float> avoidanceRadius;
        private float deflectRadius, dissolveRadius;
        private bool presentationFading;
        private bool bypassesForcedMovementProtection;
        public bool IsHarmless => avoidanceTarget != null;
        private ICombatProjectileMotion pathMotion;
        private Graphic[] graphics;
        private Color[] authoredColors;
        private Color[] fadeStartColors;

        public RectTransform RectTransform => rectTransform;
        public CombatBulletView SourcePrefab { get; private set; }
        public int OwnerSessionVersion { get; private set; }
        public int OwnerPhaseVersion { get; private set; }
        public bool CollisionActive => collisionActive && !IsHarmless;
        public bool AttackActive => collisionActive;
        public bool BypassesForcedMovementProtection => bypassesForcedMovementProtection;
        public int PoolLeaseVersion { get; private set; }

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
            ClearAvoidance();
            presentationFading = false;
            bypassesForcedMovementProtection = false;
            PoolLeaseVersion++;
            SourcePrefab = sourcePrefab;
            OwnerSessionVersion = sessionVersion;
            OwnerPhaseVersion = phaseVersion;
            rectTransform.anchoredPosition = startPosition;
            rectTransform.localRotation = Quaternion.identity;
            returningOrbit = false;
            horizontalReturn = false;
            orbitElapsed = 0f;
            activationDelay = Mathf.Max(0f, telegraphDelay);
            telegraphFadeDuration = 0f;
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

        public void FadeInDuringTelegraph()
        {
            if (activationDelay <= 0f) return;
            EnsurePresentation();
            telegraphFadeDuration = activationDelay;
            fadeStartColors = (Color[])authoredColors.Clone();
            SetPresentationFade(0f);
        }

        public void AllowHitDuringForcedMovement()
        {
            bypassesForcedMovementProtection = true;
        }

        public void ConfigureHorizontalReturn(Rect bounds, float duration, float lane, float direction)
        {
            ConfigureReturningOrbit(bounds, duration, Mathf.Clamp01(lane), direction);
            horizontalReturn = true;
            rectTransform.anchoredPosition = ReturningOrbitMove.EvaluateHorizontalPosition(bounds, 0f, orbitStartAngle, orbitDirection);
        }

        public void BeginPresentationFade()
        {
            presentationFading = true;
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

        // Distances are measured OUTSIDE the catch circle and the projectile footprint.
        public void ConfigureHarmlessAvoidance(System.Func<Vector2> target, float deflectDistance, float dissolveDistance)
            => ConfigureHarmlessAvoidance(target, null, deflectDistance, dissolveDistance);

        public void ConfigureHarmlessAvoidance(System.Func<Vector2> target, System.Func<float> radius,
            float deflectDistance, float dissolveDistance)
        {
            avoidanceTarget = target ?? throw new System.ArgumentNullException(nameof(target));
            avoidanceRadius = radius;
            dissolveRadius = Mathf.Clamp(dissolveDistance, .5f, 4f);
            deflectRadius = Mathf.Max(dissolveRadius + 1f, deflectDistance);
        }

        public bool TickMovement(Rect playRect, float deltaTime, float despawnMargin = 36f)
        {
            if (rectTransform == null) return false;
            if (!IsHarmless || presentationFading)
                return TickAuthoredMovement(playRect, deltaTime, despawnMargin);

            Vector2 center = avoidanceTarget();
            float footprint = ProjectileRadius();
            float clearance = Mathf.Max(0f, avoidanceRadius?.Invoke() ?? 0f) + footprint;
            Vector2 before = rectTransform.anchoredPosition;
            Vector2 away = before - center;
            // A chasing/moving cursor pushes a projectile back outside its rim. Only
            // a near-rim overlap with no room to escape may recycle the projectile.
            if (away.magnitude <= clearance + dissolveRadius)
            {
                Vector2 normal = away.sqrMagnitude > .0001f ? away.normalized :
                    (pendingVelocity.sqrMagnitude > .0001f ? -pendingVelocity.normalized : Vector2.up);
                if (!TryRepel(center, normal, clearance + dissolveRadius + 1f, playRect, footprint, out before))
                    return false;
                rectTransform.anchoredPosition = before;
                Redirect((before - center).normalized, Mathf.Max(90f, pendingVelocity.magnitude));
            }

            bool alive = TickAuthoredMovement(playRect, deltaTime, despawnMargin);
            Vector2 segment = rectTransform.anchoredPosition - before;
            if (segment.sqrMagnitude < .0001f) return alive;
            float influence = clearance + deflectRadius;
            if (!TryEnteringCircle(before, segment, center, influence, out float fraction)) return alive;

            // Sweep even long frames and authored orbit/path moves. Turn the whole
            // projectile at the field boundary instead of fading or tunnelling through it.
            Vector2 contact = before + segment * fraction;
            Vector2 outward = (contact - center).normalized;
            Vector2 incoming = segment.normalized;
            Vector2 reflected = Vector2.Reflect(incoming, outward).normalized;
            Vector2 direction = (reflected + outward * .35f).normalized;
            Redirect(direction, Mathf.Max(90f, segment.magnitude / Mathf.Max(.001f, deltaTime)));
            rectTransform.anchoredPosition = contact + direction * segment.magnitude * (1f - fraction);
            return InsideBounds(rectTransform.anchoredPosition, playRect, despawnMargin);
        }

        private float ProjectileRadius()
        {
            Transform space = rectTransform.parent;
            Vector3 x = rectTransform.TransformVector(Vector3.right * rectTransform.rect.width * .5f);
            Vector3 y = rectTransform.TransformVector(Vector3.up * rectTransform.rect.height * .5f);
            if (space != null) { x = space.InverseTransformVector(x); y = space.InverseTransformVector(y); }
            return Mathf.Sqrt(x.sqrMagnitude + y.sqrMagnitude);
        }

        private void Redirect(Vector2 direction, float speed)
        {
            ClearPathMotion(); returningOrbit = false; horizontalReturn = false;
            pendingVelocity = direction * speed;
            velocity = activationDelay > 0f ? Vector2.zero : pendingVelocity;
        }

        private static bool TryEnteringCircle(Vector2 start, Vector2 segment, Vector2 center, float radius, out float fraction)
        {
            fraction = 0f;
            Vector2 offset = start - center;
            float dot = Vector2.Dot(offset, segment);
            if (dot >= 0f) return false;
            float outside = offset.sqrMagnitude - radius * radius;
            if (outside <= 0f) return true;
            float discriminant = dot * dot - segment.sqrMagnitude * outside;
            if (discriminant < 0f) return false;
            fraction = (-dot - Mathf.Sqrt(discriminant)) / segment.sqrMagnitude;
            return fraction >= 0f && fraction <= 1f;
        }

        private static bool TryRepel(Vector2 center, Vector2 normal, float radius, Rect bounds,
            float footprint, out Vector2 position)
        {
            // Prefer the shortest outward push; try either side if the board edge blocks it.
            for (int i = 0; i < 8; i++)
            {
                int turn = i % 2 == 0 ? -(i / 2) : (i + 1) / 2;
                Vector2 direction = Quaternion.Euler(0f, 0f, turn * 45f) * normal;
                position = center + direction * radius;
                if (InsideBounds(position, bounds, -footprint)) return true;
            }
            position = center;
            return false;
        }

        private static bool InsideBounds(Vector2 position, Rect bounds, float margin) =>
            position.x >= bounds.xMin - margin && position.x <= bounds.xMax + margin &&
            position.y >= bounds.yMin - margin && position.y <= bounds.yMax + margin;

        private void ClearAvoidance()
        {
            avoidanceTarget = null; avoidanceRadius = null;
            deflectRadius = 0f; dissolveRadius = 0f; presentationFading = false;
        }

        private bool TickAuthoredMovement(Rect playRect, float deltaTime, float despawnMargin)
        {
            if (rectTransform == null)
                return false;

            if (activationDelay > 0f)
            {
                float heldTime = Mathf.Min(activationDelay, Mathf.Max(0f, deltaTime));
                activationDelay -= heldTime;
                deltaTime -= heldTime;
                if (telegraphFadeDuration > 0f)
                    SetPresentationFade(Mathf.SmoothStep(0f, 1f, 1f - activationDelay / telegraphFadeDuration));
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
            ClearAvoidance();
            ClearPathMotion();
            returningOrbit = false;
            horizontalReturn = false;
            orbitElapsed = 0f;
            collisionActive = false;
            bypassesForcedMovementProtection = false;
            velocity = Vector2.zero;
            pendingVelocity = Vector2.zero;
            activationDelay = 0f;
            telegraphFadeDuration = 0f;
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
        private void OnDestroy() { ClearAvoidance(); ClearPathMotion(); }

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
