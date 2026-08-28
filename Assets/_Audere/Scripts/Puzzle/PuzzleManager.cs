using System;
using System.Collections;
using Audere.Audio;
using Audere.Dialogue;
using Audere.Puzzle.Board;
using Audere.Puzzle.PathPieces;
using UnityEngine;

namespace Audere.Puzzle
{
    public sealed class PuzzleManager : MonoBehaviour
    {
        public enum State { Idle, Playing, Traversing, Falling, Completed, Failed }

        [SerializeField] private PuzzleData puzzleData;
        [SerializeField] private BoardManager board;
        [SerializeField] private PuzzlePlayerStart playerStart;
        [Tooltip("Shared scene Player. Each puzzle places this same actor at its own PlayerStart when Play begins.")]
        [SerializeField] private GridPlayer player;
        [SerializeField] private PathPieceHand hand;
        [Tooltip("Shared runtime under this location's Puzzle Root. It owns path placement, preview and placed path visuals.")]
        [SerializeField] private PuzzleRuntime runtime;
        [SerializeField] private PathPlacementController placement;
        [Tooltip("Runtime-only path visuals are cleared from this root during normalize/reset.")]
        [SerializeField] private Transform placedPathRoot;
        [SerializeField] private PuzzleHud hud;
        [SerializeField, Min(0f)] private float fallResetDelay = .16f;
        [SerializeField] private bool retryWhenOutOfPieces;
        [SerializeField, Min(0f)] private float failedAttemptResetDelay = .8f;

        [SerializeField] private CooperativePuzzleSession cooperative;
        private GridPlayer activePlayer;
        public GridPlayer ActivePlayer => activePlayer != null ? activePlayer : player;
        public CooperativePuzzleSession Cooperative => cooperative;

        public State CurrentState { get; private set; }
        public PuzzleData PuzzleData => puzzleData;
        public BoardManager Board => board;
        public PuzzlePlayerStart PlayerStart => playerStart;
        public GridPlayer Player => player;
        public Transform PlayerStartTransform => playerStart != null ? playerStart.transform : null;
        public event Action PuzzleCompleted;
        public event Action PuzzleFailed;
        public event Action PuzzleStarted;
        public event Action PuzzleStopped;
        public event Action PuzzleFallStarted;
        public event Action PuzzleAttemptFailed;
        public event Action GoalReachedWithPiecesRemaining;
        public event Action<PlacementResult> PlacementResolved;
        private bool completionLocked;

        // Cancellation during scene teardown must only release ownership. The UI
        // or shared actor may already have been destroyed by Unity at that point.
        public bool CanNormalize => board != null && playerStart != null && player != null &&
            (hand != null || (GameplayUIRoot.Instance != null && GameplayUIRoot.Instance.PathPieceHand != null)) &&
            (placement != null || (runtime != null && runtime.Placement != null));

        private void Awake()
        {
            ResolveRuntimeReferences();
            CurrentState = State.Idle;
        }

        private void OnValidate()
        {
            ResolveRuntimeReferences();
        }

        /// <summary>Legacy entry point. It now starts the scene-authored board.</summary>
        public void LoadPuzzle()
        {
            BeginPlay();
        }

        public bool BeginPlay()
        {
            return ResetPuzzle(true);
        }

        public bool ResetToInitialState(bool showPlayerAtStart)
        {
            StopAllCoroutines();
            cooperative?.EndAttempt();
            ResolveGameplayUiReferences();
            ResolveRuntimeReferences();

            if (board == null || playerStart == null || player == null || hand == null || placement == null)
            {
                Debug.LogError(
                    "[PuzzleManager] Cannot normalize without Board, Player Start, Player, Hand and Placement references.",
                    this);
                CurrentState = State.Idle;
                return false;
            }

            placement.Cancel();
            ClearRuntimePath();
            board.ResetSceneAuthoredState();
            hand.Setup(null);
            completionLocked = false;
            CurrentState = State.Idle;
            SetHudMessage(string.Empty);

            if (!showPlayerAtStart)
                return true;

            return PlacePlayerAtStart();
        }

