using Audere.Dialogue;
using UnityEngine;

namespace Audere.Puzzle.Board
{
    [DisallowMultipleComponent]
    public sealed class DialogueTileBehaviour : MonoBehaviour, IBoardTileBehaviour, IBoardTileDataReceiver
    {
        [SerializeField] private DialogueData dialogueData;
        [SerializeField] private bool triggerOnce = true;
        private bool triggered;

        public DialogueData DialogueData => dialogueData;
        public bool TriggerOnce => triggerOnce;
        public bool Triggered => triggered;

        public void ReceiveTileData(PuzzleTileData data)
        {
            ConfigureData(data.Dialogue, data.TriggerDialogueOnce);
        }

        public void ConfigureData(DialogueData data, bool shouldTriggerOnce)
        {
            dialogueData = data;
            triggerOnce = shouldTriggerOnce;
            triggered = false;
        }

        public void OnTileInitialized(BoardTile tile)
        {
            triggered = false;
        }

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
