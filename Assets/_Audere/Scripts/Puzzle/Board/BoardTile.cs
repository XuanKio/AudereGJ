using System.Collections.Generic;
using UnityEngine;

namespace Audere.Puzzle.Board
{
    /// <summary>
    /// Runtime instance of a tile prefab. Visuals and special behaviour live on the prefab;
    /// grid position and type remain the stable gameplay identity.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BoardTile : MonoBehaviour
    {
        [SerializeField] private Vector2Int gridPosition;
        [SerializeField] private PuzzleTileType tileType = PuzzleTileType.Grass;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField, Range(0.1f, 1f)] private float cellFill = 0.92f;

        private readonly List<IBoardTileBehaviour> behaviours = new List<IBoardTileBehaviour>();

        public Vector2Int GridPosition => gridPosition;
        public PuzzleTileType TileType => tileType;
        public string TileId => PuzzleContentConstants.GetTileId(tileType);
        public bool IsLevelGoal { get; private set; }

        public void Initialize(PuzzleTileData data, float cellSize)
        {
            gridPosition = data.Position;
            tileType = data.TileType;
            name = $"{tileType} ({gridPosition.x}, {gridPosition.y})";

            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            FitVisualToCell(cellSize);
            CacheBehaviours();

            foreach (IBoardTileBehaviour behaviour in behaviours)
                behaviour.OnTileInitialized(this);

            IsLevelGoal = behaviours.Exists(behaviour => behaviour is ILevelGoalTile);
        }

        public void NotifyPlayerEntered(GridPlayer player)
        {
            foreach (IBoardTileBehaviour behaviour in behaviours)
                behaviour.OnPlayerEntered(this, player);
        }

        public void NotifyPlayerExited(GridPlayer player)
        {
            foreach (IBoardTileBehaviour behaviour in behaviours)
                behaviour.OnPlayerExited(this, player);
        }

        private void FitVisualToCell(float cellSize)
        {
            transform.localScale = Vector3.one;
            if (spriteRenderer == null || spriteRenderer.sprite == null)
                return;

            Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
            float largestDimension = Mathf.Max(spriteSize.x, spriteSize.y);
            if (largestDimension <= Mathf.Epsilon)
                return;

            transform.localScale = Vector3.one * (cellSize * cellFill / largestDimension);
            spriteRenderer.sortingOrder = 0;
        }

        private void CacheBehaviours()
        {
            behaviours.Clear();
            MonoBehaviour[] components = GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour component in components)
            {
                if (component is IBoardTileBehaviour behaviour)
                    behaviours.Add(behaviour);
            }
        }
    }
}
