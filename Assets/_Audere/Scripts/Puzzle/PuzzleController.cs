using System;
using Audere.Dialogue;
using Audere.GameplayInput;
using Audere.Puzzle.Board;
using UnityEngine;

namespace Audere.Puzzle
{
    public enum PuzzleResult
    {
        Completed,
        Cancelled,
        Failed
    }

    public enum PuzzleFlowState
    {
        Preparing,
        Revealing,
        Playing,
        Completing,
        Collapsing,
        Completed
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(PuzzleManager))]
    public sealed class PuzzleController : MonoBehaviour
    {
        [SerializeField] private PuzzleManager puzzle;
        [Tooltip("Scene-authored root that owns this puzzle's board content and systems.")]
        [SerializeField] private Transform puzzleRoot;
        [SerializeField] private GridCameraFollow2D cameraFollow;
        [SerializeField] private bool playOnStart;

        private Action<PuzzleResult> activeCompletion;
        private bool isPlaying;
        private int playRequestVersion;
        private GameplayInputGate inputGate;
        private GameplayInputToken inputToken;
        private Vector3 authoredRootLocalPosition;
        private Quaternion authoredRootLocalRotation;
        private Vector3 authoredRootLocalScale;
        private bool authoredRootCaptured;
        private bool preservePreparedRootPoseForNextPlay;

        public bool IsPlaying => isPlaying;
        public bool PlayOnStart => playOnStart;
        public PuzzleManager Puzzle => puzzle;
        public Transform PuzzleRoot => puzzleRoot;
        public PuzzleFlowState CurrentFlowState { get; private set; } = PuzzleFlowState.Preparing;

        private void Awake()
        {
            if (puzzle == null)
                puzzle = GetComponent<PuzzleManager>();
            if (puzzleRoot == null && transform.parent != null)
                puzzleRoot = transform.parent.parent;
            CaptureAuthoredRootPose();

            if (puzzle != null)
            {
                puzzle.PuzzleCompleted += HandleCompleted;
                puzzle.PuzzleFailed += HandleFailed;
            }
        }

        private void Start()
        {
            if (playOnStart)
                Play();
        }

        private void Update()
        {
            if (!HasPuzzleInput())
                return;

            bool resetRequested = Input.GetKeyDown(KeyCode.Backspace) ||
                (!IsPlaying &&
                 puzzle != null &&
                 (puzzle.CurrentState == PuzzleManager.State.Completed ||
                  puzzle.CurrentState == PuzzleManager.State.Failed) &&
                 Input.GetKeyDown(KeyCode.R));

            if (!resetRequested)
                return;

            if (IsPlaying)
                ResetPuzzle();
            else
                Play();
        }

        private void OnDisable()
        {
            Cancel();
        }

        private void OnDestroy()
        {
            if (puzzle != null)
            {
                puzzle.PuzzleCompleted -= HandleCompleted;
                puzzle.PuzzleFailed -= HandleFailed;
            }
        }

        public bool Play(Action<PuzzleResult> onEnded = null)
        {
            if (!isPlaying && puzzleRoot != null && !preservePreparedRootPoseForNextPlay)
            {
                RestoreAuthoredRootPose();
                puzzle?.Board?.RegisterExistingTiles();
            }
            preservePreparedRootPoseForNextPlay = false;
            return PlayInternal(onEnded);
        }

        public bool PlayFromAnchor(Transform anchor, Action<PuzzleResult> onEnded = null)
        {
            if (anchor == null)
            {
                Debug.LogError("[PuzzleController] PlayFromAnchor requires an anchor Transform.", this);
                onEnded?.Invoke(PuzzleResult.Failed);
                return false;
            }

            if (isPlaying)
            {
                Debug.LogWarning("[PuzzleController] Cannot realign a puzzle that is already playing.", this);
                return false;
            }

            if (!AlignPlayerStartToAnchor(anchor))
            {
                onEnded?.Invoke(PuzzleResult.Failed);
                return false;
            }

            preservePreparedRootPoseForNextPlay = false;
            return PlayInternal(onEnded);
        }

        private bool PlayInternal(Action<PuzzleResult> onEnded)
        {
            if (!isActiveAndEnabled)
            {
                Debug.LogError("[PuzzleController] Enable the controller before calling Play.", this);
                onEnded?.Invoke(PuzzleResult.Failed);
                return false;
            }

            if (puzzle == null)
            {
                Debug.LogError("[PuzzleController] Assign a PuzzleManager.", this);
                onEnded?.Invoke(PuzzleResult.Failed);
                return false;
            }

            int requestVersion = ++playRequestVersion;
            Cancel();

            // A cancellation callback may start a newer puzzle. Do not replace it.
            if (requestVersion != playRequestVersion)
                return false;

            GameplayInputGate gate = ResolveInputGate();
            if (gate == null)
            {
                Debug.LogError("[PuzzleController] GameplayInputGate is not available.", this);
                onEnded?.Invoke(PuzzleResult.Failed);
                return false;
            }

            GameplayInputToken token = gate.PushMode(this, GameplayInputMode.Puzzle);
            if (!token.IsValid)
            {
                onEnded?.Invoke(PuzzleResult.Failed);
                return false;
            }

            inputGate = gate;
            inputToken = token;

            activeCompletion = onEnded;
            isPlaying = true;
            if (puzzle.BeginPlay())
            {
                CurrentFlowState = PuzzleFlowState.Playing;
                ConfigureCameraForThisPuzzle();
                return true;
            }

            EndPlayback(PuzzleResult.Failed);
            return false;
        }

        public void Cancel()
        {
            if (!isPlaying)
                return;

            puzzle.StopPuzzle();
            CurrentFlowState = PuzzleFlowState.Preparing;
            EndPlayback(PuzzleResult.Cancelled);
        }

        public bool ResetPuzzle()
        {
            if (puzzle == null)
                return false;

            bool reset = puzzle.ResetPuzzle(isPlaying);
            if (reset)
                CurrentFlowState = isPlaying
                    ? PuzzleFlowState.Playing
                    : PuzzleFlowState.Preparing;
            return reset;
        }

        public bool ResetToInitialState(bool rootActive, bool showPlayerAtStart)
        {
            if (isPlaying)
                Cancel();

            if (puzzle == null || puzzleRoot == null)
            {
                Debug.LogError("[PuzzleController] Assign PuzzleManager and Puzzle Root before normalizing.", this);
                return false;
            }

            if (!puzzleRoot.gameObject.activeSelf)
                puzzleRoot.gameObject.SetActive(true);

            RestoreAuthoredRootPose();
            preservePreparedRootPoseForNextPlay = false;
            bool reset = puzzle.ResetToInitialState(showPlayerAtStart);
            CurrentFlowState = PuzzleFlowState.Preparing;
            if (!rootActive)
                puzzleRoot.gameObject.SetActive(false);
            return reset;
        }

        public void SetBoardTilesVisible(bool visible)
        {
            if (puzzle != null && puzzle.Board != null)
                puzzle.Board.SetSceneTilesVisible(visible);
        }

        public bool AlignPlayerStartToAnchor(Transform anchor)
        {
            return anchor != null && AlignPlayerStartToWorldPosition(anchor.position);
        }

        public bool AlignPlayerStartToWorldPosition(Vector3 anchorWorldPosition)
        {
            if (puzzle == null || puzzleRoot == null || puzzle.PlayerStartTransform == null)
                return false;

            if (!puzzleRoot.gameObject.activeSelf)
                puzzleRoot.gameObject.SetActive(true);

            Vector3 delta = anchorWorldPosition - puzzle.PlayerStartTransform.position;
            puzzleRoot.position += delta;
            puzzle.Board?.RegisterExistingTiles();
            preservePreparedRootPoseForNextPlay = true;
            return true;
        }

        public bool BeginReveal()
        {
            if (isPlaying)
                return false;

            CurrentFlowState = PuzzleFlowState.Revealing;
            ConfigureCameraForThisPuzzle();
            return true;
        }

        public bool BeginCollapse()
        {
            if (isPlaying)
                return false;

            CurrentFlowState = PuzzleFlowState.Collapsing;
            return true;
        }

        public void CompleteCollapse()
        {
            if (CurrentFlowState == PuzzleFlowState.Collapsing)
                CurrentFlowState = PuzzleFlowState.Completed;
        }

        public bool TryGetGoalAnchor(out Transform anchor, bool convertPresentation)
        {
            anchor = null;
            if (puzzle == null || puzzle.Board == null)
                return false;

            puzzle.Board.RegisterExistingTiles();
            if (!puzzle.Board.TryGetLevelGoal(out BoardTile goal))
                return false;

            if (convertPresentation &&
                goal.TryGetBehaviour<GoalTileBehaviour>(out GoalTileBehaviour behaviour))
            {
                behaviour.BecomeTransitionAnchor();
            }

            anchor = goal.transform;
            return true;
        }

        public void Exit(bool hidePlayer)
        {
            if (isPlaying)
                Cancel();
            puzzle?.Exit(hidePlayer);
        }

        private void HandleCompleted()
        {
            CurrentFlowState = PuzzleFlowState.Completing;
            EndPlayback(PuzzleResult.Completed);
        }

        private void HandleFailed()
        {
            CurrentFlowState = PuzzleFlowState.Completed;
            EndPlayback(PuzzleResult.Failed);
        }

        private void ConfigureCameraForThisPuzzle()
        {
            if (cameraFollow != null && puzzle != null)
                cameraFollow.Configure(puzzle.Player, puzzle.Board);
        }

        private void EndPlayback(PuzzleResult result)
        {
            if (!isPlaying)
                return;

            isPlaying = false;
            Action<PuzzleResult> completion = activeCompletion;
            activeCompletion = null;
            ReleaseInputClaim();
            completion?.Invoke(result);
        }

        private GameplayInputGate ResolveInputGate()
        {
            GameplayUIRoot root = GameplayUIRoot.Instance;
            return root != null ? root.InputGate : null;
        }

        private void ReleaseInputClaim()
        {
            GameplayInputGate gate = inputGate;
            GameplayInputToken token = inputToken;
            inputGate = null;
            inputToken = default;

            if (gate != null && token.IsValid)
                gate.Release(token);
        }

        private void CaptureAuthoredRootPose()
        {
            if (authoredRootCaptured || puzzleRoot == null)
                return;

            authoredRootCaptured = true;
            authoredRootLocalPosition = puzzleRoot.localPosition;
            authoredRootLocalRotation = puzzleRoot.localRotation;
            authoredRootLocalScale = puzzleRoot.localScale;
        }

        private void RestoreAuthoredRootPose()
        {
            CaptureAuthoredRootPose();
            if (puzzleRoot == null)
                return;
            puzzleRoot.localPosition = authoredRootLocalPosition;
            puzzleRoot.localRotation = authoredRootLocalRotation;
            puzzleRoot.localScale = authoredRootLocalScale;
        }

        private bool HasPuzzleInput()
        {
            return inputGate != null &&
                   inputGate.IsActive(inputToken) &&
                   inputGate.Allows(GameplayInputMode.Puzzle);
        }
    }
}
