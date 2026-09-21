#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Audere.Combat.Editor.Tests
{
    public sealed class TimorCorridorRhythmTests
    {
        private CombatBoardView board;
        private ScrollingWordCorridorMove move;
        private ICombatMoveExecution execution;
        private Camera camera;
        private RenderTexture viewport;
        private sealed class RandomSource : ICombatRandom
        {
            private readonly System.Random random;
            public RandomSource(int seed) { random = new System.Random(seed); }
            public float Value01() => (float)random.NextDouble();
            public float Range(float min, float max) => Mathf.Lerp(min, max, Value01());
        }
        [SetUp]
        public void Setup()
        {
            board = Object.Instantiate(AssetDatabase.LoadAssetAtPath<CombatBoardView>(
                "Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab"));
            board.gameObject.SetActive(true);
            board.PrepareEncounter("Corridor rhythm QA"); board.ResetPlayer();
            Canvas.ForceUpdateCanvases();
        }
        [TearDown]
        public void Cleanup()
        {
            execution?.Cancel();
            if (board != null) Object.DestroyImmediate(board.gameObject);
            if (camera != null) Object.DestroyImmediate(camera.gameObject);
            if (viewport != null) Object.DestroyImmediate(viewport);
        }
        private void Start(bool supported = false, int seed = 731)
        {
            string file = supported ? "Move_TimorSupportedCorridor.asset" : "Move_TimorWordCorridor.asset";
            move = AssetDatabase.LoadAssetAtPath<ScrollingWordCorridorMove>(TimorFinalPolishAuthoring.Moves + file);
            Assert.NotNull(move);
            Assert.IsTrue(move.Validate(out string error), error);
            execution = move.CreateExecution(new CombatMoveExecutionContext(board, null, new RandomSource(seed), 312, 7));
        }
        private void Tick(float seconds)
        {
            const float dt = 1f / 60f;
            for (float time = 0f; time < seconds - .001f; time += dt)
            {
                execution.Tick(dt); board.TickHeartFeedback(dt); board.TickBullets(dt, .85f);
            }
        }
        private CombatBulletView[] Bullets() => board.GetComponentsInChildren<CombatBulletView>()
            .Where(b => b.CollisionActive).ToArray();

        [Test]
        public void NextLane_AlwaysChangesToAdjacentLane()
        {
            var random = new System.Random(1281);
            int lane = 1;
            for (int i = 0; i < 10000; i++)
            {
                int next = ScrollingWordCorridorMove.NextLane(lane, (float)random.NextDouble());
                Assert.AreEqual(1, Math.Abs(next - lane));
                Assert.That(next, Is.InRange(0, 2));
                lane = next;
            }
        }

        [TestCase(1920, 1080, false)]
        [TestCase(1024, 768, false)]
        [TestCase(2560, 1080, false)]
        [TestCase(1920, 1080, true)]
        [TestCase(1024, 768, true)]
        [TestCase(2560, 1080, true)]
        public void FullCorridor_RealHeartCanFollowContinuousRouteAcrossEveryTrain(int width, int height, bool supported)
        {
            viewport = new RenderTexture(width, height, 16);
            camera = new GameObject("Corridor viewport QA", typeof(Camera)).GetComponent<Camera>();
            camera.targetTexture = viewport; camera.orthographic = true; camera.orthographicSize = 540f;
            var canvas = board.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1f;
            var scaler = board.GetComponent<CanvasScaler>();
            if (scaler != null) scaler.enabled = false;
            canvas.scaleFactor = height / 1080f;
            Canvas.ForceUpdateCanvases();
            Start(supported);
            var heart = board.GetComponentInChildren<CombatPlayerView>(true).RectTransform;
            Assert.AreEqual(new Vector2(28f, 28f), heart.rect.size, "Use the production Heart footprint.");

            // Find a route in the actual live geometry, with a conservative 180 units/s
            // vertical limit. No lane schedule, hit cooldown, or hit-driven bullet removal is used.
            const float dt = 1f / 30f, spacing = 2f, minY = -96f;
            const int samples = 97, maxStep = 3;
            var reachable = new bool[samples]; reachable[48] = true;
            var previousFree = Enumerable.Repeat(true, samples).ToArray();
            var stationaryWasHit = new bool[2];
            var arrivedWords = new HashSet<(int instance, int lease)>();
            float firstArrival = float.PositiveInfinity;
            int dangerousFrames = 0;
            for (float time = 0f; time < move.Duration - .001f; time += dt)
            {
                execution.Tick(dt); board.TickHeartFeedback(dt); board.TickBullets(dt, .85f);
                CombatBulletView[] bullets = Bullets();
                var free = new bool[samples];
                float runnerX = board.PlayerPosition.x;
                foreach (var bullet in bullets)
                {
                    if (!bullet.SourcePrefab.name.StartsWith("Bullet_TimorWord") ||
                        bullet.RectTransform.anchoredPosition.x > runnerX) continue;
                    if (arrivedWords.Add((bullet.GetInstanceID(), bullet.PoolLeaseVersion)))
                        firstArrival = Mathf.Min(firstArrival, time);
                }
                for (int index = 0; index < samples; index++)
                {
                    board.CatchCursor.anchoredPosition = new Vector2(runnerX, minY + index * spacing);
                    free[index] = !bullets.Any(b => board.PlayerOverlaps(b.RectTransform));
                }
                if (free.Any(x => !x)) dangerousFrames++;
                stationaryWasHit[0] |= !free[24]; stationaryWasHit[1] |= !free[72];
                var next = new bool[samples];
                for (int target = 0; target < samples; target++)
                {
                    if (!free[target]) continue;
                    for (int from = Math.Max(0, target - maxStep); from <= Math.Min(samples - 1, target + maxStep); from++)
                    {
                        if (!reachable[from]) continue;
                        bool segmentClear = true;
                        for (int y = Math.Min(from, target); y <= Math.Max(from, target); y++)
                            segmentClear &= free[y] && previousFree[y];
                        if (segmentClear) { next[target] = true; break; }
                    }
                }
                Assert.IsTrue(next.Any(x => x), $"No continuous Heart route at {time:F2}s in {width}x{height}, supported={supported}.");
                reachable = next; previousFree = free;
            }
            Assert.Greater(dangerousFrames, supported ? 15 : 60, "The trains must actually reach the runner at this aspect.");
            Assert.LessOrEqual(firstArrival, 2.7f, "The opening must pressure Heart promptly at every aspect.");
            Assert.GreaterOrEqual(arrivedWords.Count, supported ? 12 : 60,
                "Dense trains must reach the runner, not merely spawn offscreen.");
            if (!supported)
                Assert.IsTrue(stationaryWasHit.All(x => x), "Standing in the old fixed gaps between rows must require a dodge.");
        }

        [TestCase(.25f)]
        [TestCase(5f)]
        [TestCase(15.5f)]
        public void PauseAndCancel_FreezeCadenceAndRemovePendingWordsDiceAndEcho(float at)
        {
            Vector2 originalSize = board.PlayArea.rect.size;
            Start(); Tick(at);
            var positions = board.GetComponentsInChildren<RectTransform>().ToDictionary(x => x, x => x.anchoredPosition);
            int count = Bullets().Length;
            for (int frame = 0; frame < 45; frame++) execution.Tick(0f);
            Assert.AreEqual(count, Bullets().Length);
            foreach (var pair in positions) Assert.AreEqual(pair.Value, pair.Key.anchoredPosition);
            execution.Cancel(); execution.Cancel(); execution.Tick(3f);
            Assert.AreEqual(originalSize.x, board.PlayArea.rect.width, .01f);
            Assert.AreEqual(originalSize.y, board.PlayArea.rect.height, .01f);
            Assert.IsEmpty(Bullets());
            Assert.IsFalse(board.GetComponentsInChildren<CombatDieView>().Any(die => die.CanInteract));
            Assert.IsFalse(board.GetComponentsInChildren<Transform>().Any(t => t.name == "Mount Monochrome Echo (runtime)"));
            Assert.IsFalse(board.IsCorridorRunnerActive);
            Assert.AreEqual(0, ((ICombatMoveDamageReward)execution).ConsumePendingDamage());
        }

        [Test]
        public void Exit_RemovesHazardsAndChoicesBeforeBoardStartsNarrowing()
        {
            Start(); Tick(14.9f);
            float wide = board.PlayArea.rect.width;
            // Cross the authored end by a few frames; float accumulation can stop
            // just short of Duration even when the nominal tick count matches it.
            int exitFrames = Mathf.CeilToInt((move.Duration - 14.9f + .1f) * 60f);
            for (int i = 0; i < exitFrames; i++)
            {
                execution.Tick(1f / 60f); board.TickBullets(1f / 60f, .85f);
                if (board.PlayArea.rect.width >= wide - .1f) continue;
                Assert.IsEmpty(Bullets());
                Assert.IsFalse(board.GetComponentsInChildren<CombatDieView>().Any(die => die.CanInteract));
            }
            Assert.IsTrue(execution.IsComplete);
        }

        [Test]
        public void WordPrefabs_UseExplicitMynerveFontAndFitTheirCollisionFootprint()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/_Audere/AssetGame/Font/Mynerve-Regular SDF.asset");
            Assert.NotNull(font);
            for (int index = 0; index < 4; index++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"Assets/_Audere/Prefabs/Combat/Bullets/Bullet_TimorWord_{index}.prefab");
                var label = prefab.GetComponentInChildren<TextMeshProUGUI>(true);
                Assert.AreSame(font, label.font);
                Assert.IsTrue(font.material == label.fontSharedMaterial,
                    "Use Mynerve's atlas material, comparing Unity object identity.");
                Assert.AreEqual(TextWrappingModes.NoWrap, label.textWrappingMode);
                Assert.AreEqual(Vector3.one, prefab.transform.localScale);
                Vector2 footprint = prefab.GetComponent<RectTransform>().rect.size;
                Assert.LessOrEqual(label.GetPreferredValues(label.text).x, footprint.x);
                Assert.AreEqual(32f, footprint.y);
            }
        }

        [Test]
        public void WordsRemainAfterHit_AndMissedAttackStillUsesSharedDamageOnce()
        {
            Start(); Tick(4f);
            var word = Bullets().First(b => b.SourcePrefab.name.StartsWith("Bullet_TimorWord"));
            board.CatchCursor.position = word.RectTransform.position;
            board.TickHeartFeedback(1f);
            Assert.Greater(board.TickBullets(.001f, .85f), 0);
            Assert.IsTrue(word.gameObject.activeInHierarchy);
            Assert.IsFalse(word.ReturnOnPlayerHit);
            Tick(3f);
            var die = board.GetComponentsInChildren<CombatDieView>().First(x => x.CanInteract);
            board.CatchCursor.position = die.RectTransform.position;
            ((ICombatMoveInputHandler)execution).HandleInput(true, false);
            Assert.AreEqual(CombatDiceConstants.AttackDamage, ((ICombatMoveDamageReward)execution).ConsumePendingDamage());
            Assert.AreEqual(0, ((ICombatMoveDamageReward)execution).ConsumePendingDamage());
        }
    }
}
#endif
