using System.Collections;
using System.Collections.Generic;
using Audere.Audio;
using Audere.Dialogue;
using UnityEngine;

namespace Audere.Combat
{
    public sealed partial class CombatController
    {
        private readonly HashSet<CombatTutorialData> completedGuidedTutorials = new HashSet<CombatTutorialData>();
        private bool guidedTutorialRunning, guidedLessonComplete, guidedRerolled;
        private int guidedLessonIndex = -1, guidedInputFrame, guidedShots;
        private float guidedElapsed, guidedNextShot, guidedDodgeTime;
        private CombatDieView guidedDie;
        private CombatTutorialLesson guidedLesson;
        private Vector2 guidedTarget;
        private CombatTutorialView GuidedHud => GameplayUIRoot.Instance != null ? GameplayUIRoot.Instance.CombatTutorial : null;
        public int GuidedLessonIndex => guidedLessonIndex;
        public CombatTutorialLessonKind? GuidedLessonKind => guidedLesson?.Kind;
        public bool IsGuidedTutorialRunning => guidedTutorialRunning;

        private IEnumerator RunGuidedTutorial(int session)
        {
            guidedTutorialRunning = true;
            tutorialOpeningBatchPending = false;
            boardView.ResetPlayer();
            boardView.BeginGuidedTutorialPresentation(encounterData.TutorialData.SquareBoardSize);
            GuidedHud?.SetGuidedBoardTarget(boardView.PlayArea);
            var lessons = encounterData.TutorialData.GuidedLessons;
            for (guidedLessonIndex = 0; guidedLessonIndex < lessons.Count && SessionIsCurrent(session); guidedLessonIndex++)
            {
                guidedLesson = lessons[guidedLessonIndex];
                PrepareGuidedLesson();
                CurrentState = State.DialoguePause;
                boardView.ActiveEnemyActor?.SetPaused(true);
                ShowGuidedPrompt(guidedLesson.Instruction);
                var dialogue = GameplayUIRoot.Instance != null ? GameplayUIRoot.Instance.Dialogue : null;
                bool finished = false;
                if (dialogue != null && dialogue.Play(guidedLesson.Dialogue, _ => finished = true,
                    DialoguePlaybackMode.CallerOwnedPause, false))
                    while (!finished && SessionIsCurrent(session) && guidedTutorialRunning) yield return null;
                if (!SessionIsCurrent(session) || !guidedTutorialRunning) yield break;
                CurrentState = State.Playing;
                guidedInputFrame = Time.frameCount;
                ShowGuidedPrompt(guidedLesson.Instruction);
                while (!guidedLessonComplete && SessionIsCurrent(session) && guidedTutorialRunning) yield return null;
                if (!SessionIsCurrent(session) || !guidedTutorialRunning) yield break;
                boardView.ClearPlayerConstraint();
                boardView.ClearRuntimeBullets(session);
                boardView.SetGuidedTutorialTarget(null);
                for (float hold = 0f; hold < 2.5f && SessionIsCurrent(session); hold += Time.deltaTime) yield return null;
            }
            if (!SessionIsCurrent(session) || !guidedTutorialRunning) yield break;
            // Retry returns directly to the real encounter once this lesson sequence is complete.
            StartActualCombatAfterTutorial(session, enemyRuntime.PhaseVersion);
        }

