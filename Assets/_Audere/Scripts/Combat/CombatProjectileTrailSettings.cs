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
                bool alive = motion.Tick(target, activeDeltaTime);
                Vector2 current = target.anchoredPosition;
                if (!initialized) { previous = current; initialized = true; }
                if (activeDeltaTime > 0f && !(owner is ICombatMoveExecution execution && execution.IsComplete) &&
                    ((current - previous).sqrMagnitude >= 16f * 16f || !alive))
                {
                    context.Board.EmitStunTrail(owner, context.SessionVersion, context.PhaseVersion, previous, current, width, hold, fade);
                    previous = current;
                }
                return alive;
            }
            public void Cancel() { if (cancelled) return; cancelled = true; motion.Cancel(); }
        }
    }
}
