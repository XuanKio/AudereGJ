using System.Collections.Generic;
using System.Linq;
using Audere.Puzzle.Board;
using Audere.Puzzle.PathPieces;
using UnityEngine;

namespace Audere.Puzzle
{
    [CreateAssetMenu(fileName = "Puzzle_", menuName = "Audere/Puzzle/Puzzle Data")]
    public sealed class PuzzleData : ScriptableObject
    {
        [SerializeField] private string puzzleId = "new-puzzle";
        [SerializeField] private List<PuzzleTileData> boardTiles = new List<PuzzleTileData>();
        [SerializeField, HideInInspector] private List<Vector2Int> boardCells = new List<Vector2Int>();
        [SerializeField] private Vector2Int playerStartPosition;
        [SerializeField, HideInInspector] private Vector2Int goalPosition;
        [SerializeField] private List<PathPieceData> availablePathPieces = new List<PathPieceData>();

        public string PuzzleId => puzzleId;
        public IReadOnlyList<PuzzleTileData> BoardTiles
        {
            get
            {
                if (boardTiles != null && boardTiles.Count > 0)
                    return boardTiles;

                return boardCells
                    .Select(position => new PuzzleTileData(position, PuzzleTileType.Grass))
                    .ToList();
            }
        }

        public IReadOnlyList<Vector2Int> BoardCells => BoardTiles.Select(tile => tile.Position).ToList();
        public Vector2Int PlayerStartPosition => playerStartPosition;
        public Vector2Int GoalPosition
        {
            get
            {
                foreach (PuzzleTileData tile in BoardTiles)
                    if (tile.TileType == PuzzleTileType.Goal)
                        return tile.Position;

                return goalPosition;
            }
        }
        public IReadOnlyList<PathPieceData> AvailablePathPieces => availablePathPieces;
    }
}
