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
        private bool guidedInstructionVisible;
        private bool guidedContentWasActive;
        private RectTransform guidedBoardTarget;
        private RectTransform guidedTimerTarget;
        private RectTransform guidedPanel;
        private RectTransform guidedFocusOutline;
        private TMP_Text guidedInstructionText;
        private TMP_Text guidedProgressText;
        private Image guidedAccent;
        private readonly Image[] guidedFocusEdges = new Image[4];
        private readonly Vector3[] guidedWorldCorners = new Vector3[4];
        private bool guidedSuccess;

        public bool IsVisible => group != null && group.alpha > .001f;
        public string CurrentInstruction => guidedInstructionVisible && guidedInstructionText != null
            ? guidedInstructionText.text : instructionText != null ? instructionText.text : string.Empty;
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
            if (guidedInstructionVisible)
            {
                UpdateGuidedLayout();
                return;
            }
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
            HideGuidedVisuals();

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

        public void SetGuidedBoardTarget(RectTransform target)
        {
            guidedBoardTarget = target;
            CombatBoardView board = target != null ? target.GetComponentInParent<CombatBoardView>() : null;
            guidedTimerTarget = board != null ? board.TimerFocusTarget : null;
            if (guidedInstructionVisible)
                UpdateGuidedLayout();
        }

        /// <summary>Persistent action hint. Does not claim input or dim the practice board.</summary>
        public void ShowGuidedInstruction(string text, int step, int total, RectTransform focusTarget = null)
        {
            ResolveReferences();
            CapturePosition();
            presentationVersion++;
            StopPresentation();
            if (string.IsNullOrWhiteSpace(text))
            {
                ForceHide();
                return;
            }
            BuildGuidedVisuals();
            gameObject.SetActive(true);
            ConfigureFocus(CombatTutorialFocus.None, currentShowcasedSymbol, null);
            this.focusTarget = focusTarget;
            if (guidedBoardTarget == null && focusTarget != null)
            {
                CombatBoardView board = focusTarget.GetComponentInParent<CombatBoardView>();
                if (board != null)
                    SetGuidedBoardTarget(board.PlayArea);
            }
            if (content != null)
            {
                if (!guidedInstructionVisible)
                    guidedContentWasActive = content.gameObject.activeSelf;
                content.gameObject.SetActive(false);
            }
            guidedInstructionVisible = true;
            guidedSuccess = false;
            guidedInstructionText.text = text.Trim();
            guidedInstructionText.fontStyle &= ~FontStyles.Strikethrough;
            guidedProgressText.text = Mathf.Clamp(step, 1, Mathf.Max(1, total)).ToString("00") +
                " / " + Mathf.Max(1, total).ToString("00");
            guidedPanel.gameObject.SetActive(true);
            group.interactable = false;
            group.blocksRaycasts = false;
            group.alpha = 1f;
            UpdateGuidedLayout();
        }

        /// <summary>Brief visual acknowledgement while the owner decides when to advance.</summary>
        public void ShowGuidedSuccess()
        {
            if (!guidedInstructionVisible)
                return;
            guidedSuccess = true;
            guidedInstructionText.fontStyle |= FontStyles.Strikethrough;
            UpdateGuidedLayout();
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
            HideGuidedVisuals();
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

        private void HideGuidedVisuals()
        {
            if (guidedInstructionVisible && content != null)
                content.gameObject.SetActive(guidedContentWasActive);
            guidedInstructionVisible = false;
            guidedSuccess = false;
            if (guidedPanel != null)
                guidedPanel.gameObject.SetActive(false);
            if (guidedFocusOutline != null)
                guidedFocusOutline.gameObject.SetActive(false);
        }

        private void BuildGuidedVisuals()
        {
            if (guidedPanel != null)
                return;
            guidedPanel = CreateGuidedRect("Guided Instruction (runtime)", transform);
            Image background = guidedPanel.gameObject.AddComponent<Image>();
            background.color = new Color(.045f, .035f, .075f, .96f);
            background.raycastTarget = false;

            RectTransform accentRect = CreateGuidedRect("Progress Accent", guidedPanel);
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.pivot = new Vector2(0f, .5f);
            accentRect.sizeDelta = new Vector2(3f, 0f);
            guidedAccent = accentRect.gameObject.AddComponent<Image>();
            guidedAccent.raycastTarget = false;

            guidedProgressText = CreateGuidedText("Progress", guidedPanel, 20f);
            RectTransform progress = guidedProgressText.rectTransform;
            progress.anchorMin = progress.anchorMax = new Vector2(0f, 1f);
            progress.pivot = new Vector2(0f, 1f);
            progress.anchoredPosition = new Vector2(22f, -12f);
            progress.sizeDelta = new Vector2(130f, 28f);
            guidedProgressText.alignment = TextAlignmentOptions.TopLeft;
            guidedProgressText.color = new Color(.73f, .80f, .85f, 1f);

            guidedInstructionText = CreateGuidedText("Action", guidedPanel, 28f);
            RectTransform action = guidedInstructionText.rectTransform;
            action.anchorMin = Vector2.zero;
            action.anchorMax = Vector2.one;
            action.offsetMin = new Vector2(22f, 12f);
            action.offsetMax = new Vector2(-22f, -43f);
            guidedInstructionText.alignment = TextAlignmentOptions.MidlineLeft;
            guidedInstructionText.enableAutoSizing = true;
            guidedInstructionText.fontSizeMin = 23f;
            guidedInstructionText.fontSizeMax = 28f;
            guidedInstructionText.textWrappingMode = TextWrappingModes.Normal;
            guidedInstructionText.overflowMode = TextOverflowModes.Overflow;

            guidedFocusOutline = CreateGuidedRect("Guided Focus (runtime)", transform);
            guidedFocusOutline.SetAsFirstSibling();
            for (int i = 0; i < guidedFocusEdges.Length; i++)
            {
                RectTransform edge = CreateGuidedRect("Edge " + i, guidedFocusOutline);
                guidedFocusEdges[i] = edge.gameObject.AddComponent<Image>();
                guidedFocusEdges[i].raycastTarget = false;
            }
        }

        private static RectTransform CreateGuidedRect(string objectName, Transform parent)
        {
            RectTransform rect = new GameObject(objectName, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            return rect;
        }

        private TMP_Text CreateGuidedText(string objectName, Transform parent, float fontSize)
        {
            RectTransform rect = CreateGuidedRect(objectName, parent);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (instructionText != null)
            {
                text.font = instructionText.font;
                text.fontSharedMaterial = instructionText.fontSharedMaterial;
            }
            text.fontSize = fontSize;
            text.color = new Color(.96f, .95f, .98f, 1f);
            text.raycastTarget = false;
            return text;
        }

        private void UpdateGuidedLayout()
        {
            if (rootRect == null || guidedPanel == null)
                return;
            Rect bounds = rootRect.rect;
            float width = Mathf.Min(680f, Mathf.Max(240f, bounds.width - 48f));
            float height = 160f;
            guidedPanel.sizeDelta = new Vector2(width, height);
            Vector2 position = new Vector2(bounds.center.x, bounds.yMin + 24f + height * .5f);
            if (TryGetGuidedBounds(guidedBoardTarget, out Rect boardBounds))
            {
                float bottom = boardBounds.yMin;
                // Reserve TIME's slot before it is revealed so the instruction does not jump sides.
                if (guidedTimerTarget != null &&
                    TryGetGuidedBounds(guidedTimerTarget, out Rect timerBounds))
                    bottom = Mathf.Min(bottom, timerBounds.yMin);
                position = new Vector2(boardBounds.center.x, bottom - 24f - height * .5f);
                // Keep the lesson clear of TIME when a short viewport has no room underneath.
                if (position.y - height * .5f < bounds.yMin + 24f)
                {
                    float rightSpace = bounds.xMax - boardBounds.xMax - 48f;
                    float leftSpace = boardBounds.xMin - bounds.xMin - 48f;
                    bool right = rightSpace >= leftSpace;
                    float sideWidth = Mathf.Min(width, right ? rightSpace : leftSpace);
                    if (sideWidth >= 460f)
                    {
                        width = sideWidth;
                        height = Mathf.Max(height, guidedInstructionText.GetPreferredValues(
                            guidedInstructionText.text, width - 44f, 0f).y + 55f);
                        guidedPanel.sizeDelta = new Vector2(width, height);
                        position = new Vector2(right ? boardBounds.xMax + 24f + width * .5f
                            : boardBounds.xMin - 24f - width * .5f, boardBounds.center.y);
                    }
                }
            }
            position.x = Mathf.Clamp(position.x, bounds.xMin + 24f + width * .5f, bounds.xMax - 24f - width * .5f);
            position.y = Mathf.Clamp(position.y, bounds.yMin + 24f + height * .5f, bounds.yMax - 24f - height * .5f);
            guidedPanel.anchoredPosition = position - bounds.center;

            bool success = guidedSuccess;
            Color accent = success ? new Color(.57f, 1f, .73f, 1f) : new Color(.74f, .84f, 1f, .92f);
            guidedAccent.color = accent;
            guidedProgressText.color = success ? accent : new Color(.73f, .80f, .85f, 1f);
            Rect focusBounds = default;
            bool showFocus = focusTarget != null && focusTarget.gameObject.activeInHierarchy &&
                TryGetGuidedBounds(focusTarget, out focusBounds);
            guidedFocusOutline.gameObject.SetActive(showFocus);
            if (!showFocus)
                return;
            float pad = 7f;
            float xMin = Mathf.Max(bounds.xMin + 4f, focusBounds.xMin - pad);
            float xMax = Mathf.Min(bounds.xMax - 4f, focusBounds.xMax + pad);
            float yMin = Mathf.Max(bounds.yMin + 4f, focusBounds.yMin - pad);
            float yMax = Mathf.Min(bounds.yMax - 4f, focusBounds.yMax + pad);
            guidedFocusOutline.anchoredPosition = new Vector2((xMin + xMax) * .5f, (yMin + yMax) * .5f) - bounds.center;
            guidedFocusOutline.sizeDelta = new Vector2(Mathf.Max(1f, xMax - xMin), Mathf.Max(1f, yMax - yMin));
            Vector2 size = guidedFocusOutline.sizeDelta;
            SetGuidedEdge(0, new Vector2(0f, size.y * .5f), new Vector2(size.x, 2f));
            SetGuidedEdge(1, new Vector2(0f, -size.y * .5f), new Vector2(size.x, 2f));
            SetGuidedEdge(2, new Vector2(-size.x * .5f, 0f), new Vector2(2f, size.y));
            SetGuidedEdge(3, new Vector2(size.x * .5f, 0f), new Vector2(2f, size.y));
            accent.a *= .70f + .15f * Mathf.Sin(Time.unscaledTime * 4f);
            for (int i = 0; i < guidedFocusEdges.Length; i++)
                guidedFocusEdges[i].color = accent;
        }

        private void SetGuidedEdge(int index, Vector2 position, Vector2 size)
        {
            RectTransform rect = guidedFocusEdges[index].rectTransform;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private bool TryGetGuidedBounds(RectTransform target, out Rect bounds)
        {
            bounds = default;
            if (target == null || rootRect == null)
                return false;
            Canvas sourceCanvas = target.GetComponentInParent<Canvas>();
            Camera sourceCamera = sourceCanvas != null && sourceCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? sourceCanvas.worldCamera != null ? sourceCanvas.worldCamera : Camera.main : null;
            Canvas overlayCanvas = GetComponentInParent<Canvas>();
            Camera overlayCamera = overlayCanvas != null && overlayCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? overlayCanvas.worldCamera : null;
            target.GetWorldCorners(guidedWorldCorners);
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            for (int i = 0; i < guidedWorldCorners.Length; i++)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(sourceCamera, guidedWorldCorners[i]);
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRect, screen, overlayCamera, out Vector2 local))
                    return false;
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }
            bounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return true;
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
