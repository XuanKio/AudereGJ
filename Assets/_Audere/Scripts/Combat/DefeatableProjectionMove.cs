using System;
using UnityEngine;
using UnityEngine.UI;
using Audere.Audio;

namespace Audere.Combat
{
    // A memory runs complete authored attacks, then yields only after the player answers it.
    [CreateAssetMenu(menuName = "Audere/Combat/Moves/Defeatable Projection")]
    public sealed class DefeatableProjectionMove : CombatMoveDefinition, ICombatExclusiveDiceMove
    {
        [SerializeField] private CombatEnemyActor actorPrefab;
        [SerializeField] private Sprite sprite;
        [SerializeField] private string displayName;
        [SerializeField] private CombatMoveDefinition[] signatureMoves;
        [SerializeField] private Vector2 visualSize = new Vector2(420, 560);
        [SerializeField, Range(-1, 1)] private int side = -1;
        [SerializeField, Min(1)] private int attacksToDefeat = 3;
        public CombatMoveDefinition[] SignatureMoves => signatureMoves;
        public int AttacksToDefeat => attacksToDefeat;
        public override bool Validate(out string error)
        {
            if (!base.Validate(out error)) return false;
            if (actorPrefab == null || sprite == null || string.IsNullOrEmpty(displayName) ||
                signatureMoves == null || signatureMoves.Length == 0 || attacksToDefeat != 3)
            { error = "A projection needs an actor, sprite, complete signatures and three counterattacks."; return false; }
            foreach (var move in signatureMoves)
            {
                if (move == null || move == this || move is DefeatableProjectionMove)
                { error = "Projection signatures must be direct, non-recursive moves."; return false; }
                if (!move.Validate(out error)) return false;
            }
            return true;
        }
        public override ICombatMoveExecution CreateExecution(CombatMoveExecutionContext context) => new Execution(this, context);

