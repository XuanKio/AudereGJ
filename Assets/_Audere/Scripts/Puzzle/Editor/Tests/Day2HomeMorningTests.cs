#if UNITY_EDITOR
using System.Linq;
using Audere.Dialogue;
using Audere.Puzzle.Board;
using Audere.Story;
using Audere.Story.Steps;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Audere.Puzzle.Editor.Tests
{
    public sealed class Day2HomeMorningTests
    {
        private const string ScenePath = "Assets/_Audere/Scenes/50_D2_Home_Morning.unity";

        [Test]
        public void OneUseTile_AllowsOneEntryAndResetRestoresIt()
        {
            GameObject root = new GameObject("OneUse Test");
            try
            {
                SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
                OneUseTileBehaviour behaviour = root.AddComponent<OneUseTileBehaviour>();
                BoardTile tile = root.AddComponent<BoardTile>();

                SerializedObject behaviourSerialized = new SerializedObject(behaviour);
                behaviourSerialized.FindProperty("tileRenderer").objectReferenceValue = renderer;
                behaviourSerialized.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject tileSerialized = new SerializedObject(tile);
                tileSerialized.FindProperty("tileType").enumValueIndex = (int)PuzzleTileType.OneUse;
                tileSerialized.FindProperty("spriteRenderer").objectReferenceValue = renderer;
                tileSerialized.ApplyModifiedPropertiesWithoutUndo();

                tile.InitializeSceneAuthored(Vector2Int.zero);
                Assert.IsTrue(tile.CanPlayerEnter(null));
                tile.NotifyPlayerEntered(null);
                Assert.IsFalse(tile.CanPlayerEnter(null));
                tile.NotifyPlayerExited(null);
                Assert.IsFalse(renderer.enabled, "A spent red tile must disappear immediately.");
                Assert.AreEqual(0f, renderer.color.a);

                tile.ResetToAuthoredState();
                Assert.IsTrue(tile.CanPlayerEnter(null));
                Assert.IsTrue(renderer.enabled);
                Assert.AreEqual(1f, renderer.color.a, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Day2Scene_HasValidEventsDialogueAndPuzzleBoards()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            StoryEvent[] events = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<StoryEvent>(true))
                .ToArray();
            Assert.AreEqual(2, events.Length);

            StoryEvent morning = events.Single(item => item.EventId == "D2_HOME_MORNING");
            StoryEvent bus = events.Single(item => item.EventId == "D2_TO_BUS_STOP");
            Assert.IsTrue(morning.AutoPlayNextEvent);
            Assert.AreSame(bus, morning.NextEvent);
            Assert.IsFalse(bus.AutoPlayNextEvent);
            SceneLoadStep schoolLoad = bus.GetComponentInChildren<SceneLoadStep>(true);
            Assert.IsNotNull(schoolLoad);
            Assert.AreEqual("60_D2_School_Morning", new SerializedObject(schoolLoad).FindProperty("sceneName").stringValue);
            Assert.AreEqual(bus.transform.childCount - 1, schoolLoad.transform.GetSiblingIndex());

            Transform busPresentationHold = bus.transform.Find("50_HoldBusStopPresentation");
            Assert.IsNotNull(busPresentationHold);
            Assert.IsNotNull(busPresentationHold.GetComponent<WaitStep>());
            Assert.IsNull(busPresentationHold.GetComponent<BoardTileTransitionStep>());
            Assert.IsFalse(bus.GetComponentsInChildren<BoardTileTransitionStep>(true)
                .Any(step => new SerializedObject(step)
                    .FindProperty("sourcePuzzle").objectReferenceValue != null));

            CanvasFadeStep finalFade = bus.transform
                .Find("110_FadeAfterBusStopDialogue")
                .GetComponent<CanvasFadeStep>();
            Assert.AreEqual(schoolLoad.transform.GetSiblingIndex() - 1, finalFade.transform.GetSiblingIndex());
            SerializedObject finalFadeSerialized = new SerializedObject(finalFade);
            Assert.AreEqual(1f, finalFadeSerialized.FindProperty("targetAlpha").floatValue, 0.0001f);
            Assert.IsNotNull(finalFadeSerialized.FindProperty("canvasGroup").objectReferenceValue);

            foreach (StoryEvent storyEvent in events)
            {
                foreach (Transform child in storyEvent.transform)
                    Assert.AreEqual(1, child.GetComponents<StoryStep>().Length, child.name);
            }

            DialogueStep[] dialogueSteps = events
                .SelectMany(item => item.GetComponentsInChildren<DialogueStep>(true))
                .ToArray();
            Assert.AreEqual(8, dialogueSteps.Length);
            Assert.IsTrue(dialogueSteps.All(step => step.DialogueData != null));
            Assert.IsTrue(dialogueSteps.All(step => step.DialogueController != null));
            Assert.IsTrue(dialogueSteps.All(step =>
                step.DialogueData.LeftCharacter == DialogueCharacterId.Audere &&
                step.DialogueData.RightCharacter == DialogueCharacterId.Timor));
            Assert.IsTrue(dialogueSteps.SelectMany(step => step.DialogueData.Lines)
                .All(line => line.Text.Length <= 42));

            PuzzleController[] puzzles = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<PuzzleController>(true))
                .OrderBy(item => item.PuzzleRoot.name)
                .ToArray();
            Assert.AreEqual(3, puzzles.Length);
            CollectionAssert.AreEquivalent(
                new[] { "PZ_D2_WASHROOM", "PZ_D2_BREAKFAST", "PZ_D2_BUS_STOP" },
                puzzles.Select(item => item.PuzzleRoot.name));

            for (int index = 0; index < puzzles.Length; index++)
            {
                int expectedCellCount = puzzles[index].PuzzleRoot.name == "PZ_D2_BUS_STOP"
                    ? 13
                    : puzzles[index].PuzzleRoot.name == "PZ_D2_BREAKFAST"
                        ? 15
                        : 4;
                int expectedRedCount = puzzles[index].PuzzleRoot.name == "PZ_D2_BUS_STOP"
                    ? 3
                    : puzzles[index].PuzzleRoot.name == "PZ_D2_BREAKFAST"
                        ? 3
                        : 1;
                BoardManager board = puzzles[index].Puzzle.Board;
                board.RegisterExistingTiles();
                Assert.AreEqual(expectedCellCount, board.GridPositions.Count, puzzles[index].PuzzleRoot.name);
                int redCount = board.GridPositions.Count(position =>
                    board.TryGetTile(position, out BoardTile tile) &&
                    tile.TileType == PuzzleTileType.OneUse);
                Assert.AreEqual(expectedRedCount, redCount, puzzles[index].PuzzleRoot.name);
                Assert.AreEqual(1, board.GridPositions.Count(position =>
                    board.TryGetTile(position, out BoardTile tile) && tile.IsLevelGoal));
            }

            PuzzleRootCoordinator coordinator = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<PuzzleRootCoordinator>(true))
                .Single();
            Assert.IsTrue(coordinator.ValidateConfiguration(false));

            PuzzleController washroom = puzzles.Single(item => item.PuzzleRoot.name == "PZ_D2_WASHROOM");
            PuzzleController breakfast = puzzles.Single(item => item.PuzzleRoot.name == "PZ_D2_BREAKFAST");
            PuzzleController busStop = puzzles.Single(item => item.PuzzleRoot.name == "PZ_D2_BUS_STOP");
            AssertGoalMatchesNextStart(washroom, breakfast);
            AssertGoalMatchesNextStart(breakfast, busStop);

            BoardTileTransitionStep washroomCollapse = morning.transform
                .Find("40_HideWashroomBoard")
                .GetComponent<BoardTileTransitionStep>();
            BoardTileTransitionStep breakfastReveal = morning.transform
                .Find("60_RevealBreakfastBoard")
                .GetComponent<BoardTileTransitionStep>();
            BoardTileTransitionStep breakfastCollapse = morning.transform
                .Find("80_HideBreakfastBoard")
                .GetComponent<BoardTileTransitionStep>();
            Assert.IsNotNull(new SerializedObject(washroomCollapse)
                .FindProperty("goalToBecomeAnchor").objectReferenceValue);
            Assert.IsNotNull(new SerializedObject(breakfastReveal)
                .FindProperty("revealFromAnchor").objectReferenceValue);
            Assert.IsNotNull(new SerializedObject(breakfastCollapse)
                .FindProperty("goalToBecomeAnchor").objectReferenceValue);

            PuzzleSequencePrepareStep prepareBus = bus.transform
                .Find("10_PrepareBusStopPuzzle")
                .GetComponent<PuzzleSequencePrepareStep>();
            Assert.IsTrue(new SerializedObject(prepareBus)
                .FindProperty("alignToPreviousGoal").boolValue);
        }

        [Test]
        public void Day2Scene_IsEnabledInBuildSettings()
        {
            Assert.IsTrue(EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == ScenePath));
        }

        [Test]
        public void Day2BoardAttemptReset_RestoresConsumedOneUseTiles()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            PuzzleController washroom = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<PuzzleController>(true))
                .Single(item => item.PuzzleRoot.name == "PZ_D2_WASHROOM");
            BoardManager board = washroom.Puzzle.Board;
            board.RegisterExistingTiles();
            BoardTile redTile = board.GridPositions
                .Select(position => board.TryGetTile(position, out BoardTile tile) ? tile : null)
                .Single(tile => tile != null && tile.TileType == PuzzleTileType.OneUse);
            Assert.IsTrue(redTile.TryGetBehaviour(out OneUseTileBehaviour behaviour));

            redTile.NotifyPlayerEntered(washroom.Puzzle.Player);
            Assert.IsTrue(behaviour.IsConsumed);
            board.ResetSceneAuthoredState();
            Assert.IsFalse(behaviour.IsConsumed);
            Assert.IsTrue(redTile.CanPlayerEnter(washroom.Puzzle.Player));
        }

        [Test]
        public void Day2Scene_RestoresScene20GoalPresentation()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            PuzzleController[] puzzles = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<PuzzleController>(true))
                .ToArray();

            AssertGoalItem(
                puzzles.Single(item => item.PuzzleRoot.name == "PZ_D2_WASHROOM"),
                "Assets/_Audere/AssetGame/Item/banchai.aseprite");
            AssertGoalItem(
                puzzles.Single(item => item.PuzzleRoot.name == "PZ_D2_BREAKFAST"),
                "Assets/_Audere/AssetGame/Item/banhmi.aseprite");

            PuzzleController bus = puzzles.Single(item => item.PuzzleRoot.name == "PZ_D2_BUS_STOP");
            Assert.IsTrue(bus.TryGetGoalAnchor(out Transform busGoal, false));
            SpriteRenderer busStopWide = busGoal.Find("busstop_0").GetComponent<SpriteRenderer>();
            SpriteRenderer busStopSign = busGoal.Find("busstop_2").GetComponent<SpriteRenderer>();
            Assert.AreEqual("Assets/_Audere/AssetGame/Item/busstop.aseprite", AssetDatabase.GetAssetPath(busStopWide.sprite));
            Assert.AreEqual("Assets/_Audere/AssetGame/Item/busstop.aseprite", AssetDatabase.GetAssetPath(busStopSign.sprite));
            Assert.AreEqual(3, busStopWide.sortingOrder);
            Assert.AreEqual(4, busStopSign.sortingOrder);
        }

        private static void AssertGoalMatchesNextStart(PuzzleController source, PuzzleController target)
        {
            Assert.IsTrue(source.TryGetGoalAnchor(out Transform goal, false));
            Transform nextStart = target.Puzzle.PlayerStartTransform;
            Assert.Less(
                Vector3.Distance(goal.position, nextStart.position),
                0.0001f,
                $"{source.PuzzleRoot.name} Goal must equal {target.PuzzleRoot.name} PlayerStart.");
        }

        private static void AssertGoalItem(PuzzleController puzzle, string expectedAssetPath)
        {
            Assert.IsTrue(puzzle.TryGetGoalAnchor(out Transform goal, false));
            Transform item = goal.Find("Visual Root/Item");
            Assert.IsNotNull(item, $"{puzzle.PuzzleRoot.name} Goal needs Visual Root/Item.");
            Assert.IsTrue(item.gameObject.activeSelf);
            SpriteRenderer renderer = item.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(renderer.sprite);
            Assert.AreEqual(expectedAssetPath, AssetDatabase.GetAssetPath(renderer.sprite));
            Assert.AreEqual(GoalItemMotionMode.Floating, item.GetComponent<GoalItemMotion>().Motion);
        }
    }
}
#endif
