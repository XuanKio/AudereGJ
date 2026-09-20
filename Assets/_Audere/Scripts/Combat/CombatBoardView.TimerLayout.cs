using UnityEngine;

namespace Audere.Combat
{
    public sealed partial class CombatBoardView
    {
        private bool timerInsetCaptured;
        private float timerBoardInset;
        private const float GuidedTimerGap = 12f;

        private void CaptureTimerBoardInset()
        {
            if (timerInsetCaptured || battleBoxFrame == null || playArea == null) return;
            float fieldWidth = Mathf.Abs(battleBoxFrame.InverseTransformVector(
                playArea.TransformVector(Vector3.right * playArea.rect.width)).x);
            timerBoardInset = Mathf.Max(0f, (battleBoxFrame.rect.width - fieldWidth) * .5f);
            timerInsetCaptured = true;
        }

        private float TimerHeightInFrame => TimerFocusTarget != null && battleBoxFrame != null
            ? Mathf.Abs(battleBoxFrame.InverseTransformVector(TimerFocusTarget.TransformVector(
                Vector3.up * TimerFocusTarget.rect.height)).y) : 0f;

        /// <summary>TIME belongs to the visible board, including its bottom gutter.</summary>
        public void SyncTimerToBoard()
        {
            CaptureTimerBoardInset();
            RectTransform track = TimerFocusTarget;
            if (battleBoxFrame == null || playArea == null || track == null || track.parent == null) return;
            float split = Mathf.Abs(battleBoxFrame.InverseTransformVector(
                playArea.TransformVector(Vector3.right * boardSeparation)).x);
            Rect bounds = battleBoxFrame.rect;
            float width = Mathf.Max(1f, bounds.width - timerBoardInset * 2f + split * 2f);
            float localWidth = Mathf.Abs(track.InverseTransformVector(
                battleBoxFrame.TransformVector(Vector3.right * width)).x);
            track.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, localWidth);
            Vector3 center = battleBoxFrame.TransformPoint(new Vector2(bounds.center.x,
                bounds.yMin + timerBoardInset + TimerHeightInFrame * .5f));
            track.position += center - track.TransformPoint(track.rect.center);
        }
    }
}
