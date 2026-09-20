#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Audere.Combat;
using Audere.Dialogue;
using Audere.Puzzle;
using Audere.Story.Steps;
using Audere.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Audere.Story.Editor.Tests
{
    public sealed class CombatShortcutTests
    {
        private const string School = "Assets/_Audere/Scenes/60_D2_School_Morning.unity";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void EveryAuthoredCombatHasShortcutPresentationBindings()
        {
            foreach (string name in new[] { "30_Classroom", "40_Evening", "60_D2_School_Morning",
                "120_D3_School_Teacher", "140_D4_Classroom", "150_D4_Home_Evening" })
            {
                Scene scene = EditorSceneManager.OpenScene("Assets/_Audere/Scenes/" + name + ".unity");
                var director = All<StoryDirector>(scene).Single();
                var combats = director.StoryEventsRoot.GetComponentsInChildren<CombatStep>(true);
                Assert.IsNotEmpty(combats, name);
                foreach (var combat in combats.Where(c => c.isActiveAndEnabled))
                {
                    Assert.IsNotNull(combat.CombatEncounterData, name);
                    Assert.IsNotNull(combat.CombatController.BoardView, name);
                    Assert.IsNotNull(typeof(StoryDirector).GetMethod("FindCombatWorld", BindingFlags.Static | BindingFlags.NonPublic)
                        .Invoke(null, new object[] { combat.transform.parent.GetComponent<StoryEvent>() }), name + "/" + combat.name);
                }
            }
        }

        [UnityTest]
        public IEnumerator FivePressesLeavePuzzleAndPreserveCombatStoryOwnership()
        {
            var scene = EditorSceneManager.OpenScene(School);
            var startup = new SerializedObject(All<StoryDirector>(scene).Single());
            startup.FindProperty("playOnStart").boolValue = false;
            startup.ApplyModifiedPropertiesWithoutUndo();
            yield return new EnterPlayMode();
            yield return VerifyInPlayMode();
            yield return new ExitPlayMode();
        }

        private static IEnumerator VerifyInPlayMode()
        {
            Application.runInBackground = true;
            Time.timeScale = 1f;
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            var scene = SceneManager.GetActiveScene();
            var director = All<StoryDirector>(scene).Single();
            Assert.IsNotNull(GameplayUIRoot.Instance, "Scene UI must have completed Awake.");
            Assert.IsTrue(director.PlayEventById("D2_SCHOOL_COOP_01"));
            double deadline = EditorApplication.timeSinceStartup + 15;
            while (director.CurrentEvent != null && !(director.CurrentEvent.CurrentStep is PuzzleStep) && EditorApplication.timeSinceStartup < deadline)
            {
                var dialogue = GameplayUIRoot.Instance.Dialogue;
                if (dialogue.IsPlaying)
                    typeof(DialogueController).GetMethod("EndPlayback", Private).Invoke(dialogue, new object[] { DialogueResult.Completed, true });
                EditorApplication.QueuePlayerLoopUpdate();
                yield return null;
            }
            Assert.IsNotNull(director.CurrentEvent, "Cooperative event must remain active until the shortcut.");
            var puzzle = director.CurrentEvent.CurrentStep as PuzzleStep;
            Assert.IsNotNull(puzzle);
            Assert.IsTrue(puzzle.PuzzleController.IsPlaying);

            var press = typeof(StoryDirector).GetMethod("RegisterDebugCombatPress", Private);
            Func<float, bool> tap = t => (bool)press.Invoke(director, new object[] { t });
            for (int i = 0; i < 4; i++) Assert.IsFalse(tap(i * .1f));
            Assert.IsTrue(puzzle.PuzzleController.IsPlaying);
            // A long gap clears the earlier partial sequence.
            for (int i = 0; i < 4; i++) Assert.IsFalse(tap(3f + i * .1f));
            Assert.IsTrue(puzzle.PuzzleController.IsPlaying);
            Assert.IsTrue(tap(3.4f));
            yield return null;

            var combat = director.CurrentEvent.CurrentStep as CombatStep;
            Assert.IsNotNull(combat);
            Assert.AreEqual("D2_SCHOOL_WRONG_SUPPLIES", director.CurrentEvent.EventId);
            Assert.IsTrue(combat.CombatController.IsPlaying);
            Assert.IsFalse(puzzle.PuzzleController.IsPlaying);
            Assert.IsFalse(GameplayUIRoot.Instance.Dialogue.IsPlaying);
            Assert.AreEqual(WorldGameplayMode.Combat, All<WorldModeController>(scene).Single().CurrentMode);
            foreach (var fade in director.StoryEventsRoot.GetComponentsInChildren<CanvasFadeStep>(true))
                if (fade.CanvasGroup != null) Assert.AreEqual(0f, fade.CanvasGroup.alpha);
            var enemy = combat.CombatController.EnemyRuntime;
            for (int i = 0; i < 5; i++) Assert.IsFalse(tap(4f + i * .1f));
            Assert.AreSame(enemy, combat.CombatController.EnemyRuntime, "Repeated shortcut must not restart combat.");

            Assert.IsTrue(combat.CombatEncounterData.OutcomeRules.Allows(CombatResult.Victory));
            typeof(CombatController).GetMethod("EndCombat", Private)
                .Invoke(combat.CombatController, new object[] { CombatController.State.Victory });
            deadline = EditorApplication.timeSinceStartup + 8;
            while (director.CurrentEvent != null && director.CurrentEvent.CurrentStep == combat && EditorApplication.timeSinceStartup < deadline)
            {
                var dialogue = GameplayUIRoot.Instance.Dialogue;
                if (dialogue.IsPlaying)
                    typeof(DialogueController).GetMethod("EndPlayback", Private).Invoke(dialogue, new object[] { DialogueResult.Completed, true });
                EditorApplication.QueuePlayerLoopUpdate();
                yield return null;
            }
            Assert.IsTrue(director.IsPlaying, "The event must retain its authored continuation after combat.");
            Assert.AreNotSame(combat, director.CurrentEvent.CurrentStep);

            // L jumps past the event's opening staging. Its victory continuation must therefore
            // restore the two-tile floor itself, before the first post-combat dialogue is visible.
            deadline = EditorApplication.timeSinceStartup + 10;
            while (director.CurrentEvent != null &&
                (director.CurrentEvent.CurrentStep == null || director.CurrentEvent.CurrentStep.name != "270_BiancaChecksOnAudere") &&
                EditorApplication.timeSinceStartup < deadline)
            {
                var dialogue = GameplayUIRoot.Instance.Dialogue;
                if (dialogue.IsPlaying)
                    typeof(DialogueController).GetMethod("EndPlayback", Private).Invoke(dialogue, new object[] { DialogueResult.Completed, true });
                EditorApplication.QueuePlayerLoopUpdate();
                yield return null;
            }
            Assert.IsNotNull(director.CurrentEvent);
            Assert.IsNotNull(director.CurrentEvent.CurrentStep);
            Assert.AreEqual("270_BiancaChecksOnAudere", director.CurrentEvent.CurrentStep.name);
            AssertSeparateReturnTiles(scene, director.CurrentEvent);
            director.CancelCurrentEvent();
            Assert.AreEqual(0, GameplayUIRoot.Instance.InputGate.ActiveClaimCount);
            Assert.IsFalse(combat.CombatController.IsPlaying);
            LogAssert.NoUnexpectedReceived();
        }

        private static void AssertSeparateReturnTiles(Scene scene, StoryEvent storyEvent)
        {
            Transform school = scene.GetRootGameObjects().Single(r => r.name == "SCHOOL").transform;
            Transform returnFloor = school.Find("SCHOOL ART PLACEHOLDER/Supplies Return Board");
            Assert.IsTrue(returnFloor.gameObject.activeInHierarchy,
                "The L shortcut bypasses 020_StageSuppliesReturn; victory must activate the authored return floor.");
            Assert.IsFalse(school.Find("COOP PUZZLES").gameObject.activeInHierarchy,
                "Old cooperative puzzle tiles must be hidden before the return dialogue.");
            Assert.AreEqual(WorldGameplayMode.Story, All<WorldModeController>(scene).Single().CurrentMode);

            var aMove = storyEvent.transform.Find("030_AudereOnOwnTile").GetComponent<MoveActorStep>();
            var bMove = storyEvent.transform.Find("040_BiancaOnOwnTile").GetComponent<MoveActorStep>();
            Transform[] actors = { aMove.Actor, bMove.Actor };
            Transform[] tiles = { returnFloor.Find("Tile_Audere_Return"), returnFloor.Find("Tile_Bianca_Return") };
            var feet = new Vector2[2];
            for (int i = 0; i < actors.Length; i++)
            {
                Assert.IsTrue(actors[i].gameObject.activeInHierarchy);
                Assert.IsTrue(tiles[i].gameObject.activeInHierarchy);
                Assert.IsTrue(tiles[i].GetComponentsInChildren<SpriteRenderer>().Any(r => r.enabled && r.color.a > .99f),
                    "Each actor needs its visible authored tile.");
                var body = actors[i].GetComponent<SpriteRenderer>();
                Assert.IsTrue(body.enabled);
                Assert.AreEqual(1f, body.color.a, .001f, "A completed puzzle's arrival fade must not hide the returning actor.");
                feet[i] = actors[i].TransformPoint(new Vector3(body.sprite.bounds.center.x, body.sprite.bounds.min.y, 0f));
                Assert.Less(Vector2.Distance(feet[i], tiles[i].position), .001f,
                    actors[i].name + " must stand with her visual feet at her own tile's center.");
            }
            Assert.AreNotSame(tiles[0], tiles[1]);
            Assert.Greater(Vector2.Distance(feet[0], feet[1]), .1f, "Audere and Bianca must occupy two distinct tiles.");
            var cameraMove = storyEvent.transform.Find("090_FrameTheTwoTiles").GetComponent<MoveActorStep>();
            Assert.Less(Vector3.Distance(cameraMove.Actor.position, cameraMove.TargetTransform.position), .001f);
            var cameraFollow = cameraMove.Actor.GetComponent<UnityEngine.Animations.PositionConstraint>();
            if (cameraFollow != null) Assert.IsFalse(cameraFollow.enabled);
        }

        private static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();

        [UnityTearDown]
        public IEnumerator RestoreScene()
        {
            if (EditorApplication.isPlaying) yield return new ExitPlayMode();
            Time.timeScale = 1f;
            EditorSceneManager.OpenScene(School);
        }
    }
}
#endif
