using UnityEngine;

namespace Audere.Puzzle
{
    /// <summary>
    /// Infinite mathematical grid. It only converts coordinates and never knows
    /// which cells are present, walkable, blocked or owned by a board.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GridSpace2D : MonoBehaviour
    {
        [SerializeField, Min(.01f)] private float cellSize = 1f;
        [SerializeField] private Vector2 localOrigin;

        public float CellSize => cellSize;

        public Vector2 WorldToGrid(Vector3 worldPosition)
        {
            Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
            return ((Vector2)localPosition - localOrigin) / cellSize;
        }

        public Vector2Int WorldToCell(Vector3 worldPosition)
        {
            Vector2 gridPosition = WorldToGrid(worldPosition);
            return new Vector2Int(
                Mathf.RoundToInt(gridPosition.x),
                Mathf.RoundToInt(gridPosition.y));
        }

        public Vector3 CellToWorldCenter(Vector2Int cell)
        {
            Vector2 localPosition = localOrigin + (Vector2)cell * cellSize;
            return transform.TransformPoint(new Vector3(localPosition.x, localPosition.y, 0f));
        }

        public bool TryScreenToWorld(
            Camera worldCamera,
            Vector2 screenPosition,
            out Vector3 worldPosition)
        {
            worldPosition = default;
            if (worldCamera == null)
                return false;

            Plane gridPlane = new Plane(transform.forward, CellToWorldCenter(Vector2Int.zero));
            Ray pointerRay = worldCamera.ScreenPointToRay(screenPosition);
            if (!gridPlane.Raycast(pointerRay, out float distance))
                return false;

            worldPosition = pointerRay.GetPoint(distance);
            return true;
        }

        public bool TryScreenToCell(
            Camera worldCamera,
            Vector2 screenPosition,
            out Vector2Int cell,
            out Vector3 worldPosition)
        {
            cell = default;
            if (!TryScreenToWorld(worldCamera, screenPosition, out worldPosition))
                return false;

            cell = WorldToCell(worldPosition);
            return true;
        }

    }
}
