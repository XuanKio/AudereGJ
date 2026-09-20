using System;
using UnityEngine;

namespace Audere.Combat
{
    [CreateAssetMenu(menuName = "Audere/Combat/Moves/Enemy Mount Dive", fileName = "Move_EnemyMountDive")]
    public sealed class EnemyMountDiveMove : CombatMoveDefinition
    {
        [SerializeField] private float[] impactOffsets = { 0f, -.18f, .18f };
        [SerializeField] private bool aimAtPlayer;
        [SerializeField, Min(.2f)] private float windup = .65f;
        [SerializeField, Min(.1f)] private float plunge = .4f;
        [SerializeField, Min(0f)] private float hold = .3f;
        [SerializeField, Min(.1f)] private float returnDuration = .55f;
        [SerializeField, Min(.2f)] private float recovery = .65f;
        [SerializeField, Range(.03f, .15f)] private float separationFraction = .11f;
        [SerializeField, Range(.08f, .28f)] private float bodyWidthFraction = .22f;
        [SerializeField, Min(16f)] private float bodyHeight = 148f;
        [SerializeField] private Material rainbowEchoMaterial;

        public float CycleDuration => windup + plunge + hold + returnDuration + recovery;
        public float SequenceDuration => CycleDuration * (impactOffsets?.Length ?? 0);
        public Material RainbowEchoMaterial => rainbowEchoMaterial;

        public override bool Validate(out string error)
        {
            if (!base.Validate(out error)) return false;
            if (impactOffsets == null || impactOffsets.Length == 0 ||
                Array.Exists(impactOffsets, x => float.IsNaN(x) || Mathf.Abs(x) > .22f))
            { error = "Mount dives require impact offsets within the middle 44% of the board."; return false; }
            if (windup < .2f || plunge < .1f || hold < 0f || returnDuration < .1f || recovery < .2f ||
                Duration + .001f < SequenceDuration || rainbowEchoMaterial == null)
            { error = "Mount dive needs valid timing, an echo material, and enough duration to return every plunge."; return false; }
            if (separationFraction < .03f || separationFraction > .15f || bodyWidthFraction < .08f ||
                bodyWidthFraction > .28f || bodyHeight < 16f)
            { error = "Mount dive geometry must preserve the authored dodge space."; return false; }
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
            private readonly EnemyMountDiveMove data;
            private readonly CombatMoveExecutionContext context;
            private readonly Vector2 home;
            private Vector2 lastBody;
            private int cycle = -1;
            private float impactX, elapsed;
            private bool cancelled, restored;

            public Execution(EnemyMountDiveMove data, CombatMoveExecutionContext context)
            {
                this.data = data;
                this.context = context;
                if (context.Board == null || context.Actor == null ||
                    !context.Board.BeginMountDive(this, context.Actor, data.rainbowEchoMaterial))
                { cancelled = true; return; }
                home = context.Board.MountDiveHome;
                lastBody = home;
            }

            public bool IsComplete => cancelled || restored || elapsed >= data.Duration;

            public void Tick(float activeDeltaTime)
            {
                if (cancelled || restored || activeDeltaTime <= 0f) return;
                if (context.Board == null || !context.Board.OwnsMountDive(this))
                { cancelled = true; return; }
                // Bounded substeps preserve the body sweep and impact across a slow frame.
                float remaining = activeDeltaTime;
                while (remaining > .00001f && !restored)
                {
                    float dt = Mathf.Min(remaining, 1f / 60f);
                    remaining -= dt;
                    elapsed = Mathf.Min(data.Duration, elapsed + dt);
                    if (elapsed >= data.SequenceDuration || elapsed >= data.Duration)
                    { Restore(); break; }
                    int nextCycle = Mathf.FloorToInt(elapsed / data.CycleDuration);
                    Rect rect = context.Board.PlayArea.rect;
                    if (nextCycle != cycle)
                    {
                        cycle = nextCycle;
                        float offset = data.impactOffsets[cycle];
                        if (data.aimAtPlayer) offset += context.Board.PlayerPosition.x / rect.width;
                        impactX = rect.center.x + Mathf.Clamp(offset, -.22f, .22f) * rect.width;
                        lastBody = home;
                    }
                    float t = elapsed - cycle * data.CycleDuration;
                    Vector2 apex = new Vector2(impactX, Mathf.Max(home.y, rect.yMax + 190f) + 45f);
                    Vector2 bottom = new Vector2(impactX, rect.yMin + 24f);
                    Vector2 body;
                    float opening = 0f, tilt = 0f, glow = 0f;
                    bool dangerous = false, echoes = false, constrainGap = false;
                    if (t < data.windup)
                    {
                        context.Board.ShowAttackWarning(this,new Vector2(impactX,rect.yMax+30f),t);
                        float u = Smooth(t / data.windup);
                        body = Vector2.Lerp(home, apex, u);
                        tilt = Mathf.Sin(t * 42f) * 2.5f * u;
                    }
                    else if ((t -= data.windup) < data.plunge)
                    {
                        float u = t / data.plunge;
                        body = Vector2.Lerp(apex, bottom, u * u);
                        dangerous = echoes = true;
                        opening = Smooth(Mathf.InverseLerp(rect.yMax + 65f, rect.center.y - 45f, body.y));
                        glow = Mathf.Sin(opening * Mathf.PI);
                    }
                    else if ((t -= data.plunge) < data.hold)
                    { body = bottom; opening = 1f; constrainGap = true; }
                    else if ((t -= data.hold) < data.returnDuration)
                    {
                        float u = Smooth(t / data.returnDuration);
                        body = Vector2.Lerp(bottom, home, u);
                        // A curved, harmless return keeps the recovery readable.
                        body.x += Mathf.Sin(u * Mathf.PI) * (impactX <= rect.center.x ? -75f : 75f);
                        opening = 1f;
                        constrainGap = echoes = true;
                    }
                    else
                    {
                        t -= data.returnDuration;
                        body = home;
                        opening = 1f - Smooth(Mathf.Clamp01(t / (data.recovery * .7f)));
                        constrainGap = opening > .05f;
                    }
                    context.Board.SetMountDivePose(this, body, tilt, dt, echoes);
                    if(dangerous || constrainGap)context.Board.HideAttackWarning(this);
                    if (dangerous)
                        context.Board.AccumulateMountDiveBody(this, lastBody, body,
                            rect.width * data.bodyWidthFraction, data.bodyHeight);
                    context.Board.SetMountDiveSplit(this, impactX,
                        rect.width * data.separationFraction * opening, glow, constrainGap);
                    lastBody = body;
                }
            }

            public void Cancel() { cancelled = true; Restore(); }
            private void Restore()
            {
                if (restored) return;
                restored = true;
                context.Board?.HideAttackWarning(this);
                context.Board?.EndMountDive(this);
            }
            private static float Smooth(float t) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
        }
    }
}
