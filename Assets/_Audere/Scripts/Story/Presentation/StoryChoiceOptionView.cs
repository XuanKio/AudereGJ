using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Audere.Story.Presentation
{
    [DisallowMultipleComponent]
    public sealed class StoryChoiceOptionView : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        ISelectHandler,
        IDeselectHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, .58f);
        [SerializeField] private Color focusedColor = Color.white;
        [SerializeField, Range(.5f, 1f)] private float idleScale = .92f;

        private string authoredText;
        private int optionIndex;
        private Action<int> selected;

        public Button Button => button;
        public TMP_Text Label => label;

        private void Awake()
        {
            if (button != null)
                button.onClick.AddListener(HandleClicked);
            SetFocused(false);
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClicked);
        }

        public void Bind(int index, string text, Action<int> onSelected)
        {
            optionIndex = index;
            authoredText = text ?? string.Empty;
            selected = onSelected;
            if (button != null)
                button.interactable = true;
            SetFocused(false);
        }

        public void Clear()
        {
            selected = null;
            if (button != null)
                button.interactable = false;
            SetFocused(false);
        }

        public void OnPointerEnter(PointerEventData eventData) => SetFocused(true);
        public void OnPointerExit(PointerEventData eventData) => SetFocused(false);
        public void OnSelect(BaseEventData eventData) => SetFocused(true);
        public void OnDeselect(BaseEventData eventData) => SetFocused(false);

        private void HandleClicked()
        {
            if (button == null || !button.interactable)
                return;
            button.interactable = false;
            selected?.Invoke(optionIndex);
        }

        private void SetFocused(bool focused)
        {
            if (label != null)
            {
                label.text = focused ? $"> {authoredText} <" : authoredText;
                label.color = focused ? focusedColor : idleColor;
            }
            transform.localScale = Vector3.one * (focused ? 1f : idleScale);
        }
    }
}
