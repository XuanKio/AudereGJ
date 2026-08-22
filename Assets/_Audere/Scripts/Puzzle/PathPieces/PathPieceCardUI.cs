using System.Collections.Generic;
using Audere.Dialogue;
using Audere.GameplayInput;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Audere.Puzzle.PathPieces
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class PathPieceCardUI : MonoBehaviour,
        IPointerClickHandler,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        [Header("Prefab References")]
        [SerializeField] private RectTransform slotMotionRoot;
        [SerializeField] private Image slotFrame;
        [SerializeField] private RectTransform shapeRoot;
        [SerializeField] private Image middleNodeTemplate;
        [SerializeField] private Image endpointNodeTemplate;

        [Header("Shape")]
        [SerializeField, Min(4f)] private float endpointNodeSize = 10f;
        [SerializeField, Min(2f)] private float middleNodeSize = 7f;
        [SerializeField, Min(4f)] private float nodeSpacing = 18f;

        [Header("Motion")]
        [SerializeField, Min(1f)] private float motionSharpness = 18f;
        [SerializeField, Min(0f)] private float hoverLift = 9f;
        [SerializeField, Min(0f)] private float selectedLift = 18f;
        [SerializeField, Range(1f, 1.1f)] private float selectedScale = 1.025f;

        private readonly List<Image> nodes = new List<Image>();
        private PathPieceHand owner;
        private int pieceIndex;
        private bool selected;
        private bool hovered;
        private Vector2 homePosition;
        private Vector2 slotHomePosition;
        private float selectionBlend;
        private float hoverBlend;
        private bool tutorialAttention;
        private GameplayInputGate inputGate;

        public RectTransform RectTransform { get; private set; }

        private void Awake()
        {
            RectTransform = GetComponent<RectTransform>();
            GameplayUIRoot uiRoot = GameplayUIRoot.Instance;
            inputGate = uiRoot != null ? uiRoot.InputGate : null;
            ApplyStaticVisuals();
        }

        private void OnValidate()
        {
            RectTransform = GetComponent<RectTransform>();
            ApplyStaticVisuals();
        }

        public void ConfigurePrefab(
            RectTransform motionRoot,
            Image slotImage,
            RectTransform root,
            Image middlePrototype,
            Image endpointPrototype)
        {
            slotMotionRoot = motionRoot;
            slotFrame = slotImage;
            shapeRoot = root;
            middleNodeTemplate = middlePrototype;
            endpointNodeTemplate = endpointPrototype;
            ApplyStaticVisuals();
        }

        public void Bind(PathPieceHand pieceOwner, PathPieceData piece, int index)
        {
            owner = pieceOwner;
            pieceIndex = index;
            if (RectTransform == null) RectTransform = GetComponent<RectTransform>();
            homePosition = RectTransform.anchoredPosition;
            slotHomePosition = slotMotionRoot != null
                ? slotMotionRoot.anchoredPosition
                : Vector2.zero;
            selected = false;
            hovered = false;
            selectionBlend = 0f;
            hoverBlend = 0f;
            ResetMotionVisuals();
            BuildShape(piece);
        }

        public void SetSelected(bool value)
        {
            selected = value;
        }

        public void SetTutorialAttention(bool value)
        {
            tutorialAttention = value;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!HasPuzzleInput())
                return;

            owner?.ToggleSelection(pieceIndex);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!HasPuzzleInput())
                return;

            hovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
        }

        private void LateUpdate()
        {
            if (RectTransform == null)
                RectTransform = GetComponent<RectTransform>();

            float deltaTime = Time.unscaledDeltaTime;
            float blend = 1f - Mathf.Exp(-motionSharpness * deltaTime);
            selectionBlend = Mathf.Lerp(selectionBlend, selected ? 1f : 0f, blend);
            hoverBlend = Mathf.Lerp(hoverBlend, hovered ? 1f : 0f, blend);
            if (Mathf.Abs(selectionBlend - (selected ? 1f : 0f)) < .001f)
                selectionBlend = selected ? 1f : 0f;
            if (Mathf.Abs(hoverBlend - (hovered ? 1f : 0f)) < .001f)
                hoverBlend = hovered ? 1f : 0f;

            float selectedAmount = EaseOutCubic(selectionBlend);
            float hoverAmount = EaseOutCubic(hoverBlend) * (1f - selectedAmount);
            float attentionAmount = tutorialAttention && !selected
                ? .5f + .5f * Mathf.Sin(Time.unscaledTime * 4.5f)
                : 0f;
            float lift = selectedLift * selectedAmount + hoverLift * hoverAmount +
                attentionAmount * 5f;
            Vector2 targetPosition = homePosition + Vector2.up * lift;
            float attentionScale = 1f + attentionAmount * .025f;
            Vector3 targetScale = Vector3.one *
                (Mathf.Lerp(1f, selectedScale, selectedAmount) * attentionScale);
            RectTransform.anchoredPosition = Vector2.Lerp(RectTransform.anchoredPosition, targetPosition, blend);
            RectTransform.localScale = Vector3.Lerp(RectTransform.localScale, targetScale, blend);

            if (slotMotionRoot != null)
            {
                slotMotionRoot.anchoredPosition = slotHomePosition;
                slotMotionRoot.localRotation = Quaternion.identity;
            }
        }

        private void ResetMotionVisuals()
        {
            if (RectTransform != null)
            {
                RectTransform.anchoredPosition = homePosition;
                RectTransform.localScale = Vector3.one;
            }

            if (slotMotionRoot != null)
            {
                slotMotionRoot.anchoredPosition = slotHomePosition;
                slotMotionRoot.localRotation = Quaternion.identity;
            }
        }

        private static float EaseOutCubic(float value)
        {
            float inverse = 1f - Mathf.Clamp01(value);
            return 1f - inverse * inverse * inverse;
        }

        private bool HasPuzzleInput()
        {
            if (inputGate == null)
            {
                GameplayUIRoot uiRoot = GameplayUIRoot.Instance;
                inputGate = uiRoot != null ? uiRoot.InputGate : null;
            }

            return inputGate != null && inputGate.Allows(GameplayInputMode.Puzzle);
        }

        private void BuildShape(PathPieceData piece)
        {
            foreach (Image node in nodes)
                if (node != null) Destroy(node.gameObject);
            nodes.Clear();

            if (piece == null || middleNodeTemplate == null || shapeRoot == null)
                return;

            IReadOnlyList<Vector2Int> path = piece.OrderedLocalPath;
            Vector2 center = Vector2.zero;
            foreach (Vector2Int coordinate in path)
                center += coordinate;
            center /= path.Count;

            for (int index = 0; index < path.Count; index++)
            {
                bool endpoint = index == 0 || index == path.Count - 1;
                Image template = endpoint && endpointNodeTemplate != null
                    ? endpointNodeTemplate
                    : middleNodeTemplate;
                Image node = Instantiate(template, shapeRoot);
                node.name = endpoint ? $"Endpoint {index}" : $"Middle {index}";
                node.gameObject.SetActive(true);
                node.raycastTarget = false;
                node.rectTransform.anchoredPosition = ((Vector2)path[index] - center) * nodeSpacing;
                node.rectTransform.sizeDelta = Vector2.one * (endpoint ? endpointNodeSize : middleNodeSize);
                nodes.Add(node);
            }
        }

        private void ApplyStaticVisuals()
        {
            if (slotFrame != null)
                slotFrame.raycastTarget = true;
            if (middleNodeTemplate != null)
                middleNodeTemplate.raycastTarget = false;
            if (endpointNodeTemplate != null)
                endpointNodeTemplate.raycastTarget = false;
        }
    }
}
