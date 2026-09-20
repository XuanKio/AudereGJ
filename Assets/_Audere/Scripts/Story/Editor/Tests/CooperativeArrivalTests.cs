#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Audere.Dialogue;
using Audere.EditorTools;
using Audere.Puzzle;
using Audere.Puzzle.Board;
using Audere.Puzzle.PathPieces;
using Audere.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Audere.Story.Editor.Tests
{
    public sealed class CooperativeArrivalTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnityTest]
        public IEnumerator SingleBoard_GoalArrivalStaysSeparateAndOnlyRemainingActorMoves()
        {
            var scene = EditorSceneManager.OpenScene(Day2SchoolMorningSetupTool.ScenePath);
            var director = All<StoryDirector>(scene).Single();
            var startup = new SerializedObject(director);
            startup.FindProperty("playOnStart").boolValue = false;
            startup.ApplyModifiedPropertiesWithoutUndo(); // In memory only; never save the test startup override.
            yield return new EnterPlayMode();
            yield return VerifySecondBoard();
            yield return new ExitPlayMode();
        }

        private static IEnumerator VerifySecondBoard()
        {
            Application.runInBackground = true;
            Time.timeScale = 1f;
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            var scene = SceneManager.GetActiveScene();
            var director = All<StoryDirector>(scene).Single();
            var pair = All<CooperativePuzzleSession>(scene).Single(p => p.Puzzle.PuzzleData.PuzzleId == "PZ_D2_COOP_01");

            var controller = pair.Puzzle.GetComponent<PuzzleController>();
            var storyEvent = director.StoryEventsRoot.Find("D2_SCHOOL_COOP_01").GetComponent<StoryEvent>();
            director.CancelCurrentEvent();
            // Establish the same puzzle mode used by the classroom hand-off.
            controller.PuzzleRoot.parent.gameObject.SetActive(true);
            All<WorldModeController>(scene).Single().ApplyModeImmediate(WorldGameplayMode.Puzzle);

            Set(storyEvent, "autoPlayNextEvent", false);
            Assert.IsTrue(director.PlayEvent(storyEvent));
            yield return Until(() => pair.Puzzle.CurrentState == PuzzleManager.State.Playing);

            var a = pair.Puzzle.Player;
            var b = pair.Partner;
            var aRenderers = a.GetComponentsInChildren<SpriteRenderer>(true);
            var bRenderers = b.GetComponentsInChildren<SpriteRenderer>(true);
            var aAlphas = aRenderers.Select(r => r.color.a).ToArray();
            var bAlphas = bRenderers.Select(r => r.color.a).ToArray();
            var aStart = a.transform.position;
            var bStart = b.transform.position;
            var aCell = a.GridPosition;
            var bCell = b.GridPosition;
            float cellSize = Vector3.Distance(pair.Puzzle.Board.GridSpace.CellToWorldCenter(Vector2Int.zero),
                pair.Puzzle.Board.GridSpace.CellToWorldCenter(Vector2Int.right));
            var routes = new[]
            {
                Path(0,1, 1,1), Path(1,0, 1,1, 2,1), Path(1,1, 2,1, 2,2),
                Path(2,1, 2,2), Path(2,2, 3,2, 4,2, 4,1)
            };
            for (int i = 0; i < 3; i++)
            {
                CommitPointerRoute(pair.Puzzle, routes[i].Select(p => Cell(pair, p)).ToArray());
                yield return Until(() => pair.Puzzle.CurrentState == PuzzleManager.State.Playing);
            }
            Assert.AreEqual(pair.AudereGoal.GridPosition, b.GridPosition,
                "Bianca pauses at Audere's destination before continuing to her own destination.");
            Assert.IsFalse(pair.HasArrived(b));

            CommitPointerRoute(pair.Puzzle, routes[3].Select(p => Cell(pair, p)).ToArray());
            yield return Until(() => pair.HasArrived(a));
            Vector3 arrivedPose = a.transform.position;
            Vector2Int arrivedCell = a.GridPosition;
            int visibleFadeFrames = 0;
            double deadline = EditorApplication.timeSinceStartup + 8;
            while (pair.Puzzle.CurrentState != PuzzleManager.State.Playing && EditorApplication.timeSinceStartup < deadline)
            {
                Assert.Less(Vector3.Distance(arrivedPose, a.transform.position), .0001f,
                    "Arrival owns Audere's settled pose; standing presentation must not slide her during the fade.");
                if (aRenderers[0].color.a > .02f)
                {
                    visibleFadeFrames++;
                    Assert.Greater(b.transform.position.x - a.transform.position.x, cellSize * .35f,
                        "Visible actors on the same tile must remain separated while arrival fades out.");
                }
                EditorApplication.QueuePlayerLoopUpdate();
                yield return null;
            }
            Assert.AreEqual(PuzzleManager.State.Playing, pair.Puzzle.CurrentState, "Arrival fade must release the next card.");
            Assert.Greater(visibleFadeFrames, 0, "The test must observe the actual fade, not only its final frame.");
            foreach (var renderer in aRenderers) Assert.AreEqual(0f, renderer.color.a, .0001f);
            Assert.AreSame(b, pair.ActorAtStart(arrivedCell), "Only Bianca remains selectable on their shared tile.");
            Assert.AreEqual(1, GameplayUIRoot.Instance.PathPieceHand.Count);

            Vector3 partnerBeforeLastPath = b.transform.position;
            bool completed = false;
            pair.Puzzle.PuzzleCompleted += () => completed = true;
            CommitPointerRoute(pair.Puzzle, routes[4].Select(p => Cell(pair, p)).ToArray());
            Assert.AreSame(b, pair.Puzzle.ActivePlayer);
            deadline = EditorApplication.timeSinceStartup + 8;
            while (!completed && EditorApplication.timeSinceStartup < deadline)
            {
                Assert.AreEqual(arrivedCell, a.GridPosition);
                Assert.IsFalse(a.IsMoving);
                Assert.Less(Vector3.Distance(arrivedPose, a.transform.position), .0001f,
                    "Audere must not follow Bianca after arriving, even while invisible.");
                foreach (var renderer in aRenderers) Assert.AreEqual(0f, renderer.color.a, .0001f);
                EditorApplication.QueuePlayerLoopUpdate();
                yield return null;
            }
            Assert.IsTrue(completed, "The actual puzzle completion callback must fire.");
            Assert.IsTrue(pair.BothAtGoals);
            Assert.AreEqual(pair.PartnerGoal.GridPosition, b.GridPosition);
            Assert.Greater(Vector3.Distance(partnerBeforeLastPath, b.transform.position), cellSize);
            Assert.AreEqual(0, GameplayUIRoot.Instance.PathPieceHand.Count);

            // End the story owner before starting a new attempt; no collapse or next event may compete with reset.
            director.CancelCurrentEvent();
            Assert.IsTrue(controller.Play());
            Assert.IsFalse(pair.HasArrived(a));
            Assert.IsFalse(pair.HasArrived(b));
            Assert.AreEqual(aCell, a.GridPosition);
            Assert.AreEqual(bCell, b.GridPosition);
            Assert.Less(Vector3.Distance(aStart, a.transform.position), .0001f);
            Assert.Less(Vector3.Distance(bStart, b.transform.position), .0001f);
            for (int i = 0; i < aRenderers.Length; i++) Assert.AreEqual(aAlphas[i], aRenderers[i].color.a, .0001f);
            for (int i = 0; i < bRenderers.Length; i++) Assert.AreEqual(bAlphas[i], bRenderers[i].color.a, .0001f);
            Assert.AreEqual(5, GameplayUIRoot.Instance.PathPieceHand.Count);
            controller.Cancel();
            Assert.AreEqual(0, GameplayUIRoot.Instance.InputGate.ActiveClaimCount);
            LogAssert.NoUnexpectedReceived();
        }

        private static T[] All<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();

        private static Vector2Int Cell(CooperativePuzzleSession pair, Vector2Int authored)
        {
            var tile = pair.Puzzle.GetComponent<PuzzleController>().PuzzleRoot.GetComponentsInChildren<BoardTile>(true)
                .Single(t => t.name == "Tile_" + authored.x + "_" + authored.y);
            return pair.Puzzle.Board.GridSpace.WorldToCell(tile.transform.position);
        }

        private static Vector2Int[] Path(params int[] xy) => Enumerable.Range(0, xy.Length / 2)
            .Select(i => new Vector2Int(xy[i * 2], xy[i * 2 + 1])).ToArray();

        private static void CommitPointerRoute(PuzzleManager puzzle, Vector2Int[] wanted)
        {
            var placement = puzzle.Board.GridSpace.GetComponentInChildren<PuzzleRuntime>(true).Placement;
            for (int slot = 0; slot < GameplayUIRoot.Instance.PathPieceHand.Count; slot++)
            {
            GameplayUIRoot.Instance.PathPieceHand.Select(slot);
            for (int rotation = 0; rotation < 4; rotation++)
            {
                Set(placement, "rotation", (GridRotation)rotation);
                for (int x = wanted.Min(p => p.x) - 1; x <= wanted.Max(p => p.x) + 1; x++)
                for (int y = wanted.Min(p => p.y) - 1; y <= wanted.Max(p => p.y) + 1; y++)
                {
                    Set(placement, "hasAnchoredOrigin", false);
                    var screen = Camera.main.WorldToScreenPoint(puzzle.Board.GridSpace.CellToWorldCenter(new Vector2Int(x, y)));
                    if (!placement.TryMovePreviewToScreenPosition(screen)) continue;
                    var result = (PlacementResult)typeof(PathPlacementController).GetField("currentResult", Private).GetValue(placement);
                    if (!result.CanCommit || result.WillFall || !result.GridPath.SequenceEqual(wanted)) continue;
                    Assert.IsTrue(placement.TryCommitPreview());
                    return;
                }
            }
            }
            Assert.Fail("No legal pointer/drop found for " + string.Join(" -> ", wanted.Select(p => p.ToString())));
        }

        private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);

        private static IEnumerator Until(Func<bool> ready)
        {
            double deadline = EditorApplication.timeSinceStartup + 15;
            while (!ready() && EditorApplication.timeSinceStartup < deadline)
            {
                var dialogue = GameplayUIRoot.Instance != null ? GameplayUIRoot.Instance.Dialogue : null;
                if (dialogue != null && dialogue.IsPlaying)
                    typeof(DialogueController).GetMethod("EndPlayback", Private).Invoke(dialogue, new object[] { DialogueResult.Completed, true });
                EditorApplication.QueuePlayerLoopUpdate();
                yield return null;
            }
            Assert.IsTrue(ready(), "Timed out waiting for the focused cooperative arrival flow.");
        }

        [UnityTearDown]
        public IEnumerator RestoreEditor()
        {
            if (EditorApplication.isPlaying) yield return new ExitPlayMode();
            Time.timeScale = 1f;
            EditorSceneManager.OpenScene(Day2SchoolMorningSetupTool.ScenePath);
        }
    }
}
#endif
