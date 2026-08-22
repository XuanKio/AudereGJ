using TMPro;
using UnityEngine;

namespace Audere.Puzzle
{
    public sealed class PuzzleHud : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI instructionLabel;
        [SerializeField] private TextMeshProUGUI companionLabel;

        private void Awake()
        {
            if (instructionLabel == null)
                instructionLabel = GetComponent<TextMeshProUGUI>();
        }

        public void SetMessage(string value)
        {
            SetInstruction(value);
        }

        public void SetInstruction(string value)
        {
            if (instructionLabel != null)
                instructionLabel.text = value ?? string.Empty;
        }

        public void SetCompanionMessage(string value)
        {
            if (companionLabel != null)
                companionLabel.text = value ?? string.Empty;
        }

        public void Clear()
        {
            SetInstruction(string.Empty);
            SetCompanionMessage(string.Empty);
        }
    }
}
