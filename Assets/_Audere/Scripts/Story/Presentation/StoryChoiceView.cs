using System;
using Audere.GameplayInput;
using UnityEngine;

namespace Audere.Story.Presentation
{
    [DisallowMultipleComponent]
    public sealed class StoryChoiceView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private GameplayInputGate inputGate;
        [SerializeField] private StoryChoiceOptionView[] options;

        private UnityEngine.Object activeOwner;
        private Action<int> activeSelection;
        private GameplayInputToken inputToken;
        private bool isShowing;

        public bool IsShowing => isShowing;

        private void Awake() => ForceHide();
        private void OnDisable() => ForceHide();

        public bool Show(UnityEngine.Object owner, string[] labels, Action<int> onSelected)
        {
            // The scene's duplicate UI root is discarded after a cross-scene load.
            // Use the retained UI root's gate, just as DialogueStep uses its retained controller.
            if (inputGate == null && Audere.Dialogue.GameplayUIRoot.Instance != null)
                inputGate = Audere.Dialogue.GameplayUIRoot.Instance.InputGate;
            if (owner == null || onSelected == null || canvasGroup == null || inputGate == null ||
                options == null || labels == null || labels.Length == 0 || labels.Length > options.Length)
            {
                Debug.LogError("[StoryChoiceView] Direct references and one label per option are required.", this);
                return false;
            }

            ForceHide();
            GameplayInputToken token = inputGate.PushMode(owner, GameplayInputMode.Dialogue);
            if (!token.IsValid)
                return false;

            activeOwner = owner;
            activeSelection = onSelected;
            inputToken = token;
            isShowing = true;
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            for (int index = 0; index < options.Length; index++)
            {
                bool visible = index < labels.Length;
                if (options[index] == null)
                    continue;
                options[index].gameObject.SetActive(visible);
                if (visible)
                    options[index].Bind(index, labels[index], HandleSelected);
            }
            return true;
        }

        public void ForceHide(UnityEngine.Object owner = null)
        {
            if (owner != null && activeOwner != null && activeOwner != owner)
                return;

            isShowing = false;
            activeOwner = null;
            activeSelection = null;
            for (int index = 0; options != null && index < options.Length; index++)
                options[index]?.Clear();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            if (inputGate != null && inputToken.IsValid)
                inputGate.Release(inputToken);
            inputToken = default;
        }

        private void HandleSelected(int index)
        {
            if (!isShowing || activeOwner == null)
                return;
            Action<int> selected = activeSelection;
            ForceHide();
            selected?.Invoke(index);
        }
    }
}
