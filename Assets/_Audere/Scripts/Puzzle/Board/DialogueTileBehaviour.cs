using Audere.Dialogue;
using UnityEngine;

namespace Audere.Puzzle.Board
{
    [DisallowMultipleComponent]
    public sealed class DialogueTileBehaviour : MonoBehaviour, IBoardTileBehaviour, IBoardTileDataReceiver
    {
        private DialogueData dialogueData;
        private bool triggerOnce = true;
        private bool triggered;

        public void ReceiveTileData(PuzzleTileData data)
        {
            dialogueData = data.Dialogue;
            triggerOnce = data.TriggerDialogueOnce;
        }

        public void OnTileInitialized(BoardTile tile) { }

        public void OnPlayerEntered(BoardTile tile, GridPlayer player)
        {
            if (dialogueData == null || (triggerOnce && triggered))
                return;

            GameplayUIRoot root = GameplayUIRoot.Instance;
            if (root == null || root.Dialogue == null)
            {
                Debug.LogError("[DialogueTile] GameplayUIRoot is not available in this gameplay scene.", this);
                return;
            }

            if (root.Dialogue.Play(dialogueData, triggerOnce))
                triggered = true;
        }

        public void OnPlayerExited(BoardTile tile, GridPlayer player) { }
    }
}
