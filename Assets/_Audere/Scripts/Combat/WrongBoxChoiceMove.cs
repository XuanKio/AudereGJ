using System;
using Audere.Audio;
using UnityEngine;

namespace Audere.Combat
{
    [CreateAssetMenu(menuName = "Audere/Combat/Moves/Wrong Box Choice")]
    public sealed class WrongBoxChoiceMove : CombatMoveDefinition
    {
        [SerializeField] private CombatBulletView projectilePrefab;
        [SerializeField, Range(0f, 1f)] private float explosionChance = .6f;
        [SerializeField, Min(1)] private int requiredSuccesses = 2;
        [SerializeField, Min(.1f)] private float roundDelay = 1.4f;
        [SerializeField, Min(.1f)] private float telegraphDuration = .4f;
        [SerializeField, Min(3)] private int burstCount = 12;
        [SerializeField, Min(1f)] private float burstSpeed = 210f;
        public float ExplosionChance => explosionChance;
        public int RequiredSuccesses => requiredSuccesses;
        public override bool Validate(out string error)
        {
            if (!base.Validate(out error)) return false;
            if (projectilePrefab == null || requiredSuccesses < 1 || explosionChance < 0f || explosionChance >= 1f)
            { error = "Wrong Box requires a projectile, a reachable success probability and a positive target."; return false; }
            error = null; return true;
        }
        public override ICombatMoveExecution CreateExecution(CombatMoveExecutionContext context) => new Execution(this, context);

        private sealed class Execution : ICombatMoveExecution, ICombatMoveInputHandler
        {
            private readonly WrongBoxChoiceMove data;
            private readonly CombatMoveExecutionContext context;
            private readonly CombatChoiceRoundState progress = new CombatChoiceRoundState();
            private readonly CombatDieView[] choices = new CombatDieView[3];
            private float cooldown = .35f;
            private bool visible, cancelled;
            public Execution(WrongBoxChoiceMove data, CombatMoveExecutionContext context) { this.data = data; this.context = context; }
            public bool IsComplete => cancelled || progress.Successes >= data.requiredSuccesses && cooldown <= 0f;
            public void Tick(float delta)
            {
                if (cancelled || context.Board == null) return;
                cooldown -= Mathf.Max(0f, delta);
                if (!visible && cooldown <= 0f && progress.Successes < data.requiredSuccesses)
                {
                    for (int i = 0; i < 3; i++)
                        choices[i] = context.Board.SpawnChoiceDie((CombatSymbol)i, new Vector2(.15f + .35f * i, .55f));
                    visible = true;
                    context.Board.SetMechanicHint($"Chọn 1 hộp — Đúng {progress.Successes}/{data.requiredSuccesses}");
                }
            }
            public void HandleInput(bool catchPressed, bool rerollPressed)
            {
                if (!catchPressed || !visible || cancelled) return;
                for (int i = 0; i < choices.Length; i++)
                {
                    var die = choices[i];
                    if (die == null || !die.CanInteract || !context.Board.CursorOverlaps(die)) continue;
                    if (context.Board.IsCursorStunned) { context.Board.PlayBlockedCursorFeedback(); return; }
                    Vector2 origin = context.Board.WorldToPlayArea(die.transform.position);
                    bool success = progress.Resolve(context.Random.Value01(), data.explosionChance);
                    HideChoices();
                    AudioService.Instance?.Play(AudioId.Dice_Catch);
                    if (!success)
                    {
                        float angle = context.Random.Range(0f, 360f);
                        for (int n = 0; n < data.burstCount; n++)
                        {
                            float a = (angle + n * 360f / data.burstCount) * Mathf.Deg2Rad;
                            context.Board.SpawnEnemyBullet(data.projectilePrefab, origin,
                                new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * data.burstSpeed,
                                context.SessionVersion, context.PhaseVersion, data.telegraphDuration);
                        }
                    }
                    context.Board.SetMechanicHint(success ? $"Đúng {progress.Successes}/{data.requiredSuccesses}" : "Nhầm hộp — né đạn!");
                    cooldown = success ? .65f : data.roundDelay;
                    return;
                }
            }
            private void HideChoices()
            {
                visible = false;
                for (int i = 0; i < choices.Length; i++) { choices[i]?.ReturnToPool(); choices[i] = null; }
            }
            public void Cancel() { if (cancelled) return; cancelled = true; HideChoices(); context.Board?.SetMechanicHint(null); }
        }
    }
}
