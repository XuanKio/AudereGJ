using System;
using System.Collections.Generic;
using UnityEngine;

namespace Audere.Combat
{
    // Parallel ranks preserve a real aisle instead of converging on Heart.
    [CreateAssetMenu(menuName = "Audere/Combat/Moves/Procession Gate", fileName = "Move_ProcessionGate")]
    public sealed class ProcessionGateMove : CombatMoveDefinition
    {
        [SerializeField] private CombatBulletView projectilePrefab;
        [SerializeField] private bool fromBothSides;
        [SerializeField] private float[] gapOffsets = { -.23f, 0f, .23f, 0f };
        [SerializeField, Min(100f)] private float gapWidth = 154f;
        [SerializeField, Min(40f)] private float spacing = 64f;
        [SerializeField, Min(40f)] private float speed = 190f;
        [SerializeField, Min(.7f)] private float volleyInterval = 1.15f;
        [SerializeField, Min(.1f)] private float entryDelay = .25f;
        [SerializeField, Min(0f)] private float visualHalfWidth;

        public float GapWidth => gapWidth;
        public bool FromBothSides => fromBothSides;
        public override bool Validate(out string error)
        {
            if (!base.Validate(out error)) return false;
            if (projectilePrefab == null || gapOffsets == null || gapOffsets.Length == 0 ||
                Array.Exists(gapOffsets, x => float.IsNaN(x) || Mathf.Abs(x) > .28f) ||
                gapWidth < 100f || spacing < 40f || speed < 40f || volleyInterval < .7f || entryDelay < .1f ||
                Duration < (gapOffsets.Length - 1) * volleyInterval + entryDelay + 1f)
            { error = "Procession gates require separated ranks, a wide aisle and time for the last rank to pass."; return false; }
            error = null; return true;
        }
        public override ICombatMoveExecution CreateExecution(CombatMoveExecutionContext context)
        {
            if (!Validate(out string error)) throw new InvalidOperationException(error);
            return new Execution(this, context);
        }
        private sealed class Execution : ICombatMoveExecution
        {
            private readonly ProcessionGateMove data;
            private readonly CombatMoveExecutionContext context;
            private readonly List<(CombatBulletView bullet, int lease)> ranks = new List<(CombatBulletView, int)>();
            private float elapsed;
            private int volley;
            private bool done;
            public Execution(ProcessionGateMove data, CombatMoveExecutionContext context) { this.data = data; this.context = context; }
            public bool IsComplete => done;
            public void Tick(float activeDeltaTime)
            {
                if (done || activeDeltaTime <= 0f) return;
                if (context.Board == null || !context.Board.gameObject.activeInHierarchy) { Cancel(); return; }
                elapsed += activeDeltaTime;
                if (elapsed >= data.Duration) { Cancel(); return; }
                // One rank per tick prevents a stalled frame stacking several volleys.
                if (volley < data.gapOffsets.Length && elapsed >= volley * data.volleyInterval)
                    SpawnRank(volley++);
            }
            private void SpawnRank(int index)
            {
                Rect r = context.Board.PlayArea.rect;
                float min = data.fromBothSides ? r.yMin : r.xMin;
                float max = data.fromBothSides ? r.yMax : r.xMax;
                float center = (min + max) * .5f + data.gapOffsets[index] * (max - min);
                int count = Mathf.FloorToInt((max - min - 36f) / data.spacing) + 1;
                float first = (min + max) * .5f - (count - 1) * data.spacing * .5f;
                for (int lane = 0; lane < count; lane++)
                {
                    float p = first + lane * data.spacing;
                    float half = Mathf.Max(data.visualHalfWidth,((RectTransform)data.projectilePrefab.transform).rect.width * .5f);
                    if (Mathf.Abs(p - center) < data.gapWidth * .5f + half + 8f) continue;
                    if (data.fromBothSides)
                    {
                        Spawn(new Vector2(r.xMin + 16f, p), Vector2.right);
                        Spawn(new Vector2(r.xMax - 16f, p), Vector2.left);
                    }
                    else Spawn(new Vector2(p, r.yMax - 16f), Vector2.down);
                }
            }
            private void Spawn(Vector2 position, Vector2 direction)
            {
                var bullet = context.Board.SpawnEnemyBullet(data.projectilePrefab, position, direction * data.speed,
                    context.SessionVersion, context.PhaseVersion, data.entryDelay);
                if (bullet == null) return;
                bullet.RectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
                bullet.FadeInDuringTelegraph();
                ranks.Add((bullet, bullet.PoolLeaseVersion));
            }
            public void Cancel()
            {
                if (done) return;
                done = true;
                foreach (var rank in ranks) context.Board?.ReturnEnemyBullet(rank.bullet, rank.lease);
                ranks.Clear();
            }
        }
    }
}
