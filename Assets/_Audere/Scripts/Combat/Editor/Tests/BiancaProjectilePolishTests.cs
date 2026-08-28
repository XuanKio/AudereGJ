#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using Audere.EditorTools;
using Audere.Dialogue;
using Audere.Story;
using Audere.Story.Steps;
using Audere.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Audere.Combat.Editor.Tests
{
    public sealed class BiancaProjectilePolishTests
    {
        [UnityTest]
        public IEnumerator Scene60_OpeningFiresAndHorizontalWavesUseProductionPool()
        {
            var scene = EditorSceneManager.OpenScene(Day2SchoolMorningSetupTool.ScenePath);
            var director = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<StoryDirector>(true)).Single();
            var serialized = new SerializedObject(director);
            serialized.FindProperty("playOnStart").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo(); // Test-only; do not save.
            yield return new EnterPlayMode();
            yield return VerifyProductionBoard();
            yield return new ExitPlayMode();
        }

        private static IEnumerator VerifyProductionBoard()
        {
            Application.runInBackground = true;
            EditorWindow.GetWindow(System.Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            var step = roots.SelectMany(r => r.GetComponentsInChildren<CombatStep>(true)).Single();
            var mode = roots.SelectMany(r => r.GetComponentsInChildren<WorldModeController>(true)).Single();
            mode.ApplyModeImmediate(WorldGameplayMode.Combat);
            var controller = step.CombatController;
            var board = controller.BoardView;
            int completions = 0;
            Assert.IsTrue(controller.Play(step.CombatEncounterData, result => completions++));
            double deadline = EditorApplication.timeSinceStartup + 10;
            while (controller.CurrentState != CombatController.State.Playing && EditorApplication.timeSinceStartup < deadline)
                yield return null;
            Assert.AreEqual(CombatController.State.Playing, controller.CurrentState);
            yield return new WaitForSeconds(.25f);
            var ordinary = AssetDatabase.LoadAssetAtPath<CombatBulletView>("Assets/_Audere/Prefabs/Combat/Bullets/EnemyBullet.prefab");
            System.IO.Directory.CreateDirectory("Temp/BiancaPolishQA");
            ScreenCapture.CaptureScreenshot("Temp/BiancaPolishQA/opening.png");
            TestContext.WriteLine("Opening state=" + controller.CurrentState + " phaseTime=" + controller.EnemyRuntime.PhaseElapsed +
                " move=" + controller.EnemyRuntime.CurrentMove.name + " bullets=" + board.GetComponentsInChildren<CombatBulletView>(true).Length);
            Assert.IsTrue(board.GetComponentsInChildren<CombatBulletView>().Any(b => b.SourcePrefab == ordinary && b.CollisionActive),
                "Ordinary projectiles must already be visible in the first quarter-second of active combat.");
            yield return null;
            Assert.IsTrue(controller.Cancel());
            Assert.IsFalse(controller.Cancel());
            Assert.AreEqual(1, completions);
            Assert.AreEqual(0, GameplayUIRoot.Instance.InputGate.ActiveClaimCount);
            Assert.AreEqual(0, board.GetComponentsInChildren<CombatBulletView>().Length);

            // Exercise the authored special on the same live pool, without skipping
            // the production Wrong Box phase or modifying its completion policy.
            var move = AssetDatabase.LoadAssetAtPath<ReturningOrbitMove>("Assets/_Audere/Data/Combat/BiancaSupplies/Moves/Move_Bianca_ReturningOrbit.asset");
            var execution = move.CreateExecution(new CombatMoveExecutionContext(board, null, new SystemCombatRandom(121), 121, 3));
            execution.Tick(.2f);
            for (int wave = 1; wave <= 3; wave++)
            {
                var bullets = board.GetComponentsInChildren<CombatBulletView>();
                Assert.AreEqual(wave, bullets.Length);
                float flight = Mathf.Lerp(4f, 2.8f, (wave - 1f) / 2f);
                foreach (var bullet in bullets)
                {
                    Assert.AreEqual(new Vector2(69f, 69f), bullet.RectTransform.sizeDelta);
                    var start = bullet.RectTransform.anchoredPosition;
                    bullet.TickMovement(board.PlayArea.rect, .65f);
                    Assert.AreEqual(start, bullet.RectTransform.anchoredPosition);
                    bullet.TickMovement(board.PlayArea.rect, flight * .25f);
                    var midpoint = bullet.RectTransform.anchoredPosition;
                    bullet.TickMovement(board.PlayArea.rect, flight * .25f);
                    Assert.Greater(Mathf.Abs(bullet.RectTransform.anchoredPosition.x - start.x), board.PlayArea.rect.width * .7f);
                    bullet.TickMovement(board.PlayArea.rect, flight * .25f);
                    Assert.Less(Vector2.Distance(midpoint, bullet.RectTransform.anchoredPosition), .001f);
                    Assert.AreEqual(start.y, midpoint.y, .001f);
                }
                ScreenCapture.CaptureScreenshot("Temp/BiancaPolishQA/return-wave-" + wave + ".png");
                yield return null;
                board.ClearRuntimeBullets(121, 3);
                execution.Tick(flight + .65f + .55f + .001f);
            }
            Assert.IsTrue(execution.IsComplete);
            execution.Cancel(); execution.Cancel();
            board.ClearRuntimeBullets();
            Assert.AreEqual(0, board.GetComponentsInChildren<CombatBulletView>().Length);
            Assert.IsFalse(GameplayUIRoot.Instance.Dialogue.IsPlaying);
            mode.ApplyModeImmediate(WorldGameplayMode.Story);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTearDown]
        public IEnumerator RestoreScene()
        {
            if (EditorApplication.isPlaying) yield return new ExitPlayMode();
            EditorSceneManager.OpenScene(Day2SchoolMorningSetupTool.ScenePath);
        }

        [TestCase(16f/9f, 1f)] [TestCase(4f/3f, 1f)] [TestCase(21f/9f, 1f)]
        [TestCase(16f/9f, -1f)] [TestCase(4f/3f, -1f)] [TestCase(21f/9f, -1f)]
        public void HorizontalReturn_CrossesBoardThenRetracesSameLane(float aspect, float direction)
        {
            var bounds = new Rect(-150f * aspect, -150f, 300f * aspect, 300f);
            var start = ReturningOrbitMove.EvaluateHorizontalPosition(bounds, 0f, .25f, direction);
            var turn = ReturningOrbitMove.EvaluateHorizontalPosition(bounds, .5f, .25f, direction);
            Assert.AreEqual(bounds.width, Mathf.Abs(turn.x - start.x), .001f);
            Assert.AreEqual(start, ReturningOrbitMove.EvaluateHorizontalPosition(bounds, 1f, .25f, direction));
            for (int i = 0; i <= 100; i++)
            {
                float t = i / 100f;
                var point = ReturningOrbitMove.EvaluateHorizontalPosition(bounds, t, .25f, direction);
                var reverse = ReturningOrbitMove.EvaluateHorizontalPosition(bounds, 1f - t, .25f, direction);
                Assert.AreEqual(start.y, point.y, .001f);
                Assert.Less(Vector2.Distance(point, reverse), .001f);
                Assert.That(point.x, Is.InRange(bounds.xMin, bounds.xMax));
            }
        }

        [Test]
        public void HorizontalReturn_TelegraphPauseFadeAndPoolDoNotLeakMotion()
        {
            var go = new GameObject("Return test", typeof(RectTransform), typeof(CombatBulletView));
            try
            {
                var bullet = go.GetComponent<CombatBulletView>();
                var bounds = new Rect(-200f, -100f, 400f, 200f);
                bullet.Setup(null, Vector2.zero, Vector2.zero, 2, 3, .5f);
                bullet.ConfigureHorizontalReturn(bounds, 2f, .5f, 1f);
                var start = bullet.RectTransform.anchoredPosition;
                bullet.TickMovement(bounds, .25f);
                Assert.IsFalse(bullet.CollisionActive); Assert.AreEqual(start, bullet.RectTransform.anchoredPosition);
                bullet.TickMovement(bounds, .25f);
                Assert.IsTrue(bullet.CollisionActive); Assert.AreEqual(start, bullet.RectTransform.anchoredPosition);
                bullet.TickMovement(bounds, .5f);
                var middle = bullet.RectTransform.anchoredPosition;
                bullet.TickMovement(bounds, 0f);
                Assert.AreEqual(middle, bullet.RectTransform.anchoredPosition);
                bullet.TickMovement(bounds, .5f);
                Assert.AreEqual(200f, bullet.RectTransform.anchoredPosition.x, .001f);
                bullet.TickMovement(bounds, .5f);
                Assert.AreEqual(middle, bullet.RectTransform.anchoredPosition);
                bullet.BeginPresentationFade();
                bullet.TickMovement(bounds, .3f);
                Assert.AreEqual(middle, bullet.RectTransform.anchoredPosition);
                Assert.IsFalse(bullet.CollisionActive);
                bullet.ReturnToPool();
                bullet.Setup(null, Vector2.zero, Vector2.right * 10f, 4, 1);
                bullet.TickMovement(bounds, .1f);
                Assert.AreEqual(new Vector2(1f, 0f), bullet.RectTransform.anchoredPosition);
                Assert.AreEqual(Quaternion.identity, bullet.RectTransform.localRotation);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void ProductionOpening_HasImmediateOrdinaryBulletsAndEnlargedHorizontalReturns()
        {
            const string folder = "Assets/_Audere/Data/Combat/BiancaSupplies/Moves/";
            var opening = AssetDatabase.LoadAssetAtPath<CompositeCombatMove>(folder + "Move_Bianca_0.asset");
            Assert.That(opening.LeadInDuration, Is.GreaterThan(0f).And.LessThanOrEqualTo(.05f));
            Assert.AreEqual(1, opening.Children.OfType<LinearProjectilePatternMove>().Count());
            Assert.IsTrue(opening.Validate(out var error), error);
            var returning = AssetDatabase.LoadAssetAtPath<ReturningOrbitMove>(folder + "Move_Bianca_ReturningOrbit.asset");
            Assert.IsTrue(returning.HorizontalTraversal);
            Assert.AreEqual(3, returning.MaximumSimultaneous);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Audere/Prefabs/Combat/Bullets/Bullet_Bianca_Returning.prefab");
            Assert.AreEqual(new Vector2(69f, 69f), prefab.GetComponent<RectTransform>().sizeDelta);
        }
    }
}
#endif
