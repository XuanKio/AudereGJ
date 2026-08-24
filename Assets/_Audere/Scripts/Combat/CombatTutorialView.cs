using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class CombatTutorialView : MonoBehaviour
    {
        [Header("Shared Presentation")]
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform content;
        [SerializeField] private TMP_Text instructionText;
        [SerializeField, Min(.01f)] private float fadeDuration = .12f;
        [SerializeField, Min(0f)] private float verticalTravel = 8f;

        [Header("Spotlight Cutout")]
        [SerializeField] private RectTransform spotlightRoot;
        [SerializeField] private Image dimTop;
        [SerializeField] private Image dimBottom;
        [SerializeField] private Image dimLeft;
        [SerializeField] private Image dimRight;
        [SerializeField, Range(0f, 1f)] private float dimOpacity = .76f;
        [SerializeField, Min(0f)] private float focusPadding = 18f;

        [Header("Dice Showcase")]
        [SerializeField] private RectTransform diceShowcaseRoot;
        [SerializeField] private CombatDieView attackDicePrefab;
        [SerializeField] private CombatDieView shieldDicePrefab;
        [SerializeField] private CombatDieView healDicePrefab;

        private Coroutine presentationRoutine;
        private RectTransform rootRect;
        private RectTransform focusTarget;
        private CombatTutorialFocus currentFocus;
        private CombatSymbol currentShowcasedSymbol;
        private CombatDieView attackPreview;
        private CombatDieView shieldPreview;
        private CombatDieView healPreview;
        private Vector2 authoredPosition;
        private bool capturedPosition;
        private int presentationVersion;

        public bool IsVisible => group != null && group.alpha > .001f;
        public string CurrentInstruction => instructionText != null ? instructionText.text : string.Empty;
        public CombatTutorialFocus CurrentFocus => currentFocus;

        private void Awake()
        {
            ResolveReferences();
            CapturePosition();
            BuildDicePreviews();
            ForceHide();
        }

        private void LateUpdate()
        {
            if (!IsVisible || focusTarget == null ||
                (currentFocus != CombatTutorialFocus.Time && currentFocus != CombatTutorialFocus.StunZone))
                return;
            UpdateSpotlightCutout();
        }

        private void OnDisable()
        {
            ForceHide();
        }

        public void ShowInstruction(
            string value,
            float visibleDuration = 0f,
            CombatTutorialFocus focus = CombatTutorialFocus.None,
            CombatSymbol showcasedSymbol = CombatSymbol.Attack,
            RectTransform target = null)
        {
            ResolveReferences();
            CapturePosition();
            BuildDicePreviews();
            presentationVersion++;
            StopPresentation();

            if (string.IsNullOrWhiteSpace(value))
            {
                ForceHide();
                return;
            }

            instructionText.text = value.Trim();
            gameObject.SetActive(true);
            ConfigureFocus(focus, showcasedSymbol, target);

            // The host stays active and hides through CanvasGroup. A disabled parent
            // can still make this component inactive, so never start a coroutine then.
            if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
                return;

            // Tutorial instructions are dismissed by the combat owner after the
            // player's next interaction. Keep the duration argument for existing
            // authored call sites, but never auto-hide here.
            _ = visibleDuration;
            presentationRoutine = StartCoroutine(Present(presentationVersion));
        }

        public void ForceHide()
        {
            presentationVersion++;
            StopPresentation();
            HideVisuals();
        }

        private void ConfigureFocus(
            CombatTutorialFocus focus,
            CombatSymbol showcasedSymbol,
            RectTransform target)
        {
            currentFocus = focus;
            currentShowcasedSymbol = showcasedSymbol;
            focusTarget = target;

            bool hasSpotlight = focus != CombatTutorialFocus.None;
            if (spotlightRoot != null)
                spotlightRoot.gameObject.SetActive(hasSpotlight);
            if (diceShowcaseRoot != null)
                diceShowcaseRoot.gameObject.SetActive(
                    focus == CombatTutorialFocus.Dice || focus == CombatTutorialFocus.DiceAll);

            bool showAllDice = focus == CombatTutorialFocus.DiceAll;
            SetPreviewPose(attackPreview,
                showAllDice || focus == CombatTutorialFocus.Dice && showcasedSymbol == CombatSymbol.Attack,
                showAllDice ? -130f : 0f,
                showAllDice ? 1.15f : 1.5f);
            SetPreviewPose(shieldPreview,
                showAllDice || focus == CombatTutorialFocus.Dice && showcasedSymbol == CombatSymbol.Shield,
                0f,
                showAllDice ? 1.15f : 1.5f);
            SetPreviewPose(healPreview,
                showAllDice || focus == CombatTutorialFocus.Dice && showcasedSymbol == CombatSymbol.Heal,
                showAllDice ? 130f : 0f,
                showAllDice ? 1.15f : 1.5f);

            if (!hasSpotlight)
                return;
            ApplyDimColors();
            if (focus == CombatTutorialFocus.Dice || focus == CombatTutorialFocus.DiceAll || target == null)
                ApplyFullDim();
            else
                UpdateSpotlightCutout();
        }

        private void HideVisuals()
        {
            ResolveReferences();
            CapturePosition();
            if (group != null)
            {
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }
            if (content != null)
            {
                content.anchoredPosition = authoredPosition;
                content.localScale = Vector3.one;
            }
            if (instructionText != null)
                instructionText.text = string.Empty;
            ConfigureFocus(CombatTutorialFocus.None, currentShowcasedSymbol, null);
        }

        private IEnumerator Present(int version)
        {
            group.interactable = false;
            group.blocksRaycasts = false;
            float elapsed = 0f;
            while (elapsed < fadeDuration && version == presentationVersion)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Smooth(elapsed / fadeDuration);
                group.alpha = t;
                content.anchoredPosition = authoredPosition + Vector2.down * verticalTravel * (1f - t);
                content.localScale = Vector3.one * Mathf.Lerp(.98f, 1f, t);
                yield return null;
            }

            if (version != presentationVersion)
                yield break;

            group.alpha = 1f;
            content.anchoredPosition = authoredPosition;
            content.localScale = Vector3.one;
            presentationRoutine = null;
        }

        private void UpdateSpotlightCutout()
        {
            if (rootRect == null || focusTarget == null)
            {
                ApplyFullDim();
                return;
            }

            Canvas sourceCanvas = focusTarget.GetComponentInParent<Canvas>();
            Camera sourceCamera = sourceCanvas != null && sourceCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? sourceCanvas.worldCamera != null ? sourceCanvas.worldCamera : Camera.main
                : null;
            Canvas overlayCanvas = GetComponentInParent<Canvas>();
            Camera overlayCamera = overlayCanvas != null && overlayCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? overlayCanvas.worldCamera
                : null;

            Vector3[] corners = new Vector3[4];
            focusTarget.GetWorldCorners(corners);
            Vector2 minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            for (int i = 0; i < corners.Length; i++)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(sourceCamera, corners[i]);
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRect, screen, overlayCamera, out Vector2 local))
                {
                    ApplyFullDim();
                    return;
                }
                minimum = Vector2.Min(minimum, local);
                maximum = Vector2.Max(maximum, local);
            }

            Rect rootBounds = rootRect.rect;
            minimum -= Vector2.one * focusPadding;
            maximum += Vector2.one * focusPadding;
            minimum.x = Mathf.Clamp(minimum.x, rootBounds.xMin, rootBounds.xMax);
            minimum.y = Mathf.Clamp(minimum.y, rootBounds.yMin, rootBounds.yMax);
            maximum.x = Mathf.Clamp(maximum.x, rootBounds.xMin, rootBounds.xMax);
            maximum.y = Mathf.Clamp(maximum.y, rootBounds.yMin, rootBounds.yMax);

            SetDimRect(dimLeft, rootBounds.xMin, minimum.x, rootBounds.yMin, rootBounds.yMax);
            SetDimRect(dimRight, maximum.x, rootBounds.xMax, rootBounds.yMin, rootBounds.yMax);
            SetDimRect(dimBottom, minimum.x, maximum.x, rootBounds.yMin, minimum.y);
            SetDimRect(dimTop, minimum.x, maximum.x, maximum.y, rootBounds.yMax);
        }

        private void ApplyFullDim()
        {
            if (rootRect == null)
                return;
            Rect bounds = rootRect.rect;
            SetDimRect(dimTop, bounds.xMin, bounds.xMax, bounds.yMin, bounds.yMax);
            SetDimRect(dimBottom, 0f, 0f, 0f, 0f);
            SetDimRect(dimLeft, 0f, 0f, 0f, 0f);
            SetDimRect(dimRight, 0f, 0f, 0f, 0f);
        }

        private void ApplyDimColors()
        {
            SetDimColor(dimTop);
            SetDimColor(dimBottom);
            SetDimColor(dimLeft);
            SetDimColor(dimRight);
        }

        private void SetDimColor(Image image)
        {
            if (image == null)
                return;
            image.color = new Color(0f, 0f, 0f, dimOpacity);
            image.raycastTarget = false;
        }

        private static void SetDimRect(Image image, float xMin, float xMax, float yMin, float yMax)
        {
            if (image == null)
                return;
            bool visible = xMax - xMin > .5f && yMax - yMin > .5f;
            image.gameObject.SetActive(visible);
            if (!visible)
                return;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2((xMin + xMax) * .5f, (yMin + yMax) * .5f);
            rect.sizeDelta = new Vector2(xMax - xMin, yMax - yMin);
        }

        private void BuildDicePreviews()
        {
            if (diceShowcaseRoot == null || attackPreview != null || shieldPreview != null || healPreview != null)
                return;
            attackPreview = CreatePreview(attackDicePrefab, "Attack Dice Preview");
            shieldPreview = CreatePreview(shieldDicePrefab, "Shield Dice Preview");
            healPreview = CreatePreview(healDicePrefab, "Heal Dice Preview");
        }

        private CombatDieView CreatePreview(CombatDieView prefab, string previewName)
        {
            if (prefab == null)
                return null;
            CombatDieView preview = Instantiate(prefab, diceShowcaseRoot);
            preview.name = previewName;
            preview.enabled = false;
            RectTransform rect = preview.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one * 1.5f;
            CanvasGroup previewGroup = preview.GetComponent<CanvasGroup>();
            if (previewGroup != null)
            {
                previewGroup.alpha = 1f;
                previewGroup.interactable = false;
                previewGroup.blocksRaycasts = false;
            }
            Graphic[] graphics = preview.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
                graphics[i].raycastTarget = false;
            preview.gameObject.SetActive(false);
            return preview;
        }

        private static void SetPreviewVisible(CombatDieView preview, bool visible)
        {
            if (preview != null && preview.gameObject.activeSelf != visible)
                preview.gameObject.SetActive(visible);
        }

        private static void SetPreviewPose(CombatDieView preview, bool visible, float x, float scale)
        {
            if (preview == null)
                return;
            RectTransform rect = preview.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.localScale = Vector3.one * scale;
            SetPreviewVisible(preview, visible);
        }

        private void ResolveReferences()
        {
            if (rootRect == null) rootRect = GetComponent<RectTransform>();
            if (group == null) group = GetComponent<CanvasGroup>();
            if (content == null) content = transform.Find("Tutorial Instruction") as RectTransform;
            if (instructionText == null && content != null)
                instructionText = content.GetComponent<TMP_Text>();
            if (spotlightRoot == null) spotlightRoot = transform.Find("Spotlight") as RectTransform;
            if (spotlightRoot != null)
            {
                if (dimTop == null) dimTop = spotlightRoot.Find("Dim Top")?.GetComponent<Image>();
                if (dimBottom == null) dimBottom = spotlightRoot.Find("Dim Bottom")?.GetComponent<Image>();
                if (dimLeft == null) dimLeft = spotlightRoot.Find("Dim Left")?.GetComponent<Image>();
                if (dimRight == null) dimRight = spotlightRoot.Find("Dim Right")?.GetComponent<Image>();
            }
            if (diceShowcaseRoot == null) diceShowcaseRoot = transform.Find("Dice Showcase") as RectTransform;
        }

        private void CapturePosition()
        {
            if (capturedPosition || content == null)
                return;
            authoredPosition = content.anchoredPosition;
            capturedPosition = true;
        }

        private void StopPresentation()
        {
            if (presentationRoutine == null)
                return;
            StopCoroutine(presentationRoutine);
            presentationRoutine = null;
        }

        private static float Smooth(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }
    }
}
