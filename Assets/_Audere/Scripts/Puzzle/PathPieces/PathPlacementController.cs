using System;
using System.Collections.Generic;
using Audere.Dialogue;
using Audere.GameplayInput;
using Audere.Puzzle.Board;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Audere.Puzzle.PathPieces
{
    [DisallowMultipleComponent]
    public sealed class PathPlacementController : MonoBehaviour
    {
        [SerializeField] private Camera boardCamera;
        [SerializeField] private Canvas puzzleCanvas;
        [SerializeField] private GridSpace2D gridSpace;
        [SerializeField] private PathPreview preview;

        [Header("Cursor Anchoring")]
        [Tooltip("Prevents the preview from flickering between two equally close grid origins.")]
        [SerializeField, Range(0f, .5f)] private float originSwitchHysteresis = .12f;

        private PuzzleManager puzzle;
        private BoardManager board;
        private GridPlayer player;
        private PathPieceHand hand;
        private GridRotation rotation;
        private Vector2Int origin;
        private PlacementResult currentResult;
        private bool previewActive;
        private bool rotationInputEnabled = true;
        private float cursorToPreviewMidpointDistance;
        private bool hasAnchoredOrigin;
        private PathPieceData observedSelectedPiece;
        private GameplayInputGate inputGate;
        private readonly List<Vector2Int> previewGridPath = new List<Vector2Int>();
        private readonly List<Vector3> previewWorldPath = new List<Vector3>();

        public Vector2 PointerGridPosition { get; private set; }
        public Vector2Int PointerCell { get; private set; }
        public Vector2 PreviewMidpointGridPosition { get; private set; }
        public float CursorToPreviewMidpointDistance => cursorToPreviewMidpointDistance;
        public bool RotationInputEnabled => rotationInputEnabled;
        public event Action<GridRotation> RotationChanged;
        public event Action<PlacementResult> PreviewChanged;
        public event Action<PlacementResult> PlacementRejected;

        public void Setup(
            PuzzleManager owner,
            BoardManager boardManager,
            GridPlayer gridPlayer,
            PathPieceHand pieceHand)
        {
            puzzle = owner;
            board = boardManager;
            if (gridSpace == null && board != null) gridSpace = board.GridSpace;
            player = gridPlayer;
            if (hand != null)
                hand.SelectionChanged -= HandleSelectionChanged;
            hand = pieceHand;
            if (hand != null)
            {
                hand.SelectionChanged += HandleSelectionChanged;
                Canvas handCanvas = hand.GetComponentInParent<Canvas>();
                if (handCanvas != null)
                    puzzleCanvas = handCanvas;
            }
            observedSelectedPiece = hand != null ? hand.SelectedPiece : null;
            if (boardCamera == null) boardCamera = Camera.main;
            GameplayUIRoot uiRoot = GameplayUIRoot.Instance;
            inputGate = uiRoot != null ? uiRoot.InputGate : null;
            if (preview != null) preview.Setup();
            if (observedSelectedPiece != null)
                ShowInitialPreview();
        }

        private void Update()
        {
            if (!HasPuzzleInput() ||
                puzzle == null ||
                puzzle.CurrentState != PuzzleManager.State.Playing ||
                boardCamera == null ||
                gridSpace == null)
                return;

            PathPieceData selectedPiece = hand != null ? hand.SelectedPiece : null;
            if (selectedPiece != observedSelectedPiece)
                HandleSelectionChanged(selectedPiece);

            if (selectedPiece == null)
                return;

            if (!TryGetPointerInput(out Vector2 pointerScreenPosition, out bool pointerPressedThisFrame))
                return;

            // Only the hand owns pointer input.  A broad IsPointerOverGameObject()
            // check also catches decorative HUD graphics and can permanently block
            // preview movement/placement over the board.
            if (IsPointerOverHand(pointerScreenPosition))
                return;

            if (!TryMovePreviewToScreenPosition(pointerScreenPosition))
                return;

            if (rotationInputEnabled &&
                (Input.GetKeyDown(KeyCode.R) || Input.GetMouseButtonDown(1)))
            {
                rotation = (GridRotation)(((int)rotation + 1) % 4);
                TryMovePreviewToScreenPosition(pointerScreenPosition);
                RotationChanged?.Invoke(rotation);
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cancel();
                return;
            }

            if (pointerPressedThisFrame)
                TryCommitPreview();
        }

        public void Cancel()
        {
            rotation = GridRotation.Degrees0;
            observedSelectedPiece = null;
            HidePreview();
            if (hand != null) hand.ClearSelection();
        }

        /// <summary>
        /// Lets a scene-authored tutorial introduce rotation after the player has
        /// learned the basic select-and-place action. Regular puzzles keep it on.
        /// </summary>
        public void SetRotationInputEnabled(bool value)
        {
            rotationInputEnabled = value;
            if (rotationInputEnabled)
                return;

            rotation = GridRotation.Degrees0;
        }

        private void HandleSelectionChanged(PathPieceData selectedPiece)
        {
            observedSelectedPiece = selectedPiece;
            if (selectedPiece != null)
                ShowInitialPreview();
            else
                HidePreview();
        }

        private void ShowInitialPreview()
        {
            if (hand == null ||
                hand.SelectedPiece == null ||
                player == null ||
                board == null ||
                gridSpace == null)
                return;

            rotation = GridRotation.Degrees0;
            Vector2Int rotatedEndpointA = GridRotationUtility.Rotate(
                hand.SelectedPiece.EndpointA,
                rotation);
            origin = player.GridPosition - rotatedEndpointA;
            hasAnchoredOrigin = true;
            previewActive = true;
            currentResult = PathPlacementValidator.Validate(
                hand.SelectedPiece,
                origin,
                rotation,
                player.GridPosition,
                board);

            if (!BuildWorldPreviewPath(
                    hand.SelectedPiece,
                    origin,
                    rotation,
                    out float cellWorldSize))
            {
                previewActive = false;
                if (preview != null) preview.Clear();
                return;
            }

            int lastIndex = previewGridPath.Count - 1;
            PreviewMidpointGridPosition =
                ((Vector2)previewGridPath[0] + previewGridPath[lastIndex]) * .5f;
            PointerGridPosition = PreviewMidpointGridPosition;
            PointerCell = Vector2Int.RoundToInt(PointerGridPosition);
            cursorToPreviewMidpointDistance = 0f;
            if (preview != null)
            {
                preview.Show(previewWorldPath, cellWorldSize);
                preview.SetState(GetPresentationState(currentResult));
            }
        }

        public void HidePreview()
        {
            previewActive = false;
            hasAnchoredOrigin = false;
            cursorToPreviewMidpointDistance = 0f;
            currentResult = default;
            if (preview != null) preview.Clear();
        }

        /// <summary>
        /// Moves the selected piece to the grid position under a gameplay pointer.
        /// Both the desktop mouse loop and touch input use this same path.
        /// </summary>
        public bool TryMovePreviewToScreenPosition(Vector2 screenPosition)
        {
            if (!HasPuzzleInput() ||
                puzzle == null ||
                puzzle.CurrentState != PuzzleManager.State.Playing ||
                hand == null ||
                hand.SelectedPiece == null ||
                boardCamera == null ||
                gridSpace == null ||
                !gridSpace.TryScreenToCell(
                    boardCamera,
                    screenPosition,
                    out Vector2Int pointerCell,
                    out Vector3 world))
                return false;

            RefreshPreview(world);
            PointerCell = pointerCell;
            return previewActive;
        }

        /// <summary>
        /// Drops the current preview.  Only a path that starts at Player can be
        /// committed, matching the Select/Drop flow of Steptile.
        /// </summary>
        public bool TryCommitPreview()
        {
            if (!HasPuzzleInput() ||
                !previewActive ||
                puzzle == null)
                return false;

            if (!currentResult.CanCommit)
            {
                PlacementRejected?.Invoke(currentResult);
                return false;
            }

            puzzle.SubmitPlacement(currentResult);
            return true;
        }

        private void RefreshPreview(Vector3 mouseWorld)
        {
            PointerCell = gridSpace.WorldToCell(mouseWorld);
            PointerGridPosition = PointerCell;

            if (!PathPreviewAnchorSolver.TrySolve(
                    hand.SelectedPiece,
                    rotation,
                    PointerGridPosition,
                    hasAnchoredOrigin,
                    origin,
                    originSwitchHysteresis,
                    out PathPreviewAnchorSolver.Result anchor))
            {
                previewActive = false;
                hasAnchoredOrigin = false;
                currentResult = PlacementResult.Invalid("The selected path cannot be previewed.");
                if (preview != null) preview.Clear();
                return;
            }

            origin = anchor.Origin;
            hasAnchoredOrigin = true;
            PreviewMidpointGridPosition = anchor.EndpointMidpoint;
            cursorToPreviewMidpointDistance = anchor.PointerDistance;
            previewActive = true;
            currentResult = PathPlacementValidator.Validate(
                hand.SelectedPiece,
                origin,
                rotation,
                player.GridPosition,
                board);

            if (!BuildWorldPreviewPath(hand.SelectedPiece, origin, rotation, out float cellWorldSize))
            {
                previewActive = false;
                if (preview != null) preview.Clear();
                return;
            }

            preview.Show(previewWorldPath, cellWorldSize);
            preview.SetState(GetPresentationState(currentResult));
            PreviewChanged?.Invoke(currentResult);
        }

        private bool BuildWorldPreviewPath(
            PathPieceData piece,
            Vector2Int pathOrigin,
            GridRotation pathRotation,
            out float cellWorldSize)
        {
            cellWorldSize = 0f;
            previewGridPath.Clear();
            previewWorldPath.Clear();

            foreach (Vector2Int localPosition in piece.OrderedLocalPath)
            {
                Vector2Int cell = pathOrigin + GridRotationUtility.Rotate(localPosition, pathRotation);
                previewGridPath.Add(cell);
                previewWorldPath.Add(gridSpace.CellToWorldCenter(cell));
            }

            cellWorldSize = Vector3.Distance(
                gridSpace.CellToWorldCenter(Vector2Int.zero),
                gridSpace.CellToWorldCenter(Vector2Int.right));
            return cellWorldSize > Mathf.Epsilon;
        }

        private static PathPreview.PresentationState GetPresentationState(PlacementResult result)
        {
            if (!result.CanCommit)
                return PathPreview.PresentationState.Invalid;
            return result.WillFall
                ? PathPreview.PresentationState.Dangerous
                : PathPreview.PresentationState.Valid;
        }

        private bool TryGetPointerInput(
            out Vector2 screenPosition,
            out bool pressedThisFrame)
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                screenPosition = touch.position;
                pressedThisFrame = touch.phase == TouchPhase.Began;
                return touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled;
            }

            screenPosition = Input.mousePosition;
            pressedThisFrame = Input.GetMouseButtonDown(0);
            return true;
        }

        private bool IsPointerOverHand(Vector2 screenPosition)
        {
            if (EventSystem.current == null || hand == null)
                return false;

            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                position = screenPosition
            };
            List<RaycastResult> hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, hits);
            foreach (RaycastResult hit in hits)
            {
                if (hit.gameObject != null &&
                    hit.gameObject.transform.IsChildOf(hand.transform))
                    return true;
            }

            return false;
        }

        private bool HasPuzzleInput()
        {
            if (inputGate == null)
            {
                GameplayUIRoot uiRoot = GameplayUIRoot.Instance;
                inputGate = uiRoot != null ? uiRoot.InputGate : null;
            }

            return inputGate != null && inputGate.Allows(GameplayInputMode.Puzzle);
        }

        private void OnDestroy()
        {
            if (hand != null)
                hand.SelectionChanged -= HandleSelectionChanged;
        }
    }
}
