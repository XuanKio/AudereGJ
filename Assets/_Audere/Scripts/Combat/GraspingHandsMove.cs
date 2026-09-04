using System;
using System.Collections.Generic;
using UnityEngine;

namespace Audere.Combat
{
    [CreateAssetMenu(menuName = "Audere/Combat/Moves/Grasping Hands")]
    public sealed class GraspingHandsMove : CombatMoveDefinition
    {
        [SerializeField] private CombatBulletView handPrefab;
        [SerializeField] private CombatBulletView bulletPrefab;
        [SerializeField] private bool pullToCorners;
        [SerializeField, Min(.5f)] private float warning = .8f;
        [SerializeField, Min(.4f)] private float strike = .65f;
        [SerializeField, Min(.3f)] private float hold = .7f;
        [SerializeField, Min(.35f)] private float retreat = .55f;
        [SerializeField, Min(.1f)] private float rest = .25f;
        [SerializeField, Range(1, 3)] private int handsPerBeat = 2;
        [SerializeField, Range(0, 3)] private int palmVolleys = 2;
        [SerializeField, Range(4, 20)] private int bulletsPerVolley = 10;
        [SerializeField, Min(20f)] private float bulletSpeed = 110f;
        [SerializeField, Range(0f, 90f)] private float sweepDegrees = 32f;
        public bool PullToCorners => pullToCorners;
        public float BeatDuration => warning + strike + hold + retreat + rest;

        public override bool Validate(out string error)
        {
            if (!base.Validate(out error)) return false;
            if (handPrefab == null || bulletPrefab == null || warning < .5f || strike < .4f || hold < .3f ||
                retreat < .35f || rest < .1f || handsPerBeat < 1 || handsPerBeat > 3 ||
                palmVolleys < 0 || palmVolleys > 3 || bulletsPerVolley < 4 || bulletsPerVolley > 20 || bulletSpeed <= 0f)
            { error = "Hands require both prefabs, bounded counts and readable warning/strike/recovery timings."; return false; }
            return true;
        }

        public override ICombatMoveExecution CreateExecution(CombatMoveExecutionContext context)
        {
            if (!Validate(out string error)) throw new InvalidOperationException(error);
            return new Execution(this, context);
        }

        private sealed class Execution : ICombatMoveExecution
        {
            private sealed class Hand
            {
                public CombatBulletView Bullet;
                public int Lease;
                public Vector2 Start, End;
            }
            private readonly GraspingHandsMove data;
            private readonly CombatMoveExecutionContext context;
            private readonly List<Hand> hands = new List<Hand>();
            private float elapsed;
            private int beat = -1, emittedVolleys, lastCorner = -1;
            private bool cancelled;
            private Vector2 corner;
            public bool IsComplete => cancelled || elapsed >= data.Duration;
            public Execution(GraspingHandsMove data, CombatMoveExecutionContext context) { this.data = data; this.context = context; }

            public void Tick(float dt)
            {
                if (IsComplete || dt <= 0f) return;
                if (context.Board == null || !context.Board.isActiveAndEnabled) { Cancel(); return; }
                elapsed += dt;
                if (elapsed >= data.Duration) { Cancel(); return; }
                int index = Mathf.FloorToInt(elapsed / data.BeatDuration);
                float time = elapsed - index * data.BeatDuration;
                if (index != beat) { ReleaseHands(); beat = index; emittedVolleys = 0; EmitHands(); }
                bool pulling = data.pullToCorners && time >= data.warning && time < data.warning + data.strike;
                if (pulling) context.Board.SetForcedPlayerControl(this, corner, 1050f, dt);
                else context.Board.ReleaseForcedPlayerControl(this);
                if (data.pullToCorners)
                    context.Board.SetMechanicHint(time < data.warning ? "Cánh tay đang chụp tới…" : pulling ? "Bị kéo — chờ bàn tay buông ra" : null);

                float volleyStart = data.warning + data.strike;
                while (emittedVolleys < data.palmVolleys && time >= volleyStart + emittedVolleys * .35f)
                {
                    foreach (var h in hands)
                    {
                        if (h.Bullet == null || !h.Bullet.gameObject.activeSelf || h.Bullet.PoolLeaseVersion != h.Lease) continue;
                        Vector2 origin = h.Bullet.RectTransform.anchoredPosition;
                        for (int i = 0; i < data.bulletsPerVolley; i++)
                        {
                            // One broad missing sector keeps the ring readable, even at full density.
                            if (i == (beat + emittedVolleys) % data.bulletsPerVolley || i == (beat + emittedVolleys + 1) % data.bulletsPerVolley) continue;
                            float a = (i * 360f / data.bulletsPerVolley + emittedVolleys * 12f) * Mathf.Deg2Rad;
                            context.Board.SpawnEnemyBullet(data.bulletPrefab, origin,
                                new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * data.bulletSpeed, context.SessionVersion, context.PhaseVersion);
                        }
                    }
                    emittedVolleys++;
                }
            }

