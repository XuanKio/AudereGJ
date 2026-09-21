#if UNITY_EDITOR
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Audere.Combat.Editor.Tests
{
    public sealed class TimorRhythmPressureTests
    {
        private CombatBoardView board;
        private ICombatMoveExecution execution;
        [SetUp] public void Setup()
        {
            board = Object.Instantiate(AssetDatabase.LoadAssetAtPath<CombatBoardView>(
                "Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab"));
            board.gameObject.SetActive(true); board.PrepareEncounter("Rhythm QA"); board.ResetPlayer();
            board.SetEncounterPresentationVisible(true); Canvas.ForceUpdateCanvases();
        }
        [TearDown] public void Cleanup()
        { execution?.Cancel(); if (board != null) Object.DestroyImmediate(board.gameObject); }
        private TimorPressureWaveMove Start(string name)
        {
            var move = AssetDatabase.LoadAssetAtPath<TimorPressureWaveMove>(TimorFinalPolishAuthoring.Moves + "Move_" + name + ".asset");
            Assert.IsTrue(move.Validate(out string error), error);
            execution = move.CreateExecution(new CombatMoveExecutionContext(board, null, new SystemCombatRandom(62), 814, 3));
            return move;
        }
        [TestCase("TimorOpeningRhythm", 1f / 60f)]
        [TestCase("TimorCrossRhythm", .05f)]
        [TestCase("TimorPressureWaves", 1f / 60f)]
        [TestCase("TimorSupportedWaves", .05f)]
        public void MovingOpeningFitsActualHeartThroughOverlappingPhrases(string name, float dt)
        {
            Vector2 originalSize = board.PlayArea.rect.size, originalPosition = board.PlayArea.anchoredPosition;
            var move = Start(name); int hits = 0, peak = 0; float age = 0, narrowest = originalSize.x, biggestShift = 0;
            while (age < move.Duration)
            {
                float step = Mathf.Min(dt, move.Duration - age); age += step;
                execution.Tick(step);
                board.CatchCursor.position = board.PlayArea.TransformPoint(move.SafePointAt(board.PlayArea.rect, age));
                board.TickHeartFeedback(step); hits += board.TickBullets(step, .1f);
                peak = Mathf.Max(peak, board.GetComponentsInChildren<CombatBulletView>().Count(b => b.CollisionActive &&
                    board.PlayArea.rect.Contains(b.RectTransform.anchoredPosition)));
                narrowest = Mathf.Min(narrowest, board.PlayArea.rect.width);
                biggestShift = Mathf.Max(biggestShift, Mathf.Abs(board.PlayArea.anchoredPosition.x - originalPosition.x));
            }
            Assert.AreEqual(0, hits, "The continuous route must remain open across every overlapping live row.");
            Assert.Greater(peak, 12, "The proof must include a barrage, not a single isolated marker.");
            Assert.Less(narrowest, originalSize.x * .9f); Assert.Greater(biggestShift, 8f);
            execution.Cancel(); Assert.AreEqual(originalSize, board.PlayArea.rect.size);
            Assert.AreEqual(originalPosition, board.PlayArea.anchoredPosition);
        }
        [Test] public void CampingAtCenterIsHitAndPauseDoesNotAdvanceTheRhythm()
        {
            var move = Start("TimorPressureWaves"); int hits = 0;
            for (float age = 0; age < move.Duration; age += .02f)
            {
                execution.Tick(.02f); board.CatchCursor.anchoredPosition = Vector2.zero;
                board.TickHeartFeedback(.02f); hits += board.TickBullets(.02f, .15f);
                if (age > 2f && age < 2.03f)
                {
                    var bullets = board.GetComponentsInChildren<CombatBulletView>();
                    var positions = bullets.Select(b => b.RectTransform.anchoredPosition).ToArray();
                    Vector2 field = board.PlayArea.rect.size, center = board.PlayArea.anchoredPosition;
                    execution.Tick(0); board.TickBullets(0, .15f);
                    CollectionAssert.AreEqual(positions, bullets.Select(b => b.RectTransform.anchoredPosition).ToArray());
                    Assert.AreEqual(field, board.PlayArea.rect.size); Assert.AreEqual(center, board.PlayArea.anchoredPosition);
                }
            }
            Assert.Greater(hits, 0, "The moving corridor must not leave a permanent center pocket.");
        }
        [Test] public void CancelDuringBarrageClearsRowsAndReturnsAuthoredLayout()
        {
            Vector2 size = board.PlayArea.rect.size;
            Start("TimorCrossRhythm");
            for (int i = 0; i < 170; i++) { execution.Tick(.02f); board.TickBullets(.02f, 1f); }
            Assert.IsTrue(board.GetComponentsInChildren<CombatBulletView>().Any(b => b.CollisionActive));
            execution.Cancel(); execution.Cancel();
            Assert.AreEqual(size, board.PlayArea.rect.size);
            Assert.IsFalse(board.GetComponentsInChildren<CombatBulletView>().Any(b => b.CollisionActive));
            Assert.IsFalse(board.GetComponentsInChildren<RectTransform>().Any(t => t.name == "Timor rhythm marks"));
        }
        [Test] public void CloneMuzzleShortensTheEmptyTransitWithoutMovingOrScalingTheActor()
        {
            Rect field = new Rect(-145, -95, 290, 190); Vector2 size = new Vector2(667.8f, 629f);
            for (int i = 0; i < 4; i++)
            {
                Vector2 center = OrbitingCloneVolleyMove.ClonePosition(i, field, .4f, size);
                Vector2 muzzle = OrbitingCloneVolleyMove.MuzzlePosition(center, size, Vector2.zero);
                Assert.Less(muzzle.magnitude, center.magnitude * .65f);
                Assert.IsFalse(field.Contains(muzzle), "Shots should still enter from outside the battle box.");
                Assert.IsFalse(field.Overlaps(new Rect(center - size * .5f, size)));
            }
        }
        [TestCase("TimorClockwiseClones")]
        [TestCase("TimorPeakClones")]
        public void EveryCloneVolleyReachesTheBoxIncludingTheReversalShot(string name)
        {
            var move = AssetDatabase.LoadAssetAtPath<OrbitingCloneVolleyMove>(TimorFinalPolishAuthoring.Moves + "Move_" + name + ".asset");
            execution = move.CreateExecution(new CombatMoveExecutionContext(board, null, new SystemCombatRandom(6), 815, 3));
            var seen = new HashSet<(int id, int lease)>();
            var groups = new List<List<(CombatBulletView bullet, int lease)>>();
            var entered = new List<bool>();
            for (float age = 0; age < move.Duration; age += 1f / 60f)
            {
                execution.Tick(1f / 60f); board.CatchCursor.anchoredPosition = Vector2.zero;
                board.TickBullets(1f / 60f, .1f);
                var spawned = new List<(CombatBulletView, int)>();
                foreach (var bullet in board.GetComponentsInChildren<CombatBulletView>())
                    if (bullet.gameObject.activeSelf && seen.Add((bullet.GetInstanceID(), bullet.PoolLeaseVersion)))
                        spawned.Add((bullet, bullet.PoolLeaseVersion));
                if (spawned.Count > 0) { groups.Add(spawned); entered.Add(false); }
                for (int group = 0; group < groups.Count; group++)
                    foreach (var item in groups[group])
                        if (item.bullet.PoolLeaseVersion == item.lease && item.bullet.gameObject.activeSelf &&
                            board.PlayArea.rect.Contains(item.bullet.RectTransform.anchoredPosition)) entered[group] = true;
            }
            Assert.AreEqual(16, groups.Count);
            Assert.IsTrue(groups.All(group=>group.Count==5));
            Assert.IsTrue(entered.All(value => value), "Every telegraphed shot must enter before it is returned or reversed.");
        }
    }
}
#endif
