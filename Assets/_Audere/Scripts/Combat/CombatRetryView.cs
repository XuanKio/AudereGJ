using System;
using Audere.Audio;
using Audere.GameplayInput;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Audere.Combat
{
    [DisallowMultipleComponent]
    public sealed class CombatRetryView : MonoBehaviour
    {
        [SerializeField] private GameObject retryRoot;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button retryButton;
        [SerializeField] private CombatRetryPresentationProfile presentation;
        [SerializeField] private Image intactHeart;
        [SerializeField] private CombatHeartHalfGraphic leftHeart;
        [SerializeField] private CombatHeartHalfGraphic rightHeart;
        [SerializeField] private CanvasGroup retryButtonGroup;
        [SerializeField] private GameplayInputGate inputGate;
        [SerializeField] private AudioSource crackSource;
        [SerializeField] private Image backgroundCover;

        private UnityEngine.Object activeOwner;
        private Action pendingRetry;
        private GameplayInputToken inputToken;
        private float presentationElapsed;
        private bool crackPlayed;
        private int messageCharacters;
        private float heartMotionScale = 1f;
        private RawImage frozenBackground;
        private Texture2D ownedBackdrop;

        public bool IsShowing { get; private set; }
        public bool IsReadyToRetry { get; private set; }
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

        private void Update()
        {
            if (!IsShowing) return;
            if (activeOwner == null) { ForceHide(); return; }
            TickPresentation(Time.unscaledDeltaTime);
        }

        public bool Show(UnityEngine.Object owner, Action onRetry, CombatHeartScreenPose deathHeart = default,
            Texture2D deathBackdrop = null)
        {
            ResolveReferences();

            if (owner == null || onRetry == null)
            {
                Debug.LogError("[CombatRetryView] Retry presentation requires an owner and callback.", this);
                return false;
            }

            if (!isActiveAndEnabled || retryRoot == null || messageText == null || retryButton == null ||
                presentation == null || intactHeart == null || leftHeart == null || rightHeart == null ||
                retryButtonGroup == null || inputGate == null || crackSource == null || backgroundCover == null)
            {
                Debug.LogError(
                    "[CombatRetryView] Assign the shared presentation, Heart parts, Retry UI and input gate.",
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
            if (IsShowing)
            {
                if (deathBackdrop != null && deathBackdrop != ownedBackdrop)
                {
                    if (Application.isPlaying) Destroy(deathBackdrop);
                    else DestroyImmediate(deathBackdrop);
                }
                return true;
            }
            inputToken = inputGate.PushMode(this, GameplayInputMode.Modal);
            if (!inputToken.IsValid) { activeOwner = null; pendingRetry = null; return false; }
            presentationElapsed = 0f;
            crackPlayed = false;
            IsReadyToRetry = false;
            retryButton.interactable = false;
            retryRoot.SetActive(true);
            Canvas.ForceUpdateCanvases();
            ApplyPresentationLayout();
            SetBackdrop(deathBackdrop);
            PositionHeart(deathHeart);
            messageText.text = presentation.Message;
            messageText.maxVisibleCharacters = 0;
            messageText.ForceMeshUpdate();
            messageCharacters = messageText.textInfo.characterCount;
            IsShowing = true;
            AudioService.Instance?.SetMusicDuck(this, 0f);
            RenderPresentation();
            return true;
        }

        private void TickPresentation(float deltaTime)
        {
            if (!IsShowing || IsReadyToRetry) return;
            presentationElapsed += Mathf.Max(0f, deltaTime);
            if (!crackPlayed && presentationElapsed >= presentation.CrackDelay)
            {
                crackPlayed = true;
                AudioService audio = AudioService.Instance;
                if (audio != null && audio.TryResolveSfx(AudioId.Player_HeartBreak, out AudioClip clip, out float volume))
                    crackSource.PlayOneShot(clip, volume);
            }
            RenderPresentation();
        }

        private void RenderPresentation()
        {
            float blackout = Mathf.Clamp01((presentationElapsed - presentation.BlackoutStart) /
                presentation.BlackoutDuration);
            backgroundCover.color = new Color(0f, 0f, 0f, Mathf.SmoothStep(0f, 1f, blackout));
            bool broken = presentationElapsed >= presentation.CrackDelay;
            bool heartHidden = presentationElapsed >= presentation.HeartHiddenTime;
            intactHeart.gameObject.SetActive(!broken);
            leftHeart.gameObject.SetActive(broken && !heartHidden);
            rightHeart.gameObject.SetActive(broken && !heartHidden);
            float heartAlpha = 1f - Mathf.SmoothStep(0f, 1f, blackout);
            leftHeart.color = rightHeart.color = new Color(1f, 1f, 1f, heartAlpha);
            float split = Mathf.Clamp01((presentationElapsed - presentation.CrackDelay) / presentation.SplitDuration);
            float separation = 1f - Mathf.Pow(1f - split, 3f);
            float fall = Mathf.Clamp01((presentationElapsed - presentation.CrackDelay) /
                Mathf.Max(.01f, presentation.HeartHiddenTime - presentation.CrackDelay));
            PoseHalf(leftHeart.rectTransform, -1f, separation, fall);
            PoseHalf(rightHeart.rectTransform, 1f, separation, fall);
            if (heartHidden) ReleaseBackdrop();
            float textTime = Mathf.Max(0f, presentationElapsed - presentation.MessageStart);
            messageText.maxVisibleCharacters = Mathf.FloorToInt(textTime * presentation.CharactersPerSecond);
            bool ready = presentationElapsed >= presentation.MessageStart &&
                messageText.maxVisibleCharacters >= messageCharacters;
            retryButtonGroup.alpha = ready ? 1f : 0f;
            retryButtonGroup.blocksRaycasts = ready;
            retryButtonGroup.interactable = ready;
            retryButton.interactable = ready;
            if (ready && !IsReadyToRetry)
            {
                IsReadyToRetry = true;
                EventSystem.current?.SetSelectedGameObject(retryButton.gameObject);
            }
        }

        private void PoseHalf(RectTransform half, float side, float separation, float fall)
        {
            half.anchoredPosition = new Vector2(side * presentation.HalfSeparation * separation,
                -presentation.HalfDrop * fall * fall) * heartMotionScale;
            half.localRotation = Quaternion.Euler(0f, 0f, -side * presentation.HalfRotation * separation);
        }

        // Shared layout also reaches scenes with legacy local Retry UI copies.
        public void ApplyPresentationLayout()
        {
            ResolveReferences();
            if (retryRoot == null || messageText == null || retryButton == null) return;
            RectTransform panel = (RectTransform)retryRoot.transform;
            RectTransform message = messageText.rectTransform;
            message.anchorMin = message.anchorMax = message.pivot = Vector2.one * .5f;
            message.anchoredPosition = new Vector2(0f, 56f);
            message.sizeDelta = new Vector2(panel.rect.width > 0f ? Mathf.Min(1100f, panel.rect.width * .84f) : 1100f, 176f);
            messageText.fontSize = messageText.fontSizeMax = 52f;
            messageText.fontSizeMin = 36f;
            messageText.enableAutoSizing = true;
            messageText.alignment = TextAlignmentOptions.Center;
            RectTransform button = (RectTransform)retryButton.transform;
            button.anchorMin = button.anchorMax = button.pivot = Vector2.one * .5f;
            button.anchoredPosition = new Vector2(0f, -100f);
            button.sizeDelta = new Vector2(340f, 104f);
            TMP_Text label = retryButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.fontSize = 48f;
        }

        private void SetBackdrop(Texture2D backdrop)
        {
            ReleaseBackdrop();
            if (backdrop == null) return;
            if (frozenBackground == null)
            {
                var go = new GameObject("Frozen Combat Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                go.layer = retryRoot.layer;
                go.transform.SetParent(retryRoot.transform, false);
                go.transform.SetAsFirstSibling();
                frozenBackground = go.GetComponent<RawImage>();
                RectTransform rect = frozenBackground.rectTransform;
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                frozenBackground.raycastTarget = false;
            }
            ownedBackdrop = backdrop;
            frozenBackground.texture = backdrop;
            frozenBackground.gameObject.SetActive(true);
        }

        private void ReleaseBackdrop()
        {
            if (frozenBackground != null)
            {
                frozenBackground.texture = null;
                frozenBackground.gameObject.SetActive(false);
            }
            if (ownedBackdrop != null)
            {
                if (Application.isPlaying) Destroy(ownedBackdrop);
                else DestroyImmediate(ownedBackdrop);
                ownedBackdrop = null;
            }
        }

        private void PositionHeart(CombatHeartScreenPose pose)
        {
            RectTransform heart = (RectTransform)intactHeart.transform.parent;
            RectTransform parent = (RectTransform)heart.parent;
            heart.anchorMin = heart.anchorMax = pose.IsValid ? pose.ViewportCenter : new Vector2(.5f, .45f);
            heart.anchoredPosition = Vector2.zero;
            heart.sizeDelta = pose.IsValid ? Vector2.Scale(pose.ViewportSize, parent.rect.size) : new Vector2(80f, 80f);
            heart.localRotation = Quaternion.Euler(0f, 0f, pose.IsValid ? pose.Rotation : 0f);
            heartMotionScale = Mathf.Max(.1f, heart.sizeDelta.x / 80f);
            if (pose.IsValid) intactHeart.sprite = pose.Sprite;
            leftHeart.Configure(intactHeart.sprite, false);
            rightHeart.Configure(intactHeart.sprite, true);
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
            if (!IsShowing || !IsReadyToRetry || !retryButton.interactable)
                return;

            Action retry = pendingRetry;
            HideImmediate();
            retry?.Invoke();
        }

        private void HideImmediate()
        {
            ReleaseBackdrop();
            if (inputGate != null && inputToken.IsValid) inputGate.Release(inputToken);
            inputToken = default;
            AudioService.Instance?.ReleaseMusicOwner(this);
            if (crackSource != null) crackSource.Stop();
            if (retryButton != null && EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject == retryButton.gameObject)
                EventSystem.current.SetSelectedGameObject(null);
            IsShowing = false;
            IsReadyToRetry = false;
            activeOwner = null;
            pendingRetry = null;
            presentationElapsed = 0f;
            crackPlayed = false;
            if (backgroundCover != null) backgroundCover.color = Color.clear;

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