            private void EmitHands()
            {
                Rect r = context.Board.PlayArea.rect;
                int cornerIndex = Mathf.FloorToInt(context.Random.Value01() * 4f) % 4;
                if (cornerIndex == lastCorner) cornerIndex = (cornerIndex + 1) % 4;
                lastCorner = cornerIndex;
                corner = new Vector2(cornerIndex % 2 == 0 ? .12f : .88f, cornerIndex < 2 ? .15f : .85f);
                for (int i = 0; i < data.handsPerBeat; i++)
                {
                    int side = (int)(context.Random.Value01() * 4f) % 4;
                    Vector2 end = data.pullToCorners
                        ? new Vector2(Mathf.Lerp(r.xMin, r.xMax, corner.x), Mathf.Lerp(r.yMin, r.yMax, corner.y))
                        : new Vector2(Mathf.Lerp(r.xMin, r.xMax, context.Random.Range(.23f, .77f)),
                            Mathf.Lerp(r.yMin, r.yMax, context.Random.Range(.25f, .75f)));
                    Vector2 start = side == 0 ? new Vector2(r.xMin + 18f, end.y) : side == 1 ? new Vector2(r.xMax - 18f, end.y) :
                        side == 2 ? new Vector2(end.x, r.yMin + 18f) : new Vector2(end.x, r.yMax - 18f);
                    if (data.pullToCorners) start = context.Board.PlayerPosition;
                    Vector2 direction = (end - start).sqrMagnitude > .01f ? (end - start).normalized : Vector2.up;
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
                    var b = context.Board.SpawnEnemyBullet(data.handPrefab, start, Vector2.zero,
                        context.SessionVersion, context.PhaseVersion, data.warning);
                    if (b == null) continue;
                    b.RectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
                    var h = new Hand { Bullet = b, Lease = b.PoolLeaseVersion, Start = start, End = end };
                    hands.Add(h);
                    float flight = data.strike + data.hold + data.retreat;
                    b.ConfigurePathMotion(new ParametricProjectileMotion(flight, t =>
                    {
                        float sec = t * flight;
                        if (sec < data.strike) return Vector2.Lerp(start, end, Mathf.SmoothStep(0f, 1f, sec / data.strike));
                        if (sec < data.strike + data.hold) return end;
                        return Vector2.Lerp(end, start, Mathf.SmoothStep(0f, 1f, (sec - data.strike - data.hold) / data.retreat));
                    }, t => angle + Mathf.Sin(t * Mathf.PI * 2f) * data.sweepDegrees));
                    b.FadeInDuringTelegraph();
                }
            }

            private void ReleaseHands()
            {
                foreach (var h in hands) context.Board?.ReturnEnemyBullet(h.Bullet, h.Lease);
                hands.Clear();
                context.Board?.ReleaseForcedPlayerControl(this);
            }
            public void Cancel()
            {
                if (cancelled) return;
                cancelled = true; ReleaseHands();
                if (data.pullToCorners) context.Board?.SetMechanicHint(null);
            }
        }
    }
}
