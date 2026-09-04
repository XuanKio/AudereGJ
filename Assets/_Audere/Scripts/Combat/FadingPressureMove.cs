using System.Collections.Generic;
using UnityEngine;

namespace Audere.Combat
{
    [CreateAssetMenu(menuName = "Audere/Combat/Moves/Fading Pressure")]
    public sealed class FadingPressureMove : CombatMoveDefinition
    {
        [SerializeField] private CombatBulletView projectilePrefab;
        [SerializeField] private bool returningOrbit;
        [SerializeField, Min(.2f)] private float beatInterval = .8f;
        [SerializeField, Min(1f)] private float speed = 90f;
        [SerializeField, Min(1f)] private float deflectRadius = 36f;
        [SerializeField, Min(1f)] private float dissolveRadius = 3f;

        public override bool Validate(out string error)
        {
            if (!base.Validate(out error)) return false;
            if (projectilePrefab == null || beatInterval <= 0f || speed <= 0f ||
                dissolveRadius <= 0f || deflectRadius <= dissolveRadius)
            { error = "Fading pressure needs a projectile, positive rhythm/speed and an outer deflection radius."; return false; }
            error = null; return true;
        }
        public override ICombatMoveExecution CreateExecution(CombatMoveExecutionContext context) => new Execution(this, context);

        private sealed class Execution : ICombatMoveExecution
        {
            private readonly FadingPressureMove data;
            private readonly CombatMoveExecutionContext context;
            private readonly List<(CombatBulletView bullet, int lease)> spawned = new List<(CombatBulletView, int)>();
            private float elapsed, cooldown;
            private int beat;
            private bool cancelled;
            public Execution(FadingPressureMove data, CombatMoveExecutionContext context)
            { this.data = data; this.context = context; }
            public bool IsComplete => cancelled || elapsed >= data.Duration;
            public void Tick(float deltaTime)
            {
                if (IsComplete || context.Board == null || context.Board.PlayArea == null) return;
                elapsed += Mathf.Max(0f, deltaTime); cooldown -= Mathf.Max(0f, deltaTime);
                if (cooldown > 0f || elapsed >= data.Duration - 1.2f) return;
                cooldown = data.beatInterval; beat++;
                Rect rect = context.Board.PlayArea.rect;
                int count = data.returningOrbit ? 1 : 3;
                for (int i = 0; i < count; i++)
                {
                    float side = beat % 2 == 0 ? -1f : 1f;
                    Vector2 start = new Vector2(rect.center.x + side * rect.width * .46f,
                        Mathf.Lerp(rect.yMin, rect.yMax, (i + 1f) / (count + 1f)));
                    Vector2 direction = (context.Board.PlayerPosition - start).normalized;
                    var bullet = context.Board.SpawnEnemyBullet(data.projectilePrefab, start,
                        direction * data.speed, context.SessionVersion, context.PhaseVersion, .2f);
                    if (bullet == null) continue;
                    if (data.returningOrbit)
                        bullet.ConfigureReturningOrbit(rect, 4.5f, context.Random.Range(0f, Mathf.PI * 2f), side);
                    bullet.ConfigureHarmlessAvoidance(() => context.Board.CatchZoneCenter, () => context.Board.CatchZoneRadius,
                        data.deflectRadius, data.dissolveRadius);
                    bullet.FadeInDuringTelegraph();
                    spawned.Add((bullet, bullet.PoolLeaseVersion));
                }
            }
            public void Cancel()
            {
                if (cancelled) return;
                cancelled = true;
                foreach (var entry in spawned)
                    if (entry.bullet != null && entry.bullet.PoolLeaseVersion == entry.lease &&
                        entry.bullet.OwnerSessionVersion == context.SessionVersion &&
                        entry.bullet.OwnerPhaseVersion == context.PhaseVersion) entry.bullet.ReturnToPool();
                spawned.Clear();
            }
        }
    }
}
