#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;
using Audere.Core;
using Audere.Dialogue;
using Audere.Story;
using Audere.World;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Audere.Combat.Editor.Tests
{
    public sealed class TeacherBossPolishTests
    {
        private const string Root = "Assets/_Audere/Data/Combat/Teacher/";
        private const string Moves = Root + "Moves/";

        [Test]
        public void Production_ThreeBands_OriginalOpening_AlternatingLessons_EscalatingFinale()
        {
            var encounter = AssetDatabase.LoadAssetAtPath<CombatEncounterData>(Root + "CombatEncounter_D3_TEACHER_PRESSURE.asset");
            var enemy = encounter.EnemyDefinition;
            Assert.IsTrue(enemy.Validate(out string error), error);
            Assert.AreEqual(21, enemy.SharedMaxHealth);
            Assert.AreEqual(120f, encounter.EncounterDuration);
            CollectionAssert.AreEqual(new[] { 14, 7, 0 }, enemy.Phases.Select(p => p.SharedExitThreshold));
            var opening = enemy.GetPhase(0).MoveSet.Entries.Select(e => e.Move).ToArray();
            Assert.IsInstanceOf<ChalkSweepMove>(opening[0]);
            Assert.IsTrue(((ChalkSweepMove)opening[0]).ClockwiseLunges);
            Assert.IsInstanceOf<ChalkFenceMove>(opening[1]);
            var subjects = enemy.GetPhase(1).MoveSet.Entries.Select(e => e.Move).ToArray();
            Assert.IsInstanceOf<TeacherGeometrySketchMove>(subjects[0]);
            Assert.IsInstanceOf<TeacherMathChoiceMove>(subjects[1]);
            Assert.IsInstanceOf<TeacherSpiralSketchMove>(subjects[2]);
            Assert.IsInstanceOf<TeacherMathChoiceMove>(subjects[3]);
            Assert.AreEqual(4,subjects.Length);
            Assert.IsInstanceOf<ICombatExclusiveDiceMove>(subjects[0]);
            Assert.IsInstanceOf<ICombatExclusiveDiceMove>(subjects[1]);
            Assert.IsTrue(subjects.All(m => m.LeadInDuration >= .25f && m.LeadInDuration <= .4f));
            var finale = enemy.GetPhase(2).MoveSet.Entries.Select(e => e.Move).ToArray();
            Assert.IsInstanceOf<RadialInwardTrailMove>(finale[0]);
            Assert.IsInstanceOf<CompositeCombatMove>(finale[1]);
            Assert.IsInstanceOf<CompositeCombatMove>(finale[2]);
            Assert.IsInstanceOf<ChalkSweepMove>(finale[3]);
            Assert.IsTrue(((ChalkSweepMove)finale[3]).ClockwiseLunges);
            Assert.AreEqual(4, finale.Length);
            Assert.AreEqual(.78f, enemy.GetPhase(2).Presentation.BoardWidth, .001f);
            Assert.IsTrue(finale.All(m => m.LeadInDuration >= .4f));
            var shiftDelays = new SerializedObject(finale[1]).FindProperty("childStartDelays");
            Assert.AreEqual(.7f, shiftDelays.GetArrayElementAtIndex(1).floatValue, .001f);
            var finalChildren = new SerializedObject(finale[2]).FindProperty("children");
            Assert.AreEqual("Move_TeacherReadableStream",
                finalChildren.GetArrayElementAtIndex(0).objectReferenceValue.name);
            var finalDelays = new SerializedObject(finale[2]).FindProperty("childStartDelays");
            Assert.AreEqual(1.4f, finalDelays.GetArrayElementAtIndex(1).floatValue, .001f);
        }

        [Test]
        public void RadialPressure_HasWiderGapsAndAClearWindowToCatchDice()
        {
            var board = Board();
            try
            {
                var radial = AssetDatabase.LoadAssetAtPath<RadialInwardTrailMove>(Moves + "Move_TeacherRadialInwardTrails.asset");
                Assert.AreEqual(8, radial.ProjectileCount);
                Assert.GreaterOrEqual(radial.TelegraphDuration, .9f);
                var execution = radial.CreateExecution(new CombatMoveExecutionContext(board, null,
                    new SystemCombatRandom(37), 437, 3));
                bool sawTrail = false;
                for (float t = 0f; t < 5.5f; t += .02f)
                {
                    execution.Tick(.02f);
                    board.TickBullets(.02f, .55f);
                    sawTrail |= board.ActiveStunTrailCount > 0;
                }
                Assert.IsTrue(sawTrail);
                Assert.AreEqual(0, ActiveBullets(board));
                Assert.AreEqual(0, board.ActiveStunTrailCount);
                Assert.IsFalse(board.IsCursorStunned);
                Assert.IsFalse(execution.IsComplete, "The move retains a clear catch window before the next attack.");
                execution.Cancel();
            }
            finally { Object.DestroyImmediate(board.gameObject); }
        }

        [Test]
        public void Geometry_SketchesOutsideBeforeWarning_ThrowsThenCancelsAllRods()
        {
            var board = Board();
            try
            {
                var move = AssetDatabase.LoadAssetAtPath<TeacherGeometrySketchMove>(Moves + "Move_TeacherGeometrySketch.asset");
                var execution = move.CreateExecution(new CombatMoveExecutionContext(board, null,
                    new SystemCombatRandom(13), 401, 2));
                execution.Tick(.01f);
                Assert.AreEqual(0, ActiveBullets(board), "Drawing is visual only until the outline closes.");
                Assert.IsFalse(board.IsAttackWarningVisible);
                var sketch=board.GetComponentInChildren<CombatChalkSketchGraphic>(true);
                Assert.IsNotNull(sketch);
                Assert.Less(board.PlayArea.InverseTransformPoint(sketch.transform.position).x,board.PlayArea.rect.xMin);
                execution.Tick(.4f);
                Assert.IsTrue(board.IsAttackWarningVisible);
                Assert.AreEqual(2,board.GetComponentsInChildren<CombatChalkSketchGraphic>(true).Count(g=>g.gameObject.activeSelf),
                    "A second chalk starts before the first drawing is launched.");
                execution.Tick(.25f);
                board.TickBullets(.25f,1f);
                Assert.AreEqual(3,ActiveBullets(board));
                var first=board.GetComponentsInChildren<CombatBulletView>(true).First(b=>b.gameObject.activeSelf);
                Vector2 position=first.RectTransform.anchoredPosition;
                board.TickBullets(.1f,1f);
                Assert.Greater(first.RectTransform.anchoredPosition.x,position.x);
                Assert.Greater(first.RectTransform.anchoredPosition.y,position.y,"First outline travels diagonally up from the left.");
                Assert.IsFalse(board.IsAttackWarningVisible);
                execution.Cancel();
                execution.Cancel();
                Assert.AreEqual(0, ActiveBullets(board));
                Assert.IsFalse(board.IsAttackWarningVisible);
            }
            finally { Object.DestroyImmediate(board.gameObject); }
        }

        [Test]
        public void Geometry_CrossingLaunchOnLongFrameSpendsFlightTimeOnlyOnce()
        {
            var board=Board();
            try
            {
                var move=AssetDatabase.LoadAssetAtPath<TeacherGeometrySketchMove>(Moves+"Move_TeacherGeometrySketch.asset");
                var execution=move.CreateExecution(new CombatMoveExecutionContext(board,null,new SystemCombatRandom(3),452,2));
                execution.Tick(.5f);board.TickBullets(.5f,1f);
                execution.Tick(.25f);board.TickBullets(.25f,1f);
                var first=board.GetComponentsInChildren<CombatBulletView>(true).First(b=>b.gameObject.activeSelf);
                move.GetFlight(board.PlayArea.rect,0,out Vector2 start,out Vector2 end);
                var vertices=TeacherGeometrySketchMove.Vertices(0,96f);
                Vector2 expected=start+(vertices[0]+vertices[1])*.5f+(end-start)*((.75f-move.LaunchTime(0))/1.7f)
                    +move.GetFlightOffset(board.PlayArea.rect,0,.75f);
                Assert.Less(Vector2.Distance(expected,first.RectTransform.anchoredPosition),.01f);
                execution.Cancel();
            }
            finally{Object.DestroyImmediate(board.gameObject);}
        }

        [Test]
        public void Geometry_DenseCrossfireKeepsSafePocket_AndPausesAndCancelsAllChalk()
        {
            var board=Board();
            try
            {
                var move=AssetDatabase.LoadAssetAtPath<TeacherGeometrySketchMove>(Moves+"Move_TeacherGeometrySketch.asset");
                var execution=move.CreateExecution(new CombatMoveExecutionContext(board,null,new SystemCombatRandom(3),451,2));
                int peakBullets=0,peakDrawings=0,hits=0;
                for(int frame=0;frame<220;frame++)
                {
                    board.CatchCursor.position=board.PlayArea.TransformPoint(move.GetSafeCenter(board.PlayArea.rect,(frame+1)*.02f));
                    execution.Tick(.02f);
                    hits+=board.TickBullets(.02f,1f);
                    peakBullets=Mathf.Max(peakBullets,ActiveBullets(board));
                    peakDrawings=Mathf.Max(peakDrawings,board.GetComponentsInChildren<CombatChalkSketchGraphic>(true).Count(g=>g.gameObject.activeSelf));
                }
                Assert.GreaterOrEqual(peakBullets,16,"Overlapping outlines should make the volley denser.");
                Assert.GreaterOrEqual(peakDrawings,3,"Several independent chalks must draw simultaneously.");
                Assert.AreEqual(0,hits,"Following the moving gap must fit the full Heart collider.");
                var active=board.GetComponentsInChildren<CombatBulletView>(true).Where(b=>b.gameObject.activeSelf).ToArray();
                var positions=active.Select(b=>b.RectTransform.anchoredPosition).ToArray();
                execution.Tick(0);board.TickBullets(0,1);
                CollectionAssert.AreEqual(positions,active.Select(b=>b.RectTransform.anchoredPosition).ToArray());
                execution.Cancel();execution.Cancel();
                Assert.AreEqual(0,ActiveBullets(board));
                Assert.IsEmpty(board.GetComponentsInChildren<CombatChalkSketchGraphic>(true));
                Assert.IsFalse(board.IsAttackWarningVisible);
            }
            finally{Object.DestroyImmediate(board.gameObject);}
        }

        [Test]
        public void Geometry_AllFourDiagonalsLeaveFullOutlinesOutsideSafeCircle()
        {
            var move=AssetDatabase.LoadAssetAtPath<TeacherGeometrySketchMove>(Moves+"Move_TeacherGeometrySketch.asset");
            Rect field=new Rect(-250,-185,580,420); // Also verify an offset field center.
            var directions=new System.Collections.Generic.HashSet<Vector2Int>();
            for(int shape=0;shape<move.ShapeCount;shape++)
            {
                move.GetFlight(field,shape,out Vector2 start,out Vector2 end);
                Vector2 direction=end-start;
                directions.Add(new Vector2Int((int)Mathf.Sign(direction.x),(int)Mathf.Sign(direction.y)));
                Assert.IsTrue(start.x<field.xMin||start.x>field.xMax);
                float outlineRadius=96f/Mathf.Sqrt(2f)+3.5f;
                float clearance=Mathf.Abs(direction.x*(field.center.y-start.y)-direction.y*(field.center.x-start.x))/direction.magnitude;
                Assert.GreaterOrEqual(clearance-outlineRadius,move.SafeRadius,
                    "Every edge/corner must avoid the safe region, even between frames.");
            }
            Assert.AreEqual(4,directions.Count);
        }

        [Test]
        public void Arithmetic_ThreeAnswersCompactBothAxes_RewardAndReset()
        {
            var board = Board();
            try
            {
                Vector2 full = board.PlayArea.rect.size;
                var move = AssetDatabase.LoadAssetAtPath<TeacherMathChoiceMove>(Moves + "Move_TeacherArithmeticChoice.asset");
                var execution = move.CreateExecution(new CombatMoveExecutionContext(board, null,
                    new SystemCombatRandom(17), 402, 2));
                execution.Tick(.7f);
                Assert.Less(board.PlayArea.rect.width, full.x * .65f);
                Assert.Less(board.PlayArea.rect.height, full.y * .6f);
                Assert.IsTrue(board.IsTeacherQuestionVisible);
                var dice = board.GetComponentsInChildren<CombatDieView>(true)
                    .Where(d => d.gameObject.activeSelf).ToArray();
                Assert.AreEqual(3, dice.Length);
                Assert.IsTrue(dice.All(d => d.GetComponentInChildren<TMP_Text>(true) != null));
                // The first question seeded below is resolved by inspecting the visible equation.
                var question = board.GetComponentsInChildren<TextMeshProUGUI>(true)
                    .Single(t => t.name == "Teacher question (runtime)").text;
                string[] equation = question.Split(' ');
                int left = int.Parse(equation[0]), right = int.Parse(equation[2]);
                string answer = (equation[1] == "+" ? left + right : left - right).ToString();
                var correct = dice.Single(d => d.GetComponentInChildren<TMP_Text>(true).text == answer);
                board.CatchCursor.position = correct.RectTransform.position;
                Assert.IsTrue(board.CursorOverlaps(correct), "The answer die must be reachable by the Heart cursor.");
                ((ICombatMoveInputHandler)execution).HandleInput(true, false);
                Assert.AreEqual(3, ((ICombatMoveDamageReward)execution).ConsumePendingDamage());
                Assert.AreEqual(0, ((ICombatMoveDamageReward)execution).ConsumePendingDamage());
                execution.Tick(.25f);
                execution.Tick(.7f);
                Assert.IsTrue(execution.IsComplete);
                Assert.AreEqual(full.x, board.PlayArea.rect.width, .01f);
                Assert.AreEqual(full.y, board.PlayArea.rect.height, .01f);
                Assert.IsFalse(board.IsTeacherQuestionVisible);
                execution.Cancel();
                Assert.IsEmpty(board.GetComponentsInChildren<CombatDieView>(true)
                    .Where(d => d.gameObject.activeSelf));
            }
            finally { Object.DestroyImmediate(board.gameObject); }
        }

        [Test]
        public void CompactMoveRecovery_RestoresHeightFrameAndCursorWithoutCollision()
        {
            var board = Board();
            try
            {
                Vector2 full = board.PlayArea.rect.size;
                board.SetBattleBoxSizeLayout(.5f, .45f, 0f);
                board.CaptureMoveExit();
                board.ResetBattleBoxLayout();
                board.RestoreMoveExitPose();
                Assert.AreEqual(full.y * .45f, board.PlayArea.rect.height, .01f);
                board.TickMoveRecovery(.2f);
                Assert.Greater(board.PlayArea.rect.height, full.y * .45f);
                board.TickMoveRecovery(.2f);
                Assert.AreEqual(full, board.PlayArea.rect.size);
            }
            finally { Object.DestroyImmediate(board.gameObject); }
        }

        [Test]
        public void ChalkWarnings_ClearOnCancel()
        {
            var board = Board();
            try
            {
                foreach (string name in new[] { "Move_ChalkFence", "Move_ChalkSweep", "Move_TeacherRadialInwardTrails" })
                {
                    var move = AssetDatabase.LoadAssetAtPath<CombatMoveDefinition>(Moves + name + ".asset");
                    var execution = move.CreateExecution(new CombatMoveExecutionContext(board, null,
                        new SystemCombatRandom(5), 403, 3));
                    execution.Tick(.01f);
                    Assert.IsTrue(board.IsAttackWarningVisible, name);
                    execution.Cancel();
                    board.ClearRuntimeBullets();
                    Assert.IsFalse(board.IsAttackWarningVisible, name);
                }
            }
            finally { Object.DestroyImmediate(board.gameObject); }
        }

        [Test]
        public void ClockwiseOpening_StaysVisibleThroughTelegraphAndCrossesIntoField()
        {
            var board=Board();
            try
            {
                var move=AssetDatabase.LoadAssetAtPath<ChalkSweepMove>(Moves+"Move_ChalkSweep.asset");
                // Keep the Heart away from the spear: this test checks its full flight,
                // while a spear reaching the centered Heart correctly returns on impact.
                board.CatchCursor.anchoredPosition=new Vector2(-240f,-160f);
                var execution=move.CreateExecution(new CombatMoveExecutionContext(board,null,new SystemCombatRandom(1),900,1));
                execution.Tick(.01f);
                var first=board.GetComponentsInChildren<CombatBulletView>(true).Single(b=>b.gameObject.activeSelf);
                Assert.AreEqual("Exterior Projectile Root",first.transform.parent.name);
                Vector2 start=first.RectTransform.anchoredPosition;
                Assert.Less(start.x,0);Assert.Greater(start.y,0);
                board.TickBullets(.49f,1);Assert.IsTrue(first.gameObject.activeSelf);Assert.IsFalse(first.CollisionActive);
                board.TickBullets(.7f,1);Assert.IsTrue(first.gameObject.activeSelf);Assert.IsTrue(first.CollisionActive);
                Assert.IsTrue(board.PlayArea.rect.Contains(first.RectTransform.anchoredPosition));
                for(int i=0;i<7;i++)Assert.Less(Vector3.Cross(ChalkSweepMove.ClockwiseDirection(i),ChalkSweepMove.ClockwiseDirection(i+1)).z,0);
                execution.Cancel();Assert.AreEqual(0,ActiveBullets(board));Assert.IsFalse(board.IsAttackWarningVisible);
            }
            finally { Object.DestroyImmediate(board.gameObject); }
        }

        [UnityTest]
        public IEnumerator Scene120_SecondPhaseOwnsDiceAndRestoresLayoutOnCancel()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Audere/Scenes/120_D3_School_Teacher.unity");
            foreach (var director in SceneObjects<StoryDirector>(scene)) director.enabled = false;
            yield return new EnterPlayMode();
            yield return null;
            scene = SceneManager.GetActiveScene();
            SceneObjects<WorldModeController>(scene).Single().ApplyModeImmediate(WorldGameplayMode.Combat);
            var combat = SceneObjects<CombatController>(scene).Single();
            Assert.IsTrue(combat.Play(combat.CurrentEncounter));
            yield return WaitUntilPlaying(combat, 0);
            for (int i = 0; i < 7 && combat.CurrentState == CombatController.State.Playing; i++)
                combat.DebugApplyDiceEffect(CombatSymbol.Attack);
            yield return WaitUntilPlaying(combat, 1);
            Assert.IsInstanceOf<TeacherGeometrySketchMove>(combat.EnemyRuntime.CurrentMove);
            Assert.IsFalse(combat.EnemyRuntime.ShouldSpawnDice);
            Assert.AreEqual(0, ActiveDice(combat.BoardView));
            float deadline = Time.realtimeSinceStartup + 15f;
            while (!combat.BoardView.IsTeacherQuestionVisible && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.IsTrue(combat.BoardView.IsTeacherQuestionVisible);
            Assert.IsInstanceOf<TeacherMathChoiceMove>(combat.EnemyRuntime.CurrentMove);
            Assert.AreEqual(3, ActiveDice(combat.BoardView));
            Assert.Less(combat.BoardView.PlayArea.rect.height, 230f);
            combat.Cancel();
            Assert.IsFalse(combat.BoardView.IsTeacherQuestionVisible);
            Assert.AreEqual(0, ActiveDice(combat.BoardView));
            yield return new ExitPlayMode();
        }

        private static IEnumerator WaitUntilPlaying(CombatController combat, int phase)
        {
            float deadline = Time.realtimeSinceStartup + 20f;
            while ((combat.CurrentState != CombatController.State.Playing ||
                    combat.EnemyRuntime.PhaseIndex != phase) && Time.realtimeSinceStartup < deadline)
            {
                var dialogue = GameplayUIRoot.Instance?.Dialogue;
                if (dialogue != null && dialogue.IsPlaying)
                    typeof(DialogueController).GetMethod("EndPlayback", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(dialogue, new object[] { DialogueResult.Completed, true });
                yield return null;
            }
            Assert.AreEqual(CombatController.State.Playing, combat.CurrentState);
            Assert.AreEqual(phase, combat.EnemyRuntime.PhaseIndex);
        }

        private static T[] SceneObjects<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        private static int ActiveDice(CombatBoardView board) =>
            board.GetComponentsInChildren<CombatDieView>(true).Count(d => d.gameObject.activeSelf);

        private static CombatBoardView Board()
        {
            var board = Object.Instantiate(AssetDatabase.LoadAssetAtPath<CombatBoardView>(
                "Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab"));
            board.gameObject.SetActive(true);
            board.PrepareEncounter("Teacher test");
            board.ResetPlayer();
            return board;
        }

        private static int ActiveBullets(CombatBoardView board) =>
            board.GetComponentsInChildren<CombatBulletView>(true).Count(b => b.gameObject.activeSelf);
    }
}
#endif
