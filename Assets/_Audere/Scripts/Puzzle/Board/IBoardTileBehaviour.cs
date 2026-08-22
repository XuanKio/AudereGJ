namespace Audere.Puzzle.Board
{
    /// <summary>Receives the complete per-cell data before tile initialization.</summary>
    public interface IBoardTileDataReceiver
    {
        void ReceiveTileData(PuzzleTileData data);
    }

    /// <summary>
    /// Optional behaviour contract implemented by components on a tile prefab.
    /// A future tile can react to the player without adding tile-specific code to BoardManager.
    /// </summary>
    public interface IBoardTileBehaviour
    {
        void OnTileInitialized(BoardTile tile);
        void OnPlayerEntered(BoardTile tile, GridPlayer player);
        void OnPlayerExited(BoardTile tile, GridPlayer player);
    }

    /// <summary>
    /// Optional presentation reset used when a scene-authored puzzle is replayed.
    /// Runtime transitions snap back to authored state instead of trying to resume.
    /// </summary>
    public interface IBoardTileResettable
    {
        void ResetToAuthoredState();
    }

    /// <summary>Marker implemented by a prefab behaviour when that tile completes a level.</summary>
    public interface ILevelGoalTile { }
}
