using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat
{
    public sealed partial class CombatBoardView
    {
        private bool guidedPresentationActive;
        private GuidedRectPose guidedFramePose, guidedFieldPose, guidedAirbornePose;
        private readonly List<GuidedGraphicState> guidedCatchGraphics = new List<GuidedGraphicState>();
        private readonly List<GuidedGraphicState> guidedEnemyGraphics = new List<GuidedGraphicState>();
        private readonly List<GuidedRendererState> guidedEnemyRenderers = new List<GuidedRendererState>();
        private readonly List<GuidedObjectState> guidedObjects = new List<GuidedObjectState>();
        private GameObject guidedTimerObject, guidedHealthObject, guidedHealthOutlineObject, guidedNameObject;
        private CombatGuidedTargetGraphic guidedTarget;

        public bool IsGuidedTutorialPresentationActive => guidedPresentationActive;
        public RectTransform GuidedEnemyHealthTarget => enemyHealthOutline != null
            ? enemyHealthOutline.rectTransform
            : enemyHealthSlider != null ? enemyHealthSlider.transform as RectTransform : null;
        public RectTransform GuidedHeartTarget => playerView != null
            ? playerView.RectTransform != null ? playerView.RectTransform : playerView.transform as RectTransform
            : playerRoot;

        /// <summary>Use a small field without changing the authored actor or board scale.</summary>
        public void BeginGuidedTutorialPresentation(float squareSize = 400f)
        {
            if (guidedPresentationActive)
                return;
            ResolveReferences();
            CaptureBattleBoxLayout();
            if (playArea == null)
                return;

            guidedFramePose = new GuidedRectPose(battleBoxFrame);
            guidedFieldPose = new GuidedRectPose(playArea);
            guidedAirbornePose = new GuidedRectPose(airborneDiceRoot);
            Vector3 center = playArea.TransformPoint(playArea.rect.center);
            float border = battleBoxFrame != null
                ? Mathf.Max(0f, (battleBoxFrame.rect.width - playArea.rect.width) * .5f)
                : 0f;
            float size = Mathf.Max(240f, squareSize);
            SetGuidedRect(battleBoxFrame, size + border * 2f, center);
            float timerSlot = battleBoxFrame != null && TimerFocusTarget != null ? TimerHeightInFrame + GuidedTimerGap : 0f;
            Vector3 fieldCenter = center + (battleBoxFrame != null
                ? battleBoxFrame.TransformVector(Vector3.up * timerSlot * .5f) : Vector3.zero);
            SetGuidedRect(playArea, size, fieldCenter);
            playArea.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(180f, size - timerSlot));
            SetGuidedRect(airborneDiceRoot, size, fieldCenter);
            if (airborneDiceRoot != null)
                airborneDiceRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, playArea.rect.height);
            SyncTimerToBoard();

            guidedTimerObject = TimerFocusTarget != null ? TimerFocusTarget.gameObject : null;
            guidedHealthObject = enemyHealthSlider != null ? enemyHealthSlider.gameObject : null;
            guidedHealthOutlineObject = enemyHealthOutline != null ? enemyHealthOutline.gameObject : null;
            // The authored Name container owns both its label and background.
            guidedNameObject = enemyNameText != null
                ? enemyNameText.transform.parent != null && enemyNameText.transform.parent != transform
                    ? enemyNameText.transform.parent.gameObject : enemyNameText.gameObject
                : null;
            CaptureGuidedObject(guidedTimerObject);
            CaptureGuidedObject(guidedHealthObject);
            CaptureGuidedObject(guidedHealthOutlineObject);
            CaptureGuidedObject(guidedNameObject);
            CaptureGuidedObject(catchCursor != null ? catchCursor.gameObject : null);
            CaptureGuidedObject(playerRoot != null ? playerRoot.gameObject : null);

            if (catchCursor != null)
            {
                Graphic[] graphics = catchCursor.GetComponentsInChildren<Graphic>(true);
                for (int i = 0; i < graphics.Length; i++)
                {
                    if (playerRoot != null && (graphics[i].transform == playerRoot ||
                        graphics[i].transform.IsChildOf(playerRoot)))
                        continue;
                    guidedCatchGraphics.Add(new GuidedGraphicState(graphics[i]));
                }
                catchCursor.gameObject.SetActive(true);
                catchCursor.anchoredPosition = Vector2.zero;
            }
            if (playerRoot != null)
                playerRoot.gameObject.SetActive(true);
            if (enemyVisual != null)
            {
                foreach (Graphic graphic in enemyVisual.GetComponentsInChildren<Graphic>(true))
                    guidedEnemyGraphics.Add(new GuidedGraphicState(graphic));
                foreach (Renderer renderer in enemyVisual.GetComponentsInChildren<Renderer>(true))
                    guidedEnemyRenderers.Add(new GuidedRendererState(renderer));
            }
            guidedPresentationActive = true;
            SetGuidedTutorialVisibility(false, false, false, false);
        }

        public void SetGuidedTutorialVisibility(
            bool timerVisible, bool enemyVisible, bool enemyHealthVisible, bool catchVisible)
        {
            if (!guidedPresentationActive)
                return;
            SetGuidedObjectVisible(guidedTimerObject, timerVisible);
            SetGuidedObjectVisible(guidedHealthObject, enemyHealthVisible);
            SetGuidedObjectVisible(guidedHealthOutlineObject, enemyHealthVisible);
            SetGuidedObjectVisible(guidedNameObject, enemyVisible);
            SetGuidedGraphicsVisible(guidedCatchGraphics, catchVisible);
            SetGuidedGraphicsVisible(guidedEnemyGraphics, enemyVisible);
            for (int i = 0; i < guidedEnemyRenderers.Count; i++)
            {
                GuidedRendererState state = guidedEnemyRenderers[i];
                if (state.Target != null)
                    state.Target.enabled = enemyVisible && state.Enabled;
            }
        }

        /// <summary>A visual destination only: it has no collider or input target.</summary>
        public void SetGuidedTutorialTarget(Vector2? localPosition)
        {
            if (!localPosition.HasValue || !guidedPresentationActive || playArea == null)
            {
                if (guidedTarget != null)
                    guidedTarget.gameObject.SetActive(false);
                return;
            }
            if (guidedTarget == null)
            {
                GameObject marker = new GameObject("Guided Movement Target (runtime)",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(CombatGuidedTargetGraphic));
                marker.transform.SetParent(playArea, false);
                marker.transform.SetAsFirstSibling();
                guidedTarget = marker.GetComponent<CombatGuidedTargetGraphic>();
                guidedTarget.raycastTarget = false;
            }
            RectTransform rect = guidedTarget.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(64f, 64f);
            Rect bounds = playArea.rect;
            Vector2 position = localPosition.Value;
            position.x = Mathf.Clamp(position.x, bounds.xMin + 36f, bounds.xMax - 36f);
            position.y = Mathf.Clamp(position.y, bounds.yMin + 36f, bounds.yMax - 36f);
            rect.anchoredPosition = position;
            guidedTarget.gameObject.SetActive(true);
        }

        /// <summary>Restore the captured scene geometry and visibility exactly, once.</summary>
        public void EndGuidedTutorialPresentation()
        {
            SetGuidedTutorialTarget(null);
            if (!guidedPresentationActive)
                return;
            guidedFramePose.Restore();
            guidedFieldPose.Restore();
            guidedAirbornePose.Restore();
            SyncTimerToBoard();
            SetGuidedGraphicsVisible(guidedCatchGraphics, true);
            SetGuidedGraphicsVisible(guidedEnemyGraphics, true);
            for (int i = 0; i < guidedEnemyRenderers.Count; i++)
            {
                GuidedRendererState state = guidedEnemyRenderers[i];
                if (state.Target != null)
                    state.Target.enabled = state.Enabled;
            }
            for (int i = 0; i < guidedObjects.Count; i++)
            {
                GuidedObjectState state = guidedObjects[i];
                if (state.Target != null)
                    state.Target.SetActive(state.Active);
            }
            guidedCatchGraphics.Clear();
            guidedEnemyGraphics.Clear();
            guidedEnemyRenderers.Clear();
            guidedObjects.Clear();
            guidedPresentationActive = false;
        }

        private void CaptureGuidedObject(GameObject target)
        {
            if (target == null)
                return;
            for (int i = 0; i < guidedObjects.Count; i++)
                if (guidedObjects[i].Target == target)
                    return;
            guidedObjects.Add(new GuidedObjectState(target));
        }

        private static void SetGuidedObjectVisible(GameObject target, bool visible)
        {
            if (target != null && target.activeSelf != visible)
                target.SetActive(visible);
        }

        private static void SetGuidedGraphicsVisible(List<GuidedGraphicState> states, bool visible)
        {
            for (int i = 0; i < states.Count; i++)
            {
                GuidedGraphicState state = states[i];
                if (state.Target != null)
                    state.Target.enabled = visible && state.Enabled;
            }
        }

        private static void SetGuidedRect(RectTransform rect, float size, Vector3 center)
        {
            if (rect == null)
                return;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
            rect.position += center - rect.TransformPoint(rect.rect.center);
        }

        private readonly struct GuidedGraphicState
        {
            public readonly Graphic Target;
            public readonly bool Enabled;
            public GuidedGraphicState(Graphic target) { Target = target; Enabled = target.enabled; }
        }

        private readonly struct GuidedRendererState
        {
            public readonly Renderer Target;
            public readonly bool Enabled;
            public GuidedRendererState(Renderer target) { Target = target; Enabled = target.enabled; }
        }

        private readonly struct GuidedObjectState
        {
            public readonly GameObject Target;
            public readonly bool Active;
            public GuidedObjectState(GameObject target) { Target = target; Active = target.activeSelf; }
        }

        private readonly struct GuidedRectPose
        {
            private readonly RectTransform target;
            private readonly Vector2 sizeDelta;
            private readonly Vector3 anchoredPosition;
            public GuidedRectPose(RectTransform rect)
            {
                target = rect;
                sizeDelta = rect != null ? rect.sizeDelta : Vector2.zero;
                anchoredPosition = rect != null ? rect.anchoredPosition3D : Vector3.zero;
            }
            public void Restore()
            {
                if (target == null)
                    return;
                target.sizeDelta = sizeDelta;
                target.anchoredPosition3D = anchoredPosition;
            }
        }
    }

    // Purely visual, pooled with its board; it never participates in combat hit tests.
    public sealed class CombatGuidedTargetGraphic : MaskableGraphic
    {
        private void Update()
        {
            color = new Color(.73f, .92f, .88f, .65f + .22f * Mathf.Sin(Time.unscaledTime * 4f));
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            const int segments = 48;
            float outer = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * .5f - 3f;
            float inner = outer - 2.5f;
            Vector2 center = rectTransform.rect.center;
            for (int i = 0; i < segments; i++)
            {
                float start = i * Mathf.PI * 2f / segments;
                float end = (i + 1) * Mathf.PI * 2f / segments;
                Vector2 a = new Vector2(Mathf.Cos(start), Mathf.Sin(start));
                Vector2 b = new Vector2(Mathf.Cos(end), Mathf.Sin(end));
                int index = helper.currentVertCount;
                helper.AddVert(center + a * inner, color, Vector2.zero);
                helper.AddVert(center + a * outer, color, Vector2.zero);
                helper.AddVert(center + b * outer, color, Vector2.zero);
                helper.AddVert(center + b * inner, color, Vector2.zero);
                helper.AddTriangle(index, index + 1, index + 2);
                helper.AddTriangle(index, index + 2, index + 3);
            }
        }
    }
}
