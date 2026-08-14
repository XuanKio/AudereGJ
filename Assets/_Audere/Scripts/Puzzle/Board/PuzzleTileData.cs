using System;
using UnityEngine;

namespace Audere.Puzzle.Board
{
    [Serializable]
    public struct PuzzleTileData
    {
        [SerializeField] private Vector2Int position;
        [SerializeField] private PuzzleTileType tileType;

        public Vector2Int Position => position;
        public PuzzleTileType TileType => tileType;
        public string TileId => PuzzleContentConstants.GetTileId(tileType);

        public PuzzleTileData(Vector2Int position, PuzzleTileType tileType)
        {
            this.position = position;
            this.tileType = tileType;
        }
    }
}
