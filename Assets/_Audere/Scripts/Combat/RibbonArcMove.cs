using System;
using UnityEngine;

namespace Audere.Combat
{
    [CreateAssetMenu(menuName = "Audere/Combat/Moves/Ribbon Arc")]
    public sealed class RibbonArcMove : CombatMoveDefinition
    {
        [SerializeField] private CombatBulletView projectilePrefab;
        [SerializeField, Min(.5f)] private float waveInterval = 1.65f;
        [SerializeField, Min(1f)] private float flightDuration = 3.8f;
        [SerializeField, Min(.2f)] private float telegraphDuration = .65f;
        [SerializeField, Range(2, 6)] private int ribbonsPerWave = 4;
        [SerializeField] private float curvature = 100f;
        [SerializeField] private bool reverseFirstArc;
        public override bool Validate(out string error)
        {
            if (!base.Validate(out error)) return false;
            if (projectilePrefab == null || waveInterval < .5f || flightDuration < 1f || ribbonsPerWave < 2)
            { error = "Ribbon arc requires a prefab and readable wave/flight spacing."; return false; }
            return true;
        }
        public override ICombatMoveExecution CreateExecution(CombatMoveExecutionContext context)
        {
            if (!Validate(out var error)) throw new InvalidOperationException(error);
            return new Execution(this, context);
        }
        private sealed class Execution : ICombatMoveExecution
        {
            private readonly RibbonArcMove data;
            private readonly CombatMoveExecutionContext context;
            private float elapsed, nextWave;
            private int wave;
            private bool cancelled;
            public Execution(RibbonArcMove data, CombatMoveExecutionContext context) { this.data = data; this.context = context; }
            public bool IsComplete => cancelled || elapsed >= data.Duration;
            public void Tick(float dt)
            {
                if (IsComplete || dt <= 0f) return;
                elapsed += dt;
                // The final volley gets time to cross the board before recovery.
                if (elapsed < nextWave || elapsed > data.Duration - data.flightDuration - data.telegraphDuration) return;
                nextWave = elapsed + data.waveInterval;
                Rect bounds = context.Board.PlayArea.rect;
                Vector2 origin = context.Board.WorldToPlayArea(context.Actor.ProjectileOriginPosition);
                float sign = ((wave++ % 2 == 0) != data.reverseFirstArc) ? 1f : -1f;
                for (int i = 0; i < data.ribbonsPerWave; i++)
                {
                    // An entire row curls high or low, leaving the opposite side open.
                    float lane = (i + .5f) / data.ribbonsPerWave;
                    Vector2 end = new Vector2(bounds.xMin - 100f, Mathf.Lerp(bounds.yMin + 40f, bounds.yMax - 40f, lane));
                    float bend = data.curvature * sign;
                    var bullet = context.Board.SpawnExteriorEnemyBullet(data.projectilePrefab, origin,
                        context.SessionVersion, context.PhaseVersion, data.telegraphDuration + i * .075f);
                    bullet.FadeInDuringTelegraph();
                    bullet.ConfigurePathMotion(new RibbonProjectileMotion(context.Board, data.flightDuration,
                        t => Vector2.Lerp(origin, end, t) + Vector2.up * (Mathf.Sin(t * Mathf.PI) * bend), sign * 160f));
                }
            }
            public void Cancel() => cancelled = true;
        }
    }
}
