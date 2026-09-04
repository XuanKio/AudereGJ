using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat
{
    [CreateAssetMenu(menuName = "Audere/Combat/Moves/Timor Tail Throw", fileName = "Move_TimorTailThrow")]
    public sealed class TimorTailThrowMove : CombatMoveDefinition
    {
        [SerializeField] private Sprite tailSprite;
        [SerializeField] private Sprite normalTimorSprite;
        [SerializeField] private Sprite noTailTimorSprite;
        [SerializeField, Min(.45f)] private float warningDuration = .72f;
        [SerializeField, Min(.2f)] private float lungeDuration = .36f;
        [SerializeField, Min(0f)] private float holdDuration = .18f;
        [SerializeField, Min(.25f)] private float throwDuration = .52f;
        [SerializeField, Min(.25f)] private float stunDuration = 1f;
        [SerializeField, Min(16f)] private float catchRadius = 62f;
        [SerializeField, Min(60f)] private float visualHeight = 230f;
        [SerializeField] private Color glowColor = new Color(.25f, .12f, .95f, .9f);

        public Sprite TailSprite => tailSprite;
        public Sprite NormalTimorSprite => normalTimorSprite;
        public Sprite NoTailTimorSprite => noTailTimorSprite;
        public float WarningDuration => warningDuration;
        public float HoldDuration => holdDuration;
        public float StunDuration => stunDuration;
        public float CatchRadius => catchRadius;

        public override bool Validate(out string error)
        {
            if (!base.Validate(out error)) return false;
            float action = warningDuration + lungeDuration + holdDuration + throwDuration + stunDuration;
            if (tailSprite == null || normalTimorSprite == null || noTailTimorSprite == null ||
                warningDuration < .45f || lungeDuration < .2f || throwDuration < .25f ||
                stunDuration < .25f || catchRadius < 16f || Duration < action)
            {
                error = "Timor tail throw requires all three sprites, readable warning, finite throw/stun and enough move duration.";
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
            private readonly TimorTailThrowMove data;
            private readonly CombatMoveExecutionContext context;
            private RectTransform tail;
            private Image tailImage;
            private Image actorImage;
            private float elapsed;
            private Vector2 start;
            private Vector2 lockedTarget;
            private Vector2 corner;
            private bool caught;
            private bool resolvedCatch;
            private bool cancelled;

            public Execution(TimorTailThrowMove data, CombatMoveExecutionContext context)
            {
                this.data = data;
                this.context = context;
                SwapActor(data.noTailTimorSprite);
                CreateTail();
            }

            public bool IsComplete => cancelled || elapsed >= data.Duration;

            public void Tick(float deltaTime)
            {
                if (cancelled || context.Board == null || context.Board.PlayArea == null) return;
                deltaTime = Mathf.Max(0f, deltaTime);
                elapsed += deltaTime;
                float warningEnd = data.warningDuration;
                float lungeEnd = warningEnd + data.lungeDuration;
                float holdEnd = lungeEnd + data.holdDuration;
                float throwEnd = holdEnd + data.throwDuration;
                float stunEnd = throwEnd + data.stunDuration;

                if (tail == null) { Cancel(); return; }

                if (elapsed < warningEnd)
                {
                    lockedTarget = context.Board.PlayerPosition;
                    float pulse = .52f + Mathf.Sin(elapsed * 18f) * .16f;
                    tailImage.color = new Color(1f, 1f, 1f, pulse);
                    tail.anchoredPosition = start + Vector2.Perpendicular((lockedTarget - start).normalized) *
                        Mathf.Sin(elapsed * 8f) * 8f;
                    AimAt(lockedTarget);
                    context.Board.SetMechanicHint("Đuôi Timor đang khóa hướng — tránh điểm bám");
                    return;
                }

                if (elapsed < lungeEnd)
                {
                    float t = Mathf.SmoothStep(0f, 1f, (elapsed - warningEnd) / data.lungeDuration);
                    tail.anchoredPosition = Vector2.LerpUnclamped(start, lockedTarget, t);
                    tailImage.color = Color.white;
                    AimAt(lockedTarget);
                    return;
                }

                if (!resolvedCatch)
                {
                    resolvedCatch = true;
                    caught = Vector2.Distance(context.Board.PlayerPosition, lockedTarget) <= data.catchRadius;
                    if (caught)
                    {
                        corner = ChooseFarCorner(context.Board.PlayerPosition, context.Board.PlayArea.rect);
                        context.Board.SetMechanicHint("Bị đuôi giữ — sắp bị quăng vào góc");
                    }
                    else
                    {
                        context.Board.SetMechanicHint("Đã né điểm bám");
                    }
                }

                if (!caught)
                {
                    float t = Mathf.Clamp01((elapsed - lungeEnd) / Mathf.Max(.15f, data.throwDuration));
                    tail.anchoredPosition = Vector2.Lerp(lockedTarget, start, t);
                    tailImage.color = new Color(1f, 1f, 1f, 1f - t);
                    return;
                }

                Vector2 normalizedCorner = Normalize(corner, context.Board.PlayArea.rect);
                if (elapsed < holdEnd)
                {
                    tail.anchoredPosition = context.Board.PlayerPosition;
                    context.Board.SetForcedPlayerControl(this, Normalize(context.Board.PlayerPosition, context.Board.PlayArea.rect), 1400f, deltaTime);
                    Writhe();
                    return;
                }
                if (elapsed < throwEnd)
                {
                    context.Board.SetForcedPlayerControl(this, normalizedCorner, 1450f, deltaTime);
                    tail.anchoredPosition = context.Board.PlayerPosition;
                    Writhe();
                    return;
                }
                if (elapsed < stunEnd)
                {
                    context.Board.SetForcedPlayerControl(this, normalizedCorner, 1800f, deltaTime);
                    tail.anchoredPosition = context.Board.PlayerPosition;
                    tailImage.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, .35f, (elapsed - throwEnd) / data.stunDuration));
                    context.Board.SetMechanicHint("Choáng — 1 giây");
                    return;
                }

                context.Board.ReleaseForcedPlayerControl(this);
                context.Board.SetMechanicHint(null);
                float fade = Mathf.Clamp01((data.Duration - elapsed) / .18f);
                tailImage.color = new Color(1f, 1f, 1f, fade);
            }

            public void Cancel()
            {
                if (cancelled) return;
                cancelled = true;
                context.Board?.ReleaseForcedPlayerControl(this);
                context.Board?.SetMechanicHint(null);
                SwapActor(data.normalTimorSprite);
                if (tail != null) UnityEngine.Object.Destroy(tail.gameObject);
                tail = null;
            }

            private void CreateTail()
            {
                RectTransform parent = context.Board != null ? context.Board.PlayArea : null;
                if (parent == null) return;
                var go = new GameObject("TIMOR TAIL CONTROL TELEGRAPH", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Shadow));
                tail = (RectTransform)go.transform;
                tail.SetParent(parent, false);
                tail.anchorMin = tail.anchorMax = new Vector2(.5f, .5f);
                tail.pivot = new Vector2(.5f, .5f);
                float aspect = data.tailSprite.rect.width / Mathf.Max(1f, data.tailSprite.rect.height);
                tail.sizeDelta = new Vector2(data.visualHeight * aspect, data.visualHeight);
                tailImage = go.GetComponent<Image>();
                tailImage.sprite = data.tailSprite;
                tailImage.preserveAspect = true;
                tailImage.raycastTarget = false;
                var shadow = go.GetComponent<Shadow>();
                shadow.effectColor = data.glowColor;
                shadow.effectDistance = new Vector2(4f, -4f);
                shadow.useGraphicAlpha = true;
                Rect r = parent.rect;
                int edge = Mathf.FloorToInt(context.Random.Value01() * 4f) % 4;
                if (edge == 0) start = new Vector2(r.xMin - 36f, context.Random.Range(r.yMin, r.yMax));
                else if (edge == 1) start = new Vector2(r.xMax + 36f, context.Random.Range(r.yMin, r.yMax));
                else if (edge == 2) start = new Vector2(context.Random.Range(r.xMin, r.xMax), r.yMax + 48f);
                else start = new Vector2(context.Random.Range(r.xMin, r.xMax), r.yMin - 48f);
                lockedTarget = context.Board.PlayerPosition;
                tail.anchoredPosition = start;
                AimAt(lockedTarget);
                tail.SetAsLastSibling();
            }

            private void SwapActor(Sprite sprite)
            {
                if (context.Actor == null || sprite == null) return;
                if (actorImage == null)
                    actorImage = context.Actor.Graphics.OfType<Image>().FirstOrDefault(x => x != null);
                if (actorImage != null) actorImage.sprite = sprite;
            }

            private void AimAt(Vector2 target)
            {
                Vector2 d = target - tail.anchoredPosition;
                tail.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f);
            }

            private void Writhe()
            {
                tail.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(elapsed * 15f) * 18f);
                tail.localScale = Vector3.one * (1f + Mathf.Sin(elapsed * 22f) * .045f);
            }

            private Vector2 ChooseFarCorner(Vector2 from, Rect r)
            {
                Vector2[] c = {
                    new Vector2(r.xMin + 18f, r.yMin + 18f), new Vector2(r.xMax - 18f, r.yMin + 18f),
                    new Vector2(r.xMin + 18f, r.yMax - 18f), new Vector2(r.xMax - 18f, r.yMax - 18f)};
                int best = 0; float bestDistance = -1f;
                for (int i = 0; i < c.Length; i++)
                {
                    float d = (c[i] - from).sqrMagnitude + context.Random.Value01() * 6f;
                    if (d > bestDistance) { bestDistance = d; best = i; }
                }
                return c[best];
            }

            private static Vector2 Normalize(Vector2 p, Rect r) =>
                new Vector2(Mathf.InverseLerp(r.xMin, r.xMax, p.x), Mathf.InverseLerp(r.yMin, r.yMax, p.y));
        }
    }
}
