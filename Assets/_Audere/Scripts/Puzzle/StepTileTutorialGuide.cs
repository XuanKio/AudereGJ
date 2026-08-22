using System.Collections;
using Audere.Puzzle.PathPieces;
using UnityEngine;

namespace Audere.Puzzle
{
    /// <summary>
    /// Small presentation layer for the first StepTile board. Gameplay remains
    /// owned by PuzzleManager, PathPieceHand and PathPlacementController.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StepTileTutorialGuide : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private PuzzleManager puzzle;
        [SerializeField] private PathPieceHand hand;
        [SerializeField] private PathPlacementController placement;
        [SerializeField] private PuzzleHud hud;

        [Header("Hint Timing")]
        [SerializeField, Min(0f)] private float idleHintDelay = 6f;

        [Header("One-Line Tutorial")]
        [SerializeField] private string selectInstruction = "Chọn một mảnh đường.";
        [SerializeField] private string firstPieceInstruction = "Đặt mảnh này nối từ Audere.";
        [SerializeField] private string rotateInstruction = "Mảnh tiếp theo cần xoay — chuột phải để xoay.";
        [SerializeField] private string placeSecondPieceInstruction = "Đúng hướng rồi. Chuột trái để đặt.";
        [SerializeField] private string firstPieceRejectedInstruction = "Mảnh phải nối từ chỗ Audere đứng.";
        [SerializeField] private string secondPieceRejectedInstruction = "Nối mảnh này vào đoạn đường đã đặt.";
        [SerializeField, TextArea] private string retryLine = "Không sao. Mình thử lại nhé.";

        private Coroutine idleHintRoutine;
        private bool tutorialActive;
        private bool firstPlacementReacted;
        private bool pendingRetryMessage;

        private void OnEnable()
        {
            SetHudVisible(false);

            if (puzzle != null)
            {
                puzzle.PuzzleStarted += HandlePuzzleStarted;
                puzzle.PuzzleStopped += HandlePuzzleStopped;
                puzzle.PuzzleFallStarted += HandleAttemptInterrupted;
                puzzle.PuzzleAttemptFailed += HandleAttemptInterrupted;
                puzzle.PlacementResolved += HandlePlacementResolved;
                puzzle.PuzzleCompleted += HandlePuzzleCompleted;
                puzzle.PuzzleFailed += HandlePuzzleStopped;
            }

            if (hand != null)
                hand.SelectionChanged += HandleSelectionChanged;

            if (placement != null)
            {
                placement.RotationChanged += HandleRotationChanged;
                placement.PreviewChanged += HandlePreviewChanged;
                placement.PlacementRejected += HandlePlacementRejected;
            }
        }

        private void OnDisable()
        {
            if (puzzle != null)
            {
                puzzle.PuzzleStarted -= HandlePuzzleStarted;
                puzzle.PuzzleStopped -= HandlePuzzleStopped;
                puzzle.PuzzleFallStarted -= HandleAttemptInterrupted;
                puzzle.PuzzleAttemptFailed -= HandleAttemptInterrupted;
                puzzle.PlacementResolved -= HandlePlacementResolved;
                puzzle.PuzzleCompleted -= HandlePuzzleCompleted;
                puzzle.PuzzleFailed -= HandlePuzzleStopped;
            }

            if (hand != null)
                hand.SelectionChanged -= HandleSelectionChanged;

            if (placement != null)
            {
                placement.RotationChanged -= HandleRotationChanged;
                placement.PreviewChanged -= HandlePreviewChanged;
                placement.PlacementRejected -= HandlePlacementRejected;
            }

            StopIdleHint();
            if (hand != null)
                hand.SetTutorialAttention(false);
            placement?.SetRotationInputEnabled(true);
            SetHudVisible(false);
        }

        private void HandlePuzzleStarted()
        {
            if (!tutorialActive)
            {
                tutorialActive = true;
                firstPlacementReacted = false;
                placement?.SetRotationInputEnabled(false);
            }

            SetHudVisible(true);
            hud?.SetInstruction(selectInstruction);
            hud?.SetCompanionMessage(string.Empty);
            hand?.SetTutorialAttention(true);

            if (pendingRetryMessage)
            {
                hud?.SetInstruction(retryLine);
                pendingRetryMessage = false;
            }

            RestartIdleHint();
        }

        private void HandlePuzzleStopped()
        {
            tutorialActive = false;
            pendingRetryMessage = false;
            StopIdleHint();
            hand?.SetTutorialAttention(false);
            placement?.SetRotationInputEnabled(true);
            hud?.Clear();
            SetHudVisible(false);
        }

        private void HandleSelectionChanged(PathPieceData selectedPiece)
        {
            if (!tutorialActive || selectedPiece == null)
                return;

            hand?.SetTutorialAttention(false);
            hud?.SetInstruction(firstPlacementReacted
                ? rotateInstruction
                : firstPieceInstruction);

            RestartIdleHint();
        }

        private void HandleRotationChanged(GridRotation rotation)
        {
            if (!tutorialActive)
                return;

            hud?.SetInstruction(placeSecondPieceInstruction);

            RestartIdleHint();
        }

        private void HandlePreviewChanged(PlacementResult result)
        {
            if (!tutorialActive || !result.CanCommit || result.WillFall)
                return;

            if (firstPlacementReacted)
                hud?.SetInstruction(placeSecondPieceInstruction);
            RestartIdleHint();
        }

        private void HandlePlacementRejected(PlacementResult result)
        {
            if (!tutorialActive)
                return;

            hud?.SetInstruction(firstPlacementReacted
                ? secondPieceRejectedInstruction
                : firstPieceRejectedInstruction);
            RestartIdleHint();
        }

        private void HandlePlacementResolved(PlacementResult result)
        {
            if (!tutorialActive)
                return;

            if (!firstPlacementReacted)
            {
                firstPlacementReacted = true;
                placement?.SetRotationInputEnabled(true);
                hud?.SetInstruction(rotateInstruction);
            }

            hand?.SetTutorialAttention(true);
            RestartIdleHint();
        }

        private void HandleAttemptInterrupted()
        {
            if (!tutorialActive)
                return;

            // The next ResetPuzzle(true) starts a fresh two-stage lesson.
            tutorialActive = false;
            pendingRetryMessage = true;
            hud?.SetInstruction(retryLine);
            hud?.SetCompanionMessage(string.Empty);
            hand?.SetTutorialAttention(false);
            StopIdleHint();
        }

        private void HandlePuzzleCompleted()
        {
            if (!tutorialActive)
                return;

            tutorialActive = false;
            StopIdleHint();
            hand?.SetTutorialAttention(false);
            hud?.SetInstruction(string.Empty);
            hud?.SetCompanionMessage(string.Empty);
        }

        private void RestartIdleHint()
        {
            StopIdleHint();
            if (tutorialActive && idleHintDelay > 0f)
                idleHintRoutine = StartCoroutine(ShowIdleHintAfterDelay());
        }

        private IEnumerator ShowIdleHintAfterDelay()
        {
            float elapsed = 0f;
            while (tutorialActive && elapsed < idleHintDelay)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            idleHintRoutine = null;
            if (!tutorialActive)
                yield break;

            hud?.SetInstruction(firstPlacementReacted
                ? rotateInstruction
                : firstPieceInstruction);
        }

        private void StopIdleHint()
        {
            if (idleHintRoutine == null)
                return;

            StopCoroutine(idleHintRoutine);
            idleHintRoutine = null;
        }

        private void SetHudVisible(bool visible)
        {
            if (hud != null && hud.gameObject.activeSelf != visible)
                hud.gameObject.SetActive(visible);
        }
    }
}
