using System.Collections;
using UnityEngine;

namespace Audere.Puzzle.Board
{
    /// <summary>
    /// A red StepTile that accepts one entry per puzzle attempt. It remains
    /// physically present while the player stands on it, then collapses when
    /// the player leaves. Reset/replay restores its authored presentation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OneUseTileBehaviour : MonoBehaviour,
        IBoardTileBehaviour, IBoardTileTraversalRule, IBoardTileResettable
    {
        [SerializeField] private SpriteRenderer tileRenderer;
        private bool consumed;
        public bool IsConsumed => consumed;

        public void OnTileInitialized(BoardTile tile)
        {
            if (tileRenderer == null) tileRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }
        public bool CanPlayerEnter(BoardTile tile, GridPlayer player) => !consumed;
        public void OnPlayerEntered(BoardTile tile, GridPlayer player) => consumed = true;
        public void OnPlayerExited(BoardTile tile, GridPlayer player)
        {
            if (!consumed) return;
            // Keep the authored object registered for reset, but leave no faded remnant.
            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(true))
            {
                var color = renderer.color; color.a = 0f;
                renderer.color = color; renderer.enabled = false;
            }
        }
        public void ResetToAuthoredState()
        {
            // BoardTile restores every authored renderer/scale before this callback.
            consumed = false;
        }
    }
}