        private sealed class Execution : ICombatMoveExecution, ICombatMoveInputHandler, ICombatMoveDamageReward
        {
            private readonly DefeatableProjectionMove data;
            private readonly CombatMoveExecutionContext context;
            private readonly CombatMoveStage stage;
            private readonly CombatEnemyActor projection;
            private readonly CombatMoveExecutionContext childContext;
            private readonly GameObject originalVisual;
            private readonly bool originalVisible;
            private readonly CanvasGroup fade;
            private readonly CombatDieView[] dice = new CombatDieView[3];
            private ICombatMoveExecution child;
            private int signatureIndex, hits, reward, spawnedCounters;
            private float elapsed, nextDie = 1.3f, breather, defeatElapsed;
            private bool sequenceComplete, defeated, cancelled;
            public Execution(DefeatableProjectionMove data, CombatMoveExecutionContext context)
            {
                this.data = data; this.context = context;
                stage = new CombatMoveStage(context.Board, Cancel);
                originalVisual = context.Actor != null && context.Actor.VisualRoot != null ? context.Actor.VisualRoot.gameObject : null;
                if (originalVisual != null) { originalVisible = originalVisual.activeSelf; originalVisual.SetActive(false); }
                var root = stage.Root("Defeatable memory: " + data.displayName, true);
                // Mount-based signatures operate on the shared mount. The temporary actor must be below it.
                if (context.Actor != null) root.SetParent(context.Actor.transform.parent, false);
                projection = UnityEngine.Object.Instantiate(data.actorPrefab, root);
                projection.gameObject.SetActive(true);
                var image = projection.GetComponentInChildren<Image>(true);
                image.sprite = data.sprite; image.color = Color.white; image.material = null;
                image.preserveAspect = true; image.rectTransform.sizeDelta = data.visualSize;
                image.rectTransform.localScale = Vector3.one;
                Rect r = context.Board.PlayArea.rect;
                Vector2 position = data.side == 0
                    ? new Vector2(0, r.yMax + data.visualSize.y * .5f + 12f)
                    : new Vector2(data.side * (r.width * .5f + data.visualSize.x * .5f + 18f), 35f);
                image.rectTransform.position = context.Board.PlayArea.TransformPoint(position);
                fade = projection.GetComponent<CanvasGroup>();
                if (fade == null) fade = projection.gameObject.AddComponent<CanvasGroup>();
                fade.alpha = 0;
                projection.Initialize(new CombatEnemyMechanicContext(context.Board, context.SessionVersion));
                projection.SetPaused(true);
                childContext = new CombatMoveExecutionContext(context.Board, projection, context.Random,
                    context.SessionVersion, context.PhaseVersion);
            }
            public bool IsComplete => cancelled || defeated && defeatElapsed >= .75f;
            public void Tick(float dt)
            {
                if (cancelled || dt <= 0f) return;
                elapsed += dt;
                if (defeated)
                {
                    defeatElapsed += dt; fade.alpha = 1f - Mathf.SmoothStep(0, 1, Mathf.Clamp01(defeatElapsed / .75f));
                    return;
                }
                fade.alpha = Mathf.Clamp01(elapsed / .65f);
                if (elapsed < .75f) return;
                if (!sequenceComplete)
                {
                    breather = Mathf.Max(0, breather - dt);
                    if (child == null && breather <= 0f)
                        child = data.signatureMoves[signatureIndex].CreateExecution(childContext);
                    child?.Tick(dt);
                    if (child != null && child.IsComplete)
                    {
                        child.Cancel(); child = null;
                        context.Board.ClearRuntimeBullets(context.SessionVersion, context.PhaseVersion);
                        signatureIndex++; breather = .6f;
                        sequenceComplete = signatureIndex >= data.signatureMoves.Length;
                    }
                }
                // Geometry/spiral signatures retain their exclusive dodge segment. Counters follow them.
                bool diceAllowed = sequenceComplete || !(data.signatureMoves[signatureIndex] is ICombatExclusiveDiceMove);
                if (diceAllowed && spawnedCounters < dice.Length && elapsed >= nextDie)
                {
                    int i = spawnedCounters++;
                    dice[i] = context.Board.SpawnChoiceDie(CombatSymbol.Attack,
                        new Vector2(.22f + i * .28f, .3f + (i % 2) * .4f));
                    nextDie = elapsed + .55f;
                }
                context.Board.SetMechanicHint(data.displayName + " — " + hits + "/3");
                if (sequenceComplete && hits >= data.attacksToDefeat)
                {
                    defeated = true;
                    context.Board.ClearRuntimeBullets(context.SessionVersion, context.PhaseVersion);
                    context.Board.SetMechanicHint(null);
                }
            }
            public void HandleInput(bool catchPressed, bool rerollPressed)
            {
                if (cancelled || defeated || !catchPressed || context.Board.IsCursorStunned) return;
                int i = context.Board.FindClosestChoiceUnderCursor(dice);
                if (i < 0 || hits >= data.attacksToDefeat) return;
                context.Board.PlayDiceCatchVfx(dice[i]); dice[i].ReturnToPool(); dice[i] = null;
                AudioService.Instance?.Play(AudioId.Dice_Catch);
                hits++; reward += CombatDiceConstants.AttackDamage;
                context.Board.DestroyBulletsNearPlayer(CombatDiceConstants.ShieldBulletClearRadius);
            }
            public int ConsumePendingDamage() { int amount = reward; reward = 0; return amount; }
            public void Cancel()
            {
                if (cancelled) return; cancelled = true; reward = 0;
                child?.Cancel(); child = null;
                foreach (var die in dice) if (die != null) die.ReturnToPool();
                if (projection != null) projection.Shutdown();
                if (originalVisual != null) originalVisual.SetActive(originalVisible);
                context.Board.SetMechanicHint(null); stage.Dispose();
            }
        }
    }
}
