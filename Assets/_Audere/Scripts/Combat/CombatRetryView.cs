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
            "Không sao. Tớ vẫn ở đây.\nMình thử lại nhé.";

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
            ForceHide();
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

        public void ForceHide()
        {
            HideImmediate();
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
                Transform child = FindDescendant(retryRoot.transform, "Retry Message");
                if (child != null)
                    messageText = child.GetComponent<TMP_Text>();
            }

            if (retryButton == null)
            {
                Transform child = FindDescendant(retryRoot.transform, "Retry Button");
                if (child != null)
                    retryButton = child.GetComponent<Button>();
            }
        }

        private static Transform FindDescendant(Transform root, string targetName)
        {
            if (root.name == targetName) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDescendant(root.GetChild(i), targetName);
                if (found != null) return found;
            }
            return null;
        }
    }
}
