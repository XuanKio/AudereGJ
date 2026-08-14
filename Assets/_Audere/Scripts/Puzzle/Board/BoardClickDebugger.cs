using UnityEngine;

namespace Audere.Puzzle.Board
{
    /// <summary>
    /// Temporary Phase 1 verification input. Replace this component in a later phase
    /// with the game-wide input adapter; BoardManager itself has no Input dependency.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoardManager))]
    public sealed class BoardClickDebugger : MonoBehaviour
    {
        [SerializeField] private Camera boardCamera;
        private BoardManager board;

        private void Awake()
        {
            board = GetComponent<BoardManager>();

            if (boardCamera == null)
                boardCamera = Camera.main;
        }

        private void Update()
        {
            if (board == null || !Input.GetMouseButtonDown(0) || boardCamera == null)
                return;

            if (board.GridSpace != null &&
                board.GridSpace.TryScreenToWorld(boardCamera, Input.mousePosition, out Vector3 worldPosition) &&
                board.ContainsCell(board.GridSpace.WorldToCell(worldPosition)))
            {
                Vector2Int gridPosition = board.GridSpace.WorldToCell(worldPosition);
                Debug.Log($"[Board] Clicked tile {gridPosition}.");
            }
        }
    }
}
