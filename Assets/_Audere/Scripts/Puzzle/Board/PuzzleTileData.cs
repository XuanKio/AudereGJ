using System;
using Audere.Dialogue;
using UnityEngine;

namespace Audere.Puzzle.Board
{
    [Serializable]
    public struct PuzzleTileData
    {
        [SerializeField] private Vector2Int position;
        [SerializeField] private PuzzleTileType tileType;
        [SerializeField] private DialogueData dialogue;
        [SerializeField] private bool triggerDialogueOnce;

        public Vector2Int Position => position;
        public PuzzleTileType TileType => tileType;
        public string TileId => PuzzleContentConstants.GetTileId(tileType);
        public DialogueData Dialogue => dialogue;
        public bool TriggerDialogueOnce => triggerDialogueOnce;

        public PuzzleTileData(
            Vector2Int position,
            PuzzleTileType tileType,
            DialogueData dialogue = null,
            bool triggerDialogueOnce = true)
        {
            this.position = position;
            this.tileType = tileType;
            this.dialogue = dialogue;
            this.triggerDialogueOnce = triggerDialogueOnce;
        }
    }
}
