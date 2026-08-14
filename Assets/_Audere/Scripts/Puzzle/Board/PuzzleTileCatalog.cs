using System;
using System.Collections.Generic;
using UnityEngine;

namespace Audere.Puzzle.Board
{
    [CreateAssetMenu(
        fileName = "PuzzleTileCatalog",
        menuName = "Audere/Puzzle/Tile Catalog")]
    public sealed class PuzzleTileCatalog : ScriptableObject
    {
        [Serializable]
        private struct Entry
        {
            [SerializeField] private PuzzleTileType tileType;
            [SerializeField] private BoardTile prefab;

            public PuzzleTileType TileType => tileType;
            public BoardTile Prefab => prefab;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        public bool TryGetPrefab(PuzzleTileType tileType, out BoardTile prefab)
        {
            foreach (Entry entry in entries)
            {
                if (entry.TileType != tileType)
                    continue;

                prefab = entry.Prefab;
                return prefab != null;
            }

            prefab = null;
            return false;
        }

        private void OnValidate()
        {
            HashSet<PuzzleTileType> foundTypes = new HashSet<PuzzleTileType>();
            foreach (Entry entry in entries)
            {
                if (!foundTypes.Add(entry.TileType))
                    Debug.LogError($"[PuzzleTileCatalog] Duplicate prefab for {entry.TileType}.", this);
            }
        }
    }
}
