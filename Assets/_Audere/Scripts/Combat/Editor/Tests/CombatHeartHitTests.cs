#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat.Editor.Tests
{
    public sealed class CombatHeartHitTests
    {
        private const float Immunity = .55f;
        private CombatBoardView board;
        private CombatPlayerView heart;
        private Image[] visuals;
        private Color normal;

        [SetUp]
        public void Setup()
        {
            board = Object.Instantiate(AssetDatabase.LoadAssetAtPath<CombatBoardView>(
                "Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab"));
            board.gameObject.SetActive(true);
            board.PrepareEncounter("Heart contact QA");
            board.ResetPlayer();
            heart = board.GetComponentInChildren<CombatPlayerView>(true);
            visuals = heart.GetComponentsInChildren<Image>(true);
            normal = visuals[0].color;
            Canvas.ForceUpdateCanvases();
        }

        [TearDown]
        public void Cleanup() { if (board != null) Object.DestroyImmediate(board.gameObject); }

        private CombatBulletView Spawn(Vector2 velocity, int phase = 1) =>
            board.SpawnEnemyBullet(null, board.PlayerPosition, velocity, 901, phase);

        [Test]
        public void SimultaneousContacts_KeepMovingAndBlink_OnlyOneHitPerImmunityWindow()
        {
            var bullets = Enumerable.Range(0, 3).Select(_ => Spawn(Vector2.right * 4f)).ToArray();
            var leases = bullets.Select(b => b.PoolLeaseVersion).ToArray();
            Assert.AreEqual(1, board.TickBullets(.01f, Immunity));
            Assert.IsTrue(visuals.All(v => v.color.r > v.color.g && v.color.r > v.color.b));
            bool redFrame = false, normalFrame = false;
            for (int i = 0; i < 10; i++)
            {
                board.TickHeartFeedback(.05f);
                redFrame |= visuals[0].color != normal;
                normalFrame |= visuals[0].color == normal;
                Assert.AreEqual(0, board.TickBullets(.05f, Immunity));
                Assert.IsTrue(bullets.All(b => b.gameObject.activeSelf && b.CollisionActive));
                CollectionAssert.AreEqual(leases, bullets.Select(b => b.PoolLeaseVersion));
            }
            Assert.IsTrue(redFrame && normalFrame, "Heart must blink red/normal throughout the immunity window.");
            Assert.IsTrue(bullets.All(b => b.RectTransform.anchoredPosition.x > 1f), "Contact must not stop bullet movement.");
            board.TickHeartFeedback(.06f);
            Assert.IsTrue(visuals.All(v => v.color == normal));
            Assert.AreEqual(1, board.TickBullets(.01f, Immunity), "Still-overlapping hazards can hit again only after immunity ends.");
            Assert.IsTrue(bullets.All(b => b.gameObject.activeSelf));
        }

        [Test]
        public void BulletAndLaser_ShareHeartImmunity_AndNeitherDisappearsOnContact()
        {
            var bullet = Spawn(Vector2.zero);
            var laser = board.SpawnEnemyLaser(board.PlayerPosition, board.PlayerPosition,
                new Vector2(120f, 18f), 0f, 0f, 5f, 901, 1);
            Assert.AreEqual(1, board.TickBullets(.01f, Immunity));
            board.TickHeartFeedback(.3f);
            Assert.AreEqual(0, board.TickBullets(.3f, Immunity));
            board.TickHeartFeedback(.26f);
            Assert.AreEqual(1, board.TickBullets(.01f, Immunity));
            Assert.IsTrue(bullet.gameObject.activeSelf && laser.gameObject.activeSelf);
        }

        [Test]
        public void PausedContact_DoesNotDamageOrAdvanceHitFlash()
        {
            var bullet = Spawn(Vector2.zero);
            var laser = board.SpawnEnemyLaser(board.PlayerPosition, board.PlayerPosition,
                new Vector2(120f, 18f), 0f, 0f, 5f, 901, 1);
            Assert.AreEqual(0, board.TickBullets(0f, Immunity));
            Assert.AreEqual(normal, visuals[0].color);
            Assert.AreEqual(1, board.TickBullets(.01f, Immunity));
            Color hit = visuals[0].color;
            for (int i = 0; i < 100; i++)
            {
                board.TickHeartFeedback(0f);
                Assert.AreEqual(0, board.TickBullets(0f, Immunity));
                Assert.AreEqual(hit, visuals[0].color);
            }
            board.TickHeartFeedback(.54f);
            Assert.AreEqual(0, board.TickBullets(.01f, Immunity));
            board.TickHeartFeedback(.02f);
            Assert.AreEqual(1, board.TickBullets(.01f, Immunity));
            Assert.IsTrue(bullet.gameObject.activeSelf && laser.gameObject.activeSelf);
        }

        [Test]
        public void ShieldBoundsAndPhaseCleanup_StillReturnPersistentBullets_AndRetryResetsHeart()
        {
            var bullet = Spawn(Vector2.zero);
            var otherPhase = Spawn(Vector2.zero, 2);
            otherPhase.RectTransform.anchoredPosition += Vector2.right * 100f;
            Assert.AreEqual(1, board.TickBullets(.01f, Immunity));
            board.ClearRuntimeBullets(901, 1);
            Assert.IsFalse(bullet.gameObject.activeSelf);
            Assert.IsTrue(otherPhase.gameObject.activeSelf);
            int oldLease = bullet.PoolLeaseVersion;
            var reused = Spawn(Vector2.zero);
            Assert.AreSame(bullet, reused);
            Assert.Greater(reused.PoolLeaseVersion, oldLease);
            Assert.IsFalse(reused.ReturnOnPlayerHit);
            board.ResetPlayer();
            Assert.IsTrue(visuals.All(v => v.color == normal));
            Assert.AreEqual(1, board.TickBullets(.01f, Immunity), "Retry must not inherit the old immunity window.");
            Assert.AreEqual(1, board.DestroyBulletsNearPlayer(20f), "Shield must still actively clear nearby bullets.");
            Assert.IsFalse(reused.gameObject.activeSelf);
            Assert.IsTrue(otherPhase.gameObject.activeSelf);
            var outbound = Spawn(Vector2.right * 10000f);
            board.TickBullets(1f, Immunity);
            Assert.IsFalse(outbound.gameObject.activeSelf, "Leaving the board still returns a projectile normally.");
            board.ClearRuntimeBullets();
            Assert.IsFalse(otherPhase.gameObject.activeSelf);
        }
    }
}
#endif
