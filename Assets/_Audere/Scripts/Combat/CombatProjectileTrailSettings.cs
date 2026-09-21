using System;
using UnityEngine;

namespace Audere.Combat
{
    [Serializable]
    public sealed class CombatProjectileTrailSettings
    {
        [SerializeField] private bool enabled;
        [SerializeField, Min(1f)] private float width = 14f;
        [SerializeField, Min(.05f)] private float blockingDuration = 3.6f;
        [SerializeField, Min(.01f)] private float fadeDuration = .3f;

        public bool Enabled => enabled;
        public float BlockingDuration => blockingDuration;
        public bool Validate(out string error)
        {
            error = enabled && (width <= 0 || blockingDuration <= 0 || fadeDuration <= 0)
                ? "Projectile trail needs positive width, blocking duration and fade duration." : null;
            return error == null;
        }

        public ICombatProjectileMotion Wrap(ICombatProjectileMotion motion, CombatMoveExecutionContext context, object owner)
        {
            return enabled ? new TrailMotion(motion, context, owner, width, blockingDuration, fadeDuration) : motion;
        }

        private sealed class TrailMotion : ICombatProjectileMotion
        {
            private const float SegmentLength = 16f;
            private readonly ICombatProjectileMotion motion;
            private readonly CombatMoveExecutionContext context;
            private readonly object owner;
            private readonly float width, hold, fade;
            private Vector2 previous;
            private bool initialized, cancelled;

            public TrailMotion(ICombatProjectileMotion motion, CombatMoveExecutionContext context, object owner,
                float width, float hold, float fade)
            { this.motion = motion; this.context = context; this.owner = owner; this.width = width; this.hold = hold; this.fade = fade; }

            public bool Tick(RectTransform target, float activeDeltaTime)
            {
                if (cancelled || target == null || context.Board == null || !context.Board.isActiveAndEnabled) return false;
                // Capture the spawn point before moving: the first active frame must paint too.
                if (!initialized) { previous = target.anchoredPosition; initialized = true; }
                bool alive = motion.Tick(target, activeDeltaTime);
                Vector2 current = target.anchoredPosition;
                if (activeDeltaTime > 0f && !(owner is ICombatMoveExecution execution && execution.IsComplete))
                {
                    Vector2 remaining = current - previous;
                    // Retain the sub-segment remainder between ticks. A long frame paints the
                    // same spacing as several short frames instead of stretching one rectangle.
                    while (remaining.sqrMagnitude >= SegmentLength * SegmentLength)
                    {
                        Vector2 next = previous + remaining.normalized * SegmentLength;
                        Emit(previous, next);
                        previous = next;
                        remaining = current - previous;
                    }
                    if (!alive && remaining.sqrMagnitude > .0001f)
                    {
                        Emit(previous, current);
                        previous = current;
                    }
                }
                return alive;
            }

            private void Emit(Vector2 from, Vector2 to) => context.Board.EmitStunTrail(
                owner, context.SessionVersion, context.PhaseVersion, from, to, width, hold, fade);

            public void Cancel() { if (cancelled) return; cancelled = true; motion.Cancel(); }
        }
    }
}
