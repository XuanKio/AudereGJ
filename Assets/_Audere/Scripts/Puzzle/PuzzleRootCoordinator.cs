using System.Collections.Generic;
using System.Linq;
using Audere.Puzzle.Board;
using Audere.Puzzle.PathPieces;
using UnityEngine;

namespace Audere.Puzzle
{
    /// <summary>
    /// Location-scoped coordinator for the one shared Player/runtime and every
    /// scene-authored puzzle level under a Puzzle Root. Story still owns order;
    /// this component only makes each hand-off deterministic.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PuzzleRootCoordinator : MonoBehaviour
    {
        [Header("Shared Runtime")]
        [SerializeField] private GridPlayer sharedPlayer;
        [SerializeField] private PuzzleRuntime runtime;

        [Header("Scene-Authored Levels")]
        [SerializeField] private List<PuzzleController> puzzles = new List<PuzzleController>();
        [SerializeField] private bool validateOnAwake = true;

        public GridPlayer SharedPlayer => sharedPlayer;
        public PuzzleRuntime Runtime => runtime;
        public IReadOnlyList<PuzzleController> Puzzles => puzzles;
        public PuzzleController ActivePuzzle { get; private set; }
        public bool HasPendingTransitionAnchor => hasPendingTransitionAnchor;
        public Vector3 PendingTransitionAnchorWorldPosition => pendingTransitionAnchorWorldPosition;

        private bool hasPendingTransitionAnchor;
        private Vector3 pendingTransitionAnchorWorldPosition;
        private PuzzleController pendingTransitionAnchorSource;

        private void Awake()
        {
            CacheScopedReferences();
            if (validateOnAwake)
                ValidateConfiguration(true);
        }

        private void OnValidate()
        {
            CacheScopedReferences();
        }

        public bool PreparePuzzle(
            PuzzleController target,
            bool showPlayerAtStart,
            bool hideBoardUntilReveal,
            bool alignToPreviousGoal = false)
        {
            CacheScopedReferences();
            if (!CanTakeOwnership(target))
                return false;

            Vector3 transitionAnchor = default;
            if (alignToPreviousGoal && !TryResolveTransitionAnchor(target, out transitionAnchor))
            {
                Debug.LogError(
                    $"[PuzzleRootCoordinator] Cannot align '{target.name}' because the previous puzzle has no captured Goal anchor.",
                    target);
                return false;
            }

            // Normalize inactive levels first. They share the Player, so the
            // target must always be normalized last and own the final position.
            foreach (PuzzleController puzzle in puzzles)
            {
                if (puzzle == null || puzzle == target)
                    continue;

                if (!puzzle.ResetToInitialState(false, false))
                    return false;
                puzzle.SetBoardTilesVisible(true);
            }

            if (!target.ResetToInitialState(true, showPlayerAtStart && !alignToPreviousGoal))
                return false;

            if (alignToPreviousGoal)
            {
                if (!target.AlignPlayerStartToWorldPosition(transitionAnchor))
                    return false;
                if (showPlayerAtStart && !target.Puzzle.PlacePlayerAtStart())
                    return false;
            }

            if (hideBoardUntilReveal)
                target.SetBoardTilesVisible(false);

            ActivePuzzle = target;
            return true;
        }

        public bool ActivateForReveal(PuzzleController target, bool showPlayerAtStart)
        {
            CacheScopedReferences();
            if (!CanTakeOwnership(target))
                return false;

            bool alreadyPrepared = ActivePuzzle == target &&
                target.CurrentFlowState == PuzzleFlowState.Preparing;
            if (alreadyPrepared)
            {
                if (target.PuzzleRoot != null && !target.PuzzleRoot.gameObject.activeSelf)
                    target.PuzzleRoot.gameObject.SetActive(true);
            }
            else if (!target.ResetToInitialState(true, showPlayerAtStart))
            {
                return false;
            }

            HideSupersededTransitionSource(target);
            ActivePuzzle = target;
            return true;
        }

        public bool CaptureTransitionAnchor(PuzzleController source, Transform anchor)
        {
            if (source == null || anchor == null)
            {
                Debug.LogError("[PuzzleRootCoordinator] A source puzzle and Goal anchor are required.", this);
                return false;
            }

            pendingTransitionAnchorSource = source;
            pendingTransitionAnchorWorldPosition = anchor.position;
            hasPendingTransitionAnchor = true;
            return true;
        }

        public bool TryBuildTileOrder(
            PuzzleController puzzle,
            bool reverse,
            Transform excluded,
            List<Transform> results)
        {
            results.Clear();
            if (puzzle == null || puzzle.Puzzle == null || puzzle.Puzzle.Board == null)
            {
                Debug.LogError("[PuzzleRootCoordinator] Cannot build tile order without a puzzle Board.", this);
                return false;
            }

            BoardManager board = puzzle.Puzzle.Board;
            board.RegisterExistingTiles();
            if (board.GridSpace == null || board.GridPositions.Count == 0 || puzzle.Puzzle.PlayerStartTransform == null)
            {
                Debug.LogError(
                    $"[PuzzleRootCoordinator] '{puzzle.name}' needs a populated Board and PlayerStart.",
                    puzzle);
                return false;
            }

            Vector2Int start = board.GridSpace.WorldToCell(puzzle.Puzzle.PlayerStartTransform.position);
            HashSet<BoardTile> uniqueTiles = new HashSet<BoardTile>();
            CollectTiles(board.BoardVisualRoot, uniqueTiles);
            CollectTiles(board.LevelObjectiveRoot, uniqueTiles);

            IEnumerable<BoardTile> candidates = uniqueTiles
                .Where(tile => tile != null && tile.transform != excluded);
            IEnumerable<BoardTile> ordered = reverse
                ? candidates
                    .OrderBy(tile => tile.IsLevelGoal ? 1 : 0)
                    .ThenByDescending(tile =>
                        Mathf.Abs(tile.GridPosition.x - start.x) +
                        Mathf.Abs(tile.GridPosition.y - start.y))
                    .ThenByDescending(tile => tile.GridPosition.y)
                    .ThenByDescending(tile => tile.GridPosition.x)
                : candidates
                    .OrderBy(tile => tile.IsLevelGoal ? 1 : 0)
                    .ThenBy(tile =>
                        Mathf.Abs(tile.GridPosition.x - start.x) +
                        Mathf.Abs(tile.GridPosition.y - start.y))
                    .ThenBy(tile => tile.GridPosition.y)
                    .ThenBy(tile => tile.GridPosition.x);

            foreach (BoardTile tile in ordered)
                results.Add(tile.transform);

            return results.Count > 0;
        }

        [ContextMenu("Validate Puzzle Root")]
        public bool ValidateConfiguration()
        {
            return ValidateConfiguration(true);
        }

        public bool ValidateConfiguration(bool logErrors)
        {
            CacheScopedReferences();
            bool valid = true;

            valid &= Require(sharedPlayer != null, "Assign the one shared GridPlayer.", logErrors);
            valid &= Require(runtime != null, "Assign the shared PuzzleRuntime.", logErrors);
            valid &= Require(puzzles.Count > 0, "Register at least one child PuzzleController.", logErrors);

            int previewCount = GetComponentsInChildren<PathPreview>(true).Length;
            int placementCount = GetComponentsInChildren<PathPlacementController>(true).Length;
            valid &= Require(previewCount == 1, $"Puzzle Root needs exactly one PathPreview; found {previewCount}.", logErrors);
            valid &= Require(placementCount == 1, $"Puzzle Root needs exactly one PathPlacementController; found {placementCount}.", logErrors);

            foreach (PuzzleController puzzle in puzzles)
            {
                if (puzzle == null)
                {
                    valid &= Require(false, "Puzzle registry contains a null entry.", logErrors);
                    continue;
                }

                PuzzleManager manager = puzzle.Puzzle;
                valid &= Require(manager != null, $"'{puzzle.name}' has no PuzzleManager.", logErrors, puzzle);
                if (manager == null)
                    continue;

                valid &= Require(manager.Player == sharedPlayer, $"'{puzzle.name}' does not use the shared Player.", logErrors, puzzle);
                valid &= Require(manager.Board != null, $"'{puzzle.name}' has no BoardManager.", logErrors, puzzle);
                valid &= Require(manager.PlayerStartTransform != null, $"'{puzzle.name}' has no PlayerStart.", logErrors, puzzle);
                valid &= Require(
                    manager.Board != null && manager.Board.GridSpace != null,
                    $"'{puzzle.name}' has no GridSpace.",
                    logErrors,
                    puzzle);
                if (manager.Board == null || manager.Board.GridSpace == null || manager.PlayerStartTransform == null)
                    continue;

                manager.Board.RegisterExistingTiles();
                Vector2Int start = manager.Board.GridSpace.WorldToCell(manager.PlayerStartTransform.position);
                valid &= Require(
                    manager.Board.ContainsCell(start),
                    $"'{puzzle.name}' PlayerStart {start} does not overlap a BoardTile.",
                    logErrors,
                    manager.PlayerStartTransform);

                int goalCount = manager.Board.GridPositions.Count(position =>
                    manager.Board.TryGetTile(position, out BoardTile tile) && tile.IsLevelGoal);
                valid &= Require(goalCount == 1, $"'{puzzle.name}' needs exactly one Goal; found {goalCount}.", logErrors, puzzle);

                int pieceCount = manager.PuzzleData != null ? manager.PuzzleData.AvailablePathPieces.Count : 0;
                valid &= Require(
                    pieceCount <= PuzzleContentConstants.Hand.MaxSlots,
                    $"'{puzzle.name}' has {pieceCount} pieces but the hand supports {PuzzleContentConstants.Hand.MaxSlots}.",
                    logErrors,
                    puzzle);
            }

            return valid;
        }

        private bool CanTakeOwnership(PuzzleController target)
        {
            if (target == null)
            {
                Debug.LogError("[PuzzleRootCoordinator] Target puzzle is required.", this);
                return false;
            }

            if (!puzzles.Contains(target))
            {
                Debug.LogError($"[PuzzleRootCoordinator] '{target.name}' is not registered under this Puzzle Root.", target);
                return false;
            }

            foreach (PuzzleController puzzle in puzzles)
            {
                if (puzzle == null || !puzzle.IsPlaying)
                    continue;

                Debug.LogError(
                    puzzle == target
                        ? $"[PuzzleRootCoordinator] Cannot prepare '{target.name}' while it is already playing."
                        : $"[PuzzleRootCoordinator] Cannot activate '{target.name}' while '{puzzle.name}' is playing.",
                    this);
                return false;
            }

            return true;
        }

        private bool TryResolveTransitionAnchor(PuzzleController target, out Vector3 worldPosition)
        {
            if (hasPendingTransitionAnchor && pendingTransitionAnchorSource != target)
            {
                worldPosition = pendingTransitionAnchorWorldPosition;
                return true;
            }

            if (ActivePuzzle != null && ActivePuzzle != target &&
                ActivePuzzle.TryGetGoalAnchor(out Transform goal, false))
            {
                worldPosition = goal.position;
                return true;
            }

            worldPosition = default;
            return false;
        }

        /// <summary>
        /// A completed puzzle keeps its Goal tile visible as the hand-off anchor
        /// while dialogue is playing. Once the next board starts revealing, that
        /// old level must disappear so only the new PlayerStart tile remains at
        /// the shared world position.
        /// </summary>
        private void HideSupersededTransitionSource(PuzzleController target)
        {
            if (!hasPendingTransitionAnchor ||
                pendingTransitionAnchorSource == null ||
                pendingTransitionAnchorSource == target)
            {
                return;
            }

            Transform sourceRoot = pendingTransitionAnchorSource.PuzzleRoot;
            if (sourceRoot != null && sourceRoot.gameObject.activeSelf)
                sourceRoot.gameObject.SetActive(false);
        }

        private void CacheScopedReferences()
        {
            if (sharedPlayer == null)
                sharedPlayer = GetComponentInChildren<GridPlayer>(true);
            if (runtime == null)
                runtime = GetComponentInChildren<PuzzleRuntime>(true);

            PuzzleController[] discovered = GetComponentsInChildren<PuzzleController>(true);
            puzzles.RemoveAll(puzzle => puzzle == null || !discovered.Contains(puzzle));
            foreach (PuzzleController puzzle in discovered)
                if (!puzzles.Contains(puzzle))
                    puzzles.Add(puzzle);

            puzzles = puzzles
                .Where(puzzle => puzzle != null)
                .OrderBy(puzzle => puzzle.PuzzleRoot != null ? puzzle.PuzzleRoot.GetSiblingIndex() : int.MaxValue)
                .ToList();
        }

        private bool Require(
            bool condition,
            string message,
            bool logErrors,
            Object context = null)
        {
            if (!condition && logErrors)
                Debug.LogError($"[PuzzleRootCoordinator] {message}", context != null ? context : this);
            return condition;
        }

        private static void CollectTiles(Transform root, HashSet<BoardTile> results)
        {
            if (root == null)
                return;
            foreach (BoardTile tile in root.GetComponentsInChildren<BoardTile>(true))
                results.Add(tile);
        }
    }
}
