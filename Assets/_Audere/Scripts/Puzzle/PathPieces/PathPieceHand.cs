using System;
using System.Collections.Generic;
using UnityEngine;

namespace Audere.Puzzle.PathPieces
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class PathPieceHand : MonoBehaviour
    {
        [Header("Prefab References")]
        [SerializeField] private RectTransform cardRoot;
        [SerializeField] private PathPieceCardUI cardPrefab;

        [Header("Layout")]
        [SerializeField] private Vector2 cardSize = new Vector2(128f, 128f);
        [SerializeField, Min(0f)] private float cardSpacing = 24f;

        private readonly List<PathPieceData> pieces = new List<PathPieceData>();
        private readonly List<PathPieceCardUI> cards = new List<PathPieceCardUI>();
        private int selectedIndex = -1;
        private bool selectionEnabled = true;
        private bool tutorialAttention;

        public bool HasPieces => pieces.Count > 0;
        public int Count => pieces.Count;
        public PathPieceData SelectedPiece =>
            selectedIndex >= 0 && selectedIndex < pieces.Count ? pieces[selectedIndex] : null;
        public bool SelectionEnabled => selectionEnabled;
        public event Action<PathPieceData> SelectionChanged;

        public void ConfigurePrefab(RectTransform root, PathPieceCardUI prefab)
        {
            cardRoot = root;
            cardPrefab = prefab;
        }

        public void Setup(IReadOnlyList<PathPieceData> initialPieces)
        {
            pieces.Clear();
            selectedIndex = -1;
            if (initialPieces != null)
            {
                int count = Mathf.Min(initialPieces.Count, PuzzleContentConstants.Hand.MaxSlots);
                for (int index = 0; index < count; index++)
                    pieces.Add(initialPieces[index]);

                if (initialPieces.Count > PuzzleContentConstants.Hand.MaxSlots)
                {
                    Debug.LogWarning(
                        $"[PathPieceHand] A hand shows at most {PuzzleContentConstants.Hand.MaxSlots} pieces. " +
                        "Extra level entries were ignored.",
                        this);
                }
            }

            RebuildCards();
        }

        public void Select(int index)
        {
            if (!selectionEnabled || index < 0 || index >= pieces.Count)
                return;

            selectedIndex = index;
            RefreshSelection();
        }

        public void ToggleSelection(int index)
        {
            if (!selectionEnabled || index < 0 || index >= pieces.Count)
                return;

            selectedIndex = selectedIndex == index ? -1 : index;
            RefreshSelection();
        }

        public void ConsumeSelected()
        {
            if (selectedIndex < 0 || selectedIndex >= pieces.Count)
                return;

            pieces.RemoveAt(selectedIndex);
            selectedIndex = -1;
            RebuildCards();
        }

        public void ClearSelection()
        {
            selectedIndex = -1;
            RefreshSelection();
        }

        /// <summary>
        /// The selected piece is reserved as soon as traversal starts. Cards stay
        /// visible, but cannot replace that in-flight path until the board returns
        /// to its next placement turn.
        /// </summary>
        public void SetSelectionEnabled(bool value)
        {
            selectionEnabled = value;
        }

        public void SetTutorialAttention(bool value)
        {
            tutorialAttention = value;
            foreach (PathPieceCardUI card in cards)
                if (card != null)
                    card.SetTutorialAttention(value);
        }

        private void RebuildCards()
        {
            if (cardRoot == null)
                cardRoot = GetComponent<RectTransform>();

            cards.Clear();
            for (int index = cardRoot.childCount - 1; index >= 0; index--)
            {
                Transform child = cardRoot.GetChild(index);
                if (cardPrefab != null && child.gameObject == cardPrefab.gameObject)
                    continue;
                Destroy(child.gameObject);
            }

            if (cardPrefab == null)
            {
                Debug.LogError("[PathPieceHand] Assign a PathPieceCardUI prefab.", this);
                return;
            }

            float totalWidth = pieces.Count * cardSize.x + Mathf.Max(0, pieces.Count - 1) * cardSpacing;
            float firstX = -totalWidth * .5f + cardSize.x * .5f;

            for (int index = 0; index < pieces.Count; index++)
            {
                PathPieceCardUI card = Instantiate(cardPrefab, cardRoot);
                card.name = $"Path Piece {index + 1:00}";
                card.gameObject.SetActive(true);
                // The hand can be rebuilt while PuzzleUI is inactive. In that case
                // the clone has not run Awake yet, so its cached RectTransform is
                // intentionally unavailable. Read the required component directly.
                RectTransform cardRect = card.GetComponent<RectTransform>();
                cardRect.anchorMin = new Vector2(.5f, .5f);
                cardRect.anchorMax = new Vector2(.5f, .5f);
                cardRect.pivot = new Vector2(.5f, .5f);
                cardRect.sizeDelta = cardSize;
                cardRect.anchoredPosition = new Vector2(firstX + index * (cardSize.x + cardSpacing), 0f);
                card.Bind(this, pieces[index], index);
                card.SetTutorialAttention(tutorialAttention);
                cards.Add(card);
            }

            RefreshSelection();
        }

        private void RefreshSelection()
        {
            for (int index = 0; index < cards.Count; index++)
                if (cards[index] != null)
                    cards[index].SetSelected(index == selectedIndex);

            SelectionChanged?.Invoke(SelectedPiece);
        }
    }
}