        public bool PlacePlayerAtStart()
        {
            ResolveRuntimeReferences();
            if (board == null || board.GridSpace == null || playerStart == null || player == null)
            {
                Debug.LogError(
                    "[PuzzleManager] Cannot place Player without Board, Grid Space, Player Start and Player references.",
                    this);
                return false;
            }

            board.RegisterExistingTiles();
            Vector2Int startPosition = board.GridSpace.WorldToCell(playerStart.transform.position);
            if (!board.ContainsCell(startPosition))
            {
                Debug.LogError(
                    $"[PuzzleManager] PlayerStart at {startPosition} must overlap a scene-authored board tile.",
                    playerStart);
                return false;
            }

            if (!player.gameObject.activeSelf)
                player.gameObject.SetActive(true);
            player.SetPosition(startPosition, board.GridSpace.CellToWorldCenter(startPosition));
            activePlayer = player;
            if (cooperative != null && !cooperative.PlacePartnerAtStart()) return false;
            PuzzleStopped?.Invoke();
            return true;
        }

        public void Exit(bool hidePlayer)
        {
            StopPuzzle();
            ClearRuntimePath();
            if (hidePlayer && player != null)
                player.gameObject.SetActive(false);
        }

        public bool ResetPuzzle(bool resumePlaying)
        {
            StopAllCoroutines();
            cooperative?.EndAttempt();
            ResolveGameplayUiReferences();
            ResolveRuntimeReferences();

            if (board == null || playerStart == null || player == null || hand == null || placement == null)
            {
                Debug.LogError(
                    "[PuzzleManager] Assign Board, Player Start, Player, Path Piece Hand and Path Placement Controller.",
                    this);
                CurrentState = State.Idle;
                return false;
            }

            // A retry/fall is a fresh authored attempt. Traversal-only state such
            // as OneUse consumption must never leak into the next attempt.
            board.ResetSceneAuthoredState();
            if (board.GridSpace == null || board.GridPositions.Count == 0)
            {
                Debug.LogError(
                    "[PuzzleManager] No scene-authored board tiles were found. Bake the PuzzleData into the scene or prefab first.",
                    board);
                CurrentState = State.Idle;
                return false;
            }

            Vector2Int startPosition = board.GridSpace.WorldToCell(playerStart.transform.position);
            if (!board.ContainsCell(startPosition))
            {
                Debug.LogError(
                    $"[PuzzleManager] PlayerStart at {startPosition} must overlap a scene-authored board tile.",
                    playerStart);
                CurrentState = State.Idle;
                return false;
            }

            placement.Cancel();
            ClearRuntimePath();
            // The location owns one shared Player. A puzzle never instantiates or
            // swaps that actor; Play simply makes it visible at this board's own
            // scene-authored PlayerStart.
            if (resumePlaying && !player.gameObject.activeSelf)
                player.gameObject.SetActive(true);
            player.SetPosition(
                startPosition,
                board.GridSpace.CellToWorldCenter(startPosition));
            activePlayer = player;
            if (cooperative != null && !cooperative.PlacePartnerAtStart())
            {
                Debug.LogError("[PuzzleManager] Cooperative partner needs an authored start tile.", this);
                CurrentState = State.Idle;
                return false;
            }
            hand.Setup(puzzleData != null ? puzzleData.AvailablePathPieces : null);
            // Placement may have been disabled in an older scene revision.  It
            // must be active for its Update loop to keep the world preview under
            // the mouse after a hand piece is selected.
            placement.enabled = true;
            placement.Setup(this, board, player, hand);
            CurrentState = resumePlaying ? State.Playing : State.Idle;
            completionLocked = false;

            if (resumePlaying)
            {
                board.NotifyPlayerEntered(startPosition, player);
                cooperative?.BeginAttempt();
                SetHudMessage("Chọn một mảnh đường.");
                PuzzleStarted?.Invoke();
            }
            else
            {
                SetHudMessage(string.Empty);
            }

            return true;
        }

        public void StopPuzzle()
        {
            StopAllCoroutines();
            cooperative?.EndAttempt();
            if (placement != null)
                placement.Cancel();
            CurrentState = State.Idle;
            completionLocked = false;
            SetHudMessage(string.Empty);
            PuzzleStopped?.Invoke();
        }

        private void ResolveGameplayUiReferences()
        {
            GameplayUIRoot uiRoot = GameplayUIRoot.Instance;
            if (uiRoot == null)
                uiRoot = FindFirstObjectByType<GameplayUIRoot>(FindObjectsInactive.Include);

            if (uiRoot != null && uiRoot.PathPieceHand != null)
                hand = uiRoot.PathPieceHand;
        }

