using System.Collections;
using System.Collections.Generic;
using Audere.Puzzle;
using UnityEngine;

namespace Audere.Story.Steps
{
    /// <summary>
    /// Establishes a deterministic authored state before a chained puzzle event.
    /// Replays and cancelled transitions return here instead of resuming mid-tween.
    /// </summary>
    public sealed class PuzzleSequencePrepareStep : StoryStep
    {
        [SerializeField] private PuzzleRootCoordinator puzzleRootCoordinator;
        [SerializeField] private PuzzleController startingPuzzle;
        [SerializeField] private List<PuzzleController> followingPuzzles = new List<PuzzleController>();
        [SerializeField] private bool showPlayerAtStart = true;
        [SerializeField] private bool hideStartingBoardUntilReveal = true;
        [Tooltip("Place this puzzle's PlayerStart at the previous active puzzle's captured Goal position.")]
        [SerializeField] private bool alignToPreviousGoal;

        protected override IEnumerator Execute()
        {
            if (!NormalizeNow())
                FailStep();
            yield break;
        }

        public bool NormalizeNow()
        {
            if (startingPuzzle == null)
            {
                Debug.LogError("[PuzzleSequencePrepareStep] Assign Starting Puzzle.", this);
                return false;
            }

            if (puzzleRootCoordinator != null)
            {
                return puzzleRootCoordinator.PreparePuzzle(
                    startingPuzzle,
                    showPlayerAtStart,
                    hideStartingBoardUntilReveal,
                    alignToPreviousGoal);
            }

            for (int index = 0; index < followingPuzzles.Count; index++)
            {
                PuzzleController puzzle = followingPuzzles[index];
                if (puzzle == null)
                    continue;
                if (!puzzle.ResetToInitialState(false, false))
                    return false;
            }

            if (!startingPuzzle.ResetToInitialState(true, showPlayerAtStart))
                return false;

            if (hideStartingBoardUntilReveal)
                startingPuzzle.SetBoardTilesVisible(false);
            return true;
        }

        public void NormalizeAfterCancel()
        {
            // Do not resurrect boards/actors while OnDisable is tearing down the
            // scene. Explicit cancellation in a live scene still restores a retry.
            if (!Application.isPlaying || !isActiveAndEnabled || !gameObject.scene.isLoaded ||
                !CanNormalize(startingPuzzle))
                return;

            var levels = puzzleRootCoordinator != null ? puzzleRootCoordinator.Puzzles : followingPuzzles;
            foreach (var level in levels)
                if (!CanNormalize(level) || level.IsPlaying)
                    return;
            NormalizeNow();
        }

        private static bool CanNormalize(PuzzleController level) => level != null &&
            level.PuzzleRoot != null && level.Puzzle != null && level.Puzzle.CanNormalize;
    }
}
