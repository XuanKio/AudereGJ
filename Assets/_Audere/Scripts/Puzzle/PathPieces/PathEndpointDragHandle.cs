using UnityEngine;

namespace Audere.Puzzle.PathPieces
{
    [DisallowMultipleComponent]
    public sealed class PathEndpointDragHandle : MonoBehaviour
    {
        [SerializeField] private PathPreview owner;
        [SerializeField, Range(0, 1)] private int endpointIndex;

        public void Configure(PathPreview preview, int index)
        {
            owner = preview;
            endpointIndex = Mathf.Clamp(index, 0, 1);
        }

    }
}
