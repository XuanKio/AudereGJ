using Audere.Puzzle.PathPieces;
using UnityEngine;

namespace Audere.Puzzle
{
    /// <summary>
    /// One location-scoped runtime shared by every scene-authored puzzle under
    /// the same Puzzle Root. Level prefabs keep layout and rules; transient path
    /// interaction lives here so two inactive/transitioning boards cannot both
    /// drive the shared hand or leave duplicate previews behind.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PuzzleRuntime : MonoBehaviour
    {
        [SerializeField] private PathPlacementController placement;
        [SerializeField] private PathPreview preview;
        [SerializeField] private Transform placedPathRoot;

        public PathPlacementController Placement => placement;
        public PathPreview Preview => preview;
        public Transform PlacedPathRoot => placedPathRoot;

        private void Awake()
        {
            CacheChildReferences();
            preview?.Setup();
        }

        private void OnValidate()
        {
            CacheChildReferences();
        }

        private void CacheChildReferences()
        {
            if (placement == null)
                placement = GetComponentInChildren<PathPlacementController>(true);
            if (preview == null)
                preview = GetComponentInChildren<PathPreview>(true);
            if (placedPathRoot == null)
            {
                Transform child = transform.Find("Placed Path Root");
                if (child != null)
                    placedPathRoot = child;
            }
        }
    }
}
