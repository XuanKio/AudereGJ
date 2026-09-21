using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat
{
    // One execution owns these visuals and leases. Never clone a live board/controller.
    internal sealed class CombatMoveStage : IDisposable
    {
        private readonly CombatBoardView board;
        private readonly Action onBoardCleared;
        private readonly List<GameObject> roots = new List<GameObject>();
        private readonly List<(CombatBulletView bullet, int lease)> bullets = new List<(CombatBulletView, int)>();
        private bool disposed;
        public CombatMoveStage(CombatBoardView board, Action onBoardCleared)
        {
            this.board = board; this.onBoardCleared = onBoardCleared;
            board.RegisterMoveStage(onBoardCleared);
        }
        public RectTransform Root(string name, bool exterior = false)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = board.PlayArea.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(exterior ? board.PlayArea.parent : board.PlayArea, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = board.PlayArea.rect.size;
            if (exterior) rect.anchoredPosition = board.PlayArea.anchoredPosition;
            roots.Add(go);
            return rect;
        }
        public static Image Picture(Transform parent, string name, Sprite sprite, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite; image.color = color; image.raycastTarget = false;
            image.preserveAspect = sprite != null;
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(.5f, .5f);
            image.rectTransform.sizeDelta = size;
            return image;
        }
        public static Vector2 OriginalActorSize(CombatMoveExecutionContext context, Sprite sprite)
        {
            if(context.Actor!=null)
                foreach(var source in context.Actor.GetComponentsInChildren<Image>(true))
                {
                    if(source.sprite!=sprite)continue;
                    Vector2 size=source.rectTransform.rect.size;
                    if(source.preserveAspect && sprite!=null)
                    {
                        float fit=Mathf.Min(size.x/sprite.rect.width,size.y/sprite.rect.height);
                        size=sprite.rect.size*fit;
                    }
                    Vector3 scaled=context.Board.PlayArea.InverseTransformVector(source.rectTransform.TransformVector(size));
                    return new Vector2(Mathf.Abs(scaled.x),Mathf.Abs(scaled.y));
                }
            return new Vector2(667.8f,629f);
        }
        public static TextMeshProUGUI Text(Transform parent, string message, TMP_FontAsset font,
            Vector2 size, float fontSize, Color color)
        {
            var go = new GameObject("Pressure words", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.layer = parent.gameObject.layer; go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = font != null ? font : TMP_Settings.defaultFontAsset;
            text.text = message; text.fontSize = fontSize; text.alignment = TextAlignmentOptions.Center;
            text.color = color; text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.NoWrap;
            text.rectTransform.sizeDelta = size;
            return text;
        }
        public CombatBulletView Bullet(CombatMoveExecutionContext context, CombatBulletView prefab,
            Vector2 position, Vector2 velocity, float warning = 0f)
        {
            var bullet = board.SpawnEnemyBullet(prefab, position, velocity,
                context.SessionVersion, context.PhaseVersion, warning);
            if (bullet != null)
            {
                bullets.Add((bullet, bullet.PoolLeaseVersion));
                if (warning > 0f) bullet.FadeInDuringTelegraph();
            }
            return bullet;
        }
        public void ClearBullets()
        {
            foreach (var item in bullets)
                if (item.bullet != null && item.bullet.PoolLeaseVersion == item.lease)
                    item.bullet.ReturnToPool();
            bullets.Clear();
        }
        public CombatBulletView ExteriorBullet(CombatMoveExecutionContext context, CombatBulletView prefab,
            Vector2 origin, Vector2 velocity)
        {
            var bullet = board.SpawnExteriorEnemyBullet(prefab, origin, context.SessionVersion, context.PhaseVersion, 0f);
            if (bullet == null) return null;
            bullets.Add((bullet, bullet.PoolLeaseVersion));
            float lifetime = (Vector2.Distance(origin, board.PlayArea.rect.center) + board.PlayArea.rect.size.magnitude + 130f) / Mathf.Max(1f, velocity.magnitude);
            bullet.ConfigurePathMotion(new ParametricProjectileMotion(lifetime, t => origin + velocity * lifetime * t, null));
            return bullet;
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true; board.UnregisterMoveStage(onBoardCleared); ClearBullets();
            foreach (var go in roots)
            {
                if (go == null) continue;
                go.SetActive(false);
                if (Application.isPlaying) UnityEngine.Object.Destroy(go);
                else UnityEngine.Object.DestroyImmediate(go);
            }
            roots.Clear();
        }
    }

    public sealed partial class CombatBoardView
    {
        private readonly List<Action> moveStageCleanup = new List<Action>();
        internal void RegisterMoveStage(Action cancel) => moveStageCleanup.Add(cancel);
        internal void UnregisterMoveStage(Action cancel) => moveStageCleanup.Remove(cancel);
        private void ClearMoveStages()
        {
            var pending = moveStageCleanup.ToArray(); moveStageCleanup.Clear();
            foreach (var cancel in pending) cancel();
        }
    }
}
