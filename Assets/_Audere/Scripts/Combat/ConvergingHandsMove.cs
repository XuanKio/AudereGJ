using System;
using System.Collections.Generic;
using UnityEngine;

namespace Audere.Combat
{
    [CreateAssetMenu(menuName = "Audere/Combat/Moves/Converging Hands")]
    public sealed class ConvergingHandsMove : CombatMoveDefinition
    {
        [SerializeField] private CombatBulletView handPrefab;
        [SerializeField, Range(2, 3)] private int gripHands = 3;
        [SerializeField, Min(.6f)] private float warning = .9f;
        [SerializeField, Min(.3f)] private float closeDuration = .45f;
        [SerializeField, Range(1f, 3f)] private float holdDuration = 2.4f;
        [SerializeField, Min(.4f)] private float releaseDuration = .55f;
        [SerializeField, Min(.28f)] private float stabInterval = .34f;
        [SerializeField, Min(.25f)] private float stabWarning = .3f;
        public override bool Validate(out string error)
        {
            if (!base.Validate(out error)) return false;
            if (handPrefab == null || gripHands < 2 || gripHands > 3 || warning < .6f || closeDuration < .3f ||
                holdDuration < 1f || holdDuration > 3f || releaseDuration < .4f || stabInterval < .28f || stabWarning < .25f ||
                Duration < warning + closeDuration + holdDuration + releaseDuration + .3f)
            { error = "Clasp requires 2–3 hands, a readable warning, finite protected hold and recovery."; return false; }
            return true;
        }
        public override ICombatMoveExecution CreateExecution(CombatMoveExecutionContext context)
        {
            if (!Validate(out string error)) throw new InvalidOperationException(error);
            return new Execution(this, context);
        }
        private sealed class Execution : ICombatMoveExecution
        {
            private sealed class Hand { public CombatBulletView Bullet; public int Lease; public Vector2 Start, End; }
            private readonly ConvergingHandsMove data;
            private readonly CombatMoveExecutionContext context;
            private readonly List<Hand> grips = new List<Hand>(), stabs = new List<Hand>();
            private float elapsed;
            private int shot;
            private bool cancelled, started, captured, released;
            private Vector2 center, normalizedCenter;
            public bool IsComplete => cancelled || elapsed >= data.Duration;
            public Execution(ConvergingHandsMove data, CombatMoveExecutionContext context) { this.data = data; this.context = context; }
            public void Tick(float dt)
            {
                if (IsComplete || dt <= 0f) return;
                if (context.Board == null || !context.Board.isActiveAndEnabled) { Cancel(); return; }
                if (!started) { started = true; SpawnGrips(); }
                elapsed += dt;
                if (elapsed >= data.Duration) { Cancel(); return; }
                float closedAt = data.warning + data.closeDuration;
                float releaseAt = closedAt + data.holdDuration;
                if (elapsed >= data.warning && !captured)
                {
                    captured = true;
                    center = context.Board.PlayerPosition;
                    Rect r = context.Board.PlayArea.rect;
                    // Keep every closing palm visible even when the cursor was against a corner.
                    float inset = Mathf.Min(64f, Mathf.Min(r.width, r.height) * .22f);
                    center = new Vector2(Mathf.Clamp(center.x, r.xMin + inset, r.xMax - inset),
                        Mathf.Clamp(center.y, r.yMin + inset, r.yMax - inset));
                    normalizedCenter = new Vector2(Mathf.InverseLerp(r.xMin, r.xMax, center.x), Mathf.InverseLerp(r.yMin, r.yMax, center.y));
                    for (int i = 0; i < grips.Count; i++)
                        grips[i].End = InBulletSpace(grips[i].Bullet, center + Direction(i) * 27f);
                }
                bool held = elapsed >= data.warning && elapsed < releaseAt;
                if (held) context.Board.SetForcedPlayerControl(this, normalizedCenter, 1200f, dt);
                else context.Board.ReleaseForcedPlayerControl(this);
                if (elapsed >= releaseAt && !released) { released = true; Return(stabs); }
                foreach (var h in grips)
                {
                    if (!Live(h)) continue;
                    float close = Mathf.SmoothStep(0f, 1f, (elapsed - data.warning) / data.closeDuration);
                    float open = Mathf.SmoothStep(0f, 1f, (elapsed - releaseAt) / data.releaseDuration);
                    h.Bullet.RectTransform.anchoredPosition = Vector2.Lerp(h.Start, h.End, close * (1f - open));
                    h.Bullet.SetPresentationFade(Mathf.Clamp01(elapsed / data.warning) * (1f - open));
                }
                // Targeted strikes land during the hold. Only the grip hands remain non-damaging.
                while (elapsed >= closedAt + shot * data.stabInterval &&
                       closedAt + shot * data.stabInterval < releaseAt - .75f && !released)
                { SpawnStab(shot++); }
                context.Board.SetMechanicHint(elapsed < data.warning ? "Những bàn tay đang khép lại…" : held ? "Bị giữ — bàn tay sẽ buông ra" : null);
            }
            private Vector2 Direction(int index)
            {
                float a = (90f + index * 360f / data.gripHands) * Mathf.Deg2Rad;
                return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            }
            private void SpawnGrips()
            {
                center = context.Board.PlayerPosition;
                Rect r = context.Board.PlayArea.rect;
                for (int i = 0; i < data.gripHands; i++)
                {
                    Vector2 outward = Direction(i);
                    Vector2 startInPlayArea = center + outward * 150f;
                    startInPlayArea = new Vector2(Mathf.Clamp(startInPlayArea.x, r.xMin + 28f, r.xMax - 28f), Mathf.Clamp(startInPlayArea.y, r.yMin + 28f, r.yMax - 28f));
                    var b = context.Board.SpawnEnemyBullet(data.handPrefab, startInPlayArea, Vector2.zero, context.SessionVersion, context.PhaseVersion, data.warning);
                    if (b == null) continue;
                    Vector2 start = InBulletSpace(b, startInPlayArea);
                    b.RectTransform.anchoredPosition = start;
                    b.RectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(-outward.y, -outward.x) * Mathf.Rad2Deg - 90f);
                    // Grip is a control telegraph, not another damaging projectile.
                    b.BeginPresentationFade(); b.SetPresentationFade(0f);
                    grips.Add(new Hand { Bullet = b, Lease = b.PoolLeaseVersion, Start = start, End = center + outward * 27f });
                }
            }
            private void SpawnStab(int index)
            {
                Rect r = context.Board.PlayArea.rect;
                int side = index % 4;
                float lane = context.Random.Range(.2f, .8f);
                Vector2 fromInPlayArea = side == 0 ? new Vector2(r.xMin + 8f, Mathf.Lerp(r.yMin, r.yMax, lane)) :
                    side == 1 ? new Vector2(Mathf.Lerp(r.xMin, r.xMax, lane), r.yMax - 8f) :
                    side == 2 ? new Vector2(r.xMax - 8f, Mathf.Lerp(r.yMin, r.yMax, lane)) :
                    new Vector2(Mathf.Lerp(r.xMin, r.xMax, lane), r.yMin + 8f);
                Vector2 target = center;
                Vector2 direction = (target - fromInPlayArea).normalized;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
                var b = context.Board.SpawnEnemyBullet(data.handPrefab, fromInPlayArea, Vector2.zero, context.SessionVersion, context.PhaseVersion, data.stabWarning);
                if (b == null) return;
                Vector2 from = InBulletSpace(b, fromInPlayArea);
                b.RectTransform.anchoredPosition = from;
                b.AllowHitDuringForcedMovement();
                stabs.Add(new Hand { Bullet = b, Lease = b.PoolLeaseVersion });
                // Hold the palm on the captured heart for a short beat. A single sine
                // apex can pass between collision samples at low frame rates. Resolve
                // the target live because the forced cursor may still be settling and
                // the Battle Box can shift while the stab is travelling.
                b.ConfigurePathMotion(new ParametricProjectileMotion(.65f, t =>
                    Vector2.Lerp(from, InBulletSpace(b, context.Board.PlayerPosition), StabReach(t)), t => angle));
                b.FadeInDuringTelegraph();
            }
            private static float StabReach(float t)
            {
                t = Mathf.Clamp01(t);
                float reach = Mathf.Min(t / .44f, (1f - t) / .44f);
                return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(reach));
            }
            private Vector2 InBulletSpace(CombatBulletView bullet, Vector2 playAreaPoint)
            {
                if (bullet == null || bullet.RectTransform == null || bullet.RectTransform.parent == null)
                    return playAreaPoint;
                Vector3 world = context.Board.PlayArea.TransformPoint(playAreaPoint);
                Vector3 local = bullet.RectTransform.parent.InverseTransformPoint(world);
                return new Vector2(local.x, local.y);
            }
            private static bool Live(Hand h) => h.Bullet != null && h.Bullet.gameObject.activeSelf && h.Bullet.PoolLeaseVersion == h.Lease;
            private void Return(List<Hand> hands)
            {
                foreach (var h in hands) context.Board?.ReturnEnemyBullet(h.Bullet, h.Lease);
                hands.Clear();
            }
            public void Cancel()
            {
                if (cancelled) return;
                cancelled = true; Return(grips); Return(stabs);
                context.Board?.ReleaseForcedPlayerControl(this); context.Board?.SetMechanicHint(null);
            }
        }
    }
}
