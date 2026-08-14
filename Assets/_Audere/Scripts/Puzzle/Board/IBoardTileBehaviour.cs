namespace Audere.Puzzle.Board
{
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

    /// <summary>Marker implemented by a prefab behaviour when that tile completes a level.</summary>
    public interface ILevelGoalTile { }
}
