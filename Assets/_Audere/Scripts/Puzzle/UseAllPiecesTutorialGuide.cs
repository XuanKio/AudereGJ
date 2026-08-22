using System.Collections.Generic;
using Audere.Puzzle.PathPieces;
using UnityEngine;

namespace Audere.Puzzle
{
    /// <summary>
    /// One-line guidance for scene-authored boards that require every piece.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UseAllPiecesTutorialGuide : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private PuzzleManager puzzle;
        [SerializeField] private PathPieceHand hand;
        [SerializeField] private PuzzleHud hud;

        [Header("One-Line Tutorial")]
        [SerializeField] private string openingInstruction =
            "Hãy dùng hết các mảnh đang có để tới bánh mì.";
        [SerializeField] private string skippedPieceMessage =
            "Timor — Chưa được đâu, Audere. Mình vẫn còn một mảnh nữa mà. Quay lại và đi hết từng bước nhé.";
        [SerializeField] private string retryInstruction =
            "Timor — Không sao. Mình làm lại từ đầu, từng bước một thôi.";
        [Tooltip("Optional non-repeating Timor barks for falls or exhausted-piece failures. The legacy Retry Instruction is used when empty.")]
        [SerializeField] private List<string> attemptFailedMessages = new List<string>();

        private int lastAttemptFailedMessageIndex = -1;

        private void OnEnable()
        {
            if (puzzle != null)
            {
                puzzle.PuzzleStarted += HandlePuzzleStarted;
                puzzle.PuzzleStopped += HandlePuzzleStopped;
                puzzle.PuzzleAttemptFailed += HandleAttemptFailed;
                puzzle.PuzzleFallStarted += HandleAttemptFailed;
                puzzle.GoalReachedWithPiecesRemaining += HandleGoalReachedWithPiecesRemaining;
                puzzle.PlacementResolved += HandlePlacementResolved;
                puzzle.PuzzleCompleted += HandlePuzzleCompleted;
                puzzle.PuzzleFailed += HandlePuzzleStopped;
            }

            if (hand != null)
                hand.SelectionChanged += HandleSelectionChanged;
        }

        private void OnDisable()
        {
            if (puzzle != null)
            {
                puzzle.PuzzleStarted -= HandlePuzzleStarted;
                puzzle.PuzzleStopped -= HandlePuzzleStopped;
                puzzle.PuzzleAttemptFailed -= HandleAttemptFailed;
                puzzle.PuzzleFallStarted -= HandleAttemptFailed;
                puzzle.GoalReachedWithPiecesRemaining -= HandleGoalReachedWithPiecesRemaining;
                puzzle.PlacementResolved -= HandlePlacementResolved;
                puzzle.PuzzleCompleted -= HandlePuzzleCompleted;
                puzzle.PuzzleFailed -= HandlePuzzleStopped;
            }

            if (hand != null)
                hand.SelectionChanged -= HandleSelectionChanged;

            hand?.SetTutorialAttention(false);
        }

        private void HandlePuzzleStarted()
        {
            SetInstruction(openingInstruction);
            hand?.SetTutorialAttention(true);
        }

        private void HandleSelectionChanged(PathPieceData selectedPiece)
        {
            if (selectedPiece == null || puzzle == null ||
                puzzle.CurrentState != PuzzleManager.State.Playing)
                return;

            hand?.SetTutorialAttention(false);
        }

        private void HandlePlacementResolved(PlacementResult result)
        {
            // The text stays focused on the only new rule. A small card pulse is
            // enough to point at the remaining piece without over-tutorializing.
            hand?.SetTutorialAttention(true);
        }

        private void HandleGoalReachedWithPiecesRemaining()
        {
            hand?.SetTutorialAttention(false);
            SetInstruction(skippedPieceMessage);
        }

        private void HandleAttemptFailed()
        {
            SetInstruction(GetAttemptFailedMessage());
            hand?.SetTutorialAttention(false);
        }

        private string GetAttemptFailedMessage()
        {
            if (attemptFailedMessages == null || attemptFailedMessages.Count == 0)
                return retryInstruction;

            if (attemptFailedMessages.Count == 1)
            {
                lastAttemptFailedMessageIndex = 0;
                return attemptFailedMessages[0];
            }

            int index;
            if (lastAttemptFailedMessageIndex < 0)
            {
                index = Random.Range(0, attemptFailedMessages.Count);
            }
            else
            {
                index = Random.Range(0, attemptFailedMessages.Count - 1);
                if (index >= lastAttemptFailedMessageIndex)
                    index++;
            }
            lastAttemptFailedMessageIndex = index;
            return attemptFailedMessages[index];
        }

        private void HandlePuzzleCompleted()
        {
            hand?.SetTutorialAttention(false);
            SetInstruction(string.Empty);
        }

        private void HandlePuzzleStopped()
        {
            hand?.SetTutorialAttention(false);
            SetInstruction(string.Empty);
        }

        private void SetInstruction(string value)
        {
            if (hud == null)
                return;

            if (!hud.gameObject.activeSelf)
                hud.gameObject.SetActive(true);
            hud.SetInstruction(value);
            hud.SetCompanionMessage(string.Empty);
        }
    }
}
