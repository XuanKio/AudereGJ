using System;
using UnityEngine;

namespace Audere.Combat
{
    [CreateAssetMenu(menuName = "Audere/Combat/Moves/Corner Bloom")]
    public sealed class CornerBloomMove : CombatMoveDefinition
    {
        [SerializeField] private CombatBulletView projectilePrefab;
        [SerializeField, Min(1f)] private float waveInterval = 2f;
        [SerializeField, Min(.4f)] private float telegraphDuration = .8f;
        public override bool Validate(out string error)
        {
            if (!base.Validate(out error)) return false;
            if (projectilePrefab == null || waveInterval < 1f) { error = "Corner bloom needs a prefab and at least a one-second wave interval."; return false; }
            return true;
        }
        public override ICombatMoveExecution CreateExecution(CombatMoveExecutionContext context)
        {
            if (!Validate(out var error)) throw new InvalidOperationException(error);
            return new Execution(this, context);
        }
        private sealed class Execution : ICombatMoveExecution
        {
            private readonly CornerBloomMove data;
            private readonly CombatMoveExecutionContext context;
            private float elapsed, nextWave;
            private int wave;
            private bool cancelled;
            public Execution(CornerBloomMove data, CombatMoveExecutionContext context) { this.data = data; this.context = context; }
            public bool IsComplete => cancelled || elapsed >= data.Duration;
            public void Tick(float dt)
            {
                if (IsComplete || dt <= 0f) return;
                elapsed += dt;
                if (elapsed < nextWave || elapsed > data.Duration - 3.4f) return;
                nextWave = elapsed + data.waveInterval;
                Rect r = context.Board.PlayArea.rect;
                int corner = wave++ % 4;
                Vector2 origin = new Vector2(corner % 2 == 0 ? r.xMin + 24f : r.xMax - 24f, corner < 2 ? r.yMax - 24f : r.yMin + 24f);
                Vector2 toward = (r.center - origin).normalized;
                float centerAngle = Mathf.Atan2(toward.y, toward.x);
                for (int i = 0; i < 5; i++)
                {
                    float angle = centerAngle + (i - 2f) * 22f * Mathf.Deg2Rad;
                    Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    var bullet = context.Board.SpawnExteriorEnemyBullet(data.projectilePrefab, origin,
                        context.SessionVersion, context.PhaseVersion, data.telegraphDuration);
                    bullet.FadeInDuringTelegraph();
                    bullet.ConfigurePathMotion(new RibbonProjectileMotion(context.Board, 2.6f,
                        t => origin + direction * (t * 520f), 220f));
                }
            }
            public void Cancel() => cancelled = true;
        }
    }
}
