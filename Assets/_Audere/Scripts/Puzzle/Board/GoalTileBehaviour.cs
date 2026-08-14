using UnityEngine;

namespace Audere.Puzzle.Board
{
    /// <summary>
    /// Prefab-owned goal capability. Board and puzzle code query the capability,
    /// so replacing the Goal prefab visual never requires gameplay code changes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GoalTileBehaviour : MonoBehaviour, IBoardTileBehaviour, ILevelGoalTile
    {
        public void OnTileInitialized(BoardTile tile) { }
        public void OnPlayerEntered(BoardTile tile, GridPlayer player) { }
        public void OnPlayerExited(BoardTile tile, GridPlayer player) { }
    }
}
