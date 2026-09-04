using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat
{
    [CreateAssetMenu(menuName = "Audere/Combat/Moves/Projection Assault", fileName = "Move_ProjectionAssault")]
    public sealed class ProjectionAssaultMove : CombatMoveDefinition
    {
        [SerializeField] private Sprite projectionSprite;
        [SerializeField] private CombatMoveDefinition childMove;
        [SerializeField, Range(1, 2)] private int copies = 2;
        [SerializeField] private Vector2 visualSize = new Vector2(150f, 190f);
        [SerializeField] private Color tint = new Color(.7f, .66f, 1f, .72f);
        [SerializeField, Min(0f)] private float boardSideOffset = 28f;

        public Sprite ProjectionSprite => projectionSprite;
        public CombatMoveDefinition ChildMove => childMove;
        public int Copies => copies;

public override bool Validate(out string error)
        {
            if (!base.Validate(out error)) return false;
            if (projectionSprite == null || childMove == null || copies < 1 || copies > 2 ||
                visualSize.x < 20f || visualSize.y < 20f)
            {
                error = "Projection assault requires a sprite, child move, one or two copies and a visible size.";
                return false;
            }
            string childError;
            if (!childMove.Validate(out childError))
            {
                error = "Projection assault child is invalid: " + childError;
                return false;
            }
            error = null;
            return true;
        }

        public override ICombatMoveExecution CreateExecution(CombatMoveExecutionContext context)
        {
            if (!Validate(out string error)) throw new InvalidOperationException(error);
            return new Execution(this, context);
        }

        private sealed class Execution : ICombatMoveExecution
        {
            private readonly ProjectionAssaultMove data;
            private readonly CombatMoveExecutionContext context;
            private readonly List<Image> visuals = new List<Image>();
            private readonly ICombatMoveExecution child;
            private float elapsed;
            private bool cancelled;

            public Execution(ProjectionAssaultMove data, CombatMoveExecutionContext context)
            {
                this.data = data;
                this.context = context;
                child = data.childMove.CreateExecution(context);
                CreateVisuals();
            }

            public bool IsComplete => cancelled || elapsed >= data.Duration;

            public void Tick(float deltaTime)
            {
                if (cancelled) return;
                elapsed += Mathf.Max(0f, deltaTime);
                if (child != null && !child.IsComplete) child.Tick(deltaTime);
                float fadeIn = Mathf.Clamp01(elapsed / .35f);
                float fadeOut = Mathf.Clamp01((data.Duration - elapsed) / .45f);
                float alpha = Mathf.Min(fadeIn, fadeOut) * data.tint.a;
                float pulse = 1f + Mathf.Sin(elapsed * 4.8f) * .035f;
                for (int i = 0; i < visuals.Count; i++)
                {
                    Image image = visuals[i];
                    if (image == null) continue;
                    image.color = new Color(data.tint.r, data.tint.g, data.tint.b, alpha);
                    image.rectTransform.localScale = Vector3.one * pulse;
                    image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(elapsed * 2.2f + i) * 2.5f);
                }
            }

            public void Cancel()
            {
                if (cancelled) return;
                cancelled = true;
                child?.Cancel();
                foreach (Image image in visuals)
                    if (image != null) UnityEngine.Object.Destroy(image.gameObject);
                visuals.Clear();
            }

            private void CreateVisuals()
            {
                if (context.Board == null || context.Board.PlayArea == null) return;
                RectTransform parent = context.Board.transform as RectTransform;
                if (parent == null) parent = context.Board.PlayArea.parent as RectTransform;
                if (parent == null) return;
                Rect r = context.Board.PlayArea.rect;
                Vector3 leftWorld = context.Board.PlayArea.TransformPoint(new Vector3(r.xMin, r.center.y, 0f));
                Vector3 rightWorld = context.Board.PlayArea.TransformPoint(new Vector3(r.xMax, r.center.y, 0f));
                Vector2 left = parent.InverseTransformPoint(leftWorld);
                Vector2 right = parent.InverseTransformPoint(rightWorld);
                for (int i = 0; i < data.copies; i++)
                {
                    var go = new GameObject("MEMORY PROJECTION " + data.projectionSprite.name + " " + (i + 1),
                        typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
                    var rect = (RectTransform)go.transform;
                    rect.SetParent(parent, false);
                    rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                    rect.pivot = new Vector2(.5f, .5f);
                    rect.sizeDelta = data.visualSize;
                    rect.anchoredPosition = i == 0
                        ? left + Vector2.left * (data.visualSize.x * .5f + data.boardSideOffset)
                        : right + Vector2.right * (data.visualSize.x * .5f + data.boardSideOffset);
                    var image = go.GetComponent<Image>();
                    image.sprite = data.projectionSprite;
                    image.preserveAspect = true;
                    image.raycastTarget = false;
                    image.color = new Color(data.tint.r, data.tint.g, data.tint.b, 0f);
                    var outline = go.GetComponent<Outline>();
                    outline.effectColor = new Color(.16f, .08f, .55f, .68f);
                    outline.effectDistance = new Vector2(3f, -3f);
                    outline.useGraphicAlpha = true;
                    if (i == 1) rect.localScale = new Vector3(-1f, 1f, 1f);
                    rect.SetAsLastSibling();
                    visuals.Add(image);
                }
            }
        }
    }
}