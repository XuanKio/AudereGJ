using System.Collections.Generic;
using UnityEngine;

namespace Audere.Puzzle.PathPieces
{
    public enum GridRotation
    {
        Degrees0 = 0,
        Degrees90 = 1,
        Degrees180 = 2,
        Degrees270 = 3
    }

    /// <summary>
    /// Rotates integer grid coordinates around a piece's local origin.
    /// It contains no MonoBehaviour state, so placement and preview can share it.
    /// </summary>
    public static class GridRotationUtility
    {
        public static Vector2Int Rotate(Vector2Int coordinate, GridRotation rotation)
        {
            return rotation switch
            {
                GridRotation.Degrees0 => coordinate,
                GridRotation.Degrees90 => new Vector2Int(-coordinate.y, coordinate.x),
                GridRotation.Degrees180 => new Vector2Int(-coordinate.x, -coordinate.y),
                GridRotation.Degrees270 => new Vector2Int(coordinate.y, -coordinate.x),
                _ => coordinate
            };
        }

        public static List<Vector2Int> RotatePath(
            IReadOnlyList<Vector2Int> orderedPath,
            GridRotation rotation)
        {
            List<Vector2Int> rotatedPath = new List<Vector2Int>(orderedPath.Count);

            foreach (Vector2Int coordinate in orderedPath)
                rotatedPath.Add(Rotate(coordinate, rotation));

            return rotatedPath;
        }
    }
}