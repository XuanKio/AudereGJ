#if UNITY_INCLUDE_TESTS
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat.Editor.Tests
{
    public sealed class CombatEnemyActorFloatTests
    {
        private GameObject root;
        private CombatEnemyActor actor;
        private Transform visual;
        private Transform projectile;
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Float test", typeof(RectTransform), typeof(CombatEnemyActor));
            actor = root.GetComponent<CombatEnemyActor>();
            visual = new GameObject("Visual", typeof(RectTransform), typeof(Image)).transform;
            visual.SetParent(root.transform, false);
            visual.localPosition = new Vector3(4f, 2f, 7f);
            projectile = new GameObject("Projectile origin").transform;
            projectile.SetParent(root.transform, false);
            projectile.localPosition = new Vector3(10f, 20f, 30f);
            Set("visualRoot", visual);
            Set("projectileOrigin", projectile);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void Float_IsSmallBoundedAndDoesNotMoveProjectileOrActor()
        {
            actor.Initialize(default);
            Vector3 anchor = projectile.position;
            Vector3 actorPosition = root.transform.position;
            Tick(.9f);
            Assert.That(visual.localPosition.y, Is.EqualTo(3.5f).Within(.0001f));
            Tick(1.8f);
            Assert.That(visual.localPosition.y, Is.EqualTo(.5f).Within(.0001f));
            for (int i = 0; i < 4000; i++)
            {
                Tick(.018f);
                Assert.That(visual.localPosition.y, Is.InRange(.4999f, 3.5001f));
            }
            Assert.AreEqual(anchor, projectile.position);
            Assert.AreEqual(actorPosition, root.transform.position);
            Assert.AreEqual(4f, visual.localPosition.x);
            Assert.AreEqual(7f, visual.localPosition.z);
        }

        [Test]
        public void Float_ComposesWithHitShakeIntroScaleAndFade()
        {
            actor.Initialize(default);
            Tick(.9f);
            visual.localPosition = new Vector3(12f, 2f, 7f); // Board hit feedback resets to authored Y.
            Vector3 scale = new Vector3(.8f, .9f, 1f);
            visual.localScale = scale;
            var image = visual.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, .4f);
            Tick(0f);
            Assert.That(visual.localPosition, Is.EqualTo(new Vector3(12f, 3.5f, 7f)));
            Assert.AreEqual(scale, visual.localScale);
            Assert.AreEqual(.4f, image.color.a);
        }

        [Test]
        public void Float_PausesForDialogueAndPhaseBreakWithoutRestarting()
        {
            actor.Initialize(default);
            Tick(.45f);
            float held = visual.localPosition.y;
            actor.SetPaused(true);
            Tick(4f);
            Assert.AreEqual(held, visual.localPosition.y);
            actor.SetPaused(false);
            Tick(.45f);
            Assert.That(visual.localPosition.y, Is.EqualTo(3.5f).Within(.0001f));
            actor.ExitPhase(null, 0);
            Tick(2f);
            Assert.That(visual.localPosition.y, Is.EqualTo(3.5f).Within(.0001f));
            actor.EnterPhase(null, 1);
            Tick(.9f);
            Assert.That(visual.localPosition.y, Is.EqualTo(2f).Within(.0001f));
        }

        [Test]
        public void Float_ShutdownRetryAndDisableRestoreAuthoredHeight()
        {
            actor.Initialize(default);
            Tick(.9f);
            actor.Shutdown();
            actor.Shutdown();
            Tick(4f);
            Assert.AreEqual(2f, visual.localPosition.y);
            visual.localPosition = new Vector3(4f, 22f, 7f);
            actor.Initialize(default);
            Tick(.9f);
            Assert.That(visual.localPosition.y, Is.EqualTo(23.5f).Within(.0001f));
            actor.enabled = false;
            Tick(.2f);
            Assert.AreEqual(22f, visual.localPosition.y);
            actor.enabled = true;
            Tick(.9f);
            Assert.That(visual.localPosition.y, Is.EqualTo(23.5f).Within(.0001f));
            actor.Shutdown();
            Assert.AreEqual(22f, visual.localPosition.y);
        }

        [Test]
        public void Float_OptOutAndUninitializedActorsStayStill()
        {
            Tick(.9f);
            Assert.AreEqual(2f, visual.localPosition.y);
            actor.Initialize(default);
            Tick(.9f);
            Set("idleFloatEnabled", false);
            Tick(.9f);
            Assert.AreEqual(2f, visual.localPosition.y);
            Set("idleFloatEnabled", true);
            Set("visualRoot", root.transform);
            Tick(.9f);
            Assert.AreEqual(Vector3.zero, root.transform.localPosition);
            Set("visualRoot", null);
            Assert.DoesNotThrow(() => Tick(.9f));
        }

        [Test]
        public void EnemyPrefabs_UseSeparateVisualRootsWithGentleDefaults()
        {
            var ids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Audere/Prefabs/Combat/Enemies" });
            int checkedActors = 0;
            foreach (string id in ids)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<CombatEnemyActor>(AssetDatabase.GUIDToAssetPath(id));
                if (prefab == null) continue;
                checkedActors++;
                Assert.IsNotNull(prefab.VisualRoot, prefab.name);
                Assert.AreNotEqual(prefab.transform, prefab.VisualRoot, prefab.name);
                Assert.IsFalse(prefab.ProjectileOrigin.IsChildOf(prefab.VisualRoot), prefab.name);
                var so = new SerializedObject(prefab);
                Assert.IsTrue(so.FindProperty("idleFloatEnabled").boolValue, prefab.name);
                Assert.That(so.FindProperty("idleFloatAmplitude").floatValue, Is.InRange(.5f, 2f), prefab.name);
                Assert.GreaterOrEqual(so.FindProperty("idleFloatPeriod").floatValue, 3f, prefab.name);
            }
            Assert.GreaterOrEqual(checkedActors, 5);
        }

        private void Set(string name, object value) => typeof(CombatEnemyActor).GetField(name, Private).SetValue(actor, value);
        private void Tick(float delta) => typeof(CombatEnemyActor).GetMethod("TickIdleFloat", Private).Invoke(actor, new object[] { delta });
    }
}
#endif

