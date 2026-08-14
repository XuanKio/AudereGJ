using UnityEngine;

namespace Audere.Puzzle.PathPieces
{
    /// <summary>
    /// Snaps a path's endpoint midpoint to the nearest legal integer origin on an
    /// infinite grid. Board coverage is deliberately not part of this calculation.
    /// </summary>
    public static class PathPreviewAnchorSolver
    {
        public readonly struct Result
        {
            public readonly Vector2Int Origin;
            public readonly Vector2 EndpointMidpoint;
            public readonly float PointerDistance;

            public Result(Vector2Int origin, Vector2 endpointMidpoint, float pointerDistance)
            {
                Origin = origin;
                EndpointMidpoint = endpointMidpoint;
                PointerDistance = pointerDistance;
            }
        }

        public static bool TrySolve(
            PathPieceData piece,
            GridRotation rotation,
            Vector2 pointerGridPosition,
            bool hasCurrentOrigin,
            Vector2Int currentOrigin,
            float switchHysteresis,
            out Result result)
        {
            result = default;
            if (piece == null || piece.OrderedLocalPath == null || piece.OrderedLocalPath.Count < 2)
                return false;

            int lastIndex = piece.OrderedLocalPath.Count - 1;
            Vector2 endpointA = GridRotationUtility.Rotate(piece.OrderedLocalPath[0], rotation);
            Vector2 endpointB = GridRotationUtility.Rotate(piece.OrderedLocalPath[lastIndex], rotation);
            Vector2 localMidpoint = (endpointA + endpointB) * .5f;
            Vector2 desiredOrigin = pointerGridPosition - localMidpoint;

            int floorX = Mathf.FloorToInt(desiredOrigin.x);
            int ceilX = Mathf.CeilToInt(desiredOrigin.x);
            int floorY = Mathf.FloorToInt(desiredOrigin.y);
            int ceilY = Mathf.CeilToInt(desiredOrigin.y);

            Vector2Int bestOrigin = new Vector2Int(floorX, floorY);
            float bestDistance = float.PositiveInfinity;

            Evaluate(new Vector2Int(floorX, floorY));
            Evaluate(new Vector2Int(floorX, ceilY));
            Evaluate(new Vector2Int(ceilX, floorY));
            Evaluate(new Vector2Int(ceilX, ceilY));

            if (hasCurrentOrigin)
            {
                float currentDistance = Vector2.Distance(
                    pointerGridPosition,
                    currentOrigin + localMidpoint);
                if (currentDistance <= bestDistance + Mathf.Max(0f, switchHysteresis))
                {
                    bestOrigin = currentOrigin;
                    bestDistance = currentDistance;
                }
            }

            result = new Result(
                bestOrigin,
                bestOrigin + localMidpoint,
                bestDistance);
            return true;

            void Evaluate(Vector2Int candidate)
            {
                float distance = Vector2.Distance(
                    pointerGridPosition,
                    candidate + localMidpoint);
                if (distance < bestDistance - .0001f ||
                    (Mathf.Abs(distance - bestDistance) <= .0001f &&
                     IsLexicographicallyEarlier(candidate, bestOrigin)))
                {
                    bestOrigin = candidate;
                    bestDistance = distance;
                }
            }
        }

        private static bool IsLexicographicallyEarlier(Vector2Int candidate, Vector2Int current)
        {
            return candidate.y < current.y ||
                (candidate.y == current.y && candidate.x < current.x);
        }
    }
}
