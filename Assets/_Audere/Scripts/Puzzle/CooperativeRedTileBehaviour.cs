using System.Collections;
using Audere.Puzzle.Board;
using UnityEngine;

namespace Audere.Puzzle
{
    /// <summary>A shared red tile stays while either carrier occupies it, then disappears after both pass.</summary>
    [DisallowMultipleComponent]
    public sealed class CooperativeRedTileBehaviour : MonoBehaviour, IBoardTileBehaviour, IBoardTileTraversalRule, IBoardTileResettable
    {
        [SerializeField] private CooperativePuzzleSession session;
        [SerializeField] private SpriteRenderer tileRenderer;
        private bool audereEntered, biancaEntered, audereOccupies, biancaOccupies, collapsed;
        public bool BothPassed => audereEntered && biancaEntered && !audereOccupies && !biancaOccupies;
        public bool IsCollapsed => collapsed;
        public bool HasBeenEntered => audereEntered || biancaEntered;

        public void OnTileInitialized(BoardTile tile) { }
        public bool CanPlayerEnter(BoardTile tile, GridPlayer player)
        {
            if (session == null || collapsed) return false;
            player = player != null ? player : session.Puzzle.ActivePlayer;
            if (!session.ContainsActor(player) || session.HasArrived(player)) return false;
            bool bianca = player == session.Partner;
            if (bianca ? biancaEntered : audereEntered) return false;
            return !HasBeenEntered || (bianca ? audereOccupies : biancaOccupies);
        }
        public void OnPlayerEntered(BoardTile tile, GridPlayer player)
        {
            if (session == null || !session.ContainsActor(player)) return;
            if (player == session.Partner) { biancaEntered = true; biancaOccupies = true; }
            else { audereEntered = true; audereOccupies = true; }
        }
        public void OnPlayerExited(BoardTile tile, GridPlayer player)
        {
            if (session == null || !session.ContainsActor(player)) return;
            if (player == session.Partner) biancaOccupies = false; else audereOccupies = false;
            if (!HasBeenEntered || audereOccupies || biancaOccupies || collapsed) return;
            // Also remove a stranded tile: after its holder leaves, neither actor can enter it.
            collapsed = true;
            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(true))
            {
                var color = renderer.color; color.a = 0f;
                renderer.color = color; renderer.enabled = false;
            }
        }
        public void ResetToAuthoredState()
        {
            // BoardTile restores all authored visuals before resetting these attempt flags.
            audereEntered = biancaEntered = audereOccupies = biancaOccupies = collapsed = false;
        }
    }
}
