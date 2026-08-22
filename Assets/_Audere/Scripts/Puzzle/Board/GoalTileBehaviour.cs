using UnityEngine;

namespace Audere.Puzzle.Board
{
    /// <summary>
    /// Prefab-owned goal capability. Board and puzzle code query the capability,
    /// so replacing the Goal prefab visual never requires gameplay code changes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GoalTileBehaviour : MonoBehaviour,
        IBoardTileBehaviour,
        ILevelGoalTile,
        IBoardTileResettable
    {
        [SerializeField] private GameObject goalVisual;
        [SerializeField] private GameObject itemVisual;

        private bool goalVisualStartsActive;
        private bool itemVisualStartsActive;
        private bool authoredStateCaptured;

        public void OnTileInitialized(BoardTile tile)
        {
            ResolveReferences();
            CaptureAuthoredState();
        }
        public void OnPlayerEntered(BoardTile tile, GridPlayer player) { }
        public void OnPlayerExited(BoardTile tile, GridPlayer player) { }

        public void BecomeTransitionAnchor()
        {
            ResolveReferences();
            if (goalVisual != null)
                goalVisual.SetActive(false);
            if (itemVisual != null)
                itemVisual.SetActive(false);
        }

        public void ResetToAuthoredState()
        {
            ResolveReferences();
            CaptureAuthoredState();
            if (goalVisual != null)
                goalVisual.SetActive(goalVisualStartsActive);
            if (itemVisual != null)
                itemVisual.SetActive(itemVisualStartsActive);
        }

        private void ResolveReferences()
        {
            Transform visualRoot = transform.Find("Visual Root");
            if (visualRoot == null)
                return;

            if (goalVisual == null)
            {
                Transform found = visualRoot.Find("Goal Visual");
                if (found != null)
                    goalVisual = found.gameObject;
            }

            if (itemVisual == null)
            {
                Transform found = visualRoot.Find("Item");
                if (found != null)
                    itemVisual = found.gameObject;
            }
        }

        private void CaptureAuthoredState()
        {
            if (authoredStateCaptured)
                return;

            authoredStateCaptured = true;
            goalVisualStartsActive = goalVisual == null || goalVisual.activeSelf;
            itemVisualStartsActive = itemVisual == null || itemVisual.activeSelf;
        }
    }
}