        private void ResolveRuntimeReferences()
        {
            if (runtime == null && board != null && board.GridSpace != null)
                runtime = board.GridSpace.GetComponentInChildren<PuzzleRuntime>(true);

            if (runtime == null)
                return;

            placement = runtime.Placement;
            placedPathRoot = runtime.PlacedPathRoot;
        }

        public void SubmitPlacement(PlacementResult result)
        {
            if (CurrentState != State.Playing || completionLocked) return;
            if (cooperative != null)
            {
                var gate = GameplayUIRoot.Instance != null ? GameplayUIRoot.Instance.InputGate : null;
                if (gate == null || !gate.Allows(Audere.GameplayInput.GameplayInputMode.Puzzle) ||
                    !result.CanCommit || hand.SelectedPiece == null || result.GridPath == null || result.GridPath.Count < 2) return;
                GridPlayer mover = cooperative.ActorAtStart(result.GridPath[0], true);
                if (mover == null) return;
                activePlayer = mover;
            }
            StartCoroutine(ExecutePlacement(result));
        }

        private IEnumerator ExecutePlacement(PlacementResult result)
        {
            CurrentState = State.Traversing;
            placement.HidePreview();
            SetHudMessage(string.Empty);
            GridPlayer moving = ActivePlayer;
            // Reserve this card before moving; changing a UI selection cannot consume another actor's next card.
            if (cooperative != null) hand.ConsumeSelected();
            yield return moving.Traverse(result.GridPath, board, HandleFallStarted);
            if (cooperative == null) hand.ConsumeSelected();

            if (moving.FellDuringTraversal)
            {
                yield return new WaitForSeconds(fallResetDelay);
                ResetPuzzle(true);
                yield break;
            }

            if (cooperative != null) yield return cooperative.ResolveLanding(moving);

            bool reachedGoal = cooperative != null
                ? cooperative.BothAtGoals
                : board.TryGetTile(player.GridPosition, out BoardTile destinationTile) && destinationTile.IsLevelGoal;
            bool mustUseRemainingPieces =
                puzzleData != null &&
                puzzleData.RequireAllPathPieces &&
                hand.HasPieces;

            if (reachedGoal && mustUseRemainingPieces)
            {
                completionLocked = true;
                // Reaching the objective early is a teachable rule violation for
                // "use every piece" puzzles, not a Story-level failure. Keep the
                // PuzzleController session alive, briefly show feedback, then
                // rebuild the same attempt from its scene-authored PlayerStart.
                CurrentState = State.Failed;
                SetHudMessage(string.Empty);
                GoalReachedWithPiecesRemaining?.Invoke();
                yield return new WaitForSecondsRealtime(failedAttemptResetDelay);
                ResetPuzzle(true);
            }
            else if (reachedGoal)
            {
                completionLocked = true;
                CurrentState = State.Completed;
                placement.Cancel();
                SetHudMessage(string.Empty);
                cooperative?.EndAttempt();
                PuzzleCompleted?.Invoke();
            }
            else if (!hand.HasPieces)
            {
                CurrentState = State.Failed;
                completionLocked = true;
                PuzzleAttemptFailed?.Invoke();
                if (retryWhenOutOfPieces)
                {
                    yield return new WaitForSeconds(failedAttemptResetDelay);
                    ResetPuzzle(true);
                }
                else
                {
                    SetHudMessage("Đã hết mảnh đường.");
                    PuzzleFailed?.Invoke();
                }
            }
            else
            {
                CurrentState = State.Playing;
                SetHudMessage("Chọn một mảnh đường.");
                PlacementResolved?.Invoke(result);
            }
        }

        private void HandleFallStarted()
        {
            AudioService.Instance?.Play(AudioId.Player_Fall);
            CurrentState = State.Falling;
            SetHudMessage("Cẩn thận…");
            PuzzleFallStarted?.Invoke();
        }

        private void SetHudMessage(string message)
        {
            if (hud != null)
                hud.SetMessage(message);
        }

        private void ClearRuntimePath()
        {
            if (placedPathRoot == null)
                ResolveRuntimeReferences();
            if (placedPathRoot == null)
                return;

            for (int index = placedPathRoot.childCount - 1; index >= 0; index--)
            {
                GameObject child = placedPathRoot.GetChild(index).gameObject;
                child.SetActive(false);
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }
        }

    }
}
