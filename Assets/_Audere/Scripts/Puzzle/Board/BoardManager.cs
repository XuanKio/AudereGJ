using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Audere.Puzzle.Board
{
    /// <summary>
    /// Owns grid state and instantiates tile-specific prefabs through the tile catalog.
    /// It never needs to know about Grass or any future tile behaviour directly.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BoardManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridSpace2D gridSpace;
        [SerializeField] private Transform boardVisualRoot;
        [SerializeField] private Transform levelObjectiveRoot;
        [SerializeField] private PuzzleTileCatalog tileCatalog;

        [Header("Optional Demo Board")]
        [SerializeField] private bool buildDemoBoardOnAwake = true;
        [SerializeField] private List<Vector2Int> demoCells = new List<Vector2Int>
        {
            new Vector2Int(-3, 1), new Vector2Int(-2, 1), new Vector2Int(-1, 1),
            new Vector2Int(0, 1),  new Vector2Int(1, 1),  new Vector2Int(2, 1),
            new Vector2Int(-3, 0), new Vector2Int(-2, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, 0),  new Vector2Int(1, 0),  new Vector2Int(2, 0),
            new Vector2Int(-2, -1), new Vector2Int(-1, -1), new Vector2Int(0, -1),
            new Vector2Int(1, -1)
        };

        private readonly Dictionary<Vector2Int, BoardTile> tilesByPosition = new Dictionary<Vector2Int, BoardTile>();
        private readonly List<Vector2Int> gridPositions = new List<Vector2Int>();
        private readonly List<BoardTile> spawnedTiles = new List<BoardTile>();

        public GridSpace2D GridSpace => gridSpace;
        public PuzzleTileCatalog TileCatalog => tileCatalog;
        public IReadOnlyList<Vector2Int> GridPositions => gridPositions;

        private void Awake()
        {
            if (gridSpace == null) gridSpace = FindFirstObjectByType<GridSpace2D>();
            if (boardVisualRoot == null) boardVisualRoot = transform;
            if (buildDemoBoardOnAwake)
                BuildDemoBoard();
            else
                RegisterExistingTiles();
        }

        public bool ContainsCell(Vector2Int gridPosition) => tilesByPosition.ContainsKey(gridPosition);
        public bool HasTile(Vector2Int gridPosition) => ContainsCell(gridPosition);

        public bool TryGetTile(Vector2Int gridPosition, out BoardTile tile)
        {
            return tilesByPosition.TryGetValue(gridPosition, out tile);
        }

        public bool TryGetWorldBounds(out Bounds bounds)
        {
            bounds = default;
            if (gridSpace == null || gridPositions.Count == 0)
                return false;

            Vector3 firstCenter = gridSpace.CellToWorldCenter(gridPositions[0]);
            bounds = new Bounds(firstCenter, Vector3.one * gridSpace.CellSize);

            for (int index = 1; index < gridPositions.Count; index++)
            {
                Vector3 center = gridSpace.CellToWorldCenter(gridPositions[index]);
                bounds.Encapsulate(new Bounds(center, Vector3.one * gridSpace.CellSize));
            }

            return true;
        }

        public bool RegisterTile(BoardTile tile)
        {
            if (tile == null)
                return false;

            if (tilesByPosition.ContainsKey(tile.GridPosition))
            {
                Debug.LogError($"[BoardManager] Duplicate board tile at {tile.GridPosition}.", tile);
                return false;
            }

            tilesByPosition.Add(tile.GridPosition, tile);
            gridPositions.Add(tile.GridPosition);
            return true;
        }

        public void NotifyPlayerEntered(Vector2Int position, GridPlayer player)
        {
            if (TryGetTile(position, out BoardTile tile))
                tile.NotifyPlayerEntered(player);
        }

        public void NotifyPlayerExited(Vector2Int position, GridPlayer player)
        {
            if (TryGetTile(position, out BoardTile tile))
                tile.NotifyPlayerExited(player);
        }

        public void ClearBoard()
        {
            tilesByPosition.Clear();
            gridPositions.Clear();
            foreach (BoardTile tile in spawnedTiles)
                if (tile != null) Destroy(tile.gameObject);
            spawnedTiles.Clear();
        }

        [ContextMenu("Build Demo Board")]
        public void BuildDemoBoard()
        {
            BuildBoard(demoCells.Select(cell => new PuzzleTileData(cell, PuzzleTileType.Grass)).ToList());
        }

        public void BuildBoard(IReadOnlyList<Vector2Int> cells)
        {
            BuildBoard(cells.Select(cell => new PuzzleTileData(cell, PuzzleTileType.Grass)).ToList());
        }

        public void BuildBoard(IReadOnlyList<PuzzleTileData> tiles)
        {
            ClearBoard();

            if (gridSpace == null || boardVisualRoot == null)
            {
                Debug.LogError("[BoardManager] Assign GridSpace and Board Visual Root.", this);
                return;
            }

            foreach (PuzzleTileData data in tiles)
            {
                if (tileCatalog == null || !tileCatalog.TryGetPrefab(data.TileType, out BoardTile prefab))
                {
                    Debug.LogError($"[BoardManager] No prefab registered for tile type {data.TileType}.", this);
                    continue;
                }

                BoardTile tile = Instantiate(prefab, boardVisualRoot);
                tile.transform.position = gridSpace.CellToWorldCenter(data.Position);
                tile.Initialize(data, gridSpace.CellSize);
                if (tile.IsLevelGoal && levelObjectiveRoot != null)
                    tile.transform.SetParent(levelObjectiveRoot, true);
                spawnedTiles.Add(tile);
                RegisterTile(tile);
            }
        }

        private void RegisterExistingTiles()
        {
            tilesByPosition.Clear();
            gridPositions.Clear();
            foreach (BoardTile tile in GetComponentsInChildren<BoardTile>())
            {
                spawnedTiles.Add(tile);
                RegisterTile(tile);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            foreach (Vector2Int coordinate in tilesByPosition.Keys)
                if (gridSpace != null)
                    Gizmos.DrawWireCube(
                        gridSpace.CellToWorldCenter(coordinate),
                        Vector3.one * gridSpace.CellSize);
        }
    }
}
