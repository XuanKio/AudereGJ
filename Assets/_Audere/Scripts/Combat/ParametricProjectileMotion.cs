using System;
using UnityEngine;

namespace Audere.Combat
{
    public interface ICombatProjectileMotion
    {
        bool Tick(RectTransform target, float activeDeltaTime);
        void Cancel();
    }

    /// <summary>One bullet's motion, owned by its pool lease; never stored in an authored asset.</summary>
    public sealed class ParametricProjectileMotion : ICombatProjectileMotion
    {
        private readonly float duration;
        private readonly Func<float, Vector2> position;
        private readonly Func<float, float> rotation;
        private float elapsed;
        private bool cancelled;
        public ParametricProjectileMotion(float duration, Func<float, Vector2> position, Func<float, float> rotation)
        {
            if (duration <= 0f || position == null) throw new ArgumentException("Projectile motion needs a duration and path.");
            this.duration = duration; this.position = position; this.rotation = rotation;
        }
        public bool Tick(RectTransform target, float activeDeltaTime)
        {
            if (cancelled || target == null) return false;
            elapsed = Mathf.Min(duration, elapsed + Mathf.Max(0f, activeDeltaTime));
            float t = elapsed / duration;
            target.anchoredPosition = position(t);
            target.localRotation = Quaternion.Euler(0f, 0f, rotation != null ? rotation(t) : 0f);
            return elapsed < duration;
        }
        public void Cancel() => cancelled = true;
    }
}
