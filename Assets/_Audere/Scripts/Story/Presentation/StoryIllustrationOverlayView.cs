using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Audere.Story.Presentation
{
    [DisallowMultipleComponent]
    public sealed class StoryIllustrationOverlayView : MonoBehaviour
    {
        [Header("Direct References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button dismissButton;
        [SerializeField] private TMP_Text caption;

        private UnityEngine.Object activeOwner;
        private Action activeDismissed;
        private bool isShowing;

        public bool IsShowing => isShowing;
        public TMP_Text Caption => caption;

        private void Awake()
        {
            ForceHide();
        }

        private void OnDisable()
        {
            ClearPresentation();
        }

        public bool Show(UnityEngine.Object owner, Action onDismissed)
        {
            if (owner == null || onDismissed == null || canvasGroup == null || dismissButton == null)
            {
                Debug.LogError(
                    "[StoryIllustrationOverlayView] Owner, callback, CanvasGroup and Dismiss Button are required.",
                    this);
                return false;
            }

            ForceHide();
            activeOwner = owner;
            activeDismissed = onDismissed;
            isShowing = true;

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            dismissButton.interactable = true;
            dismissButton.onClick.AddListener(HandleDismissed);
            return true;
        }

        public void ForceHide(UnityEngine.Object owner = null)
        {
            if (owner != null && activeOwner != null && owner != activeOwner)
                return;

            ClearPresentation();
        }

        private void HandleDismissed()
        {
            if (!isShowing || activeOwner == null)
            {
                ClearPresentation();
                return;
            }

            Action dismissed = activeDismissed;
            ClearPresentation();
            dismissed?.Invoke();
        }

        private void ClearPresentation()
        {
            isShowing = false;
            activeOwner = null;
            activeDismissed = null;

            if (dismissButton != null)
            {
                dismissButton.onClick.RemoveListener(HandleDismissed);
                dismissButton.interactable = false;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }
    }
}
