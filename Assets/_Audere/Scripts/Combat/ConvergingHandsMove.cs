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
        [SerializeField, Min(100f)] private float startDistance = 170f;
        [SerializeField, Min(100f)] private float chaseSpeed = 390f;
        [SerializeField, Min(.45f)] private float chaseDuration = .82f;
        [SerializeField, Min(.05f)] private float trackingDuration = .28f;
        [SerializeField, Min(.05f)] private float appearDuration = .14f;
        [SerializeField, Min(.2f)] private float warningDuration = .4f;
        [SerializeField, Min(.15f)] private float fadeDuration = .3f;
        [SerializeField, Min(.1f)] private float gapDuration = .18f;
        [SerializeField, Min(.8f)] private float captureHoldDuration = 1.12f;
        [SerializeField, Range(2, 4)] private int stabCount = 4;
        [SerializeField, Min(.1f)] private float stabInterval = .12f;
        [SerializeField, Min(.1f)] private float stabWarning = .16f;
        [SerializeField, Min(.2f)] private float recoveryDuration = .3f;

        public int GripHands => gripHands;
        public float ChaseDuration => chaseDuration;
        public float FadeDuration => fadeDuration;
        public float GapDuration => gapDuration;
        public float CaptureHoldDuration => captureHoldDuration;

        public override bool Validate(out string error)
        {
            if (!base.Validate(out error)) return false;
            float latestCapture = (gripHands - 1) *
                (warningDuration + chaseDuration + fadeDuration + gapDuration) + warningDuration + chaseDuration;
            if (handPrefab == null || gripHands < 2 || gripHands > 3 ||
                startDistance < 100f || chaseSpeed < 100f || chaseDuration < .45f ||
                trackingDuration < .05f || appearDuration < .05f || warningDuration < .2f || trackingDuration + appearDuration >= chaseDuration ||
                fadeDuration < .15f || gapDuration < .1f ||
                captureHoldDuration < .8f || stabCount < 2 || stabCount > 4 ||
                stabInterval < .1f || stabWarning < .1f || recoveryDuration < .2f ||
                Duration < latestCapture + captureHoldDuration + recoveryDuration ||
                stabWarning + (stabCount - 1) * stabInterval + .42f > captureHoldDuration)
            {
                error = "Chasing hands need separate dodge windows, one finite capture and enough time for the stabs to finish.";
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
            private sealed class Hand
            {
                public CombatBulletView Bullet;
                public int Lease;
                public float StartedAt;
                public bool Fading;
                public Vector2 Direction;
                public Vector2 FadeStart;
                public Vector2 WarningPoint;
            }

            private static readonly Vector2[] Directions =
                { Vector2.left, Vector2.up, Vector2.right, Vector2.down };
            private readonly ConvergingHandsMove data;
            private readonly CombatMoveExecutionContext context;
            private readonly List<Hand> stabs = new List<Hand>();
            private Hand pursuer, captor;
            private float elapsed, nextChaseAt, capturedAt, releasedAt;
            private int chaseIndex, stabIndex;
            private bool captured, released, finished, cancelled;
            private Vector2 capturePoint, normalizedCapture;

            public Execution(ConvergingHandsMove data, CombatMoveExecutionContext context)
            {
                this.data = data;
                this.context = context;
            }

            public bool IsComplete => cancelled || finished || elapsed >= data.Duration;

            public void Tick(float activeDeltaTime)
            {
                if (IsComplete || activeDeltaTime <= 0f) return;
                if (context.Board == null || !context.Board.isActiveAndEnabled)
                {
                    Cancel();
                    return;
                }

                elapsed += activeDeltaTime;
                if (elapsed >= data.Duration)
                {
                    Finish();
                    return;
                }
                if (captured)
                {
                    TickCapture(activeDeltaTime);
                    return;
                }
                if (pursuer == null && chaseIndex < data.gripHands && elapsed >= nextChaseAt)
                    SpawnPursuer();
                if (pursuer != null)
                    TickPursuer(activeDeltaTime);
                if (!captured && pursuer == null && chaseIndex >= data.gripHands)
                    Finish();
            }

            private void SpawnPursuer()
            {
                Rect field = context.Board.PlayArea.rect;
                Vector2 heart = context.Board.PlayerPosition;
                Vector2 start = FindStart(field, heart, chaseIndex, data.startDistance);
                chaseIndex++;
                nextChaseAt = elapsed + data.warningDuration + data.chaseDuration + data.fadeDuration + data.gapDuration;
                CombatBulletView bullet = context.Board.SpawnEnemyBullet(data.handPrefab, start,
                    Vector2.zero, context.SessionVersion, context.PhaseVersion);
                if (bullet == null) return;
                bullet.BeginPresentationFade(); // Capture uses overlap, never contact damage.
                bullet.SetPresentationFade(0f);
                pursuer = new Hand { Bullet = bullet, Lease = bullet.PoolLeaseVersion, StartedAt = elapsed,
                    Direction=(InBulletSpace(bullet,heart)-bullet.RectTransform.anchoredPosition).normalized,
                    WarningPoint=WarningEdge(field,start,heart) };
                Face(bullet, InBulletSpace(bullet, heart) - bullet.RectTransform.anchoredPosition);
            }

            private void TickPursuer(float activeDeltaTime)
            {
                if (!Live(pursuer))
                {
                    pursuer = null;
                    return;
                }
                float age = elapsed - pursuer.StartedAt;
                if(age<data.warningDuration)
                {
                    context.Board.ShowAttackWarning(this,pursuer.WarningPoint,age);
                    return;
                }
                context.Board.HideAttackWarning(this);
                age-=data.warningDuration;
                if (age < data.appearDuration)
                {
                    pursuer.Bullet.SetPresentationFade(Mathf.SmoothStep(0f,1f,age/data.appearDuration));
                    return;
                }
                if (age < data.chaseDuration)
                {
                    CombatBulletView bullet = pursuer.Bullet;
                    Vector2 target = InBulletSpace(bullet, context.Board.PlayerPosition);
                    Vector2 position = bullet.RectTransform.anchoredPosition;
                    if(age < data.appearDuration + data.trackingDuration && (target-position).sqrMagnitude>.001f)
                        pursuer.Direction=(target-position).normalized;
                    Face(bullet, pursuer.Direction);
                    bullet.SetPresentationFade(1f);
                    // Commit to the reach: a sideways dodge now makes the palm overshoot.
                    bullet.RectTransform.anchoredPosition=position+pursuer.Direction*data.chaseSpeed*activeDeltaTime;
                    if (context.Board.PlayerOverlaps(bullet.RectTransform)) Capture();
                    return;
                }
                if (!pursuer.Fading)
                {
                    pursuer.Fading = true;
                    pursuer.Bullet.BeginPresentationFade();
                    pursuer.FadeStart=pursuer.Bullet.RectTransform.anchoredPosition;
                }
                float fade = 1f - (age - data.chaseDuration) / data.fadeDuration;
                float eased=Mathf.SmoothStep(0f,1f,Mathf.Clamp01(fade));
                pursuer.Bullet.SetPresentationFade(eased);
                pursuer.Bullet.RectTransform.anchoredPosition=pursuer.FadeStart-pursuer.Direction*(1f-eased)*28f;
                if (age < data.chaseDuration + data.fadeDuration) return;
                Return(pursuer);
                pursuer = null;
            }

            private void Capture()
            {
                captured = true;
                capturedAt = elapsed;
                capturePoint = context.Board.PlayerPosition;
                Rect field = context.Board.PlayArea.rect;
                normalizedCapture = new Vector2(
                    Mathf.InverseLerp(field.xMin, field.xMax, capturePoint.x),
                    Mathf.InverseLerp(field.yMin, field.yMax, capturePoint.y));
                captor = pursuer;
                pursuer = null;
                context.Board.SetForcedPlayerControl(this, normalizedCapture, 1200f, 0f);
            }

            private void TickCapture(float activeDeltaTime)
            {
                if (released)
                {
                    if (elapsed - releasedAt >= data.recoveryDuration) Finish();
                    return;
                }
                context.Board.SetForcedPlayerControl(this, normalizedCapture, 1200f, activeDeltaTime);
                float age = elapsed - capturedAt;
                while (stabIndex < data.stabCount &&
                       age >= data.stabWarning + stabIndex * data.stabInterval)
                    SpawnStab(stabIndex++);
                if (age < data.captureHoldDuration) return;
                released = true;
                releasedAt = elapsed;
                context.Board.ReleaseForcedPlayerControl(this);
                Return(captor);
                captor = null;
                ReturnStabs();
            }

            private void SpawnStab(int index)
            {
                Rect field = context.Board.PlayArea.rect;
                Vector2 outward = Directions[index % Directions.Length];
                Vector2 start = new Vector2(
                    Mathf.Clamp(capturePoint.x + outward.x * 170f, field.xMin + 28f, field.xMax - 28f),
                    Mathf.Clamp(capturePoint.y + outward.y * 170f, field.yMin + 28f, field.yMax - 28f));
                CombatBulletView bullet = context.Board.SpawnEnemyBullet(data.handPrefab, start,
                    Vector2.zero, context.SessionVersion, context.PhaseVersion, data.stabWarning);
                if (bullet == null) return;
                bullet.AllowHitDuringForcedMovement();
                Vector2 from = InBulletSpace(bullet, start);
                Vector2 to = InBulletSpace(bullet, capturePoint);
                float angle = Mathf.Atan2((to - from).y, (to - from).x) * Mathf.Rad2Deg - 90f;
                bullet.ConfigurePathMotion(new ParametricProjectileMotion(.55f,
                    t => Vector2.Lerp(from, to, StabReach(t)), t => angle));
                bullet.FadeInDuringTelegraph();
                stabs.Add(new Hand { Bullet = bullet, Lease = bullet.PoolLeaseVersion });
            }

            private static float StabReach(float progress)
            {
                float reach = Mathf.Min(progress / .46f, (1f - progress) / .46f);
                return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(reach));
            }

            private static Vector2 FindStart(Rect field, Vector2 heart, int index, float distance)
            {
                Vector2 best = heart;
                float bestDistance = -1f;
                for (int option = 0; option < Directions.Length; option++)
                {
                    Vector2 direction = Directions[(index + option) % Directions.Length];
                    Vector2 candidate = new Vector2(
                        Mathf.Clamp(heart.x + direction.x * distance, field.xMin + 28f, field.xMax - 28f),
                        Mathf.Clamp(heart.y + direction.y * distance, field.yMin + 28f, field.yMax - 28f));
                    float separation = Vector2.Distance(candidate, heart);
                    if (separation >= distance * .8f) return candidate;
                    if (separation <= bestDistance) continue;
                    bestDistance = separation;
                    best = candidate;
                }
                return best;
            }

            private static Vector2 WarningEdge(Rect field,Vector2 start,Vector2 heart)
            {
                Vector2 direction=start-heart;
                if(Mathf.Abs(direction.x)>=Mathf.Abs(direction.y))return new Vector2(direction.x<0?field.xMin-22f:field.xMax+22f,start.y);
                return new Vector2(start.x,direction.y<0?field.yMin-40f:field.yMax+30f);
            }

            private static void Face(CombatBulletView bullet, Vector2 direction)
            {
                if (direction.sqrMagnitude < .0001f) return;
                bullet.RectTransform.localRotation = Quaternion.Euler(0f, 0f,
                    Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            }

            private Vector2 InBulletSpace(CombatBulletView bullet, Vector2 playAreaPoint)
            {
                Vector3 world = context.Board.PlayArea.TransformPoint(playAreaPoint);
                Vector3 local = bullet.RectTransform.parent.InverseTransformPoint(world);
                return new Vector2(local.x, local.y);
            }

            private static bool Live(Hand hand) => hand != null && hand.Bullet != null &&
                hand.Bullet.gameObject.activeSelf && hand.Bullet.PoolLeaseVersion == hand.Lease;

            private void Return(Hand hand)
            {
                if (hand != null) context.Board?.ReturnEnemyBullet(hand.Bullet, hand.Lease);
            }

            private void ReturnStabs()
            {
                foreach (Hand stab in stabs) Return(stab);
                stabs.Clear();
            }

            private void Finish()
            {
                if (finished) return;
                finished = true;
                Return(pursuer);
                Return(captor);
                pursuer = captor = null;
                ReturnStabs();
                context.Board?.ReleaseForcedPlayerControl(this);
                context.Board?.HideAttackWarning(this);
                context.Board?.SetMechanicHint(null);
            }

            public void Cancel()
            {
                if (cancelled) return;
                cancelled = true;
                Finish();
            }
        }
    }
}
