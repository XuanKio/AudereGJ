using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat
{
    public sealed partial class CombatBoardView
    {
        private const int ProjectileSortingOffset = 20;
        private Canvas bulletPresentationCanvas;
        private Canvas laserPresentationCanvas;
        private Canvas exteriorPresentationCanvas;

        /// <summary>Unmasked, field-local presentation space above the board's other content.</summary>
        public RectTransform ProjectilePresentationRoot
        {
            get
            {
                RefreshProjectilePresentationLayers();
                return exteriorProjectileRoot;
            }
        }

        public RectTransform MaskedProjectilePresentationRoot
        {
            get
            {
                RefreshProjectilePresentationLayers();
                return ProjectileSortingRoot(bulletRoot);
            }
        }

        /// <summary>Idempotent for prefab authoring and scene instances with local sorting overrides.</summary>
        public void RefreshProjectilePresentationLayers()
        {
            if (boardCanvas == null) boardCanvas = GetComponent<Canvas>();
            if (boardCanvas == null) return;
            bulletRoot = ResolveRect(bulletRoot, "Bullet Root");
            laserRoot = ResolveRect(laserRoot, "Laser Root");
            exteriorProjectileRoot = ResolveRect(exteriorProjectileRoot, "Exterior Projectile Root");
            bulletPresentationCanvas = EnsureProjectileCanvas(ProjectileSortingRoot(bulletRoot));
            laserPresentationCanvas = EnsureProjectileCanvas(ProjectileSortingRoot(laserRoot));
            exteriorPresentationCanvas = EnsureProjectileCanvas(exteriorProjectileRoot);
            SyncProjectilePresentationLayers();
        }

        private RectTransform ProjectileSortingRoot(RectTransform content)
        {
            if (content == null || !content.IsChildOf(transform)) return null;
            // An override Canvas below an ancestor RectMask2D detaches its graphics from
            // that mask. Put sorting on the mask itself and leave every transform intact.
            for (Transform candidate = content; candidate != null && candidate != transform; candidate = candidate.parent)
                if (candidate.TryGetComponent<RectMask2D>(out _)) return candidate as RectTransform;
            return content;
        }

        private Canvas EnsureProjectileCanvas(RectTransform root)
        {
            if (root == null || root == transform || !root.IsChildOf(transform)) return null;
            var canvas = root.GetComponent<Canvas>();
            return canvas != null ? canvas : root.gameObject.AddComponent<Canvas>();
        }

        private void SyncProjectilePresentationLayers()
        {
            if (boardCanvas == null) return;
            bool visible = boardCanvas.enabled && isActiveAndEnabled;
            int order = Mathf.Clamp(boardCanvas.sortingOrder + ProjectileSortingOffset, short.MinValue, short.MaxValue);
            SyncProjectileCanvas(bulletPresentationCanvas, visible, order);
            if (laserPresentationCanvas != bulletPresentationCanvas)
                SyncProjectileCanvas(laserPresentationCanvas, visible, order);
            SyncProjectileCanvas(exteriorPresentationCanvas, visible, order);
        }

        private void SyncProjectileCanvas(Canvas canvas, bool visible, int order)
        {
            if (canvas == null) return;
            if (!canvas.overrideSorting) canvas.overrideSorting = true;
            if (canvas.sortingLayerID != boardCanvas.sortingLayerID) canvas.sortingLayerID = boardCanvas.sortingLayerID;
            if (canvas.sortingOrder != order) canvas.sortingOrder = order;
            // Nested canvases can render independently when their parent's Canvas is hidden.
            if (canvas.enabled != visible) canvas.enabled = visible;
        }
    }
}
