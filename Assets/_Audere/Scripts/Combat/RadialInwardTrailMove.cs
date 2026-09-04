using System;
using System.Collections.Generic;
using UnityEngine;

namespace Audere.Combat
{
    [CreateAssetMenu(menuName = "Audere/Combat/Moves/Radial Inward Trail")]
    public sealed class RadialInwardTrailMove : CombatMoveDefinition
    {
        [SerializeField] private CombatBulletView projectilePrefab;
        [SerializeField, Range(4, 24)] private int projectileCount = 12;
        [SerializeField, Min(0)] private float outerPadding = 20f;
        [SerializeField] private float angularOffset = 15f;
        [SerializeField, Min(.2f)] private float telegraphDuration = 1.1f;
        [SerializeField, Min(.2f)] private float flightDuration = 2.6f;
        [SerializeField] private CombatProjectileTrailSettings stunTrail = new CombatProjectileTrailSettings();

        public int ProjectileCount => projectileCount;
        public float TelegraphDuration => telegraphDuration;
        public override bool Validate(out string error)
        {
            if (!base.Validate(out error)) return false;
            if (projectilePrefab == null || projectileCount < 4 || projectileCount > 24 || outerPadding < 0 ||
                telegraphDuration <= 0 || flightDuration <= 0 || Duration < telegraphDuration + flightDuration)
            { error = "Radial inward move requires projectile, 4..24 shots, padding >= 0 and a duration covering telegraph + flight."; return false; }
            return stunTrail.Validate(out error);
        }

        public static Vector2 RingDirection(int index, int count, float offsetDegrees)
        {
            float angle = (offsetDegrees + index * 360f / count) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        // A single radius enclosing the authored rays, not the unused corners of the rectangle.
        // This keeps every rod outside the field without pushing the warning ring off-screen.
        public static float RingRadius(Vector2 fieldSize, int count, float offsetDegrees, float halfLength, float padding)
        {
            float radius = 0f;
            for (int i = 0; i < count; i++)
            {
                Vector2 direction = RingDirection(i, count, offsetDegrees);
                float x = Mathf.Abs(direction.x) < .0001f ? float.PositiveInfinity : fieldSize.x * .5f / Mathf.Abs(direction.x);
                float y = Mathf.Abs(direction.y) < .0001f ? float.PositiveInfinity : fieldSize.y * .5f / Mathf.Abs(direction.y);
                radius = Mathf.Max(radius, Mathf.Min(x, y));
            }
            return radius + halfLength + padding;
        }

        public override ICombatMoveExecution CreateExecution(CombatMoveExecutionContext context)
        {
            if (!Validate(out string error)) throw new InvalidOperationException(error);
            if (context.Board == null || !context.Board.HasExteriorTrailBindings)
                throw new MissingReferenceException("Radial inward move requires the authored exterior/trail roots on CombatBoard.");
            return new Execution(this, context);
        }

        private sealed class Execution : ICombatMoveExecution
        {
            private readonly RadialInwardTrailMove data;
            private readonly CombatMoveExecutionContext context;
            private readonly List<(CombatBulletView bullet, int lease)> bullets = new List<(CombatBulletView, int)>();
            private float elapsed;
            private bool emitted, cancelled;
            public bool IsComplete => cancelled || elapsed >= data.Duration;
            public Execution(RadialInwardTrailMove data, CombatMoveExecutionContext context) { this.data = data; this.context = context; }

            public void Tick(float deltaTime)
            {
                if (IsComplete || deltaTime <= 0) return;
                if (context.Board == null || !context.Board.isActiveAndEnabled) { Cancel(); return; }
                elapsed += deltaTime;
                if (elapsed >= data.Duration) { Cancel(); return; }
                if (emitted) return;
                emitted = true;
                Rect field = context.Board.PlayArea.rect;
                float radius = RingRadius(field.size, data.projectileCount, data.angularOffset,
                    data.projectilePrefab.GetComponent<RectTransform>().rect.width * .5f, data.outerPadding);
                for (int i = 0; i < data.projectileCount; i++)
                {
                    Vector2 outward = RingDirection(i, data.projectileCount, data.angularOffset);
                    Vector2 start = field.center + outward * radius;
                    Vector2 end = field.center - outward * radius;
                    float rotation = Mathf.Atan2(-outward.y, -outward.x) * Mathf.Rad2Deg;
                    var bullet = context.Board.SpawnExteriorEnemyBullet(data.projectilePrefab, start,
                        context.SessionVersion, context.PhaseVersion, data.telegraphDuration);
                    if (bullet == null) continue;
                    bullets.Add((bullet, bullet.PoolLeaseVersion));
                    bullet.ConfigurePathMotion(data.stunTrail.Wrap(new ParametricProjectileMotion(data.flightDuration,
                        t => Vector2.Lerp(start, end, t), t => rotation), context, this));
                    bullet.FadeInDuringTelegraph();
                }
            }

            public void Cancel()
            {
                if (cancelled) return;
                cancelled = true;
                if (context.Board == null) return;
                foreach (var lease in bullets) context.Board.ReturnEnemyBullet(lease.bullet, lease.lease);
                context.Board.ClearStunTrails(context.SessionVersion, context.PhaseVersion, this);
            }
        }
    }
}
