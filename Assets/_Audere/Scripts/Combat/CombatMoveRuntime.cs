using System;
using UnityEngine;

namespace Audere.Combat
{
    public interface ICombatRandom
    {
        float Value01();
        float Range(float minimum, float maximum);
    }

    public sealed class SystemCombatRandom : ICombatRandom
    {
        private readonly System.Random random;
        public SystemCombatRandom(int seed) => random = new System.Random(seed);
        public float Value01() => (float)random.NextDouble();
        public float Range(float minimum, float maximum) => Mathf.Lerp(minimum, maximum, Value01());
    }

    public sealed class CombatMoveSelector
    {
        private readonly CombatMoveSet moveSet;
        private readonly ICombatRandom random;
        private int orderedIndex;

        public CombatMoveSelector(CombatMoveSet moveSet, ICombatRandom random)
        {
            this.moveSet = moveSet ?? throw new ArgumentNullException(nameof(moveSet));
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            if (!moveSet.Validate(out string error))
                throw new InvalidOperationException(error);
        }

        public CombatMoveDefinition Next()
        {
            if (moveSet.SelectionPolicy == CombatMoveSelectionPolicy.OrderedLoop)
            {
                CombatMoveDefinition move = moveSet.Entries[orderedIndex % moveSet.Count].Move;
                orderedIndex = (orderedIndex + 1) % moveSet.Count;
                return move;
            }

            float total = 0f;
            for (int i = 0; i < moveSet.Count; i++)
                total += Mathf.Max(0f, moveSet.Entries[i].Weight);
            if (total <= 0f)
                throw new InvalidOperationException($"Weighted moveset '{moveSet.name}' has no positive weight.");

            float roll = random.Value01() * total;
            for (int i = 0; i < moveSet.Count; i++)
            {
                float weight = Mathf.Max(0f, moveSet.Entries[i].Weight);
                if (weight <= 0f)
                    continue;
                roll -= weight;
                if (roll <= 0f)
                    return moveSet.Entries[i].Move;
            }
            return moveSet.Entries[moveSet.Count - 1].Move;
        }

        public void Reset() => orderedIndex = 0;
    }

    public readonly struct CombatMoveExecutionContext
    {
        public CombatMoveExecutionContext(
            CombatBoardView board,
            CombatEnemyActor actor,
            ICombatRandom random,
            int sessionVersion,
            int phaseVersion)
        {
            Board = board;
            Actor = actor;
            Random = random;
            SessionVersion = sessionVersion;
            PhaseVersion = phaseVersion;
        }

        public CombatBoardView Board { get; }
        public CombatEnemyActor Actor { get; }
        public ICombatRandom Random { get; }
        public int SessionVersion { get; }
        public int PhaseVersion { get; }
    }

    public interface ICombatMoveExecution
    {
        bool IsComplete { get; }
        void Tick(float activeDeltaTime);
        void Cancel();
    }

    internal sealed class LinearProjectilePatternExecution : ICombatMoveExecution
    {
        private readonly LinearProjectilePatternMove data;
        private readonly CombatMoveExecutionContext context;
        private float remaining;
        // The first projectile is emitted on the first active combat tick. Move
        // cadence only applies after that shot, so the player sees the threat
        // immediately instead of entering an apparently empty battle box.
        private float cooldown;
        private int shotIndex;
        private bool cancelled;

        public LinearProjectilePatternExecution(LinearProjectilePatternMove data, CombatMoveExecutionContext context)
        {
            this.data = data;
            this.context = context;
            remaining = data.Duration;
        }

        public bool IsComplete => cancelled || remaining <= 0f;

        public void Tick(float activeDeltaTime)
        {
            if (cancelled || context.Board == null)
                return;
            remaining -= activeDeltaTime;
            cooldown -= activeDeltaTime;
            while (cooldown <= 0f && !IsComplete)
            {
                SpawnShot();
                cooldown += Mathf.Max(.08f, data.ShotInterval);
                shotIndex++;
            }
        }

        public void Cancel() => cancelled = true;

        private void SpawnShot()
        {
            Rect rect = context.Board.PlayArea.rect;
            int count = Mathf.Max(1, data.ProjectilesPerShot);
            Vector2 baseOrigin;
            bool fromLeft = true;

            switch (data.SpawnMode)
            {
                case LinearProjectileSpawnMode.AlternatingSides:
                    fromLeft = shotIndex % 2 == 0;
                    baseOrigin = new Vector2(
                        fromLeft ? rect.xMin + 10f : rect.xMax - 10f,
                        context.Random.Range(rect.yMin + 50f, rect.yMax - 50f));
                    break;
                case LinearProjectileSpawnMode.RandomTop:
                    baseOrigin = new Vector2(
                        context.Random.Range(rect.xMin + 30f, rect.xMax - 30f),
                        rect.yMax - 10f);
                    break;
                default:
                    baseOrigin = context.Actor != null
                        ? context.Board.WorldToPlayArea(context.Actor.ProjectileOriginPosition)
                        : new Vector2(0f, rect.yMax - 10f);
                    baseOrigin = ClampActorAnchorToPlayArea(baseOrigin, rect, 10f);
                    break;
            }

            for (int i = 0; i < count; i++)
            {
                Vector2 origin = baseOrigin;
                Vector2 direction;
                if (data.SpawnMode == LinearProjectileSpawnMode.AlternatingSides)
                {
                    origin.y += (i - (count - 1) * .5f) * data.Spacing;
                    direction = fromLeft ? Vector2.right : Vector2.left;
                }
                else if (data.TargetMode == LinearProjectileTargetMode.Down)
                {
                    if (data.SpawnMode == LinearProjectileSpawnMode.RandomTop)
                        origin.x = context.Random.Range(rect.xMin + 30f, rect.xMax - 30f);
                    direction = Rotate(Vector2.down, context.Random.Range(-data.SpreadDegrees, data.SpreadDegrees));
                }
                else
                {
                    Vector2 aimed = (context.Board.PlayerPosition - origin).normalized;
                    float t = count <= 1 ? .5f : (float)i / (count - 1);
                    direction = Rotate(aimed, Mathf.Lerp(-data.SpreadDegrees, data.SpreadDegrees, t));
                }

                context.Board.SpawnEnemyBullet(
                    data.ProjectilePrefab,
                    origin,
                    direction * data.Speed,
                    context.SessionVersion,
                    context.PhaseVersion);
            }
        }

        private static Vector2 Rotate(Vector2 vector, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
        }

        private static Vector2 ClampActorAnchorToPlayArea(Vector2 origin, Rect playRect, float inset)
        {
            float safeInset = Mathf.Max(0f, inset);
            float minimumX = playRect.xMin + safeInset;
            float maximumX = playRect.xMax - safeInset;
            float minimumY = playRect.yMin + safeInset;
            float maximumY = playRect.yMax - safeInset;
            return new Vector2(
                Mathf.Clamp(origin.x, minimumX, maximumX),
                Mathf.Clamp(origin.y, minimumY, maximumY));
        }
    }
}
