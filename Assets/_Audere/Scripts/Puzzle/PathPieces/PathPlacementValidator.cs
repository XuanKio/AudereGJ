using System.Collections.Generic;
using Audere.Puzzle.Board;
using UnityEngine;

namespace Audere.Puzzle.PathPieces
{
    public static class PathPlacementValidator
    {
        public static PlacementResult Validate(
            PathPieceData piece,
            Vector2Int origin,
            GridRotation rotation,
            Vector2Int playerPosition,
            BoardManager board,
            GridPlayer mover = null)
        {
            if (piece == null)
                return PlacementResult.Invalid("No path piece selected.");

            if (!piece.IsValid(out string reason))
                return PlacementResult.Invalid(reason);

            List<Vector2Int> absolutePath = new List<Vector2Int>(piece.OrderedLocalPath.Count);

            foreach (Vector2Int localPosition in piece.OrderedLocalPath)
            {
                Vector2Int boardPosition =
                    origin + GridRotationUtility.Rotate(localPosition, rotation);
                absolutePath.Add(boardPosition);
            }

            bool connectsAtA = absolutePath[0] == playerPosition;
            bool connectsAtB = absolutePath[absolutePath.Count - 1] == playerPosition;

            if (!connectsAtA && !connectsAtB)
                return PlacementResult.Invalid("One endpoint must connect to Player.");

            if (!connectsAtA)
                absolutePath.Reverse();

            int firstMissingTileIndex = -1;
            for (int index = 1; index < absolutePath.Count; index++)
            {
                if (board.CanPlayerEnter(absolutePath[index], mover))
                    continue;

                firstMissingTileIndex = index;
                break;
            }

            return PlacementResult.Valid(
                absolutePath[0],
                absolutePath[absolutePath.Count - 1],
                absolutePath,
                firstMissingTileIndex);
        }
    }
}
