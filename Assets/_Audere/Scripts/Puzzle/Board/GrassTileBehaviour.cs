using UnityEngine;

namespace Audere.Puzzle.Board
{
    /// <summary>
    /// Behaviour hook for the standard walkable Grass tile.
    /// It is intentionally neutral; future tile prefabs can implement their own effects.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GrassTileBehaviour : MonoBehaviour, IBoardTileBehaviour
    {
        public void OnTileInitialized(BoardTile tile) { }
        public void OnPlayerEntered(BoardTile tile, GridPlayer player) { }
        public void OnPlayerExited(BoardTile tile, GridPlayer player) { }
    }
}
