using System;
using Audere.Dialogue;
using Audere.GameplayInput;
using UnityEngine;
using UnityEngine.UI;

namespace Audere.Story.Presentation
{
    public sealed class ChalkDrawingView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private ChalkDrawingSurface surface;
        [SerializeField] private Button completeButton;
        private UnityEngine.Object owner;
        private Action completed;
        private GameplayInputGate gate;
        private GameplayInputToken token;
        public bool IsShowing { get; private set; }
        public ChalkDrawingSurface Surface => surface;
        public Button CompleteButton => completeButton;
        private void Awake() => ForceHide();

        public bool Show(UnityEngine.Object requester, Action onCompleted)
        {
            if (requester == null || onCompleted == null || group == null || surface == null || completeButton == null ||
                GameplayUIRoot.Instance == null || GameplayUIRoot.Instance.Dialogue.IsPlaying || !isActiveAndEnabled)
                return false;
            ForceHide();
            gate = GameplayUIRoot.Instance.InputGate;
            token = gate.PushMode(this, GameplayInputMode.Dialogue);
            if (!token.IsValid) { gate = null; return false; }
            owner = requester; completed = onCompleted; IsShowing = true;
            surface.ResetDrawing();
            surface.DrawingChanged += UpdateButton;
            surface.AcceptsDrawing = true;
            completeButton.onClick.AddListener(Complete);
            group.alpha = 1f; group.interactable = true; group.blocksRaycasts = true;
            UpdateButton();
            return true;
        }

        private void Update() { if (IsShowing && owner == null) ForceHide(); }
        private void UpdateButton() { if (completeButton != null) completeButton.interactable = IsShowing && surface.HasDrawing; }
        private void Complete()
        {
            if (!IsShowing || owner == null || !surface.HasDrawing) return;
            Action callback = completed;
            ForceHide(); // Clear ownership/input before callback; double click cannot complete twice.
            callback?.Invoke();
        }
        public void ForceHide(UnityEngine.Object requester = null)
        {
            if (requester != null && owner != null && requester != owner) return;
            IsShowing = false; owner = null; completed = null;
            if (surface != null) { surface.AcceptsDrawing = false; surface.DrawingChanged -= UpdateButton; }
            if (completeButton != null) { completeButton.onClick.RemoveListener(Complete); completeButton.interactable = false; }
            if (group != null) { group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false; }
            if (gate != null) gate.Release(token);
            gate = null; token = default;
        }
        private void OnDisable() => ForceHide();
        private void OnDestroy() => ForceHide();
    }
}
