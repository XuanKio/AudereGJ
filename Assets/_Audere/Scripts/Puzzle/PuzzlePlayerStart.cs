using UnityEngine;

namespace Audere.Puzzle
{
    [DisallowMultipleComponent]
    public sealed class PuzzlePlayerStart : MonoBehaviour
    {
        [SerializeField] private Color gizmoColor = new Color(1f, .78f, .12f, .9f);

        private void OnDrawGizmos()
        {
            GridSpace2D grid = GetComponentInParent<GridSpace2D>();
            float size = grid != null ? grid.CellSize : 1f;

            Gizmos.color = gizmoColor;
            Gizmos.DrawWireCube(transform.position, Vector3.one * size * .7f);
            Gizmos.DrawLine(
                transform.position + Vector3.left * size * .25f,
                transform.position + Vector3.right * size * .25f);
            Gizmos.DrawLine(
                transform.position + Vector3.down * size * .25f,
                transform.position + Vector3.up * size * .25f);
        }
    }
}
