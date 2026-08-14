using System.Collections.Generic;
using UnityEngine;

namespace Audere.Puzzle.PathPieces
{
    public readonly struct PlacementResult
    {
        public readonly bool CanCommit;
        public readonly string FailureReason;
        public readonly Vector2Int StartEndpoint;
        public readonly Vector2Int EndEndpoint;
        public readonly List<Vector2Int> GridPath;
        public readonly bool WillFall;
        public readonly int FirstMissingTileIndex;
        public bool IsValid => CanCommit;
        public bool IsSafe => CanCommit && !WillFall;

        private PlacementResult(
            bool isValid,
            string failureReason,
            Vector2Int startEndpoint,
            Vector2Int endEndpoint,
            List<Vector2Int> gridPath,
            bool willFall,
            int firstMissingTileIndex)
        {
            CanCommit = isValid;
            FailureReason = failureReason;
            StartEndpoint = startEndpoint;
            EndEndpoint = endEndpoint;
            GridPath = gridPath;
            WillFall = willFall;
            FirstMissingTileIndex = firstMissingTileIndex;
        }

        public static PlacementResult Invalid(string reason)
        {
            return new PlacementResult(false, reason, default, default, null, false, -1);
        }

        public static PlacementResult Valid(
            Vector2Int start,
            Vector2Int end,
            List<Vector2Int> gridPath,
            int firstMissingTileIndex)
        {
            return new PlacementResult(
                true,
                string.Empty,
                start,
                end,
                gridPath,
                firstMissingTileIndex >= 0,
                firstMissingTileIndex);
        }
    }
}
