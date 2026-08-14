using System.Collections;
using Audere.Puzzle.Board;
using Audere.Puzzle.PathPieces;
using UnityEngine;

namespace Audere.Puzzle
{
    public sealed class PuzzleManager : MonoBehaviour
    {
        public enum State { Playing, Traversing, Falling, Completed }

        [SerializeField] private PuzzleData puzzleData;
        [SerializeField] private BoardManager board;
        [SerializeField] private GridPlayer player;
        [SerializeField] private PathPieceHand hand;
        [SerializeField] private PathPlacementController placement;
        [SerializeField] private PuzzleHud hud;
        [SerializeField, Min(0f)] private float fallResetDelay = .16f;

        public State CurrentState { get; private set; }
        public PuzzleData PuzzleData => puzzleData;
        public BoardManager Board => board;
        public GridPlayer Player => player;

        private void Awake()
        {
            if (board == null) board = FindFirstObjectByType<BoardManager>();
            if (player == null) player = FindFirstObjectByType<GridPlayer>();
            if (hand == null) hand = FindFirstObjectByType<PathPieceHand>();
            if (placement == null) placement = FindFirstObjectByType<PathPlacementController>();
            if (hud == null)
                hud = FindFirstObjectByType<PuzzleHud>(FindObjectsInactive.Include);
        }

        private void Start()
        {
            LoadPuzzle();
        }

        public void LoadPuzzle()
        {
            if (puzzleData == null || board == null || player == null || hand == null || placement == null)
            {
                Debug.LogError(
                    "[PuzzleManager] Assign PuzzleData, Board, Player, Path Piece Hand and Path Placement Controller.",
                    this);
                return;
            }

            board.BuildBoard(puzzleData.BoardTiles);
            player.SetPosition(
                puzzleData.PlayerStartPosition,
                board.GridSpace.CellToWorldCenter(puzzleData.PlayerStartPosition));
            board.NotifyPlayerEntered(puzzleData.PlayerStartPosition, player);
            hand.Setup(puzzleData.AvailablePathPieces);
            // Placement may have been disabled in an older scene revision.  It
            // must be active for its Update loop to keep the world preview under
            // the mouse after a hand piece is selected.
            placement.enabled = true;
            placement.Setup(this, board, player, hand);
            CurrentState = State.Playing;
            SetHudMessage("Choose a path piece");
        }

        public void SubmitPlacement(PlacementResult result)
        {
            if (CurrentState != State.Playing) return;
            StartCoroutine(ExecutePlacement(result));
        }

        private IEnumerator ExecutePlacement(PlacementResult result)
        {
            CurrentState = State.Traversing;
            placement.HidePreview();
            SetHudMessage("Traversing...");
            yield return player.Traverse(result.GridPath, board, HandleFallStarted);
            hand.ConsumeSelected();

            if (player.FellDuringTraversal)
            {
                yield return new WaitForSeconds(fallResetDelay);
                LoadPuzzle();
                yield break;
            }

            if (board.TryGetTile(player.GridPosition, out BoardTile destinationTile) &&
                destinationTile.IsLevelGoal)
            {
                CurrentState = State.Completed;
                SetHudMessage("Puzzle complete! Press R or Backspace to reset");
            }
            else
            {
                CurrentState = State.Playing;
                SetHudMessage(hand.HasPieces ? "Choose a path piece" : "No pieces left - press Backspace");
            }
        }

        private void HandleFallStarted()
        {
            CurrentState = State.Falling;
            SetHudMessage("Watch your step...");
        }

        private void SetHudMessage(string message)
        {
            if (hud != null)
                hud.SetMessage(message);
        }

        private void Update()
        {
            bool resetRequested = Input.GetKeyDown(KeyCode.Backspace) ||
                (CurrentState == State.Completed && Input.GetKeyDown(KeyCode.R));

            if (resetRequested)
            {
                StopAllCoroutines();
                LoadPuzzle();
            }
        }
    }
}
