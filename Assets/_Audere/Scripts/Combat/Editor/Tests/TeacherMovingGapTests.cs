#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Audere.Combat.Editor.Tests
{
    public sealed class TeacherMovingGapTests
    {
        private const string MovePath = "Assets/_Audere/Data/Combat/Teacher/Moves/Move_TeacherGeometrySketch.asset";
        private CombatBoardView board;
        private TeacherGeometrySketchMove move;
        private ICombatMoveExecution execution;

        [SetUp]
        public void Setup()
        {
            move = AssetDatabase.LoadAssetAtPath<TeacherGeometrySketchMove>(MovePath);
            board = Object.Instantiate(AssetDatabase.LoadAssetAtPath<CombatBoardView>(
                "Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab"));
            board.gameObject.SetActive(true);
            board.PrepareEncounter("Moving chalk pocket test");
            board.ResetPlayer();
            board.SetEncounterPresentationVisible(true);
            Canvas.ForceUpdateCanvases();
            execution = move.CreateExecution(new CombatMoveExecutionContext(board, null, new SystemCombatRandom(71), 701, 2));
        }

        [TearDown]
        public void Cleanup()
        {
            execution?.Cancel();
            if (board != null) Object.DestroyImmediate(board.gameObject);
        }

        [TestCase(1f / 60f)]
        [TestCase(.05f)]
        public void FollowingMovingPocketSurvivesCompleteDenseVolley(float step)
        {
            int hits = 0, peakProjectiles = 0;
            float elapsed = 0f;
            Rect field = board.PlayArea.rect;
            while (elapsed < move.Duration)
            {
                float delta = Mathf.Min(step, move.Duration - elapsed);
                elapsed += delta;
                board.CatchCursor.anchoredPosition = move.GetSafeCenter(field, elapsed);
                execution.Tick(delta);
                hits += board.TickBullets(delta, .1f);
                peakProjectiles = Mathf.Max(peakProjectiles, ActiveBullets().Length);
            }
            Assert.AreEqual(0, hits, "Following the continuous pocket must protect the full Heart collider.");
            Assert.GreaterOrEqual(peakProjectiles, 16, "The survival proof must include overlapping outlines.");
            Assert.IsTrue(execution.IsComplete);
            Assert.IsEmpty(ActiveBullets());
            Assert.IsEmpty(board.GetComponentsInChildren<CombatChalkSketchGraphic>(true));
        }

        [Test]
        public void StandingAtFieldCenterIsHitDuringTheCompleteVolley()
        {
            board.CatchCursor.anchoredPosition = board.PlayArea.rect.center;
            int hits = 0;
            float elapsed = 0f;
            while (elapsed < move.Duration)
            {
                float delta = Mathf.Min(1f / 120f, move.Duration - elapsed);
                elapsed += delta;
                execution.Tick(delta);
                hits += board.TickBullets(delta, .1f);
            }
            Assert.Greater(hits, 0, "The old permanent safe spot at field center must no longer survive this attack.");
        }

        [TestCase(580f, 420f)]
        [TestCase(360f, 190f)]
        [TestCase(900f, 600f)]
        public void MovingPocketIsContinuousInsideBounds_AndEveryApproachMaintainsClearance(float width, float height)
        {
            Rect field = new Rect(-width * .5f + 37f, -height * .5f - 19f, width, height);
            Vector2 previous = move.GetSafeCenter(field, 0f);
            Assert.AreEqual(field.center, previous);
            float largestDisplacement = 0f;
            const float step = 1f / 120f;
            const float outlineRadius = 96f / 1.41421356237f + 3.5f;
            for (int frame = 1; frame <= Mathf.CeilToInt(move.Duration / step); frame++)
            {
                float age = frame * step;
                Vector2 gap = move.GetSafeCenter(field, age);
                Assert.GreaterOrEqual(gap.x - move.SafeRadius, field.xMin);
                Assert.LessOrEqual(gap.x + move.SafeRadius, field.xMax);
                Assert.GreaterOrEqual(gap.y - move.SafeRadius, field.yMin);
                Assert.LessOrEqual(gap.y + move.SafeRadius, field.yMax);
                Assert.Less(Vector2.Distance(previous, gap) / step, 210f, "The pocket must not jump between lanes.");
                largestDisplacement = Mathf.Max(largestDisplacement, Vector2.Distance(gap, field.center));
                previous = gap;
                for (int shape = 0; shape < move.ShapeCount; shape++)
                {
                    move.GetFlight(field, shape, out Vector2 start, out Vector2 end);
                    Vector2 offset = move.GetFlightOffset(field, shape, age);
                    Assert.AreEqual(0f, offset.x);
                    Assert.IsTrue(start.x < field.xMin || start.x > field.xMax);
                    Vector2 direction = end - start;
                    Vector2 relative = gap - start - offset;
                    float clearance = Mathf.Abs(direction.x * relative.y - direction.y * relative.x) / direction.magnitude;
                    Assert.GreaterOrEqual(clearance - outlineRadius, move.SafeRadius - .001f,
                        "All simultaneous shapes must surround the same moving pocket.");
                }
            }
            Assert.Greater(largestDisplacement, 75f, "The pocket must leave the center by a meaningful distance.");
        }

        [Test]
        public void DrawingPauseAndLaunchShareTheMovingPath_ThenCancellationClearsEverything()
        {
            execution.Tick(.4f);
            board.TickBullets(.4f, 1f);
            var sketches = board.GetComponentsInChildren<CombatChalkSketchGraphic>(true)
                .Where(sketch => sketch.gameObject.activeSelf).OrderBy(sketch => sketch.transform.position.x).ToArray();
            Assert.AreEqual(2, sketches.Length);
            move.GetFlight(board.PlayArea.rect, 0, out Vector2 start, out _);
            Vector2 expected = start + move.GetFlightOffset(board.PlayArea.rect, 0, .4f);
            Vector2 actual = board.PlayArea.InverseTransformPoint(sketches[0].transform.position);
            Assert.Less(Vector2.Distance(expected, actual), .001f);
            Vector3[] positions = sketches.Select(sketch => sketch.transform.position).ToArray();
            execution.Tick(0f);
            board.TickBullets(0f, 1f);
            CollectionAssert.AreEqual(positions, sketches.Select(sketch => sketch.transform.position).ToArray());

            execution.Tick(.35f);
            board.TickBullets(.35f, 1f);
            var first = ActiveBullets().First();
            move.GetFlight(board.PlayArea.rect, 0, out start, out Vector2 end);
            var vertices = TeacherGeometrySketchMove.Vertices(0, 96f);
            expected = start + (vertices[0] + vertices[1]) * .5f +
                (end - start) * ((.75f - move.LaunchTime(0)) / 1.7f) +
                move.GetFlightOffset(board.PlayArea.rect, 0, .75f);
            Assert.Less(Vector2.Distance(expected, first.RectTransform.anchoredPosition), .01f);
            execution.Cancel();
            execution.Cancel();
            Assert.IsEmpty(ActiveBullets());
            Assert.IsEmpty(board.GetComponentsInChildren<CombatChalkSketchGraphic>(true));
            Assert.IsFalse(board.IsAttackWarningVisible);
        }

        private CombatBulletView[] ActiveBullets() => board.GetComponentsInChildren<CombatBulletView>(true)
            .Where(bullet => bullet.gameObject.activeSelf).ToArray();
    }
}
#endif
