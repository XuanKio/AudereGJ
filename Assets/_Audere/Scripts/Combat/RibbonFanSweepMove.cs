using System;
using System.Collections.Generic;
using UnityEngine;

namespace Audere.Combat
{
    [CreateAssetMenu(menuName = "Audere/Combat/Moves/Ribbon Fan Sweep")]
    public sealed class RibbonFanSweepMove : CombatMoveDefinition
    {
        [SerializeField] private CombatBulletView projectilePrefab;
        [SerializeField, Range(3, 12)] private int ribbonsPerArm = 9;
        [SerializeField, Min(40f)] private float horizontalSpacing = 96f;
        [SerializeField, Min(0f)] private float firstDistance = 48f;
        [SerializeField, Min(.2f)] private float telegraphDuration = .8f;
        [SerializeField, Range(15f, 60f)] private float halfAngle = 35f;
        [SerializeField, Min(.3f)] private float strokeDuration = 1.05f;
        [SerializeField, Min(.1f)] private float turnDuration = .45f;
        [SerializeField, Min(0f)] private float segmentDelay = .1f;
        [SerializeField, Min(.1f)] private float extensionDuration = .6f;

        // Each bow travels a straight vertical chord. The two banks exchange sides;
        // a delay along the row carries the crossing outwards from the enemy.
        public Vector2 EvaluatePosition(Vector2 origin, int segment, float arm, float seconds)
        {
            float extension = Mathf.SmoothStep(0f, 1f, seconds / extensionDuration);
            float distance = firstDistance + horizontalSpacing * segment;
            float local = Mathf.Max(0f, seconds - extensionDuration - segment * segmentDelay);
            float halfCycle = strokeDuration + turnDuration;
            int stroke = Mathf.FloorToInt(local / halfCycle);
            float phase = local - stroke * halfCycle;
            float sign = (stroke & 1) == 0 ? 1f : -1f;
            float sweep = Mathf.SmoothStep(0f, 1f, Mathf.Min(phase / strokeDuration, 1f));
            float height = distance * Mathf.Tan(halfAngle * Mathf.Deg2Rad);
            return origin + new Vector2(-distance, arm * height * sign * (1f - 2f * sweep)) * extension;
        }

        public float EvaluateRotation(int segment, float arm, float seconds)
        {
            float local = Mathf.Max(0f, seconds - extensionDuration - segment * segmentDelay);
            float halfCycle = strokeDuration + turnDuration;
            int stroke = Mathf.FloorToInt(local / halfCycle);
            float phase = local - stroke * halfCycle;
            float turn = Mathf.SmoothStep(0f, 1f, (phase - strokeDuration) / turnDuration);
            // No perpetual spinning: turn at the tip, then hold orientation on the return.
            return 90f + arm * 180f * (stroke + turn);
        }

        public override bool Validate(out string error)
        {
            if (!base.Validate(out error)) return false;
            if (projectilePrefab == null || ribbonsPerArm < 3 || ribbonsPerArm > 12 ||
                horizontalSpacing < 40f || firstDistance < 0f || telegraphDuration < .2f ||
                halfAngle < 15f || halfAngle > 60f || strokeDuration < .3f || turnDuration < .1f ||
                segmentDelay < 0f || extensionDuration < .1f ||
                Duration <= telegraphDuration + extensionDuration + segmentDelay * (ribbonsPerArm - 1) + 2f * (strokeDuration + turnDuration))
            { error = "Ribbon sweep needs separated chords and enough time to cross, turn and return."; return false; }
            return true;
        }

        public override ICombatMoveExecution CreateExecution(CombatMoveExecutionContext context)
        {
            if (!Validate(out var error)) throw new InvalidOperationException(error);
            return new Execution(this, context);
        }

        private sealed class Execution : ICombatMoveExecution
        {
            private readonly RibbonFanSweepMove data;
            private readonly CombatMoveExecutionContext context;
            private readonly List<(CombatBulletView bullet, int lease)> bullets = new List<(CombatBulletView, int)>();
            private float elapsed;
            private bool spawned, cancelled;
            public Execution(RibbonFanSweepMove data, CombatMoveExecutionContext context) { this.data = data; this.context = context; }
            public bool IsComplete => cancelled || elapsed >= data.Duration;

            public void Tick(float dt)
            {
                if (IsComplete || dt <= 0f) return;
                elapsed += dt;
                if (spawned) return;
                spawned = true;
                // The body center anchors this gesture; the projectile muzzle is above it.
                var visual = context.Actor.VisualRoot;
                var rect = visual as RectTransform;
                Vector2 origin = context.Board.WorldToPlayArea(rect != null ? rect.TransformPoint(rect.rect.center) : visual.position);
                float flight = data.Duration - data.telegraphDuration;
                for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < data.ribbonsPerArm; i++)
                {
                    int segment = i;
                    float arm = side;
                    var bullet = context.Board.SpawnExteriorEnemyBullet(data.projectilePrefab, origin,
                        context.SessionVersion, context.PhaseVersion, data.telegraphDuration);
                    bullets.Add((bullet, bullet.PoolLeaseVersion));
                    bullet.FadeInDuringTelegraph();
                    bullet.ConfigurePathMotion(new RibbonProjectileMotion(context.Board, flight,
                        t => data.EvaluatePosition(origin, segment, arm, t * flight),
                        t => data.EvaluateRotation(segment, arm, t * flight)));
                }
            }

            public void Cancel()
            {
                if (cancelled) return;
                cancelled = true;
                foreach (var entry in bullets) context.Board.ReturnEnemyBullet(entry.bullet, entry.lease);
                bullets.Clear();
            }
        }
    }
}
