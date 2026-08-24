using UnityEngine;

namespace Audere.Combat
{
    /// <summary>
    /// Immutable launch data for one catcher reroll. Planar motion is reflected
    /// inside the usable board bounds while height follows a ballistic arc.
    /// </summary>
    public readonly struct CombatRerollLaunchPlan
    {
        public CombatRerollLaunchPlan(
            Vector2 startPosition,
            Vector2 planarVelocity,
            Rect safeBounds,
            float duration,
            float gravity,
            float normalizedOffset)
        {
            StartPosition = startPosition;
            PlanarVelocity = planarVelocity;
            SafeBounds = safeBounds;
            Duration = Mathf.Max(.01f, duration);
            Gravity = Mathf.Max(0f, gravity);
            NormalizedOffset = Mathf.Clamp01(normalizedOffset);
            VerticalLaunchSpeed = Gravity * Duration * .5f;
        }

        public Vector2 StartPosition { get; }
        public Vector2 PlanarVelocity { get; }
        public Rect SafeBounds { get; }
        public float Duration { get; }
        public float Gravity { get; }
        public float NormalizedOffset { get; }
        public float VerticalLaunchSpeed { get; }
        public float ApexHeight => Gravity * Duration * Duration * .125f;
        public Vector2 LandingPosition => EvaluatePlanarPosition(Duration);

        public Vector2 EvaluatePlanarPosition(float elapsed)
        {
            float time = Mathf.Clamp(elapsed, 0f, Duration);
            Vector2 unconstrained = StartPosition + PlanarVelocity * time;
            return new Vector2(
                ReflectInside(unconstrained.x, SafeBounds.xMin, SafeBounds.xMax),
                ReflectInside(unconstrained.y, SafeBounds.yMin, SafeBounds.yMax));
        }

        public float EvaluateHeight(float elapsed)
        {
            float time = Mathf.Clamp(elapsed, 0f, Duration);
            return Mathf.Max(0f, VerticalLaunchSpeed * time - .5f * Gravity * time * time);
        }

        private static float ReflectInside(float value, float minimum, float maximum)
        {
            if (maximum <= minimum)
                return (minimum + maximum) * .5f;

            float length = maximum - minimum;
            return minimum + Mathf.PingPong(value - minimum, length);
        }
    }

    /// <summary>
    /// Converts the die's offset inside the catcher into a board-safe launch.
    /// </summary>
    public static class CombatRerollPhysics
    {
        public static CombatRerollLaunchPlan Calculate(
            Rect boardBounds,
            Vector2 dieSize,
            Vector2 diePosition,
            Vector2 catcherPosition,
            float catcherRadius)
        {
            Vector2 halfDie = Vector2.Max(Vector2.zero, dieSize * .5f);
            float padding = CombatDiceConstants.RerollBoardEdgePadding;
            Rect safeBounds = Rect.MinMaxRect(
                boardBounds.xMin + halfDie.x + padding,
                boardBounds.yMin + halfDie.y + padding,
                boardBounds.xMax - halfDie.x - padding,
                boardBounds.yMax - halfDie.y - padding);

            Vector2 safeStart = new Vector2(
                Mathf.Clamp(diePosition.x, safeBounds.xMin, safeBounds.xMax),
                Mathf.Clamp(diePosition.y, safeBounds.yMin, safeBounds.yMax));
            Vector2 offset = safeStart - catcherPosition;
            float offsetDistance = offset.magnitude;
            Vector2 direction = offsetDistance > .001f ? offset / offsetDistance : Vector2.zero;

            // Circle-versus-rectangle support distance. This normalizes a die at
            // the catcher's outer overlap edge to approximately one at any angle.
            float dieSupport = halfDie.x * Mathf.Abs(direction.x) +
                               halfDie.y * Mathf.Abs(direction.y);
            float influenceDistance = Mathf.Max(1f, catcherRadius + dieSupport);
            float rawOffset = Mathf.Clamp01(offsetDistance / influenceDistance);
            float normalizedOffset = Mathf.InverseLerp(
                CombatDiceConstants.RerollCenterDeadZoneNormalized,
                1f,
                rawOffset);

            float distanceFactor = Mathf.Pow(
                normalizedOffset,
                CombatDiceConstants.RerollDistanceExponent);
            float travelDistance = CombatDiceConstants.RerollMaximumTravelDistance * distanceFactor;
            float duration = Mathf.Lerp(
                CombatDiceConstants.RerollMinimumFlightDuration,
                CombatDiceConstants.RerollMaximumFlightDuration,
                normalizedOffset);
            Vector2 planarVelocity = duration > 0f
                ? direction * (travelDistance / duration)
                : Vector2.zero;

            return new CombatRerollLaunchPlan(
                safeStart,
                planarVelocity,
                safeBounds,
                duration,
                CombatDiceConstants.RerollGravity,
                normalizedOffset);
        }
    }
}
