using System;
using UnityEngine;

namespace Audere.Combat
{
    [CreateAssetMenu(menuName = "Audere/Combat/Moves/Converging Side Corridor", fileName = "Move_ConvergingSideCorridor")]
    public sealed class ConvergingSideCorridorMove : CombatMoveDefinition
    {
        [SerializeField] private CombatBulletView projectilePrefab;
        [SerializeField, Min(.25f)] private float waveInterval = 1.35f;
        [SerializeField, Min(20f)] private float speed = 125f;
        [SerializeField, Min(18f)] private float rowSpacing = 42f;
        [SerializeField, Range(.2f, .8f)] private float startingSafeGapFraction = .46f;
        [SerializeField, Range(.15f, .6f)] private float endingSafeGapFraction = .24f;
        [SerializeField, Min(24f)] private float minimumSafeGap = 72f;
        [SerializeField, Range(0f, 1f)] private float telegraphDuration = .35f;

        public CombatBulletView ProjectilePrefab => projectilePrefab;
        public float WaveInterval => waveInterval;
        public float Speed => speed;
        public float RowSpacing => rowSpacing;
        public float StartingSafeGapFraction => startingSafeGapFraction;
        public float EndingSafeGapFraction => endingSafeGapFraction;
        public float MinimumSafeGap => minimumSafeGap;
        public float TelegraphDuration => telegraphDuration;

        public override bool Validate(out string error)
        {
            if (!base.Validate(out error)) return false;
            if (projectilePrefab == null)
            {
                error = $"Move '{name}' requires a projectile prefab.";
                return false;
            }
            if (endingSafeGapFraction > startingSafeGapFraction)
            {
                error = $"Move '{name}' requires the ending safe gap to be no larger than the starting gap.";
                return false;
            }
            error = null;
            return true;
        }

        public override ICombatMoveExecution CreateExecution(CombatMoveExecutionContext context)
        {
            if (!Validate(out string error)) throw new InvalidOperationException(error);
            return new Execution(this, context);
        }

        private sealed class Execution : ICombatMoveExecution
        {
            private readonly ConvergingSideCorridorMove data;
            private readonly CombatMoveExecutionContext context;
            private float elapsed;
            private float cooldown;
            private bool cancelled;

            public Execution(ConvergingSideCorridorMove data, CombatMoveExecutionContext context)
            {
                this.data = data;
                this.context = context;
            }

            public bool IsComplete => cancelled || elapsed >= data.Duration;

            public void Tick(float activeDeltaTime)
            {
                if (cancelled || context.Board == null || context.Board.PlayArea == null)
                    return;
                elapsed += Mathf.Max(0f, activeDeltaTime);
                cooldown -= activeDeltaTime;
                while (cooldown <= 0f && !IsComplete)
                {
                    SpawnWave();
                    cooldown += data.WaveInterval;
                }
            }

            public void Cancel() => cancelled = true;

            private void SpawnWave()
            {
                Rect rect = context.Board.PlayArea.rect;
                float progress = Mathf.Clamp01(elapsed / Mathf.Max(.01f, data.Duration));
                float gapFraction = Mathf.Lerp(data.StartingSafeGapFraction, data.EndingSafeGapFraction, progress);
                float safeGap = Mathf.Max(data.MinimumSafeGap, rect.height * gapFraction);
                float halfGap = safeGap * .5f;
                float centerY = rect.center.y;
                float inset = 10f;
                for (float y = rect.yMin + data.RowSpacing * .5f; y <= rect.yMax; y += data.RowSpacing)
                {
                    if (Mathf.Abs(y - centerY) <= halfGap)
                        continue;
                    context.Board.SpawnEnemyBullet(
                        data.ProjectilePrefab,
                        new Vector2(rect.xMin + inset, y),
                        Vector2.right * data.Speed,
                        context.SessionVersion,
                        context.PhaseVersion,
                        data.TelegraphDuration);
                    context.Board.SpawnEnemyBullet(
                        data.ProjectilePrefab,
                        new Vector2(rect.xMax - inset, y),
                        Vector2.left * data.Speed,
                        context.SessionVersion,
                        context.PhaseVersion,
                        data.TelegraphDuration);
                }
            }
        }

        private void OnValidate()
        {
            if (!Validate(out string error)) Debug.LogError($"[ConvergingSideCorridorMove] {error}", this);
        }
    }
}