        private void PrepareGuidedLesson()
        {
            var kind = guidedLesson.Kind;
            bool keepDie = (kind == CombatTutorialLessonKind.Shield || kind == CombatTutorialLessonKind.StunReroll) && guidedDie != null;
            if (!keepDie && guidedDie != null) { guidedDie.ReturnToPool(); guidedDie = null; }
            boardView.ClearRuntimeBullets(enemyRuntime.SessionVersion);
            boardView.ClearPlayerConstraint();
            boardView.HideStunZones();
            boardView.SetGuidedTutorialTarget(null);
            guidedLessonComplete = guidedRerolled = false;
            guidedElapsed = guidedNextShot = guidedDodgeTime = 0f;
            guidedShots = 0;
            bool enemyVisible = kind >= CombatTutorialLessonKind.Attack;
            boardView.SetGuidedTutorialVisibility(kind >= CombatTutorialLessonKind.Time, enemyVisible, enemyVisible,
                kind >= CombatTutorialLessonKind.Heal);
            switch (kind)
            {
                case CombatTutorialLessonKind.Move:
                    guidedTarget = boardView.PlayArea.rect.center + new Vector2(90f, 65f);
                    boardView.SetGuidedTutorialTarget(guidedTarget);
                    break;
                case CombatTutorialLessonKind.Damage:
                    guidedTarget = boardView.PlayArea.rect.center;
                    boardView.CatchCursor.anchoredPosition = guidedTarget;
                    boardView.SetPlayerConstraint(guidedTarget, 0f);
                    break;
                case CombatTutorialLessonKind.Heal:
                    guidedDie = boardView.SpawnChoiceDie(CombatSymbol.Heal, Vector2.one * .5f);
                    break;
                case CombatTutorialLessonKind.Attack:
                    guidedDie = boardView.SpawnChoiceDie(CombatSymbol.Attack, Vector2.one * .5f);
                    break;
                case CombatTutorialLessonKind.Reroll:
                    guidedDie = boardView.SpawnChoiceDie(CombatSymbol.Heal, Vector2.one * .5f);
                    break;
                case CombatTutorialLessonKind.Shield:
                    if (guidedDie == null) guidedDie = boardView.SpawnChoiceDie(CombatSymbol.Shield, Vector2.one * .5f);
                    break;
                case CombatTutorialLessonKind.StunCatch:
                case CombatTutorialLessonKind.StunReroll:
                    boardView.SetStunZonePresentation(0, new Vector2(.28f, .5f), new Vector2(.16f, .6f), .72f, true);
                    if (guidedDie == null) guidedDie = boardView.SpawnChoiceDie(CombatSymbol.Heal, new Vector2(.36f, .5f));
                    break;
            }
        }

        private void TickGuidedTutorial(float deltaTime)
        {
            TickGuidedTutorialFrame(deltaTime, Input.mousePosition, Input.GetMouseButtonDown(0), Input.GetMouseButtonDown(1), HasCombatInput());
        }

        private void TickGuidedTutorialFrame(float deltaTime, Vector2 pointer, bool catchPressed, bool rerollPressed, bool hasInput)
        {
            if (CurrentState != State.Playing || guidedLesson == null || !hasInput || deltaTime <= 0f) return;
            if (guidedLessonComplete) return;
            deltaTime = Mathf.Min(deltaTime, .1f);
            guidedElapsed += deltaTime;
            boardView.UpdateCursor(pointer);
            boardView.TickHeartFeedback(deltaTime);
            if (guidedDie != null && guidedDie.CanInteract && !guidedDie.IsRerolling)
                guidedDie.TickMovement(boardView.PlayArea.rect, deltaTime);

            var kind = guidedLesson.Kind;
            if (kind == CombatTutorialLessonKind.Time || kind == CombatTutorialLessonKind.Dodge)
            {
                encounterTimeRemaining = Mathf.Max(1f, encounterTimeRemaining - deltaTime);
                boardView.UpdateTimer(encounterTimeRemaining / ResolveActiveMaximumTime());
            }
            if (kind == CombatTutorialLessonKind.Move && Vector2.Distance(boardView.PlayerPosition, guidedTarget) < 28f)
                CompleteGuidedLesson("Đúng rồi · Heart đi theo chuột.");
            if (kind == CombatTutorialLessonKind.Time && guidedElapsed >= 3f)
                CompleteGuidedLesson("3 giây trôi qua → TIME giảm 3. TIME là máu của bạn.");
            if (kind == CombatTutorialLessonKind.Damage && guidedShots == 0)
            {
                SpawnGuidedBullet(guidedTarget + Vector2.up * 125f, Vector2.down * 125f);
                guidedShots++;
            }
            if (kind == CombatTutorialLessonKind.Shield && guidedShots == 0)
            {
                Vector2 center = guidedDie.RectTransform.anchoredPosition;
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * Mathf.PI / 4f;
                    SpawnGuidedBullet(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 112f, Vector2.zero);
                }
                guidedShots++;
            }
            if (kind == CombatTutorialLessonKind.Dodge && guidedElapsed >= guidedNextShot)
            {
                float x = Mathf.Clamp(boardView.PlayerPosition.x, boardView.PlayArea.rect.xMin + 35f, boardView.PlayArea.rect.xMax - 35f);
                SpawnGuidedBullet(new Vector2(x, boardView.PlayArea.rect.yMax - 20f), Vector2.down * 135f);
                guidedNextShot = guidedElapsed + 1.15f;
                guidedShots++;
            }
            int hits = boardView.TickBullets(deltaTime, encounterData.PlayerHitInvulnerability);
            if (hits > 0)
            {
                ApplyPlayerHit();
                if (kind == CombatTutorialLessonKind.Damage)
                    CompleteGuidedLesson($"−{encounterData.BulletTimePenaltySeconds:0.#} TIME · Trúng đạn làm mất thêm máu.");
                else if (kind == CombatTutorialLessonKind.Dodge)
                {
                    guidedDodgeTime = 0f;
                    ShowGuidedPrompt("Trúng đạn làm TIME giảm thêm. Thử rê chuột sang khoảng trống nhé.\nNé liên tục 4 giây để hoàn thành.");
                }
            }
            if (kind == CombatTutorialLessonKind.Dodge)
            {
                guidedDodgeTime += deltaTime;
                if (guidedDodgeTime >= 4f && guidedShots >= 3) CompleteGuidedLesson("Đã né được · Di chuyển để giữ TIME lâu hơn.");
            }
            if (kind == CombatTutorialLessonKind.Reroll && guidedRerolled && guidedDie != null && guidedDie.CanInteract)
                CompleteGuidedLesson("Đã gieo ra KHIÊN · Viên đã đáp xuống và có thể bắt.");
            if (Time.frameCount > guidedInputFrame)
                HandleGuidedTutorialInput(catchPressed, rerollPressed);
        }

