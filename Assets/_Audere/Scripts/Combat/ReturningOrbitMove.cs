using System.Collections.Generic;
using UnityEngine;

namespace Audere.Combat
{
    [CreateAssetMenu(menuName = "Audere/Combat/Moves/Returning Orbit")]
    public sealed class ReturningOrbitMove : CombatMoveDefinition
    {
        [SerializeField] private CombatBulletView projectilePrefab;
        [SerializeField, Range(1, 6)] private int maximumSimultaneous = 3;
        [SerializeField, Min(.2f)] private float initialFlightDuration = 4f;
        [SerializeField, Min(.2f)] private float finalFlightDuration = 2.8f;
        [SerializeField, Min(.1f)] private float telegraphDuration = .6f;
        [SerializeField, Min(0f)] private float betweenWaves = .55f;
        [SerializeField] private bool horizontalTraversal;
        public bool HorizontalTraversal => horizontalTraversal;
        public int MaximumSimultaneous => maximumSimultaneous;
        public override bool Validate(out string error)
        {
            if (!base.Validate(out error)) return false;
            if (projectilePrefab == null || maximumSimultaneous < 1 || initialFlightDuration <= 0f || finalFlightDuration <= 0f)
            { error = "Returning orbit requires a projectile, positive flight durations and wave count."; return false; }
            error = null; return true;
        }
        public static Vector2 EvaluatePosition(Rect bounds, float progress, float startAngle, float direction)
        {
            float t = Mathf.Clamp01(progress);
            float outbound = t <= .5f ? t * 2f : (1f - t) * 2f;
            float angle = startAngle + direction * outbound * Mathf.PI * 2f;
            return bounds.center + new Vector2(Mathf.Cos(angle) * bounds.width * .43f, Mathf.Sin(angle * 2f) * bounds.height * .43f);
        }
        public static Vector2 EvaluateHorizontalPosition(Rect bounds, float progress, float lane, float direction)
        {
            float t = Mathf.Clamp01(progress);
            float outbound = t <= .5f ? t * 2f : (1f - t) * 2f;
            float x = Mathf.SmoothStep(0f, 1f, outbound);
            if (direction < 0f) x = 1f - x;
            return new Vector2(Mathf.Lerp(bounds.xMin, bounds.xMax, x), Mathf.Lerp(bounds.yMin, bounds.yMax, Mathf.Clamp01(lane)));
        }
        public override ICombatMoveExecution CreateExecution(CombatMoveExecutionContext context) => new Execution(this, context);
        private sealed class Execution : ICombatMoveExecution
        {
            private readonly ReturningOrbitMove data;
            private readonly CombatMoveExecutionContext context;
            private readonly List<CombatBulletView> bullets = new List<CombatBulletView>();
            private int wave;
            private float cooldown = .2f;
            private bool cancelled;
            public Execution(ReturningOrbitMove data, CombatMoveExecutionContext context) { this.data = data; this.context = context; }
            public bool IsComplete => cancelled || wave >= data.maximumSimultaneous && cooldown <= 0f;
            public void Tick(float delta)
            {
                if (cancelled || context.Board == null || context.Board.PlayArea == null) return;
                cooldown -= Mathf.Max(0f, delta);
                if (cooldown > 0f || wave >= data.maximumSimultaneous) return;
                wave++;
                context.Board.SetMechanicHint($"Né đường quay lại — {wave}/{data.maximumSimultaneous}");
                float flight = Mathf.Lerp(data.initialFlightDuration, data.finalFlightDuration,
                    data.maximumSimultaneous == 1 ? 0f : (wave - 1f) / (data.maximumSimultaneous - 1f));
                Rect bounds = context.Board.PlayArea.rect;
                // Keep the enlarged, spinning sprite inside the field at both turns.
                var size = data.projectilePrefab.GetComponent<RectTransform>().rect.size;
                float inset = size.magnitude * .5f;
                Rect lanes = Rect.MinMaxRect(bounds.xMin + Mathf.Min(inset, bounds.width * .45f),
                    bounds.yMin + Mathf.Min(inset, bounds.height * .45f),
                    bounds.xMax - Mathf.Min(inset, bounds.width * .45f), bounds.yMax - Mathf.Min(inset, bounds.height * .45f));
                float startAngle = context.Random.Range(0f, Mathf.PI * 2f);
                for (int i = 0; i < wave; i++)
                {
                    float angle = startAngle + i * Mathf.PI * 2f / wave;
                    float direction = context.Random.Value01() < .5f ? -1f : 1f;
                    float lane = (i + 1f) / (wave + 1f);
                    Vector2 start = data.horizontalTraversal
                        ? EvaluateHorizontalPosition(lanes, 0f, lane, direction)
                        : EvaluatePosition(bounds, 0f, angle, direction);
                    var bullet = context.Board.SpawnEnemyBullet(data.projectilePrefab,
                        start, Vector2.zero,
                        context.SessionVersion, context.PhaseVersion, data.telegraphDuration);
                    if (bullet != null)
                    {
                        if (data.horizontalTraversal) bullet.ConfigureHorizontalReturn(lanes, flight, lane, direction);
                        else bullet.ConfigureReturningOrbit(bounds, flight, angle, direction);
                        bullets.Add(bullet);
                    }
                }
                cooldown = flight + data.telegraphDuration + data.betweenWaves;
            }
            public void Cancel()
            {
                if (cancelled) return;
                cancelled = true;
                foreach (var bullet in bullets)
                    if (bullet != null && bullet.OwnerSessionVersion == context.SessionVersion && bullet.OwnerPhaseVersion == context.PhaseVersion)
                        bullet.ReturnToPool();
                bullets.Clear();
                context.Board?.SetMechanicHint(null);
            }
        }
    }
}
