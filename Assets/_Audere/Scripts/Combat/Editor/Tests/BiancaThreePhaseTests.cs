#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Audere.Dialogue;
using Audere.EditorTools;
using Audere.Story;
using Audere.Story.Steps;
using Audere.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Audere.Combat.Editor.Tests
{
    public sealed class BiancaThreePhaseTests
    {
        [Test]
        public void Production_ThreeHealthBands_SilentSecondPhase_BoxOpensFinalPhase()
        {
            var enemy = AssetDatabase.LoadAssetAtPath<CombatEnemyDefinition>(BiancaCombatAuthoring.EnemyPath);
            Assert.IsTrue(enemy.Validate(out var error), error);
            Assert.AreEqual(3, enemy.PhaseCount);
            CollectionAssert.AreEqual(new[] { 18, 9, 0 }, enemy.Phases.Select(p => p.SharedExitThreshold));
            Assert.AreEqual(0, enemy.GetPhase(1).DialogueCues.Count);
            Assert.IsNotNull(enemy.GetPhase(1).Presentation);
            Assert.IsNull(enemy.GetPhase(2).Presentation);
            Assert.IsInstanceOf<WrongBoxChoiceMove>(enemy.GetPhase(2).OpeningMove);
            Assert.IsTrue(enemy.GetPhase(2).SpawnDice);
            Assert.IsFalse(enemy.GetPhase(2).AdvanceOnMoveComplete);
            Assert.IsFalse(enemy.Phases.SelectMany(p => p.DialogueCues).Any(c => c.Trigger == CombatDialogueCueTrigger.DiceCaught));
            Assert.IsInstanceOf<RibbonFanSweepMove>(enemy.GetPhase(1).MoveSet.Entries[0].Move);
        }

        [Test]
        public void RibbonSweep_CrossesStraightChords_Returns_AndTurnsAtTips()
        {
            var move = AssetDatabase.LoadAssetAtPath<RibbonFanSweepMove>(BiancaThreePhaseAuthoring.Folder + "Moves/Move_Bianca_RibbonFanSweep.asset");
            var settings = new SerializedObject(move);
            float extension = settings.FindProperty("extensionDuration").floatValue;
            float stroke = settings.FindProperty("strokeDuration").floatValue;
            float turn = settings.FindProperty("turnDuration").floatValue;
            float lag = settings.FindProperty("segmentDelay").floatValue;
            float halfCycle = stroke + turn;
            Vector2 origin = new Vector2(530f, 40f);
            Assert.AreEqual(origin, move.EvaluatePosition(origin, 6, 1f, 0f), "Extend from the body center.");
            var first = move.EvaluatePosition(origin, 0, 1f, extension);
            var crossed = move.EvaluatePosition(origin, 0, 1f, extension + stroke);
            var returned = move.EvaluatePosition(origin, 0, 1f, extension + halfCycle + stroke);
            Assert.Greater(first.y, origin.y);
            Assert.Less(crossed.y, origin.y, "The upper bank must actually pass the lower bank.");
            Assert.Greater(returned.y, origin.y, "After turning, the same bow returns through the center line.");
            for (float t = extension; t < extension + halfCycle * 2f; t += .017f)
            {
                var top = move.EvaluatePosition(origin, 0, 1f, t);
                var bottom = move.EvaluatePosition(origin, 0, -1f, t);
                Assert.AreEqual(first.x, top.x, .001f, "A straight chord must not orbit horizontally.");
                Assert.AreEqual(origin.y * 2f, top.y + bottom.y, .001f);
            }
            float crossingWave = extension + stroke * .5f + Mathf.Min(lag * 4f, stroke * .25f);
            Assert.Less(move.EvaluatePosition(origin, 0, 1f, crossingWave).y, origin.y);
            Assert.Greater(move.EvaluatePosition(origin, 8, 1f, crossingWave).y, origin.y, "The crossing propagates from inner to outer bows.");
            Assert.AreEqual(move.EvaluateRotation(0, 1f, extension + stroke * .2f), move.EvaluateRotation(0, 1f, extension + stroke * .8f), .001f);
            float turnA = extension + stroke + turn * .1f, turnB = extension + stroke + turn * .9f;
            Assert.AreNotEqual(move.EvaluateRotation(0, 1f, turnA), move.EvaluateRotation(0, 1f, turnB));
            Assert.AreEqual(move.EvaluatePosition(origin, 0, 1f, turnA), move.EvaluatePosition(origin, 0, 1f, turnB), "Turn in place before the return stroke.");
        }

        [TestCase(.2f)] [TestCase(2.6f)] [TestCase(3.8f)]
        public void RibbonSweep_PauseAndCancelPreservePoolOwnership(float cancelAt)
        {
            var board = Object.Instantiate(AssetDatabase.LoadAssetAtPath<CombatBoardView>("Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab"));
            try
            {
                board.gameObject.SetActive(true);
                board.PrepareEncounter("Ribbon sweep QA");
                var enemy = AssetDatabase.LoadAssetAtPath<CombatEnemyDefinition>(BiancaCombatAuthoring.EnemyPath);
                var mount = (Transform)new SerializedObject(board).FindProperty("enemyMount").objectReferenceValue;
                var actor = Object.Instantiate(enemy.ActorPrefab, mount);
                board.BindAuthoredEnemyActor(actor); board.SpawnEnemyActor(enemy.ActorPrefab, 941);
                var move = enemy.GetPhase(1).MoveSet.Entries[0].Move;
                var execution = move.CreateExecution(new CombatMoveExecutionContext(board, actor, new SystemCombatRandom(41), 941, 2));
                execution.Tick(.001f);
                var bullets = board.GetComponentsInChildren<CombatBulletView>();
                Assert.AreEqual(18, bullets.Length);
                var visual = (RectTransform)actor.VisualRoot;
                Vector2 center = board.WorldToPlayArea(visual.TransformPoint(visual.rect.center));
                foreach (var bullet in bullets) Assert.AreEqual(center, bullet.RectTransform.anchoredPosition);
                for (float time = 0; time < cancelAt; time += .01f)
                {
                    execution.Tick(.01f);
                    foreach (var bullet in bullets) bullet.TickMovement(board.PlayArea.rect, .01f);
                }
                var before = bullets.Select(b => b.RectTransform.anchoredPosition).ToArray();
                execution.Tick(0f);
                foreach (var bullet in bullets) bullet.TickMovement(board.PlayArea.rect, 0f);
                CollectionAssert.AreEqual(before, bullets.Select(b => b.RectTransform.anchoredPosition).ToArray());
                // A delayed cancel must not return a bullet now leased by the next phase.
                var released = bullets[0];
                board.ReturnEnemyBullet(released, released.PoolLeaseVersion);
                var reused = board.SpawnExteriorEnemyBullet(released.SourcePrefab, Vector2.zero, 942, 3, 0f);
                int lease = reused.PoolLeaseVersion;
                execution.Cancel(); execution.Cancel(); execution.Tick(1f);
                Assert.IsTrue(execution.IsComplete);
                Assert.IsTrue(reused.gameObject.activeSelf);
                Assert.AreEqual(lease, reused.PoolLeaseVersion);
                Assert.AreEqual(1, board.GetComponentsInChildren<CombatBulletView>().Length);
                board.ClearCombatRuntime();
                Assert.AreEqual(0, board.GetComponentsInChildren<CombatBulletView>().Length);
                Assert.AreEqual(0, board.ActiveRibbonEchoCount);
            }
            finally { Object.DestroyImmediate(board.gameObject); }
        }

        [Test]
        public void MoveRecovery_RetiresCollisionImmediately_FadesSnapshot_AndRestoresField()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab");
            var root = Object.Instantiate(prefab);
            try
            {
                root.SetActive(true);
                var board = root.GetComponent<CombatBoardView>();
                var bulletPrefab = AssetDatabase.LoadAssetAtPath<CombatBulletView>(BiancaThreePhaseAuthoring.BowPath);
                var bullet = board.SpawnEnemyBullet(bulletPrefab, Vector2.zero, Vector2.right * 100f, 3, 7, 0f);
                board.SetBattleBoxHorizontalLayout(.55f, .8f);
                Vector2 before = board.PlayArea.anchoredPosition;
                board.CaptureMoveExit();
                board.ResetBattleBoxLayout();
                board.ClearRuntimeBullets(3, 7);
                board.RestoreMoveExitPose();
                Assert.IsFalse(bullet.gameObject.activeSelf);
                Assert.AreEqual(before, board.PlayArea.anchoredPosition);
                var snapshot = root.GetComponentsInChildren<Image>().Single(i => i.name == "Move exit (pooled)");
                float alpha = snapshot.color.a;
                board.TickMoveRecovery(.2f);
                Assert.That(snapshot.color.a, Is.GreaterThan(0f).And.LessThan(alpha));
                Assert.That(board.BattleBoxWidthFraction, Is.GreaterThan(.55f).And.LessThan(1f));
                board.TickMoveRecovery(0f);
                Assert.AreEqual(.775f, board.BattleBoxWidthFraction, .001f);
                board.TickMoveRecovery(.2f);
                Assert.IsFalse(snapshot.gameObject.activeSelf);
                Assert.AreEqual(1f, board.BattleBoxWidthFraction);
                Assert.IsFalse(board.IsRecoveringMove);
                board.ClearCombatRuntime();
            }
            finally { Object.DestroyImmediate(root); }
        }

        [UnityTest]
        public IEnumerator Scene60_AllPhaseTransitions_OpeningChoices_RetryAndCancel()
        {
            var scene = EditorSceneManager.OpenScene(Day2SchoolMorningSetupTool.ScenePath);
            var director = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<StoryDirector>(true)).Single();
            var so = new SerializedObject(director); so.FindProperty("playOnStart").boolValue = false; so.ApplyModifiedPropertiesWithoutUndo();
            yield return new EnterPlayMode();
            yield return VerifyProductionPhases();
            yield return new ExitPlayMode();
        }

        private static IEnumerator VerifyProductionPhases()
        {
            Application.runInBackground = true;
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            var step = roots.SelectMany(r => r.GetComponentsInChildren<CombatStep>(true)).Single();
            var mode = roots.SelectMany(r => r.GetComponentsInChildren<WorldModeController>(true)).Single();
            mode.ApplyModeImmediate(WorldGameplayMode.Combat);
            var controller = step.CombatController;
            Assert.IsNotNull(controller, "Scene CombatStep must retain its controller in Play Mode.");
            var board = controller.BoardView;
            int results = 0;
            Assert.IsTrue(controller.Play(step.CombatEncounterData, _ => results++));
            yield return WaitUntil(() => controller.CurrentState == CombatController.State.Playing, 15f);
            var runtime = controller.EnemyRuntime;
            var gate = GameplayUIRoot.Instance.InputGate;
            var qaInput = gate.PushMode(board, Audere.GameplayInput.GameplayInputMode.Modal);
            var enemyRoot = Get<RectTransform>(board, "enemyPresentationRoot");
            var health = Get<UnityEngine.UI.Slider>(board, "enemyHealthSlider").transform;
            var timer = Get<Image>(board, "timerFill").rectTransform;
            Vector2 originalEnemy = enemyRoot.anchoredPosition, originalField = board.PlayArea.anchoredPosition;
            Vector2 originalSize = board.PlayArea.sizeDelta;
            Vector3 healthOffset = health.position - enemyRoot.position;
            System.IO.Directory.CreateDirectory("Temp/BiancaThreePhaseQA");
            yield return WaitRealtime(.6f);
            ScreenCapture.CaptureScreenshot("Temp/BiancaThreePhaseQA/phase1.png");
            int moveVersion = runtime.MoveVersion;
            runtime.NotifyCapturedDiceBatch();
            Assert.AreEqual(moveVersion, runtime.MoveVersion, "Completing an HP-mode dice batch cannot restart the attack.");
            Assert.AreEqual(CombatEnemyProgression.PhaseBreak, runtime.ApplyDamage(99, out _));
            yield return null;
            float frozenTime = Get<float>(controller, "encounterTimeRemaining");
            yield return WaitRealtime(.2f);
            Assert.AreEqual(frozenTime, Get<float>(controller, "encounterTimeRemaining"), .001f);
            yield return WaitUntil(() => runtime.PhaseIndex == 1 && controller.CurrentState == CombatController.State.Playing, 15f);
            Assert.Less(Vector2.Distance(new Vector2(350f, -340f), enemyRoot.anchoredPosition - originalEnemy), .01f);
            Assert.Less(Vector3.Distance(healthOffset, health.position - enemyRoot.position), .1f);
            Assert.AreEqual(originalField.x - 260f, board.PlayArea.anchoredPosition.x, .01f);
            Assert.AreEqual(originalSize.x * .72f, board.PlayArea.sizeDelta.x, .01f);
            Assert.AreEqual(board.PlayArea.TransformPoint(board.PlayArea.rect.center).x, board.TimerFocusTarget.TransformPoint(board.TimerFocusTarget.rect.center).x, .01f);
            var camera = Camera.main;
            float originalAspect = camera.aspect;
            try
            {
                foreach (float aspect in new[] { 16f / 9f, 4f / 3f, 21f / 9f })
                {
                    camera.aspect = aspect;
                    foreach (var rect in new[] { Get<RectTransform>(board, "battleBoxFrame"), board.TimerFocusTarget,
                        Get<TMPro.TMP_Text>(board, "enemyNameText").rectTransform })
                    {
                        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
                        foreach (var corner in corners)
                        {
                            Vector3 point = camera.WorldToViewportPoint(corner);
                            Assert.That(point.x, Is.InRange(0f, 1f), rect.name + " clipped at aspect " + aspect);
                            Assert.That(point.y, Is.InRange(0f, 1f), rect.name + " clipped vertically");
                        }
                    }
                }
            }
            finally { camera.aspect = originalAspect; }
            System.IO.Directory.CreateDirectory("Temp/BiancaChordQA");
            for (int frame = 0; frame < 48; frame++)
            {
                yield return WaitRealtime(.1f);
                ScreenCapture.CaptureScreenshot("Temp/BiancaChordQA/motion-" + frame.ToString("D2") + ".png");
                yield return null;
            }
            Assert.AreEqual(CombatController.State.Playing, controller.CurrentState);
            TestContext.WriteLine("Phase2 elapsed=" + runtime.PhaseElapsed + " move=" + runtime.CurrentMove.name +
                " bullets=" + board.GetComponentsInChildren<CombatBulletView>().Length + " echoes=" + board.ActiveRibbonEchoCount);
            ScreenCapture.CaptureScreenshot("Temp/BiancaThreePhaseQA/phase2.png");
            yield return null;
            Assert.Greater(board.ActiveRibbonEchoCount, 0);
            Assert.AreEqual(0, board.ActiveStunTrailCount, "Ribbon afterimages never block catching.");
            ScreenCapture.CaptureScreenshot("Temp/BiancaThreePhaseQA/phase2.png");
            yield return null;
            Assert.AreEqual(CombatEnemyProgression.PhaseBreak, runtime.ApplyDamage(99, out _));
            yield return WaitUntil(() => runtime.PhaseIndex == 2 && GameplayUIRoot.Instance.Dialogue.IsPlaying, 10f);
            Assert.AreEqual(originalEnemy, enemyRoot.anchoredPosition, "Center the enemy before dialogue appears.");
            Assert.AreEqual(originalField, board.PlayArea.anchoredPosition);
            Assert.AreEqual(originalSize, board.PlayArea.sizeDelta);
            ScreenCapture.CaptureScreenshot("Temp/BiancaThreePhaseQA/phase3-dialogue.png");
            yield return WaitUntil(() => controller.CurrentState == CombatController.State.Playing, 15f);
            Assert.IsTrue(runtime.IsOpeningMove);
            Assert.IsFalse(runtime.ShouldSpawnDice);
            Assert.IsFalse(runtime.AcceptsDamage);
            int finalPhaseVersion = runtime.PhaseVersion;
            var cursor = Get<RectTransform>(board, "catchCursor");
            double deadline = EditorApplication.timeSinceStartup + 45f;
            while (runtime.IsOpeningMove && EditorApplication.timeSinceStartup < deadline)
            {
                var choice = board.GetComponentsInChildren<CombatDieView>().FirstOrDefault(d => d.CanInteract);
                if (choice != null)
                {
                    cursor.anchoredPosition = board.WorldToPlayArea(choice.transform.position);
                    runtime.HandleMoveInput(true, false);
                }
                yield return WaitRealtime(.2f);
            }
            Assert.IsFalse(runtime.IsOpeningMove, "Box choices must finish and resume the same HP phase.");
            Assert.AreEqual(finalPhaseVersion, runtime.PhaseVersion);
            Assert.IsTrue(runtime.ShouldSpawnDice);
            yield return WaitRealtime(2f);
            Assert.Greater(board.GetComponentsInChildren<CombatDieView>().Length, 0);
            ScreenCapture.CaptureScreenshot("Temp/BiancaThreePhaseQA/phase3-weave.png");
            yield return null;
            Assert.IsTrue(controller.Cancel());
            Assert.IsFalse(controller.Cancel());
            Assert.AreEqual(1, results);
            Assert.AreEqual(originalEnemy, enemyRoot.anchoredPosition);
            Assert.AreEqual(originalSize, board.PlayArea.sizeDelta);
            Assert.AreEqual(0, board.ActiveRibbonEchoCount);
            Assert.IsTrue(gate.Release(qaInput));
            Assert.AreEqual(0, GameplayUIRoot.Instance.InputGate.ActiveClaimCount);
            Assert.IsTrue(controller.Play(step.CombatEncounterData, _ => results++));
            yield return WaitUntil(() => controller.CurrentState == CombatController.State.Playing, 15f);
            Assert.AreEqual(0, controller.EnemyRuntime.PhaseIndex);
            Assert.IsFalse(controller.EnemyRuntime.IsOpeningMove);
            Assert.IsTrue(controller.Cancel());
            Assert.AreEqual(2, results);
            mode.ApplyModeImmediate(WorldGameplayMode.Story);
            LogAssert.NoUnexpectedReceived();
        }

        private static IEnumerator WaitRealtime(float seconds)
        {
            double until = EditorApplication.timeSinceStartup + seconds;
            while (EditorApplication.timeSinceStartup < until) yield return null;
        }
        private static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        private static IEnumerator WaitUntil(Func<bool> condition, float seconds)
        {
            double deadline = EditorApplication.timeSinceStartup + seconds;
            while (!condition() && EditorApplication.timeSinceStartup < deadline) yield return null;
            Assert.IsTrue(condition(), "Timed out waiting for combat presentation.");
        }
        [UnityTearDown]
        public IEnumerator RestoreScene()
        {
            if (EditorApplication.isPlaying) yield return new ExitPlayMode();
            EditorSceneManager.OpenScene(Day2SchoolMorningSetupTool.ScenePath);
        }
    }
}
#endif
