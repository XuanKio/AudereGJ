using System;
using UnityEngine;

namespace Audere.Combat
{
    [CreateAssetMenu(menuName = "Audere/Combat/Moves/Ribbon Weave")]
    public sealed class RibbonWeaveMove : CombatMoveDefinition
    {
        [SerializeField] private CombatBulletView projectilePrefab;
        [SerializeField, Min(.8f)] private float waveInterval = 1.6f;
        [SerializeField, Min(1f)] private float flightDuration = 2.8f;
        [SerializeField, Min(.3f)] private float telegraphDuration = .7f;
        public override bool Validate(out string error)
        {
            if (!base.Validate(out error)) return false;
            if (projectilePrefab == null || waveInterval < .8f || flightDuration < 1f)
            { error = "Ribbon weave requires a prefab and readable wave/flight spacing."; return false; }
            return true;
        }
        public override ICombatMoveExecution CreateExecution(CombatMoveExecutionContext context)
        {
            if (!Validate(out var error)) throw new InvalidOperationException(error);
            return new Execution(this, context);
        }
        private sealed class Execution : ICombatMoveExecution
        {
            private readonly RibbonWeaveMove data;
            private readonly CombatMoveExecutionContext context;
            private float elapsed, nextWave;
            private int wave;
            private bool cancelled;
            public Execution(RibbonWeaveMove data, CombatMoveExecutionContext context) { this.data = data; this.context = context; }
            public bool IsComplete => cancelled || elapsed >= data.Duration;
            public void Tick(float dt)
            {
                if (IsComplete || dt <= 0f) return;
                elapsed += dt;
                if (elapsed < nextWave || elapsed > data.Duration - data.flightDuration - data.telegraphDuration) return;
                nextWave = elapsed + data.waveInterval;
                Rect bounds = context.Board.PlayArea.rect;
                bool left = wave++ % 2 == 0;
                for (int i = 0; i < 3; i++)
                {
                    float lane = .18f + i * .32f;
                    Vector2 start = new Vector2(left ? bounds.xMin - 35f : bounds.xMax + 35f, Mathf.Lerp(bounds.yMin, bounds.yMax, lane));
                    Vector2 end = new Vector2(left ? bounds.xMax + 65f : bounds.xMin - 65f, start.y);
                    float bend = (i % 2 == 0 ? 1f : -1f) * (left ? 52f : -52f);
                    var bullet = context.Board.SpawnExteriorEnemyBullet(data.projectilePrefab, start, context.SessionVersion,
                        context.PhaseVersion, data.telegraphDuration + i * .12f);
                    bullet.FadeInDuringTelegraph();
                    bullet.ConfigurePathMotion(new RibbonProjectileMotion(context.Board, data.flightDuration,
                        t => Vector2.Lerp(start, end, t) + Vector2.up * Mathf.Sin(t * Mathf.PI * 2f) * bend, left ? 180f : -180f));
                }
            }
            public void Cancel() => cancelled = true;
        }
    }
}
