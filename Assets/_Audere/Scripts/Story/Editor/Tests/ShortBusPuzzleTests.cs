#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Audere.Dialogue;
using Audere.EditorTools;
using Audere.Puzzle;
using Audere.Puzzle.Board;
using Audere.Story.Steps;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Audere.Story.Editor.Tests
{
    public sealed class ShortBusPuzzleTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        [UnityTest] public IEnumerator Day1_ThreeDropsFinishWithFixedBusStop() { return Run(ShortPuzzleAuthoring.Day1, false); }
        [UnityTest] public IEnumerator Day2_ThreeDropsFinishWithFixedBusStop() { return Run(ShortPuzzleAuthoring.Day2, true); }

        private static IEnumerator Run(string path, bool day2)
        {
            var scene = EditorSceneManager.OpenScene(path);
            var director = ShortPuzzleAuthoring.All<StoryDirector>(scene).Single();
            var so = new SerializedObject(director);
            so.FindProperty("playOnStart").boolValue = false; so.ApplyModifiedPropertiesWithoutUndo();
            yield return new EnterPlayMode();
            yield return VerifyBus();
            yield return new ExitPlayMode();
        }

        private static IEnumerator VerifyBus()
        {
            Application.runInBackground = true;
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            var scene = SceneManager.GetActiveScene();
            bool day2 = scene.path == ShortPuzzleAuthoring.Day2;
            var director = ShortPuzzleAuthoring.All<StoryDirector>(scene).Single();
            var puzzles = ShortPuzzleAuthoring.All<PuzzleController>(scene);
            var bus = puzzles.Single(p => p.PuzzleRoot.name.EndsWith("BUS_STOP"));
            var previous = puzzles.Single(p => p.PuzzleRoot.name.EndsWith("BREAKFAST"));
            var coordinator = ShortPuzzleAuthoring.All<PuzzleRootCoordinator>(scene).Single();
            Assert.IsTrue(previous.TryGetGoalAnchor(out Transform anchor, false));
            Assert.Less(Vector3.Distance(anchor.position, bus.Puzzle.PlayerStartTransform.position), .0001f);
            Assert.IsTrue(coordinator.CaptureTransitionAnchor(previous, anchor));
            string scenery = StationPose(bus);
            var e = director.StoryEventsRoot.Find(day2 ? "D2_TO_BUS_STOP" : "D1_TO_BUS_STOP").GetComponent<StoryEvent>();
            // Stop the production event before its scene load, after proving puzzle completion.
            foreach (var load in e.GetComponentsInChildren<SceneLoadStep>(true)) load.gameObject.SetActive(false);
            Assert.IsTrue(director.PlayEvent(e));
            yield return Until(() => bus.Puzzle.CurrentState == PuzzleManager.State.Playing);
            Assert.AreEqual(scenery, StationPose(bus));
            Assert.IsTrue(bus.Puzzle.Player.gameObject.activeInHierarchy);
            var grid = bus.Puzzle.Board.GridSpace;
            var start = grid.WorldToCell(bus.Puzzle.PlayerStartTransform.position);
            Assert.AreEqual(9, bus.Puzzle.Board.GridPositions.Count);
            Assert.AreEqual(3, GameplayUIRoot.Instance.PathPieceHand.Count);
            System.IO.Directory.CreateDirectory("Temp/ShortPuzzleQA");
            ScreenCapture.CaptureScreenshot("Temp/ShortPuzzleQA/" + (day2 ? "day2" : "day1") + ".png");
            yield return null;
            var routes = day2
                ? new[] { Path(0,0, 1,0, 1,1), Path(1,1, 2,1, 3,1, 3,0), Path(3,0, 3,1, 3,2) }
                : new[] { Path(0,0, 1,0, 2,0), Path(2,0, 3,0, 3,1), Path(3,1, 3,2) };
            var drop = typeof(CooperativePuzzleCompletionTests).GetMethod("CommitPointerRoute", BindingFlags.Static | BindingFlags.NonPublic);
            for (int i = 0; i < routes.Length; i++)
            {
                drop.Invoke(null, new object[] { bus.Puzzle, routes[i].Select(p => p + start).ToArray() });
                yield return Until(() => bus.Puzzle.CurrentState == PuzzleManager.State.Playing || bus.Puzzle.CurrentState == PuzzleManager.State.Completed);
                Assert.AreEqual(2-i, GameplayUIRoot.Instance.PathPieceHand.Count);
            }
            Assert.AreEqual(PuzzleManager.State.Completed, bus.Puzzle.CurrentState);
            Assert.AreEqual(scenery, StationPose(bus));
            director.CancelCurrentEvent();
            Assert.AreEqual(0, GameplayUIRoot.Instance.InputGate.ActiveClaimCount);
            Assert.IsTrue(coordinator.CaptureTransitionAnchor(previous, anchor));
            Assert.IsTrue(director.PlayEvent(e));
            yield return Until(() => bus.Puzzle.CurrentState == PuzzleManager.State.Playing);
            Assert.AreEqual(3, GameplayUIRoot.Instance.PathPieceHand.Count);
            foreach (var tile in bus.PuzzleRoot.GetComponentsInChildren<OneUseTileBehaviour>(true)) Assert.IsFalse(tile.IsConsumed);
            director.CancelCurrentEvent();
            LogAssert.NoUnexpectedReceived();
        }

        private static string StationPose(PuzzleController bus)
        {
            bus.TryGetGoalAnchor(out Transform goal, false);
            // The tile's Visual Root bobs on arrival; the station art and logical goal stay fixed.
            return string.Join("\n", goal.GetComponentsInChildren<Transform>(true)
                .Where(t => t == goal || t.name.StartsWith("busstop_"))
                .Select(t => t.name + "|" + t.position.ToString("F6") + "|" + t.rotation.ToString("F6") + "|" + t.lossyScale.ToString("F6")));
        }

        private static Vector2Int[] Path(params int[] xy) => Enumerable.Range(0, xy.Length / 2).Select(i => new Vector2Int(xy[i*2], xy[i*2+1])).ToArray();
        private static IEnumerator Until(Func<bool> ready)
        {
            double deadline = EditorApplication.timeSinceStartup + 25;
            while (!ready() && EditorApplication.timeSinceStartup < deadline)
            {
                var dialogue = GameplayUIRoot.Instance != null ? GameplayUIRoot.Instance.Dialogue : null;
                if (dialogue != null && dialogue.IsPlaying)
                    typeof(DialogueController).GetMethod("EndPlayback", Private).Invoke(dialogue, new object[] { DialogueResult.Completed, true });
                EditorApplication.QueuePlayerLoopUpdate(); yield return null;
            }
            Assert.IsTrue(ready(), "Bus production flow timed out.");
        }
        [UnityTearDown] public IEnumerator Restore()
        {
            if (EditorApplication.isPlaying) yield return new ExitPlayMode();
            EditorSceneManager.OpenScene(Day2SchoolMorningSetupTool.ScenePath);
        }
    }
}
#endif