        private void HandleGuidedTutorialInput(bool catchPressed, bool rerollPressed)
        {
            if (guidedLessonComplete || guidedLesson == null || (!catchPressed && !rerollPressed)) return;
            var kind = guidedLesson.Kind;
            if (kind == CombatTutorialLessonKind.Finish)
            {
                if (catchPressed) CompleteGuidedLesson("Bắt đầu · TIME và máu đối thủ được khôi phục.");
                return;
            }
            if (guidedDie == null || !guidedDie.CanInteract || !boardView.CursorOverlaps(guidedDie))
            {
                if (guidedDie != null) ShowGuidedPrompt("Đợi viên đáp xuống, rồi đưa vòng bắt trùm lên nó.\n" +
                    (kind == CombatTutorialLessonKind.Reroll || kind == CombatTutorialLessonKind.StunReroll ? "CHUỘT PHẢI: gieo lại." : "CHUỘT TRÁI: bắt viên."));
                return;
            }
            if (kind == CombatTutorialLessonKind.StunCatch)
            {
                if (catchPressed && boardView.IsCursorStunned)
                {
                    boardView.PlayBlockedCursorFeedback();
                    CompleteGuidedLesson("Dấu X: vùng nhiễu chặn BẮT. Viên xúc xắc vẫn còn nguyên.");
                }
                else ShowGuidedPrompt("Đưa vòng bắt chạm vùng nhiễu và trùm lên viên.\nCHUỘT TRÁI: thử bắt trong vùng này.");
                return;
            }
            if (rerollPressed)
            {
                if (kind != CombatTutorialLessonKind.Reroll && kind != CombatTutorialLessonKind.StunReroll)
                { ShowGuidedPrompt("Đưa vòng bắt trùm lên viên xúc xắc.\nBước này dùng CHUỘT TRÁI để bắt."); return; }
                if (kind == CombatTutorialLessonKind.StunReroll && !guidedRerolled && !boardView.IsCursorStunned)
                { ShowGuidedPrompt("Giữ vòng bắt chạm vùng nhiễu.\nCHUỘT PHẢI: thử gieo ở đây."); return; }
                CombatSymbol next = guidedDie.Symbol == CombatSymbol.Shield
                    ? CombatDiceConstants.RerollSymbol(guidedDie.Symbol)
                    : CombatSymbol.Shield;
                guidedDie = boardView.RerollDie(guidedDie, next);
                guidedRerolled = true;
                AudioService.Instance?.Play(AudioId.Dice_Roll);
                if (kind == CombatTutorialLessonKind.StunReroll)
                    ShowGuidedPrompt("Vùng nhiễu vẫn cho phép gieo lại.\nĐặt viên ở mép phải vòng rồi CHUỘT PHẢI.\nRa ngoài vùng, CHUỘT TRÁI: bắt.");
                return;
            }
            if (kind == CombatTutorialLessonKind.Reroll || (kind == CombatTutorialLessonKind.StunReroll && !guidedRerolled))
            { ShowGuidedPrompt("Đưa viên vào vòng bắt.\nBước này cần CHUỘT PHẢI để gieo lại trước."); return; }
            if (boardView.IsCursorStunned)
            { boardView.PlayBlockedCursorFeedback(); ShowGuidedPrompt("Vòng bắt còn chạm vùng nhiễu. Đưa viên và vòng bắt ra ngoài rồi bắt."); return; }
            CombatSymbol symbol = guidedDie.Symbol;
            if (kind == CombatTutorialLessonKind.Heal && symbol != CombatSymbol.Heal ||
                kind == CombatTutorialLessonKind.Attack && symbol != CombatSymbol.Attack ||
                kind == CombatTutorialLessonKind.Shield && symbol != CombatSymbol.Shield) return;
            guidedDie.PlayCaptured(); guidedDie = null;
            AudioService.Instance?.Play(AudioId.Dice_Catch);
            ApplyImmediateDiceEffect(symbol);
            string feedback = kind == CombatTutorialLessonKind.Heal ? $"+{CombatDiceConstants.HealTimeSeconds:0} TIME · Viên hồi nhịp bù lại máu đã mất." :
                kind == CombatTutorialLessonKind.Attack ? $"−{CombatDiceConstants.AttackDamage} HP · Thanh máu đối thủ vừa giảm." :
                kind == CombatTutorialLessonKind.Shield ? "Khiên vừa xóa đạn quanh Heart · Dùng khi cần khoảng trống." :
                "Đã bắt được · Vùng nhiễu chỉ chặn bắt.";
            CompleteGuidedLesson(feedback);
        }

