using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Audere.Puzzle.PathPieces
{
    /// <summary>
    /// Authoring data for one path shape. The first and last coordinates are the
    /// two symmetric endpoints; every coordinate remains in traversal order.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PathPiece_",
        menuName = "Audere/Puzzle/Path Piece",
        order = 0)]
    public sealed class PathPieceData : ScriptableObject
    {
        [SerializeField] private PathPieceType pieceType = PathPieceType.Line2;
        [FormerlySerializedAs("pieceId")]
        [SerializeField, HideInInspector] private string legacyPieceId = PuzzleContentConstants.Pieces.Line2;
        [SerializeField] private List<Vector2Int> orderedLocalPath = new List<Vector2Int>
        {
            Vector2Int.zero,
            Vector2Int.right
        };

        public PathPieceType PieceType => pieceType;
        public string PieceId => PuzzleContentConstants.GetPieceId(pieceType);
        public IReadOnlyList<Vector2Int> OrderedLocalPath => orderedLocalPath;
        public Vector2Int EndpointA => orderedLocalPath[0];
        public Vector2Int EndpointB => orderedLocalPath[orderedLocalPath.Count - 1];

        public Vector2Int GetRotatedCoordinate(int pathIndex, GridRotation rotation)
        {
            return GridRotationUtility.Rotate(orderedLocalPath[pathIndex], rotation);
        }

        public bool IsValid(out string reason)
        {
            if (orderedLocalPath == null || orderedLocalPath.Count < 2)
            {
                reason = "A path piece needs two endpoints.";
                return false;
            }

            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

            for (int index = 0; index < orderedLocalPath.Count; index++)
            {
                Vector2Int coordinate = orderedLocalPath[index];

                if (!visited.Add(coordinate))
                {
                    reason = $"Coordinate {coordinate} is duplicated.";
                    return false;
                }

                if (index == 0)
                    continue;

                Vector2Int previous = orderedLocalPath[index - 1];
                int manhattanDistance =
                    Mathf.Abs(coordinate.x - previous.x) +
                    Mathf.Abs(coordinate.y - previous.y);

                if (manhattanDistance != 1)
                {
                    reason =
                        $"Coordinates {previous} and {coordinate} are not adjacent.";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        private void OnValidate()
        {
            legacyPieceId = PieceId;
        }
    }
}
