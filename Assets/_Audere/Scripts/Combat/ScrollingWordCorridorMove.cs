using System;
using System.Collections.Generic;
using Audere.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat
{
    [CreateAssetMenu(menuName = "Audere/Combat/Moves/Scrolling Word Corridor")]
    public sealed class ScrollingWordCorridorMove : CombatMoveDefinition, ICombatExclusiveDiceMove
    {
        [SerializeField] private CombatBulletView[] wordProjectiles;
        [SerializeField] private Sprite timorSprite;
        [SerializeField] private Material silhouetteMaterial;
        [SerializeField] private CombatBulletView handProjectile;
        [SerializeField, Min(1.4f)] private float waveInterval = 1.9f;
        [SerializeField, Min(.5f)] private float warningDuration = .6f;
        [SerializeField, Range(240f, 1200f)] private float travelSpeed = 960f;
        [SerializeField, Range(.65f, 1f)] private float corridorHeight = .82f;
        [SerializeField, Range(1, 3)] private int wordsPerTrain = 3;
        [SerializeField, Min(.3f)] private float trainBeat = .34f;
        [SerializeField, Range(0f, .12f)] private float rowStagger = .03f;
        [SerializeField] private Color corridorColor = new Color(.66f, .4f, .86f, .45f);
        [SerializeField] private bool supported;
        private const float EntryDuration = .8f, ExitDuration = .9f;
        private const float HeartSize = 28f, DodgeSpeed = 180f, ReactionTime = .28f;
        public float WaveInterval => waveInterval;
        public float TravelSpeed => travelSpeed;

        // Reflect at the outer lanes instead of clamping, which used to repeat a safe lane.
        public static int NextLane(int previous, float random) => previous <= 0 ? 1 : previous >= 2 ? 1 : random < .5f ? 0 : 2;
        public static float LaneY(Rect rect, int lane) => rect.center.y + (lane - 1) * Mathf.Min(96f, rect.height * .27f);

        private float WordWidth
        {
            get
            {
                float width = 0f;
                foreach (var word in wordProjectiles)
                    if (word != null) width = Mathf.Max(width, word.GetComponent<RectTransform>().rect.width);
                return width;
            }
        }
        private float RepeatDelay(float width) => Mathf.Max(trainBeat, (width + 24f) / travelSpeed);
        private float TrainDuration(float width) => (wordsPerTrain - 1) * RepeatDelay(width) + (supported ? 1 : 3) * rowStagger;
        private float SafeInterval(Rect field, float width)
        {
            // A whole old train must clear Heart before the next train can block its lane.
            // Include the real persistent projectile footprint; no reliance on hit invulnerability.
            float crossing = (width + HeartSize) / travelSpeed;
            float changeLane = Mathf.Abs(LaneY(field, 1) - LaneY(field, 0)) / DodgeSpeed;
            return Mathf.Max(waveInterval, TrainDuration(width) + crossing + changeLane + ReactionTime + .05f);
        }
        public override bool Validate(out string error)
        {
            if (!base.Validate(out error)) return false;
            if (wordProjectiles == null || wordProjectiles.Length == 0 ||
                Array.Exists(wordProjectiles, x => x == null) || waveInterval < 1.4f ||
                warningDuration < .5f || corridorHeight < .65f || travelSpeed < 240f || travelSpeed > 1200f ||
                wordsPerTrain < 1 || wordsPerTrain > 3 || trainBeat < .3f || rowStagger < 0f || rowStagger > .12f ||
                Duration < 9f || handProjectile == null)
            { error = "Word corridor needs authored words, readable staggered trains and room to change lane."; return false; }
            return true;
        }
        public override ICombatMoveExecution CreateExecution(CombatMoveExecutionContext ctx) => new Execution(this, ctx);

        private sealed class Execution : ICombatMoveExecution, ICombatMoveInputHandler, ICombatMoveDamageReward
        {
            private struct WordBeat
            {
                public CombatBulletView Prefab;
                public Vector2 Origin;
                public float At;
            }
            private readonly ScrollingWordCorridorMove data;
            private readonly CombatMoveExecutionContext ctx;
            private readonly CombatMoveStage stage;
            private readonly Image[] sleepers = new Image[18];
            private readonly CombatDieView[] choices = new CombatDieView[2];
            private readonly List<WordBeat> pendingWords = new List<WordBeat>(12);
            private readonly Image floating;
            private readonly RectTransform floatingRoot;
            private readonly CombatMountRainbowEcho trail;
            private readonly GameObject originalActorVisual;
            private readonly bool originalActorVisible;
            private readonly Vector3 actorHomeWorld;
            private readonly float wordWidth;
            private float elapsed, nextWave = EntryDuration, waveStart = -10f, dieAt = -1f, recoil;
            private Vector2 dieOrigin;
            private int lane = 1, wave, damage;
            private bool cancelled, recovering;

            public Execution(ScrollingWordCorridorMove data, CombatMoveExecutionContext ctx)
            {
                this.data = data; this.ctx = ctx;
                wordWidth = data.WordWidth;
                stage = new CombatMoveStage(ctx.Board, Cancel);
                originalActorVisual = ctx.Actor != null && ctx.Actor.VisualRoot != null ? ctx.Actor.VisualRoot.gameObject : null;
                var root = stage.Root("Moving corridor perspective");
                root.SetAsFirstSibling();
                for (int i = 0; i < sleepers.Length; i++)
                    sleepers[i] = CombatMoveStage.Picture(root, "Passing corridor mark", null, new Vector2(i % 3 == 0 ? 62f : 30f, 3f), data.corridorColor);
                floatingRoot = stage.Root("Timor beside the long road", true);
                floating = CombatMoveStage.Picture(floatingRoot, "Timor above the road", data.timorSprite,
                    CombatMoveStage.OriginalActorSize(ctx, data.timorSprite), Color.white);
                actorHomeWorld = floatingRoot.TransformPoint(new Vector2(0f,
                    ctx.Board.PlayArea.rect.yMax + floating.rectTransform.rect.height * .5f + 20f));
                if (originalActorVisual != null)
                {
                    originalActorVisible = originalActorVisual.activeSelf;
                    foreach (var image in ctx.Actor.GetComponentsInChildren<Image>(true))
                        if (image.sprite == data.timorSprite)
                        { actorHomeWorld = image.rectTransform.TransformPoint(image.rectTransform.rect.center); break; }
                    originalActorVisual.SetActive(false);
                }
                floating.rectTransform.position = actorHomeWorld;
                trail = new CombatMountRainbowEcho(floating, data.silhouetteMaterial, true);
                ctx.Board.SetMechanicHint("Rê lên/xuống để né — bắt Attack khi dice chạy tới");
            }
            public bool IsComplete => cancelled || elapsed >= data.Duration;
            public void Tick(float dt)
            {
                if (cancelled || dt <= 0f) return;
                elapsed = Mathf.Min(data.Duration, elapsed + dt);
                recoil = Mathf.Max(0f, recoil - dt);
                float enter = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / EntryDuration));
                float exit = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((data.Duration - elapsed) / ExitDuration));
                if (!recovering && elapsed >= data.Duration - ExitDuration)
                {
                    recovering = true;
                    pendingWords.Clear(); dieAt = -1f;
                    stage.ClearBullets(); ClearChoices();
                }
                // Release every collision before the first frame that narrows the corridor.
                ctx.Board.SetLongCorridorLayout(enter * exit, Mathf.Lerp(1f, data.corridorHeight, enter * exit));
                Rect field = ctx.Board.PlayArea.rect;
                Rect visible = ctx.Board.CorridorVisibleRect;
                ctx.Board.PullAlongCorridor(.22f, dt);
                AnimateRoad(field, visible, enter * exit);
                AnimateTimor(field, visible, enter, exit, dt);
                if (recovering) return;

                if (elapsed >= nextWave)
                {
                    float playerX = Mathf.Lerp(visible.xMin, visible.xMax, .22f);
                    // The warning happens ahead of the runner at every aspect. Ultrawide
                    // must not spend half a short supported move waiting for words to arrive.
                    float originX = Mathf.Min(field.xMax - 8f, visible.xMax + 64f, playerX + data.travelSpeed * .95f);
                    float clearBy = elapsed + data.warningDuration + data.TrainDuration(wordWidth) +
                        (originX - playerX + (wordWidth + HeartSize) * .5f) / data.travelSpeed;
                    if (clearBy < data.Duration - ExitDuration)
                    {
                        BeginTrain(field, visible, originX);
                        // Skip missed beats after a long frame instead of compressing two routes together.
                        nextWave = elapsed + data.SafeInterval(field, wordWidth);
                    }
                    else nextWave = data.Duration;
                }
                EmitWords();
                TickChoices(dt);
            }
            private void AnimateRoad(Rect field, Rect visible, float visibility)
            {
                for (int i = 0; i < sleepers.Length; i++)
                {
                    float x = Mathf.Repeat(i * visible.width / sleepers.Length - elapsed * data.travelSpeed * 1.35f, visible.width) + visible.xMin;
                    sleepers[i].rectTransform.anchoredPosition = new Vector2(x, i % 2 == 0 ? field.yMin + 10f : field.yMax - 10f);
                    Color color = data.corridorColor; color.a *= visibility;
                    sleepers[i].color = color;
                }
            }
            private void AnimateTimor(Rect field, Rect visible, float enter, float exit, float dt)
            {
                floatingRoot.anchoredPosition = ctx.Board.PlayArea.anchoredPosition;
                float beatAge = elapsed - waveStart;
                float anticipation = beatAge >= 0f && beatAge < data.warningDuration
                    ? Mathf.Sin(Mathf.PI * beatAge / data.warningDuration) : 0f;
                float strike = Mathf.Exp(-Mathf.Max(0f, beatAge - data.warningDuration) * 7f);
                if (beatAge < data.warningDuration) strike = 0f;
                Vector2 roadPosition = new Vector2(
                    visible.center.x + Mathf.Sin(elapsed * 1.1f) * visible.width * .2f + anticipation * 24f - strike * 32f + recoil * 36f,
                    field.yMax + floating.rectTransform.rect.height * .5f + 20f + Mathf.Sin(elapsed * 3.2f) * 13f + anticipation * 15f);
                Vector2 home = floatingRoot.InverseTransformPoint(actorHomeWorld);
                floating.rectTransform.anchoredPosition = Vector2.Lerp(home, roadPosition, enter * exit);
                trail.Tick(dt, enter > .9f && exit > .9f && (anticipation > .2f || strike > .12f || recoil > .1f));
            }
            private void BeginTrain(Rect field, Rect visible, float originX)
            {
                lane = NextLane(lane, ctx.Random.Value01());
                waveStart = elapsed;
                float pairOffset = (LaneY(field, 2) - LaneY(field, 1)) * .25f;
                for (int beat = 0; beat < data.wordsPerTrain; beat++)
                {
                    int order = 0;
                    for (int rowIndex = 0; rowIndex < 3; rowIndex++)
                    {
                        int row = wave % 2 == 0 ? rowIndex : 2 - rowIndex;
                        if (row == lane || data.supported && row == (lane + 1) % 3) continue;
                        for (int side = -1; side <= 1; side += 2)
                        {
                            pendingWords.Add(new WordBeat
                            {
                                Prefab = data.wordProjectiles[(wave + beat + rowIndex + (side + 1) / 2) % data.wordProjectiles.Length],
                                Origin = new Vector2(originX, LaneY(field, row) + side * pairOffset),
                                // Timor's anticipation telegraphs the train. Reveal each word
                                // when it launches so waiting words cannot stack at the muzzle.
                                At = elapsed + data.warningDuration + beat * data.RepeatDelay(wordWidth) + order++ * data.rowStagger
                            });
                        }
                    }
                }
                dieAt = elapsed + data.warningDuration + data.RepeatDelay(wordWidth) * .5f;
                dieOrigin = new Vector2(originX, LaneY(field, lane));
                if (!data.supported && wave % 2 == 0) SpawnHook(field, visible);
                wave++;
            }
            private void EmitWords()
            {
                for (int i = pendingWords.Count - 1; i >= 0; i--)
                {
                    WordBeat beat = pendingWords[i];
                    if (elapsed < beat.At) continue;
                    float late = elapsed - beat.At;
                    Vector2 position = beat.Origin + Vector2.left * (data.travelSpeed * late);
                    stage.Bullet(ctx, beat.Prefab, position, Vector2.left * data.travelSpeed);
                    pendingWords.RemoveAt(i);
                }
            }
            private void TickChoices(float dt)
            {
                if (dieAt >= 0f && elapsed >= dieAt)
                {
                    int slot = choices[0] == null ? 0 : choices[1] == null ? 1 : -1;
                    if (slot >= 0)
                    {
                        choices[slot] = ctx.Board.SpawnChoiceDie(CombatSymbol.Attack, new Vector2(.85f, .5f));
                        if (choices[slot] != null)
                            choices[slot].RectTransform.anchoredPosition = dieOrigin + Vector2.left * (data.travelSpeed * (elapsed - dieAt));
                    }
                    dieAt = -1f;
                }
                foreach (var die in choices)
                {
                    if (die == null) continue;
                    var rect = die.RectTransform;
                    Vector2 position = rect.anchoredPosition + Vector2.left * (data.travelSpeed * dt);
                    // A missed counter remains catchable at the runner, through later safe lanes.
                    position.x = Mathf.Max(ctx.Board.PlayerPosition.x, position.x);
                    rect.anchoredPosition = position;
                }
            }
            private void SpawnHook(Rect field, Rect visible)
            {
                bool top = lane != 2;
                float edge = top ? field.yMax : field.yMin, direction = top ? -1f : 1f;
                float palmHalf = data.handProjectile.GetComponent<RectTransform>().rect.height * .5f;
                float railRoom = field.height * .5f - Mathf.Abs(LaneY(field, 2) - field.center.y) - palmHalf - HeartSize * .5f - 8f;
                if (railRoom <= 0f) return;
                float inset = Mathf.Min(12f, railRoom);
                float x = visible.xMax - 35f;
                float duration = (visible.width + 120f) / data.travelSpeed;
                var hand = stage.Bullet(ctx, data.handProjectile, new Vector2(x, edge), Vector2.zero, data.warningDuration);
                hand?.ConfigurePathMotion(new ParametricProjectileMotion(duration,
                    t => new Vector2(x - data.travelSpeed * t * duration, edge + direction * inset * Mathf.Sin(t * Mathf.PI)),
                    t => top ? 180f : 0f));
            }
            public void HandleInput(bool catchPressed, bool rerollPressed)
            {
                if (cancelled || recovering || !catchPressed || ctx.Board.IsCursorStunned) return;
                int index = ctx.Board.FindClosestChoiceUnderCursor(choices);
                if (index < 0) return;
                ctx.Board.PlayDiceCatchVfx(choices[index]); AudioService.Instance?.Play(AudioId.Dice_Catch);
                choices[index].ReturnToPool(); choices[index] = null;
                damage += CombatDiceConstants.AttackDamage; recoil = .65f;
                ctx.Board.DestroyBulletsNearPlayer(CombatDiceConstants.ShieldBulletClearRadius);
            }
            public int ConsumePendingDamage() { int result = damage; damage = 0; return result; }
            private void ClearChoices()
            {
                for (int i = 0; i < choices.Length; i++)
                {
                    if (choices[i] != null) choices[i].ReturnToPool();
                    choices[i] = null;
                }
            }
            public void Cancel()
            {
                if (cancelled) return;
                cancelled = true; damage = 0; pendingWords.Clear(); dieAt = -1f;
                ClearChoices(); trail?.Dispose();
                if (originalActorVisual != null) originalActorVisual.SetActive(originalActorVisible);
                stage?.Dispose(); ctx.Board.ResetBattleBoxLayout(); ctx.Board.SetMechanicHint(null);
            }
        }
    }
}
