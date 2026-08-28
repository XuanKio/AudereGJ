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
using Audere.Story.Steps;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Audere.Story.Editor.Tests
{
    public sealed class CooperativePuzzleCompletionTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void FocusedPolish_PreservesSceneObjectsCombatAndBothGoalAnchors()
        {
            var scene = EditorSceneManager.OpenScene(Day2SchoolMorningSetupTool.ScenePath);
            var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            var wrong = all.Single(t => t.name == "D2_SCHOOL_WRONG_SUPPLIES");
            var before = wrong.GetComponentsInChildren<Component>(true).Select(EditorJsonUtility.ToJson).ToArray();
            var ids = all.Select(t => t.GetInstanceID()).ToArray();
            Day2SchoolCoopSetupTool.PolishExisting();
            CollectionAssert.AreEqual(before, wrong.GetComponentsInChildren<Component>(true).Select(EditorJsonUtility.ToJson).ToArray());
            CollectionAssert.AreEqual(ids, scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Select(t => t.GetInstanceID()).ToArray());
            var pairs = all.Select(t => t.GetComponent<CooperativePuzzleSession>()).Where(p => p != null)
                .OrderBy(p => p.Puzzle.PuzzleData.PuzzleId).ToArray();
            Assert.AreEqual(3, pairs.Length);
            for (int i = 0; i < pairs.Length; i++)
            {
                var controller = pairs[i].Puzzle.GetComponent<PuzzleController>();
                Assert.AreEqual(1, controller.PuzzleRoot.GetComponentsInChildren<CooperativeRedTileBehaviour>(true).Length);
                Assert.AreEqual(4, pairs[i].Puzzle.PuzzleData.AvailablePathPieces.Count);
                Assert.IsTrue(pairs[i].Puzzle.PuzzleData.RequireAllPathPieces);
                if (i > 0)
                {
                    Assert.Less(Vector3.Distance(pairs[i-1].AudereGoal.transform.position, pairs[i].Puzzle.PlayerStartTransform.position), .0001f);
                    Assert.Less(Vector3.Distance(pairs[i-1].PartnerGoal.transform.position, pairs[i].PartnerStart.position), .0001f);
                }
            }
            var title = all.Single(t => t.name == "COOP PUZZLE CONTROLS").GetComponentInChildren<TMPro.TMP_Text>(true);
            Assert.AreEqual(CooperativePuzzleControls.Objective, title.text);
            Assert.IsFalse(title.raycastTarget);
        }

        [UnityTest]
        public IEnumerator AllThreeBoards_ActualPreviewDropsCompleteAndReplayCancelIsClean()
        {
            PrepareScene();
            yield return new EnterPlayMode();
            yield return PlayAllBoards();
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator Teardown_WithDestroyedUi_DoesNotNormalizeOrResurrectPuzzle()
        {
            PrepareScene();
            yield return new EnterPlayMode();
            yield return VerifyTeardown();
            yield return new ExitPlayMode();
        }

        private static void PrepareScene()
        {
            var scene = EditorSceneManager.OpenScene(Day2SchoolMorningSetupTool.ScenePath);
            var director = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<StoryDirector>(true)).Single();
            var so = new SerializedObject(director);
            so.FindProperty("playOnStart").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo(); // Test-only; never save this startup override.
        }

        private static IEnumerator PlayAllBoards()
        {
            Application.runInBackground = true;
            Assert.AreEqual(1f, Time.timeScale, "The project must enter Play with a running clock.");
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            var scene = SceneManager.GetActiveScene();
            var director = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<StoryDirector>(true)).Single();
            var pairs = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CooperativePuzzleSession>(true))
                .OrderBy(p => p.Puzzle.PuzzleData.PuzzleId).ToArray();
            var events = pairs.Select((p, i) => director.StoryEventsRoot.Find("D2_SCHOOL_COOP_0" + (i + 1)).GetComponent<StoryEvent>()).ToArray();
            // Exercise the production puzzle chain, but leave the separately-owned combat alone.
            Set(events[2], "autoPlayNextEvent", false);
            Assert.IsTrue(director.PlayEvent(events[0]));
            for (int boardIndex = 0; boardIndex < 3; boardIndex++)
            {
                var pair = pairs[boardIndex];
                yield return Until(() => pair.Puzzle.CurrentState == PuzzleManager.State.Playing);
                // Check both depth arrangements without consuming a card or changing a tile.
                var a = pair.Puzzle.Player;
                var b = pair.Partner;
                var aCell = a.GridPosition; var bCell = b.GridPosition;
                var grid = pair.Puzzle.Board.GridSpace;
                a.SetPosition(aCell + Vector2Int.up * 2, grid.CellToWorldCenter(aCell + Vector2Int.up * 2));
                b.SetPosition(aCell, grid.CellToWorldCenter(aCell));
                Assert.AreEqual(grid.CellToWorldCenter(aCell).y, b.GroundSortY, .0001f);
                typeof(CooperativePuzzleSession).GetMethod("LateUpdate", Private).Invoke(pair, null);
                Assert.Less(a.GetComponent<UnityEngine.Rendering.SortingGroup>().sortingOrder, b.GetComponent<UnityEngine.Rendering.SortingGroup>().sortingOrder);
                a.SetPosition(aCell, grid.CellToWorldCenter(aCell));
                b.SetPosition(aCell + Vector2Int.up * 2, grid.CellToWorldCenter(aCell + Vector2Int.up * 2));
                // A visible hop must not move the grounded depth ahead/behind the other actor.
                Set(a, "motionGroundPosition", a.transform.position); Set(a, "motionActive", true);
                a.transform.position += Vector3.up * 2f;
                typeof(CooperativePuzzleSession).GetMethod("LateUpdate", Private).Invoke(pair, null);
                Assert.Greater(a.GetComponent<UnityEngine.Rendering.SortingGroup>().sortingOrder, b.GetComponent<UnityEngine.Rendering.SortingGroup>().sortingOrder);
                a.SetPosition(aCell, grid.CellToWorldCenter(aCell)); b.SetPosition(bCell, grid.CellToWorldCenter(bCell));
                foreach (var actor in new[] { a, b }) Assert.AreEqual(5, actor.GetComponent<SpriteRenderer>().sortingOrder);
                TestContext.WriteLine("Playing " + pair.Puzzle.PuzzleData.PuzzleId + " timeScale=" + Time.timeScale);
                Assert.IsFalse(scene.GetRootGameObjects().Single(r => r.name == "SCHOOL")
                    .transform.Find("SCHOOL ART PLACEHOLDER/Supplies Return Board").gameObject.activeInHierarchy,
                    "The post-combat floor must not look like extra playable puzzle tiles.");
                System.IO.Directory.CreateDirectory("Temp/CoopQA");
                ScreenCapture.CaptureScreenshot("Temp/CoopQA/board0" + (boardIndex + 1) + ".png");
                yield return null;
                Assert.AreEqual(CooperativePuzzleControls.Objective,
                    scene.GetRootGameObjects().Single(r => r.name == "COOP PUZZLE CONTROLS").GetComponentInChildren<TMPro.TMP_Text>(true).text);
                Vector3 cameraPose = Camera.main.transform.position;
                var routes = Routes(boardIndex);
                for (int move = 0; move < routes.Length; move++)
                {
                    var cells = routes[move].Select(p => Cell(pair, p.x, p.y)).ToArray();
                    CommitPointerRoute(pair.Puzzle, cells);
                    TestContext.WriteLine("Dropped move " + (move + 1) + " on " + pair.Puzzle.PuzzleData.PuzzleId);
                    int remaining = 3 - move;
                    yield return Until(() => pair.Puzzle.CurrentState == PuzzleManager.State.Playing || pair.BothAtGoals);
                    Assert.AreEqual(remaining, GameplayUIRoot.Instance.PathPieceHand.Count, "A dropped card must be consumed exactly once.");
                    Assert.AreEqual(cameraPose, Camera.main.transform.position);
                    if (boardIndex == 2 && move == 0)
                    {
                        ScreenCapture.CaptureScreenshot("Temp/CoopQA/upper-behind-lower.png");
                        yield return null;
                    }
                }
                Assert.IsTrue(pair.BothAtGoals);
                Assert.AreEqual(0, GameplayUIRoot.Instance.PathPieceHand.Count);
                yield return Until(() => !events[boardIndex].IsPlaying);
            }
            Assert.AreEqual(0, GameplayUIRoot.Instance.InputGate.ActiveClaimCount);
            Assert.IsFalse(GameplayUIRoot.Instance.Dialogue.IsPlaying);

            // Retry a failed attempt through the real fall lifecycle.
            Assert.IsTrue(director.PlayEvent(events[0]));
            var first = pairs[0];
            yield return Until(() => first.Puzzle.CurrentState == PuzzleManager.State.Playing);
            var hand = GameplayUIRoot.Instance.PathPieceHand;
            hand.Select(0);
            var start = first.Puzzle.Player.GridPosition;
            var fall = PathPlacementValidator.Validate(hand.SelectedPiece, start, GridRotation.Degrees90, start, first.Puzzle.Board, first.Puzzle.Player);
            Assert.IsTrue(fall.WillFall);
            first.Puzzle.SubmitPlacement(fall);
            yield return Until(() => first.Puzzle.CurrentState == PuzzleManager.State.Playing && hand.Count == 4);
            foreach (var red in first.Puzzle.GetComponent<PuzzleController>().PuzzleRoot.GetComponentsInChildren<CooperativeRedTileBehaviour>(true))
                Assert.IsFalse(red.HasBeenEntered);
            CommitPointerRoute(first.Puzzle, Routes(0)[0].Select(p => Cell(first, p.x, p.y)).ToArray());
            director.CancelCurrentEvent();
            Assert.AreEqual(0, GameplayUIRoot.Instance.InputGate.ActiveClaimCount);
            Assert.IsFalse(first.Puzzle.Player.IsMoving);
            Assert.IsFalse(first.Partner.IsMoving);
            Assert.IsTrue(director.PlayEvent(events[0]));
            yield return Until(() => first.Puzzle.CurrentState == PuzzleManager.State.Playing);
            Assert.AreEqual(4, hand.Count);
            director.CancelCurrentEvent();
            LogAssert.NoUnexpectedReceived();
        }

        private static IEnumerator VerifyTeardown()
        {
            Application.runInBackground = true;
            Assert.AreEqual(1f, Time.timeScale, "The project must enter Play with a running clock.");
            var scene = SceneManager.GetActiveScene();
            var director = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<StoryDirector>(true)).Single();
            var e = director.StoryEventsRoot.Find("D2_SCHOOL_COOP_01").GetComponent<StoryEvent>();
            Assert.IsTrue(director.PlayEvent(e));
            yield return Until(() => e.CurrentStep is PuzzleStep);
            var step = (PuzzleStep)e.CurrentStep;
            Object.Destroy(GameplayUIRoot.Instance.gameObject);
            yield return null;
            // Reproduce the reported order: shared UI disappears before Story.OnDisable.
            e.gameObject.SetActive(false);
            yield return null;
            Assert.IsFalse(step.IsRunning);
            Assert.IsFalse(step.PuzzleController.IsPlaying);
            var empty = SceneManager.CreateScene("Coop teardown destination");
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(scene);
            LogAssert.NoUnexpectedReceived();
        }

        private static Vector2Int Cell(CooperativePuzzleSession pair, int x, int y)
        {
            var root = pair.Puzzle.GetComponent<PuzzleController>().PuzzleRoot;
            var tile = root.GetComponentsInChildren<BoardTile>(true).Single(t => t.name == "Tile_" + x + "_" + y);
            return pair.Puzzle.Board.GridSpace.WorldToCell(tile.transform.position);
        }

        private static Vector2Int[][] Routes(int board)
        {
            if (board == 0) return new[] { Path(0,1, 1,1), Path(1,0, 1,1, 2,1), Path(1,1, 2,1, 3,1), Path(2,1, 2,2) };
            if (board == 1) return new[] { Path(0,1, 1,1), Path(1,0, 1,1, 2,1), Path(1,1, 2,1, 3,1, 4,1), Path(2,1, 2,2) };
            return new[] { Path(0,1, 1,1, 2,1), Path(2,0, 2,1, 3,1), Path(2,1, 3,1, 4,1), Path(3,1, 3,2) };
        }

        private static Vector2Int[] Path(params int[] xy) => Enumerable.Range(0, xy.Length / 2).Select(i => new Vector2Int(xy[i*2], xy[i*2+1])).ToArray();

        private static void CommitPointerRoute(PuzzleManager puzzle, Vector2Int[] wanted)
        {
            var runtime = puzzle.Board.GridSpace.GetComponentInChildren<PuzzleRuntime>(true);
            var placement = runtime.Placement;
            var hand = GameplayUIRoot.Instance.PathPieceHand;
            // Cards are authored in the intended order; rotation remains player-controlled.
            hand.Select(0);
            for (int rotation = 0; rotation < 4; rotation++)
            {
                Set(placement, "rotation", (GridRotation)rotation);
                for (int x = wanted.Min(p => p.x)-1; x <= wanted.Max(p => p.x)+1; x++)
                for (int y = wanted.Min(p => p.y)-1; y <= wanted.Max(p => p.y)+1; y++)
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
            Assert.Fail("No legal pointer/drop found for " + string.Join(" -> ", wanted.Select(p => p.ToString())));
        }

        private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);

        private static IEnumerator Until(Func<bool> ready)
        {
            double deadline = EditorApplication.timeSinceStartup + 20;
            while (!ready() && EditorApplication.timeSinceStartup < deadline)
            {
                // Dialogue copy/timing is outside this test: finish through its normal cleanup callback.
                var dialogue = GameplayUIRoot.Instance != null ? GameplayUIRoot.Instance.Dialogue : null;
                if (dialogue != null && dialogue.IsPlaying)
                    typeof(DialogueController).GetMethod("EndPlayback", Private).Invoke(dialogue, new object[] { DialogueResult.Completed, true });
                EditorApplication.QueuePlayerLoopUpdate();
                yield return null;
            }
            var scene = SceneManager.GetActiveScene();
            var states = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PuzzleManager>(true))
                .Select(p => p.PuzzleData.PuzzleId + ":" + p.CurrentState + ",arrived=" + p.Cooperative.BothAtGoals);
            Assert.IsTrue(ready(), "Timed out waiting for cooperative production flow. timeScale=" + Time.timeScale + " " + string.Join(";", states));
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
