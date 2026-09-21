using System;
using Audere.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat
{
    [CreateAssetMenu(menuName = "Audere/Combat/Moves/Targeted Dice Break")]
    public sealed class TargetedDiceBreakMove : CombatMoveDefinition, ICombatExclusiveDiceMove
    {
        [SerializeField] private Sprite tailSprite;
        [SerializeField, Min(.65f)] private float warningDuration = .85f;
        [SerializeField, Min(.2f)] private float strikeDuration = .28f;
        [SerializeField, Min(.8f)] private float replacementDelay = 1.1f;
        [SerializeField] private bool releaseChoice;
        public override bool Validate(out string error)
        {
            if (!base.Validate(out error)) return false;
            if (tailSprite == null || warningDuration < .65f || strikeDuration < .2f ||
                replacementDelay < .8f || Duration < warningDuration + strikeDuration + replacementDelay + 2.2f)
            { error = "Dice break needs a tail, readable lock and time to collect replacement dice."; return false; }
            return true;
        }
        public override ICombatMoveExecution CreateExecution(CombatMoveExecutionContext context) => new Execution(this, context);
        private sealed class Execution : ICombatMoveExecution, ICombatMoveInputHandler, ICombatMoveDamageReward
        {
            private readonly TargetedDiceBreakMove data;
            private readonly CombatMoveExecutionContext ctx;
            private readonly CombatDieView[] dice = new CombatDieView[3];
            private readonly CombatMoveStage stage;
            private readonly Image tail;
            private readonly Image[] fragments = new Image[8];
            private readonly Vector2 start;
            private Vector2 target;
            private float elapsed, lockTime = -1f, smashTime;
            private int locked = -1, damage;
            private bool resolved, replaced, cancelled;
            public Execution(TargetedDiceBreakMove data, CombatMoveExecutionContext ctx)
            {
                this.data = data; this.ctx = ctx;
                stage = new CombatMoveStage(ctx.Board, Cancel);
                var root = stage.Root("Dice break stage");
                start = new Vector2(ctx.Board.PlayArea.rect.xMax - 24f, ctx.Board.PlayArea.rect.yMax - 35f);
                tail = CombatMoveStage.Picture(root, "Locking tail", data.tailSprite, new Vector2(94f, 165f), Color.white);
                tail.rectTransform.anchoredPosition = start;
                for (int i = 0; i < 3; i++) Spawn(i);
                for (int i = 0; i < fragments.Length; i++)
                {
                    fragments[i] = CombatMoveStage.Picture(root, "Dice fragment", null, Vector2.one * 8f, Color.white);
                    fragments[i].gameObject.SetActive(false);
                }
                ctx.Board.SetMechanicHint("Đuôi khóa một viên — đổi sang viên khác");
            }
            public bool IsComplete => cancelled || elapsed >= data.Duration;
            private void Spawn(int i) => dice[i] = ctx.Board.SpawnChoiceDie(CombatSymbol.Attack, new Vector2(.16f + i * .34f, .4f));
            public void Tick(float dt)
            {
                if (cancelled || dt <= 0f) return;
                elapsed += dt;
                if (lockTime < 0f && elapsed >= .5f)
                {
                    float best = float.MaxValue;
                    for (int i = 0; i < dice.Length; i++)
                    {
                        if (dice[i] == null || !dice[i].CanInteract) continue;
                        float d = Vector2.Distance(ctx.Board.CatchZoneCenter, dice[i].RectTransform.anchoredPosition);
                        if (d < best) { best = d; locked = i; }
                    }
                    if (locked >= 0 && (best < 140f || elapsed >= 1.1f))
                    { lockTime = elapsed; target = dice[locked].RectTransform.anchoredPosition;
                      ctx.Board.ShowAttackWarnings(this, new[] { target }, 0f); }
                }
                if (lockTime < 0f) return;
                float age = elapsed - lockTime;
                float strike = Mathf.Clamp01((age - data.warningDuration) / data.strikeDuration);
                tail.rectTransform.anchoredPosition = Vector2.Lerp(start, target, Mathf.SmoothStep(0f, 1f, strike));
                tail.rectTransform.localRotation = Quaternion.Euler(0, 0,
                    Mathf.Atan2(target.y - start.y, target.x - start.x) * Mathf.Rad2Deg - 90f);
                tail.color = Color.white;
                if (age >= data.warningDuration) ctx.Board.HideAttackWarning(this);
                if (!resolved && strike >= 1f)
                {
                    resolved = true; smashTime = elapsed;
                    // Catch wins if the die was already collected. Never reverse a successful input.
                    if (!data.releaseChoice && dice[locked] != null && dice[locked].CanInteract)
                    {
                        ctx.Board.PlayDiceCatchVfx(dice[locked]); dice[locked].ReturnToPool(); dice[locked] = null;
                        foreach (var image in fragments) image.gameObject.SetActive(true);
                    }
                }
                if (!resolved) return;
                float fade = Mathf.Clamp01((elapsed - smashTime) / .75f);
                tail.rectTransform.anchoredPosition = Vector2.Lerp(target, start, fade);
                tail.color = new Color(1f, 1f, 1f, 1f - fade);
                for (int i = 0; i < fragments.Length; i++)
                {
                    float angle = i * Mathf.PI * .25f;
                    fragments[i].rectTransform.anchoredPosition = target + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * fade * 78f;
                    fragments[i].color = new Color(1f, 1f, 1f, 1f - fade);
                }
                if (!replaced && elapsed - smashTime >= data.replacementDelay)
                { replaced = true; if (dice[locked] == null) Spawn(locked); }
            }
            public void HandleInput(bool catchPressed, bool rerollPressed)
            {
                if (cancelled || !catchPressed || ctx.Board.IsCursorStunned) return;
                int index = ctx.Board.FindClosestChoiceUnderCursor(dice);
                if (index < 0) return;
                ctx.Board.PlayDiceCatchVfx(dice[index]); AudioService.Instance?.Play(AudioId.Dice_Catch);
                dice[index].ReturnToPool(); dice[index] = null; damage += CombatDiceConstants.AttackDamage;
            }
            public int ConsumePendingDamage() { int value = damage; damage = 0; return value; }
            public void Cancel()
            {
                if (cancelled) return; cancelled = true; damage = 0;
                foreach (var die in dice) if (die != null) die.ReturnToPool();
                ctx.Board.HideAttackWarning(this);
                ctx.Board.SetMechanicHint(null); stage?.Dispose();
            }
        }
    }
}

