using UnityEngine;

namespace Audere.Combat
{
    public sealed partial class CombatBoardView
    {
        private bool longCorridorActive;
        private float corridorLeft, corridorRight;
        private bool corridorRunnerActive;
        private float corridorRunnerX;
        private Vector2 corridorFrameHome;
        private bool corridorFrameCaptured;
        public bool IsCorridorRunnerActive => corridorRunnerActive;
        public Rect CorridorVisibleRect => longCorridorActive
            ? Rect.MinMaxRect(corridorLeft, playArea.rect.yMin, corridorRight, playArea.rect.yMax)
            : playArea.rect;

        public void SetLongCorridorLayout(float blend, float heightFraction)
        {
            Vector3 cursorWorld = catchCursor != null ? catchCursor.position : Vector3.zero;
            if(!corridorFrameCaptured && battleBoxFrame!=null)
            {corridorFrameHome=battleBoxFrame.anchoredPosition;corridorFrameCaptured=true;}
            SetBattleBoxSizeLayout(1f, heightFraction, 0f);
            var canvas = playArea.GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            float half = battleBoxAuthoredSize.x * .95f;
            float viewportHeight=battleBoxAuthoredSize.y*2.5f;
            corridorLeft = -half; corridorRight = half;
            if (camera != null && camera.pixelWidth > 0)
            {
                Vector2 left, right;
                Rect viewport = camera.pixelRect;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(playArea,
                        new Vector2(viewport.xMin, viewport.center.y), camera, out left) &&
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(playArea,
                        new Vector2(viewport.xMax, viewport.center.y), camera, out right))
                { corridorLeft = left.x; corridorRight = right.x; }
                Vector2 bottom,top;
                if(RectTransformUtility.ScreenPointToLocalPointInRectangle(playArea,new Vector2(viewport.center.x,viewport.yMin),camera,out bottom) &&
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(playArea,new Vector2(viewport.center.x,viewport.yMax),camera,out top))
                    viewportHeight=top.y-bottom.y;
            }
            float descent=viewportHeight*.22f*Mathf.Clamp01(blend);
            playArea.anchoredPosition=battleBoxAuthoredPosition+Vector2.down*descent;
            if(airborneDiceRoot!=null)airborneDiceRoot.anchoredPosition=airborneDiceAuthoredPosition+Vector2.down*descent;
            if(battleBoxFrame!=null)battleBoxFrame.anchoredPosition=corridorFrameHome+Vector2.down*descent;
            SyncTimerToBoard();
            float width = Mathf.Max(battleBoxAuthoredSize.x,
                2f * Mathf.Max(Mathf.Abs(corridorLeft), Mathf.Abs(corridorRight)) + 260f);
            playArea.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                Mathf.Lerp(battleBoxAuthoredSize.x, width, Mathf.Clamp01(blend)));
            if (battleBoxFrame != null && compactFrameCaptured)
                battleBoxFrame.sizeDelta = playArea.sizeDelta + compactFrameMargin;
            // Keep the TIME bar at its normal readable width while the side walls leave the screen.
            longCorridorActive = true;
            if (catchCursor != null)
                catchCursor.anchoredPosition = ClampCursorToBattleBox(playArea.InverseTransformPoint(cursorWorld));
        }
        private void RestoreCorridorFrame()
        {
            if(corridorFrameCaptured && battleBoxFrame!=null)battleBoxFrame.anchoredPosition=corridorFrameHome;
            corridorFrameCaptured=false;
        }

        private void ClampLongCorridorCursor(ref Vector2 point, Vector2 half)
        {
            if (!longCorridorActive) return;
            point.x = Mathf.Clamp(point.x, corridorLeft + half.x + 8f, corridorRight - half.x - 8f);
            if(corridorRunnerActive)point.x=Mathf.Clamp(corridorRunnerX,corridorLeft+half.x+8f,corridorRight-half.x-8f);
        }
        public void PullAlongCorridor(float normalizedX,float dt)
        {
            if(!longCorridorActive || catchCursor==null || dt<=0f)return;
            if(!corridorRunnerActive){corridorRunnerX=catchCursor.anchoredPosition.x;corridorRunnerActive=true;}
            corridorRunnerX=Mathf.MoveTowards(corridorRunnerX,Mathf.Lerp(corridorLeft,corridorRight,normalizedX),1100f*dt);
            catchCursor.anchoredPosition=ClampCursorToBattleBox(catchCursor.anchoredPosition);
        }
    }
}
