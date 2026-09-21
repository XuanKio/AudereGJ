using UnityEngine;

namespace Audere.Combat
{
    public sealed partial class CombatBoardView
    {
        private bool compactFrameCaptured;
        private Vector2 compactFrameMargin;

        public RectTransform EnemyPresentationRoot => enemyPresentationRoot;

        // Keeps the authored frame margin around a smaller two-dimensional dice field.
        public void SetBattleBoxSizeLayout(float widthFraction, float heightFraction, float normalizedX)
        {
            CaptureBattleBoxLayout();
            if (playArea == null) return;
            if (!compactFrameCaptured && battleBoxFrame != null)
            {
                compactFrameMargin = battleBoxFrame.sizeDelta - playArea.sizeDelta;
                compactFrameCaptured = true;
            }
            SetBattleBoxHorizontalLayout(widthFraction, normalizedX);
            float height = battleBoxAuthoredSize.y * Mathf.Clamp(heightFraction, .25f, 1f);
            playArea.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            if (airborneDiceRoot != null)
                airborneDiceRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                    airborneDiceAuthoredSize.y + height - battleBoxAuthoredSize.y);
            if (battleBoxFrame != null && compactFrameCaptured)
                battleBoxFrame.sizeDelta = playArea.sizeDelta + compactFrameMargin;
            if (catchCursor != null)
            {
                Rect bounds = playArea.rect;
                Vector2 half = catchCursor.rect.size * .5f;
                Vector2 position = catchCursor.anchoredPosition;
                catchCursor.anchoredPosition = new Vector2(
                    Mathf.Clamp(position.x, bounds.xMin + half.x, bounds.xMax - half.x),
                    Mathf.Clamp(position.y, bounds.yMin + half.y, bounds.yMax - half.y));
            }
            SyncTimerToBoard();
        }
    }
}
