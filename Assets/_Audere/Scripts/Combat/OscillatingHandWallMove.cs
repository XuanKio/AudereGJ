using System;
using System.Collections.Generic;
using UnityEngine;

namespace Audere.Combat
{
    [CreateAssetMenu(menuName = "Audere/Combat/Moves/Oscillating Hand Wall")]
    public sealed class OscillatingHandWallMove : CombatMoveDefinition
    {
        [SerializeField] private CombatBulletView handPrefab;
        [SerializeField, Range(3, 12)] private int handsPerSide = 8;
        [SerializeField, Min(.5f)] private float warning = .8f;
        [SerializeField, Min(1f)] private float period = 2.3f;
        [SerializeField, Range(.1f, .35f)] private float meanDepth = .27f;
        [SerializeField, Range(.02f, .2f)] private float amplitude = .14f;
        public override bool Validate(out string error)
        {
            if (!base.Validate(out error)) return false;
            if (handPrefab == null || handsPerSide < 3 || handsPerSide > 12 || warning < .5f ||
                period < 1f || Duration <= warning + 1f || meanDepth <= amplitude || meanDepth + amplitude > .48f)
            { error = "Hand waves require bounded density, warning and an open middle corridor."; return false; }
            return true;
        }
        public override ICombatMoveExecution CreateExecution(CombatMoveExecutionContext context)
        {
            if (!Validate(out string error)) throw new InvalidOperationException(error);
            return new Execution(this, context);
        }
        private sealed class Execution : ICombatMoveExecution
        {
            private readonly OscillatingHandWallMove data;
            private readonly CombatMoveExecutionContext context;
            private readonly List<(CombatBulletView bullet, int lease)> hands = new List<(CombatBulletView, int)>();
            private float elapsed;
            private bool started, cancelled;
            public bool IsComplete => cancelled || elapsed >= data.Duration;
            public Execution(OscillatingHandWallMove data, CombatMoveExecutionContext context) { this.data = data; this.context = context; }
            public void Tick(float dt)
            {
                if (IsComplete || dt <= 0f) return;
                if (context.Board == null || !context.Board.isActiveAndEnabled) { Cancel(); return; }
                if (!started) { started = true; Spawn(); }
                elapsed += dt;
                if (elapsed >= data.Duration) Cancel();
            }
            private void Spawn()
            {
                Rect bounds = context.Board.PlayArea.rect;
                float life = data.Duration - data.warning;
                for (int side = 0; side < 2; side++) for (int index = 0; index < data.handsPerSide; index++)
                {
                    bool top = side == 1;
                    float lane = (index + .5f) / data.handsPerSide;
                    float x = Mathf.Lerp(bounds.xMin + 24f, bounds.xMax - 24f, lane);
                    float edge = top ? bounds.yMax : bounds.yMin;
                    Vector2 start = new Vector2(x, edge + (top ? -10f : 10f));
                    float angle = top ? 180f : 0f;
                    var b = context.Board.SpawnEnemyBullet(data.handPrefab, start, Vector2.zero,
                        context.SessionVersion, context.PhaseVersion, data.warning);
                    if (b == null) continue;
                    hands.Add((b, b.PoolLeaseVersion));
                    b.ConfigurePathMotion(new ParametricProjectileMotion(life, t =>
                    {
                        float sec = t * life;
                        float envelope = Mathf.SmoothStep(0f, 1f, sec / .5f) * Mathf.SmoothStep(0f, 1f, (life - sec) / .6f);
                        // Opposing walls share a travelling wave: the open corridor bends, never closes.
                        float wave = Mathf.Sin(lane * Mathf.PI * 2f - sec * Mathf.PI * 2f / data.period);
                        float depth = (data.meanDepth + (top ? -wave : wave) * data.amplitude) * bounds.height;
                        return new Vector2(x, edge + (top ? -1f : 1f) * Mathf.Lerp(10f, depth, envelope));
                    }, t => angle + Mathf.Sin(lane * 6.283f - t * life * 2.7f) * 7f));
                    b.FadeInDuringTelegraph();
                }
            }
            public void Cancel()
            {
                if (cancelled) return;
                cancelled = true;
                foreach (var hand in hands) context.Board?.ReturnEnemyBullet(hand.bullet, hand.lease);
                hands.Clear();
            }
        }
    }
}
