using Audere.Dialogue;
using Audere.GameplayInput;
using Audere.Puzzle.Board;
using UnityEngine;

namespace Audere.Puzzle
{
    /// <summary>Optional two-actor rules on an authored level; inventory and traversal stay in PuzzleManager.</summary>
    [DisallowMultipleComponent]
    public sealed class CooperativePuzzleSession : MonoBehaviour
    {
        [SerializeField] private PuzzleManager puzzle;
        [SerializeField] private GridPlayer partner;
        [SerializeField] private Transform partnerStart;
        [SerializeField] private BoardTile audereGoal;
        [SerializeField] private BoardTile partnerGoal;
        [SerializeField] private CooperativePuzzleControls controls;

        public PuzzleManager Puzzle => puzzle;
        public GridPlayer Partner => partner;
        public Transform PartnerStart => partnerStart;
        public BoardTile AudereGoal => audereGoal;
        public BoardTile PartnerGoal => partnerGoal;
        [SerializeField] private Audere.Story.Steps.SpriteGroupFadeStep audereArrivalFade;
        [SerializeField] private Audere.Story.Steps.SpriteGroupFadeStep partnerArrivalFade;
        [SerializeField] private Audere.Story.Steps.SpriteGroupFadeStep audereRestore;
        [SerializeField] private Audere.Story.Steps.SpriteGroupFadeStep partnerRestore;
        [SerializeField] private Audere.Story.Steps.DialogueStep encouragement;
        private bool audereArrived, partnerArrived;
        private int completedPlacements;
        public bool HasArrived(GridPlayer actor) => actor == partner ? partnerArrived : actor == puzzle.Player && audereArrived;
        public bool BothAtGoals => audereArrived && partnerArrived;
        public bool PlacePartnerAtStart()
        {
            if (puzzle == null || puzzle.Board == null || puzzle.Board.GridSpace == null ||
                partner == null || partnerStart == null || partner == puzzle.Player)
                return false;
            var grid = puzzle.Board.GridSpace;
            Vector2Int cell = grid.WorldToCell(partnerStart.position);
            if (!puzzle.Board.ContainsCell(cell))
                return false;
            partner.gameObject.SetActive(true);
            partner.SetPosition(cell, grid.CellToWorldCenter(cell));
            audereArrived = partnerArrived = false;
            completedPlacements = 0;
            audereRestore?.Play();
            partnerRestore?.Play();
            return true;
        }

        public void BeginAttempt()
        {
            puzzle.Board.NotifyPlayerEntered(partner.GridPosition, partner);
            if (controls != null) controls.Bind(this);
        }

        public void EndAttempt()
        {
            if (puzzle != null && puzzle.Player != null) puzzle.Player.CancelMotion();
            if (partner != null) partner.CancelMotion();
            if (puzzle != null && puzzle.Player != null) puzzle.Player.ResetDepthOrder();
            if (partner != null) partner.ResetDepthOrder();
            audereArrivalFade?.Cancel();
            partnerArrivalFade?.Cancel();
            encouragement?.Cancel();
            if (controls != null) controls.Unbind(this);
        }

        // Preview stays deterministic; a shared-cell tie is rolled once, only on drop.
        public GridPlayer ActorAtStart(Vector2Int cell, bool randomizeShared)
        {
            bool a = puzzle != null && puzzle.Player != null && !audereArrived && puzzle.Player.GridPosition == cell;
            bool b = partner != null && !partnerArrived && partner.GridPosition == cell;
            if (a && b) return randomizeShared && Random.Range(0, 2) == 1 ? partner : puzzle.Player;
            return a ? puzzle.Player : b ? partner : null;
        }

        public System.Collections.IEnumerator ResolveLanding(GridPlayer actor)
        {
            completedPlacements++;
            BoardTile goal = actor == partner ? partnerGoal : audereGoal;
            if (goal != null && actor.GridPosition == goal.GridPosition && !HasArrived(actor))
            {
                if (actor == partner) partnerArrived = true; else audereArrived = true;
                // Arrival is locked even after its visual exits.
                var fade = actor == partner ? partnerArrivalFade : audereArrivalFade;
                if (fade != null && fade.Play()) while (fade.IsRunning) yield return null;
            }
            if (completedPlacements == 2 && encouragement != null && encouragement.Play())
                while (encouragement.IsRunning) yield return null;
        }

        public bool ContainsActor(GridPlayer actor) => puzzle != null &&
            (actor == puzzle.Player || actor == partner);

        public bool AnyActorAt(BoardTile tile) => tile != null && puzzle != null &&
            ((puzzle.Player != null && puzzle.Player.GridPosition == tile.GridPosition) ||
             (partner != null && partner.GridPosition == tile.GridPosition));

        public Vector3 ArrivalOffset(GridPlayer mover, Vector2Int destination)
        {
            GridPlayer other = mover == partner ? puzzle.Player : partner;
            return other != null && !HasArrived(other) && other.GridPosition == destination ? SplitOffset(mover) : Vector3.zero;
        }

        private Vector3 SplitOffset(GridPlayer actor)
        {
            var grid = puzzle.Board.GridSpace;
            float cell = Vector3.Distance(grid.CellToWorldCenter(Vector2Int.zero), grid.CellToWorldCenter(Vector2Int.right));
            return actor == partner ? new Vector3(cell * .28f, -cell * .04f, 0f)
                : new Vector3(-cell * .28f, cell * .04f, 0f);
        }

        private void LateUpdate()
        {
            if (puzzle == null || partner == null || puzzle.Player == null ||
                puzzle.CurrentState == PuzzleManager.State.Idle || puzzle.CurrentState == PuzzleManager.State.Completed) return;
            GridPlayer audere = puzzle.Player;
            bool sharing = !audereArrived && !partnerArrived && (audere.GridPosition == partner.GridPosition ||
                (audere.IsMoving && audere.MotionTargetCell == partner.GridPosition) ||
                (partner.IsMoving && partner.MotionTargetCell == audere.GridPosition));
            // Logical cells never change here. Only the two settled presentations separate.
            // Sort the whole actor (body + grounded shadow) by its floor position,
            // never by the temporary hop height. Equal rows keep a stable tie.
            bool audereInFront = audere.GroundSortY < partner.GroundSortY - .0001f;
            audere.SetStandingPresentation(sharing ? SplitOffset(audere) : Vector3.zero, audereInFront ? 6 : 5);
            partner.SetStandingPresentation(sharing ? SplitOffset(partner) : Vector3.zero, audereInFront ? 5 : 6);
        }

    }
}
