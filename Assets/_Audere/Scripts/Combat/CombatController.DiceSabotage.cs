using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat
{
    public sealed partial class CombatController
    {
        private bool TrySabotageDie(CombatDieView die)
        {
            if (die == null || !die.CanInteract || !activeDice.Contains(die) || tutorialActive ||
                isRecoveringPlayerTime || CurrentState != State.Playing || encounterData == null ||
                encounterData.DiceBreakTailSprite == null || encounterData.DiceBreakChance <= 0f ||
                !DiceCatchSabotage.ShouldBreak(UnityEngine.Random.value, encounterData.DiceBreakChance)) return false;
            Vector2 position = boardView.WorldToPlayArea(die.RectTransform.position);
            activeDice.Remove(die); die.ReturnToPool();
            diceSabotage?.Cancel();
            diceSabotage = new DiceCatchSabotage(boardView, encounterData.DiceBreakTailSprite, position);
            // Destruction never counts as a catch or applies the die's ability.
            if (activeDice.Count == 0 && !isBatchSpawning && enemyRuntime != null && enemyRuntime.ShouldSpawnDice)
                ScheduleNextBatch(encounterData.BatchRespawnDelay);
            return true;
        }
    }

    internal sealed class DiceCatchSabotage
    {
        private readonly CombatMoveStage stage;
        private readonly Image tail;
        private readonly Image[] fragments = new Image[7];
        private readonly Vector2 target;
        private float elapsed;
        private bool cancelled;
        public static bool ShouldBreak(float roll, float chance) => roll < chance;
        public DiceCatchSabotage(CombatBoardView board, Sprite sprite, Vector2 target)
        {
            this.target = target; stage = new CombatMoveStage(board, Cancel);
            var root = stage.Root("Dice catch interception");
            tail = CombatMoveStage.Picture(root, "Timor tail strike", sprite, new Vector2(112, 190), Color.white);
            tail.rectTransform.anchoredPosition = target;
            tail.rectTransform.localRotation = Quaternion.Euler(0, 0, -25);
            for (int i = 0; i < fragments.Length; i++)
            {
                fragments[i] = CombatMoveStage.Picture(root, "Broken die fragment", null, new Vector2(7, 7), Color.white);
                fragments[i].rectTransform.anchoredPosition = target;
            }
        }
        public void Tick(float dt)
        {
            if (cancelled || dt <= 0) return;
            elapsed += dt; float t = Mathf.Clamp01(elapsed / .55f);
            tail.rectTransform.anchoredPosition = target + new Vector2(75, 160) * Mathf.SmoothStep(0, 1, t);
            tail.color = new Color(1, 1, 1, 1 - t);
            for (int i = 0; i < fragments.Length; i++)
            {
                float a = i * Mathf.PI * 2 / fragments.Length;
                fragments[i].rectTransform.anchoredPosition = target + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * t * 70;
                fragments[i].color = new Color(1, 1, 1, 1 - t);
            }
            if (t >= 1) Cancel();
        }
        public void Cancel() { if (cancelled) return; cancelled = true; stage.Dispose(); }
    }
}
