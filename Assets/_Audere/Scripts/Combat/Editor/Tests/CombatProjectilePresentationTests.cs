#if UNITY_EDITOR
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat.Editor.Tests
{
    public sealed class CombatProjectilePresentationTests
    {
        private CombatBoardView board;
        private Canvas boardCanvas;
        private RectTransform maskRoot;
        private RectTransform bulletRoot;
        private RectTransform laserRoot;

        [SetUp]
        public void Setup()
        {
            board = Object.Instantiate(AssetDatabase.LoadAssetAtPath<CombatBoardView>(
                "Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab"));
            board.gameObject.SetActive(true);
            boardCanvas = board.GetComponent<Canvas>();
            maskRoot = (RectTransform)board.PlayArea.Find("Projectile Mask");
            bulletRoot = (RectTransform)maskRoot.Find("Bullet Root");
            laserRoot = (RectTransform)maskRoot.Find("Laser Root");
            board.RefreshProjectilePresentationLayers();
            board.SetEncounterPresentationVisible(true);
            Canvas.ForceUpdateCanvases();
        }

        [TearDown]
        public void Cleanup()
        {
            if (board != null) Object.DestroyImmediate(board.gameObject);
        }

        [Test]
        public void AllProjectilesSortAboveBoard_WhileNormalBulletsAndLasersKeepTheirMask()
        {
            var bullet = board.SpawnEnemyBullet(null, Vector2.zero, Vector2.right, 71, 2);
            var laser = board.SpawnEnemyLaser(Vector2.zero, Vector2.zero, new Vector2(120f, 16f),
                0f, .4f, 1f, 71, 2);
            var chalk = AssetDatabase.LoadAssetAtPath<CombatBulletView>(
                "Assets/_Audere/Prefabs/Combat/Bullets/Bullet_ChalkRod.prefab");
            var exterior = board.SpawnExteriorEnemyBullet(chalk, new Vector2(450f, 0f), 71, 2, .4f);
            var mask = maskRoot.GetComponent<RectMask2D>();
            var maskedCanvas = maskRoot.GetComponent<Canvas>();
            var exteriorCanvas = board.ProjectilePresentationRoot.GetComponent<Canvas>();
            Assert.AreSame(maskedCanvas, bullet.GetComponentInParent<Canvas>());
            Assert.AreSame(maskedCanvas, laser.GetComponentInParent<Canvas>());
            Assert.AreSame(exteriorCanvas, exterior.GetComponentInParent<Canvas>());
            foreach (var canvas in new[] { maskedCanvas, exteriorCanvas })
            {
                Assert.IsTrue(canvas.overrideSorting);
                Assert.AreEqual(boardCanvas.sortingLayerID, canvas.sortingLayerID);
                Assert.AreEqual(boardCanvas.sortingOrder + 20, canvas.sortingOrder);
            }
            Assert.AreSame(mask, MaskUtilities.GetRectMaskForClippable(bullet.GetComponent<Image>()));
            Assert.AreSame(mask, MaskUtilities.GetRectMaskForClippable(laser.GetComponent<Image>()));
            Assert.IsNull(MaskUtilities.GetRectMaskForClippable(exterior.GetComponent<Image>()));
            Assert.IsNull(bulletRoot.GetComponent<Canvas>(), "The Canvas belongs on the mask, not below it.");
            Assert.IsNull(laserRoot.GetComponent<Canvas>());
            Assert.AreEqual(RenderMode.WorldSpace, boardCanvas.renderMode,
                "Projectiles remain world-space content below screen-space Dialogue/Retry overlays.");
        }

        [Test]
        public void RepeatedRefreshPreservesTransforms_AndFollowsSceneSortingOverrides()
        {
            var roots = new[] { maskRoot, bulletRoot, laserRoot, board.ProjectilePresentationRoot };
            var positions = roots.Select(root => root.localPosition).ToArray();
            var scales = roots.Select(root => root.localScale).ToArray();
            var rotations = roots.Select(root => root.localRotation).ToArray();
            var sizes = roots.Select(root => root.sizeDelta).ToArray();
            var parents = roots.Select(root => root.parent).ToArray();
            int canvasCount = board.GetComponentsInChildren<Canvas>(true).Length;
            boardCanvas.sortingOrder = 57;
            board.RefreshProjectilePresentationLayers();
            board.RefreshProjectilePresentationLayers();
            Assert.AreEqual(canvasCount, board.GetComponentsInChildren<Canvas>(true).Length);
            Assert.AreEqual(77, maskRoot.GetComponent<Canvas>().sortingOrder);
            Assert.AreEqual(77, board.ProjectilePresentationRoot.GetComponent<Canvas>().sortingOrder);
            for (int index = 0; index < roots.Length; index++)
            {
                Assert.AreEqual(positions[index], roots[index].localPosition);
                Assert.AreEqual(scales[index], roots[index].localScale);
                Assert.AreEqual(rotations[index], roots[index].localRotation);
                Assert.AreEqual(sizes[index], roots[index].sizeDelta);
                Assert.AreSame(parents[index], roots[index].parent);
            }
        }

        [Test]
        public void VisibilityAndPausePreserveHazards_AndDisableHidesEveryProjectileCanvas()
        {
            var bullet = board.SpawnEnemyBullet(null, new Vector2(-90f, 60f), new Vector2(10f, 0f), 71, 2);
            Vector2 position = bullet.RectTransform.anchoredPosition;
            var maskedCanvas = maskRoot.GetComponent<Canvas>();
            var exteriorCanvas = board.ProjectilePresentationRoot.GetComponent<Canvas>();
            board.TickBullets(0f, 1f);
            Assert.AreEqual(position, bullet.RectTransform.anchoredPosition);
            Assert.IsTrue(maskedCanvas.enabled);
            Assert.IsTrue(exteriorCanvas.enabled);
            board.SetEncounterPresentationVisible(false);
            Assert.IsFalse(maskedCanvas.enabled);
            Assert.IsFalse(exteriorCanvas.enabled);
            Assert.IsTrue(bullet.gameObject.activeSelf, "Hiding presentation must not clear the paused projectile lease.");
            board.SetEncounterPresentationVisible(true);
            Assert.IsTrue(maskedCanvas.enabled);
            Assert.IsTrue(exteriorCanvas.enabled);
            Assert.AreEqual(position, bullet.RectTransform.anchoredPosition);

            boardCanvas.enabled = false;
            typeof(CombatBoardView).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(board, null);
            Assert.IsFalse(maskedCanvas.enabled, "Direct Canvas changes are synchronized before rendering.");
            Assert.IsFalse(exteriorCanvas.enabled);
            board.SetEncounterPresentationVisible(true);
            board.enabled = false;
            board.RefreshProjectilePresentationLayers();
            Assert.IsFalse(maskedCanvas.enabled);
            Assert.IsFalse(exteriorCanvas.enabled);
        }

        [Test]
        public void ExteriorPoolLeaseCanReturnToMaskedLayerWithoutChangingSortingContract()
        {
            var chalk = AssetDatabase.LoadAssetAtPath<CombatBulletView>(
                "Assets/_Audere/Prefabs/Combat/Bullets/Bullet_ChalkRod.prefab");
            var bullet = board.SpawnExteriorEnemyBullet(chalk, new Vector2(420f, 0f), 71, 2, .4f);
            int lease = bullet.PoolLeaseVersion;
            board.ReturnEnemyBullet(bullet, lease);
            var reused = board.SpawnEnemyBullet(chalk, new Vector2(30f, 40f), Vector2.right, 72, 3);
            Assert.AreSame(bullet, reused);
            Assert.AreSame(bulletRoot, reused.transform.parent);
            Assert.AreSame(maskRoot.GetComponent<Canvas>(), reused.GetComponentInParent<Canvas>());
            Assert.AreSame(maskRoot.GetComponent<RectMask2D>(), MaskUtilities.GetRectMaskForClippable(reused.GetComponent<Image>()));
            board.ReturnEnemyBullet(reused, lease);
            Assert.IsTrue(reused.gameObject.activeSelf);
            board.ClearCombatRuntime();
            Assert.IsFalse(reused.gameObject.activeSelf);
            Assert.IsFalse(maskRoot.GetComponent<Canvas>().enabled);
            Assert.IsFalse(board.ProjectilePresentationRoot.GetComponent<Canvas>().enabled);
        }
    }
}
#endif
