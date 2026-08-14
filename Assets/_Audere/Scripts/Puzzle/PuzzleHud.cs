using TMPro;
using UnityEngine;

namespace Audere.Puzzle
{
    public sealed class PuzzleHud : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        private void Awake()
        {
            if (label == null)
                label = GetComponent<TextMeshProUGUI>();
        }

        public void SetMessage(string value)
        {
            if (label != null)
                label.text = value;
        }
    }
}
