using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat
{
    [DisallowMultipleComponent]
    public sealed class CombatRetryView : MonoBehaviour
    {
        [SerializeField] private GameObject retryRoot;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button retryButton;
        [SerializeField, TextArea] private string defaultMessage =
            "Tiếp tục đi, Audere.\nCậu làm được mà.";

        private UnityEngine.Object activeOwner;
        private Action pendingRetry;

        public bool IsShowing { get; private set; }
        public UnityEngine.Object ActiveOwner => activeOwner;

        private void Awake()
        {
            ResolveReferences();
            if (retryButton != null)
                retryButton.onClick.AddListener(HandleRetryClicked);
            HideImmediate();
        }

        private void OnDisable()
        {
            HideImmediate();
        }

        private void OnDestroy()
        {
            if (retryButton != null)
                retryButton.onClick.RemoveListener(HandleRetryClicked);
        }

        public bool Show(UnityEngine.Object owner, Action onRetry)
        {
            ResolveReferences();

            if (owner == null || onRetry == null)
            {
                Debug.LogError("[CombatRetryView] Retry presentation requires an owner and callback.", this);
                return false;
            }

            if (retryRoot == null || messageText == null || retryButton == null)
            {
                Debug.LogError(
                    "[CombatRetryView] Assign Retry Root, Message Text and Retry Button.",
                    this);
                return false;
            }

            if (IsShowing && activeOwner != owner)
            {
                Debug.LogWarning(
                    $"[CombatRetryView] Retry presentation is already owned by '{activeOwner.name}'.",
                    this);
                return false;
            }

            activeOwner = owner;
            pendingRetry = onRetry;
            messageText.text = defaultMessage;
            retryButton.interactable = true;
            retryRoot.SetActive(true);
            IsShowing = true;
            return true;
        }

        public bool Hide(UnityEngine.Object owner)
        {
            if (!IsShowing || owner == null || activeOwner != owner)
                return false;

            HideImmediate();
            return true;
        }

        private void HandleRetryClicked()
        {
            if (!IsShowing || !retryButton.interactable)
                return;

            Action retry = pendingRetry;
            HideImmediate();
            retry?.Invoke();
        }

        private void HideImmediate()
        {
            IsShowing = false;
            activeOwner = null;
            pendingRetry = null;

            if (retryButton != null)
                retryButton.interactable = false;
            if (retryRoot != null)
                retryRoot.SetActive(false);
        }

        private void ResolveReferences()
        {
            if (retryRoot == null)
            {
                Transform child = transform.Find("Retry Panel");
                if (child != null)
                    retryRoot = child.gameObject;
            }

            if (retryRoot == null)
                return;

            if (messageText == null)
            {
                Transform child = retryRoot.transform.Find("Retry Message");
                if (child != null)
                    messageText = child.GetComponent<TMP_Text>();
            }

            if (retryButton == null)
            {
                Transform child = retryRoot.transform.Find("Retry Button");
                if (child != null)
                    retryButton = child.GetComponent<Button>();
            }
        }
    }
}
