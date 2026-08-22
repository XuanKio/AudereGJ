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
        private SpriteRenderer[] authoredRenderers;
        private Color[] authoredRendererColors;
        private bool[] authoredRendererEnabled;
        private Vector3 authoredLocalScale;
        private bool authoredStateCaptured;

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
                if (behaviour is IBoardTileDataReceiver receiver)
                    receiver.ReceiveTileData(data);

            foreach (IBoardTileBehaviour behaviour in behaviours)
                behaviour.OnTileInitialized(this);

            IsLevelGoal = behaviours.Exists(behaviour => behaviour is ILevelGoalTile);
        }

        /// <summary>
        /// Initializes a tile that already exists in a scene or prefab. Its transform,
        /// name and visual scale are authoring data and must not be overwritten at runtime.
        /// </summary>
        public void InitializeSceneAuthored(Vector2Int position)
        {
            gridPosition = position;
            CaptureAuthoredState();
            CacheBehaviours();

            foreach (IBoardTileBehaviour behaviour in behaviours)
                behaviour.OnTileInitialized(this);

            IsLevelGoal = behaviours.Exists(behaviour => behaviour is ILevelGoalTile);
        }

        public void ResetToAuthoredState()
        {
            CaptureAuthoredState();
            gameObject.SetActive(true);
            transform.localScale = authoredLocalScale;

            for (int index = 0; index < authoredRenderers.Length; index++)
            {
                SpriteRenderer renderer = authoredRenderers[index];
                if (renderer == null)
                    continue;

                renderer.enabled = authoredRendererEnabled[index];
                renderer.color = authoredRendererColors[index];
            }

            CacheBehaviours();
            foreach (IBoardTileBehaviour behaviour in behaviours)
                if (behaviour is IBoardTileResettable resettable)
                    resettable.ResetToAuthoredState();
        }

        public bool TryGetBehaviour<T>(out T result) where T : class, IBoardTileBehaviour
        {
            CacheBehaviours();
            foreach (IBoardTileBehaviour behaviour in behaviours)
            {
                if (behaviour is T match)
                {
                    result = match;
                    return true;
                }
            }

            result = null;
            return false;
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

        private void CaptureAuthoredState()
        {
            if (authoredStateCaptured)
                return;

            authoredStateCaptured = true;
            authoredLocalScale = transform.localScale;
            authoredRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            authoredRendererColors = new Color[authoredRenderers.Length];
            authoredRendererEnabled = new bool[authoredRenderers.Length];
            for (int index = 0; index < authoredRenderers.Length; index++)
            {
                authoredRendererColors[index] = authoredRenderers[index].color;
                authoredRendererEnabled[index] = authoredRenderers[index].enabled;
            }
        }
    }
}