        private void SpawnGuidedBullet(Vector2 position, Vector2 velocity) => boardView.SpawnEnemyBullet(
            encounterData.TutorialData.DemonstrationBullet, position, velocity, enemyRuntime.SessionVersion, enemyRuntime.PhaseVersion);

        private void ShowGuidedPrompt(string instruction)
        {
            RectTransform focus = guidedLesson != null && (guidedLesson.Kind == CombatTutorialLessonKind.Time || guidedLesson.Kind == CombatTutorialLessonKind.Damage)
                ? boardView.TimerFocusTarget : guidedLesson != null && guidedLesson.Kind == CombatTutorialLessonKind.Attack
                ? boardView.GuidedEnemyHealthTarget : guidedDie != null ? guidedDie.RectTransform : boardView.PlayArea;
            GuidedHud?.ShowGuidedInstruction(instruction, guidedLessonIndex + 1, encounterData.TutorialData.GuidedLessons.Count,
                focus);
        }

        private void CompleteGuidedLesson(string feedback)
        {
            if (guidedLessonComplete) return;
            guidedLessonComplete = true;
            // Restore the actual objective if a corrective hint was showing.
            ShowGuidedPrompt(guidedLesson.Instruction);
            GuidedHud?.ShowGuidedSuccess();
        }

        private void ResetGuidedTutorial()
        {
            if (!guidedTutorialRunning) return;
            guidedTutorialRunning = false;
            guidedLessonIndex = -1;
            guidedLesson = null;
            if (guidedDie != null) guidedDie.ReturnToPool();
            guidedDie = null;
            boardView?.ClearPlayerConstraint();
            boardView?.HideStunZones();
            boardView?.EndGuidedTutorialPresentation();
            GuidedHud?.ForceHide();
        }
    }
}
