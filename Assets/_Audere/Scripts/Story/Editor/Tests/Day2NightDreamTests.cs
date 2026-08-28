#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Audere.Core;
using Audere.Dialogue;
using Audere.EditorTools;
using Audere.Puzzle;
using Audere.Puzzle.Board;
using Audere.Puzzle.PathPieces;
using Audere.Story.Presentation;
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
    public sealed class Day2NightDreamTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void Scenes_KeepFifteenStepsOnePlayerAndExactHandoffs()
        {
            var s = EditorSceneManager.OpenScene(Day2NightDreamSetupTool.DreamPath);
            var levels = All<PuzzleController>(s).OrderBy(x => x.PuzzleRoot.name).ToArray();
            Assert.AreEqual(5, levels.Length);
            Assert.AreEqual(1, All<GridPlayer>(s).Length);
            Assert.AreEqual(1, All<PuzzleRuntime>(s).Length);
            int total = 0;
            for (int i = 0; i < levels.Length; i++)
            {
                var p = levels[i].Puzzle;
                p.Board.RegisterExistingTiles();
                Assert.AreEqual(4, p.Board.GridPositions.Count);
                Assert.AreEqual(3, p.PuzzleData.AvailablePathPieces.Count);
                Assert.IsTrue(p.PuzzleData.RequireAllPathPieces);
                Assert.IsTrue(p.Board.TryGetLevelGoal(out BoardTile goal));
                Assert.AreEqual(new Vector2Int(i * 3 + 3, 0), goal.GridPosition);
                total += p.PuzzleData.AvailablePathPieces.Sum(x => x.OrderedLocalPath.Count - 1);
                if (i < levels.Length - 1)
                    Assert.Less(Vector3.Distance(goal.transform.position, levels[i + 1].Puzzle.PlayerStartTransform.position), .00001f);
            }
            Assert.AreEqual(15, total);
            Assert.Greater(All<TMPro.TextMeshPro>(s).Select(t => t.transform.position).Distinct().Count(), 15,
                "Murmurs must remain spread across the background after scene serialization.");
            var decor = s.GetRootGameObjects().Single(r => r.name == "WORLD").transform.Find("Floating Tiles PLACEHOLDER - NOT WALKABLE");
            Assert.AreEqual(0, decor.GetComponentsInChildren<BoardTile>(true).Length);
            Assert.AreEqual(0, decor.GetComponentsInChildren<Collider2D>(true).Length);
            foreach (string path in new[] { Day2NightDreamSetupTool.HomePath, Day2NightDreamSetupTool.DreamPath, Day2NightDreamSetupTool.WakePath })
            {
                s = EditorSceneManager.OpenScene(path);
                var cover = s.GetRootGameObjects().Single(r => r.name == "Scene Transition Overlay").GetComponentInChildren<CanvasGroup>(true);
                Assert.IsTrue(cover.gameObject.activeInHierarchy, "Destination must start covered, not just hold an inactive alpha=1 group.");
                foreach (var t in All<Transform>(s)) Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
                foreach (var e in All<StoryEvent>(s))
                    foreach (Transform child in e.transform) Assert.AreEqual(1, child.GetComponents<StoryStep>().Length, child.name);
                foreach (var d in All<DialogueStep>(s))
                {
                    Assert.AreEqual(DialogueCharacterId.Audere, d.DialogueData.LeftCharacter);
                    foreach (var line in d.DialogueData.Lines) Assert.LessOrEqual(line.Text.Length, 42, line.Text);
                }
                Assert.IsTrue(EditorBuildSettings.scenes.Any(x => x.path == path && x.enabled));
            }
        }

        [Test]
        public void RerunAuthor_PreservesExistingAssetsAndSchoolContent()
        {
            var paths = new[] { Day2SchoolMorningSetupTool.ScenePath, Day2NightDreamSetupTool.HomePath,
                Day2NightDreamSetupTool.DreamPath, Day2NightDreamSetupTool.WakePath };
            var before = paths.Select(System.IO.File.ReadAllText).ToArray();
            Day2NightDreamSetupTool.Author();
            CollectionAssert.AreEqual(before, paths.Select(System.IO.File.ReadAllText).ToArray());
        }

        [Test]
        public void SchoolClosure_PreservesEarlierDialogueAndUsesSharedBellBeforeFade()
        {
            var s = EditorSceneManager.OpenScene(Day2SchoolMorningSetupTool.ScenePath);
            var e = All<StoryEvent>(s).Single(x => x.EventId == "D2_SCHOOL_WRONG_SUPPLIES");
            string[] tail = e.GetComponentsInChildren<StoryStep>(true).Select(x => x.name).SkipWhile(n => n != "345_PreparationsAreDone").ToArray();
            CollectionAssert.AreEqual(new[] { "345_PreparationsAreDone", "346_LetTomorrowStaySmall", "347_BiancaSaysGoodbye",
                "348_BiancaTurnsToLeave", "348_EnableDepartureCover", "349_SchoolBell", "350_FadeOutAfterAnswer", "360_GoHomeAfterBell" }, tail);
            var bell = e.transform.Find("349_SchoolBell").GetComponent<PlayAudioStep>();
            Assert.AreEqual((int)Audere.Audio.AudioId.School_Bell, new SerializedObject(bell).FindProperty("audioId").intValue);
            var catalog = AssetDatabase.LoadAssetAtPath<Audere.Audio.AudioCatalog>("Assets/_Audere/Data/Audio/AudioCatalog.asset");
            Assert.IsTrue(catalog.TryGet(Audere.Audio.AudioId.School_Bell, out var sound));
            Assert.AreEqual("Assets/_Audere/Audio/SchoolBell.mp3", AssetDatabase.GetAssetPath(sound.clip));
            Assert.IsNotNull(e.transform.Find("330_AudereQuestionsTimor"));
        }

        [UnityTest]
        public IEnumerator HomeDreamAndAwakening_ProductionFlowFifteenRealDrops()
        {
            EditorSceneManager.OpenScene(Day2NightDreamSetupTool.HomePath);
            yield return new EnterPlayMode();
            yield return RunProduction();
            yield return new ExitPlayMode();
        }

        private static IEnumerator RunProduction()
        {
            Application.runInBackground = true;
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            // SceneFlow is the same service used by Bootstrap; no scene-loading shortcut in story code.
            var flow = new GameObject("TEST SceneFlow").AddComponent<SceneFlow>();
            flow.Initialize();
            Object.DontDestroyOnLoad(flow.gameObject);
            foreach (string scene in new[] { GameScenes.Day2Dream, GameScenes.Day2HomeAwakening })
            {
                LogAssert.Expect(LogType.Log, "[SceneFlow] Loading '" + scene + "'...");
                LogAssert.Expect(LogType.Log, "[SceneFlow] Loaded '" + scene + "'.");
            }
            System.IO.Directory.CreateDirectory("Temp/Day2NightDreamQA");
            yield return Until(() => GameplayUIRoot.Instance.Dialogue.IsPlaying, false);
            double portraitSettle = EditorApplication.timeSinceStartup + .8;
            yield return Until(() => EditorApplication.timeSinceStartup >= portraitSettle, false);
            ScreenCapture.CaptureScreenshot("Temp/Day2NightDreamQA/home.png");
            yield return Until(() => SceneManager.GetActiveScene().name == GameScenes.Day2Dream, true, 40);
            var s = SceneManager.GetActiveScene();
            var director = All<StoryDirector>(s).Single();
            var e = All<StoryEvent>(s).Single();
            var levels = All<PuzzleController>(s).OrderBy(x => x.PuzzleRoot.name).ToArray();
            var actor = All<GridPlayer>(s).Single();
            int actorId = actor.GetInstanceID();
            for (int segment = 0; segment < 5; segment++)
            {
                var p = levels[segment].Puzzle;
                yield return Until(() => levels[segment].IsPlaying && p.CurrentState == PuzzleManager.State.Playing);
                Assert.AreEqual(segment * 3, actor.GridPosition.x);
                Assert.AreEqual(3, GameplayUIRoot.Instance.PathPieceHand.Count);
                if (segment == 0) ScreenCapture.CaptureScreenshot("Temp/Day2NightDreamQA/dream-start.png");
                for (int cell = 0; cell < 3; cell++)
                {
                    CommitRight(p);
                    int x = segment * 3 + cell + 1;
                    yield return Until(() => !actor.IsMoving && actor.GridPosition.x == x);
                    Assert.IsTrue(actor.gameObject.activeInHierarchy);
                    Assert.AreEqual(actorId, actor.GetInstanceID());
                    Assert.Less(Mathf.Abs(actor.GetComponent<SpriteRenderer>().bounds.min.y - p.Board.GridSpace.CellToWorldCenter(actor.GridPosition).y), .002f);
                    if (cell < 2) yield return Until(() => p.CurrentState == PuzzleManager.State.Playing);
                }
            }
            Assert.AreEqual(15, actor.GridPosition.x);
            yield return Until(() => e.CurrentStep != null && e.CurrentStep.name == "270_TimorCallsFromTheVoid", false);
            var atmosphere = All<DreamAtmosphereView>(s).Single();
            Assert.AreEqual(1f, atmosphere.Chaos);
            var path = (SpriteRenderer[])typeof(DreamAtmosphereView).GetField("pathRenderers", Private).GetValue(atmosphere);
            Assert.IsTrue(path.All(r => r.color.a == 0f));
            Assert.IsFalse(GameplayUIRoot.Instance.PuzzleUi.gameObject.activeSelf);
            ScreenCapture.CaptureScreenshot("Temp/Day2NightDreamQA/dream-collapse.png");
            yield return Until(() => SceneManager.GetActiveScene().name == GameScenes.Day2HomeAwakening, true, 25);
            s = SceneManager.GetActiveScene();
            e = All<StoryEvent>(s).Single();
            actor = All<GridPlayer>(s).Single();
            var motion = All<CharacterMotionStep>(s).Single();
            Vector3 start = actor.transform.position;
            Vector3 shadow = motion.GroundedShadow.position;
            Vector3 shadowScale = motion.GroundedShadow.lossyScale;
            float highest = start.y;
            yield return Until(() => motion.IsRunning, false);
            while (motion.IsRunning)
            {
                highest = Mathf.Max(highest, actor.transform.position.y);
                Assert.AreEqual(start.x, actor.transform.position.x, .00001f);
                Assert.AreEqual(start.z, actor.transform.position.z, .00001f);
                Assert.Less(Vector3.Distance(shadow, motion.GroundedShadow.position), .0001f);
                Assert.Less(Vector3.Distance(shadowScale, motion.GroundedShadow.lossyScale), .0001f);
                EditorApplication.QueuePlayerLoopUpdate();
                yield return null;
            }
            Assert.Greater(highest, start.y + .025f);
            Assert.AreEqual(start.y, actor.transform.position.y, .0001f);
            yield return Until(() => e.CurrentStep is StoryTitleCardStep, false);
            Assert.AreEqual(0, GameplayUIRoot.Instance.InputGate.ActiveClaimCount);
            ScreenCapture.CaptureScreenshot("Temp/Day2NightDreamQA/awake.png");
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator DreamCancelAndReplay_ClearsMotionInputAndEnvironment()
        {
            EditorSceneManager.OpenScene(Day2NightDreamSetupTool.DreamPath);
            yield return new EnterPlayMode();
            yield return CancelReplay();
            yield return new ExitPlayMode();
        }

        private static IEnumerator CancelReplay()
        {
            var s = SceneManager.GetActiveScene();
            var director = All<StoryDirector>(s).Single();
            var e = All<StoryEvent>(s).Single();
            var atmosphere = All<DreamAtmosphereView>(s).Single();
            yield return Until(() => e.CurrentStep is PuzzleStep);
            var p = ((PuzzleStep)e.CurrentStep).PuzzleController.Puzzle;
            CommitRight(p);
            yield return null;
            director.CancelCurrentEvent();
            yield return null;
            Assert.IsFalse(p.Player.IsMoving);
            Assert.IsFalse(atmosphere.IsRunning);
            Assert.AreEqual(0, GameplayUIRoot.Instance.InputGate.ActiveClaimCount);
            Assert.IsTrue(director.PlayEvent(e));
            yield return Until(() => e.CurrentStep is PuzzleStep);
            Assert.AreEqual(0, p.Player.GridPosition.x);
            Assert.AreEqual(3, GameplayUIRoot.Instance.PathPieceHand.Count);
            Assert.AreEqual(0f, atmosphere.Chaos);
            director.CancelCurrentEvent();
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        private static void CommitRight(PuzzleManager p)
        {
            var placement = p.Board.GridSpace.GetComponentInChildren<PuzzleRuntime>(true).Placement;
            GameplayUIRoot.Instance.PathPieceHand.Select(0);
            typeof(PathPlacementController).GetField("rotation", Private).SetValue(placement, GridRotation.Degrees0);
            typeof(PathPlacementController).GetField("hasAnchoredOrigin", Private).SetValue(placement, false);
            // Pointer is snapped to a cell by the production solver. Aim inside the destination,
            // not at an exact half-cell where floating-point rounding can choose the previous cell.
            var pointer = p.Board.GridSpace.CellToWorldCenter(p.Player.GridPosition + Vector2Int.right);
            Assert.IsTrue(placement.TryMovePreviewToScreenPosition(Camera.main.WorldToScreenPoint(pointer)));
            var result = (PlacementResult)typeof(PathPlacementController).GetField("currentResult", Private).GetValue(placement);
            Assert.IsFalse(result.WillFall);
            Assert.AreEqual(p.Player.GridPosition + Vector2Int.right, result.GridPath.Last());
            Assert.IsTrue(placement.TryCommitPreview());
        }

        private static IEnumerator Until(Func<bool> ready, bool advanceDialogue = true, float timeout = 20f)
        {
            double deadline = EditorApplication.timeSinceStartup + timeout;
            while (!ready() && EditorApplication.timeSinceStartup < deadline)
            {
                var d = GameplayUIRoot.Instance != null ? GameplayUIRoot.Instance.Dialogue : null;
                if (advanceDialogue && d != null && d.IsPlaying)
                    typeof(DialogueController).GetMethod("EndPlayback", Private).Invoke(d, new object[] { DialogueResult.Completed, true });
                EditorApplication.QueuePlayerLoopUpdate();
                yield return null;
            }
            Assert.IsTrue(ready(), "Timed out in " + SceneManager.GetActiveScene().name);
        }

        private static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (EditorApplication.isPlaying) yield return new ExitPlayMode();
            EditorSceneManager.OpenScene(Day2SchoolMorningSetupTool.ScenePath);
        }
    }
}
#endif
